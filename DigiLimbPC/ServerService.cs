#if WINDOWS
using DigiLimbDesktop.Platforms.Windows;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using InputSimulatorEx;
using InputSimulatorEx.Native;
using System.Security.Cryptography;
using ZXing;
using ZXing.QrCode;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using ZXing.Net.Maui;
using ZXing.Rendering;
#if ANDROID
using Android.Graphics;
#endif
#if IOS || MACCATALYST
using UIKit;
#endif
#if WINDOWS
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
#endif

namespace DigiLimbDesktop
{
    public class ServerService
    {
        private HttpListener _httpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Action<string, bool> _updateStatusCallback;
        private readonly List<WebSocket> _clients;
        private readonly InputSimulator _inputSimulator = new();
        private readonly int _port = 8080;
        private string _qrData;
#if WINDOWS
        private readonly GameControllerManager _controllerManager;
#endif
        private DateTime _lastControllerLogTime = DateTime.MinValue;
        public bool IsRunning { get; private set; } = false;
        public int ConnectedClientCount => _clients.Count;

        private const string HEARTBEAT_PING = "PING";
        private const string HEARTBEAT_PONG = "PONG";
        private const string CHAT_PREFIX = "CHAT:";
        private const string CONTROLLER_PREFIX = "CONTROLLER:";

        private string _generatedPasskey;
        private bool _clientConnected = false;
        private string _serverIP;

        private bool _isCapturing = false;
        private CancellationTokenSource _captureCts;

        public ServerService(Action<string, bool> updateStatusCallback)
        {
            _updateStatusCallback = updateStatusCallback;
            _clients = new List<WebSocket>();
#if WINDOWS
            Task.Run(async () => await ViGEmDriverManager.EnsureViGEmBusInstalled()).Wait();
            _controllerManager = new GameControllerManager();
#endif
        }
        private void ThrottledLog(string message)
        {
            {
                const int throttleMs = 100;
                var now = DateTime.Now;
                if ((now - _lastControllerLogTime).TotalMilliseconds > throttleMs)
                {
                    {
                        _lastControllerLogTime = now;
                        _updateStatusCallback?.Invoke(message, true);
                    }
                }
            }
        }
        public void StartServer()
        {
            if (IsRunning)
            {
                Debug.WriteLine("⚠️ WebSocket Server is already running!");
                return;
            }

            try
            {
                RunNetshCommand($"http add urlacl url=http://+:{_port}/ user=Everyone");
                RunNetshCommand($"advfirewall firewall add rule name=\"DigiLimb WebSocket\" dir=in action=allow protocol=TCP localport={_port}");

                if (_httpListener != null && _httpListener.IsListening)
                {
                    Debug.WriteLine("⚠️ WebSocket Server is already running!");
                    return;
                }

                _generatedPasskey = GeneratePasskey();
                _serverIP = GetLocalIPAddress();

                string qrData = $"{_serverIP}:{_port}:{_generatedPasskey}";
                _qrData = qrData;
                Debug.WriteLine($"🔑 Generated Passkey: {_generatedPasskey}");

                IsRunning = true;
                Task.Run(() => StartListener());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error configuring network settings: {ex.Message}");
            }
        }
        private async Task StartListener()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://+:{_port}/");
                _httpListener.Start();

                _updateStatusCallback?.Invoke($"✅ WebSocket Server Running on {_serverIP}:{_port}", true);
                Debug.WriteLine($"✅ WebSocket Server started on {_serverIP}:{_port}");

                Task.Run(() => StartHeartbeatLoop(), _cancellationTokenSource.Token);

                while (_httpListener.IsListening)
                {
                    HttpListenerContext context = await _httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ WebSocket Server error: {ex.Message}");
                _updateStatusCallback?.Invoke("Error starting WebSocket server", false);
                IsRunning = false;
            }
        }

        private async Task StartHeartbeatLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(30000, _cancellationTokenSource.Token);
                await BroadcastMessage(HEARTBEAT_PING, null);
                _updateStatusCallback?.Invoke("Sent heartbeat PING", true);
            }
        }

        private async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            try
            {
                if (_clientConnected)
                {
                    Debug.WriteLine("⚠️ Only one client allowed. Connection rejected.");
                    context.Response.StatusCode = 403;
                    context.Response.Close();
                    return;
                }

                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                WebSocket webSocket = wsContext.WebSocket;

                var buffer = new byte[256];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (receivedMessage.Trim() == _generatedPasskey)
                {
                    _clientConnected = true;
                    _clients.Add(webSocket);

                    string clientIP = context.Request.RemoteEndPoint.ToString();
                    _updateStatusCallback?.Invoke($"🔵 Client Connected: {clientIP}", true);
                    Debug.WriteLine($"🔵 Client authenticated and connected: {clientIP}");

                    await ReceiveLoop(webSocket);
                }
                else
                {
                    Debug.WriteLine($"❌ Passkey mismatch!");
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Authentication failed", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ WebSocket error: {ex.Message}");
            }
        }

        private async Task ReceiveLoop(WebSocket webSocket)
        {
            var buffer = new byte[4096];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        _clients.Remove(webSocket);
                        _updateStatusCallback?.Invoke("🔴 Client disconnected", true);
                        Debug.WriteLine("🔴 Client disconnected.");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.WriteLine($"📥 Received: {message}");
                        try
                        {
                            var json = JsonDocument.Parse(message);
                            if (json.RootElement.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "presentation")
                            {
                                var command = json.RootElement.GetProperty("command").GetString();
                                HandlePresentationCommand(command);
                                continue; // stay on loop for more inputs
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"❌ Failed to parse presentation command: {ex.Message}");
                        }


                        if (message == HEARTBEAT_PONG)
                        {
                            _updateStatusCallback?.Invoke("Received heartbeat PONG", true);
                        }
                        else if (message.StartsWith(CHAT_PREFIX))
                        {
                            string chatText = message.Substring(CHAT_PREFIX.Length);
                            _updateStatusCallback?.Invoke($"Chat: {chatText}", true);
                            await BroadcastMessage(message, webSocket);
                        }
                        else if (message.StartsWith(CONTROLLER_PREFIX))
                        {
                            await ProcessControllerInput(webSocket, message);
                        }
#if WINDOWS
                        else if (message.StartsWith("{"))
                        {
                            _controllerManager.HandleIncomingMessage(message);
                        }
#endif
                        else
                        {
                            await BroadcastMessage(message, webSocket);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        byte[] rawBytes = buffer[..result.Count];
                        HandleMouseData(rawBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Receive loop error: {ex.Message}");
                _clients.Remove(webSocket);
            }
        }

        private async Task BroadcastMessage(string message, WebSocket sender)
        {
            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(messageBuffer);

            foreach (var client in _clients)
            {
                if (client != sender && client.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Broadcast error: {ex.Message}");
                    }
                }
            }
        }
        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        private bool isMoving = false;

        private void HandleMouseData(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            double x = 0, y = 0;
            int scroll = 0;
            bool leftClick = false, rightClick = false;

            while (stream.Position < stream.Length)
            {
                byte header = reader.ReadByte();
                switch (header)
                {
                    case 0x01:
                        x = reader.ReadDouble();
                        break;
                    case 0x02:
                        y = reader.ReadDouble();
                        break;
                    case 0x03:
                        leftClick = reader.ReadByte() != 0;
                        break;
                    case 0x04:
                        rightClick = reader.ReadByte() != 0;
                        break;
                    case 0x05:
                        scroll = reader.ReadInt32();
                        break;
                    default:
                        Debug.WriteLine($"⚠️ Unknown mouse data header: {header}");
                        break;
                }
            }

#if WINDOWS
            if (x != 0 || y != 0)
            {
                if (!isMoving)
                {
                    isMoving = true;
                    MouseEmulator.StartMouseMovement();
                }

                MouseEmulator.SimulateMouseMove(x, y);
            }
            else if (isMoving)
            {
                isMoving = false;
                MouseEmulator.StopMouseMovement();
            }

            if (scroll != 0)
            {
                MouseEmulator.SimulateMouseScroll(scroll);
            }

            if (leftClick && !isLeftPressed)
            {
                MouseEmulator.SimulateLeftPress();
                isLeftPressed = true;
            }
            else if (!leftClick && isLeftPressed)
            {
                MouseEmulator.SimulateLeftRelease();
                isLeftPressed = false;
            }

            if (rightClick && !isRightPressed)
            {
                MouseEmulator.SimulateRightPress();
                isRightPressed = true;
            }
            else if (!rightClick && isRightPressed)
            {
                MouseEmulator.SimulateRightRelease();
                isRightPressed = false;
            }
#endif
        }

        private async Task ProcessControllerInput(WebSocket webSocket, string message)
        {
            {
#if WINDOWS
            if (message.StartsWith("CONTROLLER:"))
            {{
                string input = message.Substring("CONTROLLER:".Length);
                ThrottledLog($"Game Controller Input: {{input}}");
                Debug.WriteLine($"🎮 Received Controller Input: {{input}}");

                await _controllerManager.ProcessControllerInput(input);
            }}
#endif
            }
        }

        public async Task BroadcastScreenFrame(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                return;

            string frameMessage = $"FRAME_START:{base64Image}:FRAME_END";
            byte[] messageBytes = Encoding.UTF8.GetBytes(frameMessage);
            var segment = new ArraySegment<byte>(messageBytes);

            foreach (var client in _clients)
            {
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Frame broadcast error: {ex.Message}");
                    }
                }
            }
        }

        public void StopServer()
        {
            try
            {
                IsRunning = false;
                _cancellationTokenSource?.Cancel();
                _httpListener?.Stop();

                foreach (var client in new List<WebSocket>(_clients))
                {
                    if (client.State == WebSocketState.Open)
                    {
                        client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None).Wait();
                    }
                }

                _clients.Clear();

                RunNetshCommand($"http delete urlacl url=http://+:{_port}/");
                RunNetshCommand($"advfirewall firewall delete rule name=\"DigiLimb WebSocket\" protocol=TCP localport={_port}");

                _updateStatusCallback?.Invoke("🔴 Server stopped", false);
                Debug.WriteLine("🔴 Server stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error stopping server: {ex.Message}");
            }
        }

        private void RunNetshCommand(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    Debug.WriteLine($"✅ netsh output: {output}");
                    if (!string.IsNullOrEmpty(error))
                        Debug.WriteLine($"⚠️ netsh error: {error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ netsh command failed: {ex.Message}");
            }
        }
        private ImageSource GenerateQRCode(string text)
        {
            try
            {
                var qrWriter = new BarcodeWriter
                {
                    Format = ZXing.BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Width = 300,
                        Height = 300,
                        Margin = 1
                    }
                };

                var generatedImage = qrWriter.Write(text);
                return ConvertToImageSource(generatedImage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ QR Code generation failed: {ex.Message}");
                return null;
            }
        }
        private ImageSource ConvertToImageSource(object image)
        {
#if ANDROID
            if (image is Android.Graphics.Bitmap androidBitmap)
            {
                using (var stream = new MemoryStream())
                {
                    androidBitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    return ImageSource.FromStream(() => new MemoryStream(stream.ToArray()));
                }
            }
#endif

#if IOS || MACCATALYST
            if (image is UIKit.UIImage iosImage)
            {
                using (var stream = new MemoryStream())
                {
                    iosImage.AsPNG().AsStream().CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    return ImageSource.FromStream(() => new MemoryStream(stream.ToArray()));
                }
            }
#endif

#if WINDOWS
    if (image is WriteableBitmap windowsBitmap)
    {
        using (var stream = new MemoryStream())
        {
            var encoder = BitmapEncoder.CreateAsync(
                BitmapEncoder.PngEncoderId,
                stream.AsRandomAccessStream()).GetAwaiter().GetResult();

            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)windowsBitmap.PixelWidth,
                (uint)windowsBitmap.PixelHeight,
                96, 96,
                ConvertIBufferToByteArray(windowsBitmap.PixelBuffer)
            );

            encoder.FlushAsync().GetAwaiter().GetResult();
            return ImageSource.FromStream(() => new MemoryStream(stream.ToArray()));
        }
    }
#endif

            Debug.WriteLine("❌ Unsupported image type for QR code conversion.");
            return null;
        }
#if WINDOWS
private static byte[] ConvertIBufferToByteArray(IBuffer buffer)
{
    using (var dataReader = DataReader.FromBuffer(buffer))
    {
        byte[] bytes = new byte[buffer.Length];
        dataReader.ReadBytes(bytes);
        return bytes;
    }
}
#endif



        public void ShowQRCodePopup(string qrData)
        {
            var qrImage = GenerateQRCode(qrData);
            if (qrImage != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var qrPopup = new ContentPage
                    {
                        Content = new VerticalStackLayout
                        {
                            Padding = 20,
                            Spacing = 20,
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            Children =
                    {
                        new Label
                        {
                            Text = "Scan this QR Code to connect:",
                            FontSize = 18,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Image
                        {
                            Source = qrImage,
                            WidthRequest = 250,
                            HeightRequest = 250,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Button
                        {
                            Text = "Close",
                            BackgroundColor = Colors.Purple,
                            TextColor = Colors.White,
                            Command = new Command(() => Application.Current.MainPage.Navigation.PopModalAsync())
                        }
                    }
                        }
                    };

                    Application.Current.MainPage.Navigation.PushModalAsync(qrPopup);
                });
            }
            else
            {
                Debug.WriteLine("❌ Failed to generate QR code image.");
            }
        }


        public void ShowQRCodePopupAgain()
        {
            if (!string.IsNullOrEmpty(_qrData))
            {
                ShowQRCodePopup(_qrData);
            }
            else
            {
                Debug.WriteLine("⚠️ No QR data available.");
            }
        }

        private string GeneratePasskey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[8];
                rng.GetBytes(tokenData);
                return BitConverter.ToString(tokenData).Replace("-", "").Substring(0, 8);
            }
        }

        public string GetLocalIPAddress()
        {
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up ||
                        ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback ||
                        ni.Description.ToLower().Contains("virtual") ||
                        ni.Description.ToLower().Contains("vmware") ||
                        ni.Description.ToLower().Contains("hyper-v") ||
                        ni.Description.ToLower().Contains("docker"))
                    {
                        continue;
                    }

                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            Debug.WriteLine($"✅ Selected IP: {addr.Address}");
                            return addr.Address.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting IP: {ex.Message}");
            }

            return "127.0.0.1";
        }

        private void HandlePresentationCommand(string command)
        {
            switch (command)
            {
                case "next":
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
                    break;
                case "previous":
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.LEFT);
                    break;
                case "start":
                    _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.F5);
                    break;
                case "end":
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                    break;
                case "playMedia":
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_K);
                    break;
                case "pauseMedia":
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_K);
                    break;
                case "fullscreenMedia":
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_F); // Common key is F for YouTube, VLC, Slides
                    break;
                case "exitFullscreen":
                    _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE); // Standard fullscreen exit
                    break;
                default:
                    Debug.WriteLine($"⚠️ Unknown presentation command: {command}");
                    break;
            }
        }
    }
}
