using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly int _port = 8080; // Fixed port for the WebSocket server
#if WINDOWS
        private readonly GameControllerManager _controllerManager;

#endif

        // Heartbeat and chat constants
        private const string HEARTBEAT_PING = "PING";
        private const string HEARTBEAT_PONG = "PONG";
        private const string CHAT_PREFIX = "CHAT:";
        private const string CONTROLLER_PREFIX = "CONTROLLER:";

        private string _generatedPasskey;
        private bool _clientConnected = false; // Ensures only one client per session
        private string _serverIP;

        public ServerService(Action<string, bool> updateStatusCallback)
        {
            _updateStatusCallback = updateStatusCallback;
            _clients = new List<WebSocket>();
#if WINDOWS
            Task.Run(async () => await ViGEmDriverManager.EnsureViGEmBusInstalled()).Wait();
            _controllerManager = new GameControllerManager();
#endif
        }

        public void StartServer()
        {
            try
            {
                // Ensure admin privileges to modify firewall and HTTP settings
                RunNetshCommand($"http add urlacl url=http://+:{_port}/ user=Everyone");
                RunNetshCommand($"advfirewall firewall add rule name=\"DigiLimb WebSocket\" dir=in action=allow protocol=TCP localport={_port}");

                if (_httpListener != null && _httpListener.IsListening)
                {
                    Debug.WriteLine("⚠️ WebSocket Server is already running!");
                    return;
                }

                // Generate new passkey and get IP
                _generatedPasskey = GeneratePasskey();
                _serverIP = GetLocalIPAddress();

                // Generate and display QR code
                string qrData = $"{_serverIP}:{_port}:{_generatedPasskey}";
                ShowQRCodePopup(qrData);

                Task.Run(() => StartListener());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error configuring network settings: {ex.Message}");
            }
        }

        public async Task StartListener()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _httpListener = new HttpListener();
                string prefix = $"http://+:{_port}/";
                _httpListener.Prefixes.Add(prefix);
                _httpListener.Start();

                string localIP = GetLocalIPAddress();
                _updateStatusCallback?.Invoke($"WebSocket Server Running on {localIP}:{_port}", true);
                Debug.WriteLine($"✅ WebSocket Server started on {localIP}:{_port}");

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

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            try
            {
                if (_clientConnected)
                {
                    Debug.WriteLine("⚠️ New connection attempt rejected: Only one client allowed per session.");
                    context.Response.StatusCode = 403;
                    context.Response.Close();
                    return;
                }

                // Accept WebSocket Connection
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                WebSocket webSocket = wsContext.WebSocket;

                // Read authentication message
                var buffer = new byte[256];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (receivedMessage == _generatedPasskey)
                {
                    _clientConnected = true;
                    _clients.Add(webSocket);

                    string clientIP = context.Request.RemoteEndPoint.ToString();
                    _updateStatusCallback?.Invoke($"Client Connected: {clientIP}", true);
                    Debug.WriteLine($"🔵 Client authenticated and connected: {clientIP}");

                    await ReceiveLoop(webSocket);
                }
                else
                {
                    Debug.WriteLine("❌ Authentication failed. Rejecting client.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Authentication failed", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error processing WebSocket request: {ex.Message}");
            }
        }

        private void ShowQRCodePopup(string qrData)
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
                            Source = qrImage, // ✅ Uses AsImageSource() correctly
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



        private ImageSource GenerateQRCode(string text)
        {
            try
            {
                var qrWriter = new BarcodeWriter
                {
                    Format = ZXing.BarcodeFormat.QR_CODE, // ✅ Correct
                    Options = new QrCodeEncodingOptions
                    {
                        Width = 300,
                        Height = 300,
                        Margin = 1
                    }
                };

                var generatedImage = qrWriter.Write(text); // ✅ This now works correctly
                return ConvertToImageSource(generatedImage); // ✅ Convert for MAUI
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
if (image is Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap windowsBitmap)
{
    using (var stream = new MemoryStream())
    {
        var encoder = Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
            Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, 
            stream.AsRandomAccessStream()).GetAwaiter().GetResult();

        encoder.SetPixelData(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
            (uint)windowsBitmap.PixelWidth,
            (uint)windowsBitmap.PixelHeight,
            96, 96,
            ConvertIBufferToByteArray(windowsBitmap.PixelBuffer) // ✅ Fixed here
        );

        encoder.FlushAsync().GetAwaiter().GetResult();
        return ImageSource.FromStream(() => new MemoryStream(stream.ToArray()));
    }
}
#endif



            Debug.WriteLine("❌ Unsupported image type.");
            return null;
        }

#if WINDOWS
private static byte[] ConvertIBufferToByteArray(Windows.Storage.Streams.IBuffer buffer)
{
    using (var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
    {
        byte[] bytes = new byte[buffer.Length];
        dataReader.ReadBytes(bytes);
        return bytes;
    }
}
#endif




        private string GeneratePasskey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[8];
                rng.GetBytes(tokenData);
                return BitConverter.ToString(tokenData).Replace("-", "").Substring(0, 8);
            }
        }

        private async Task ReceiveLoop(WebSocket webSocket)
        {
            var buffer = new byte[1024];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        _clients.Remove(webSocket);
                        _updateStatusCallback?.Invoke("Client disconnected", true);
                        Debug.WriteLine("🔴 Client disconnected.");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.WriteLine($"📥 Received: {message}");

                        if (message == HEARTBEAT_PONG)
                        {
                            _updateStatusCallback?.Invoke("Received heartbeat PONG", true);
                        }
                        else if (message == HEARTBEAT_PING)
                        {
                            await SendMessage(webSocket, HEARTBEAT_PONG);
                        }
                        else if (message.StartsWith(CHAT_PREFIX))
                        {
                            string chatText = message.Substring(CHAT_PREFIX.Length);
                            _updateStatusCallback?.Invoke("Chat from client: " + chatText, true);
                            await BroadcastMessage(message, webSocket);
                        }
                        else if (message.StartsWith(CONTROLLER_PREFIX))
                        {
                            await ProcessControllerInput(webSocket, message);
                        }
                        else if (message.StartsWith("{"))
                        {
                            _controllerManager.HandleIncomingMessage(message);
                        }

                        // Keep existing broadcast functionality
                        await BroadcastMessage(message, webSocket);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in receive loop: {ex.Message}");
                _clients.Remove(webSocket);
            }
        }

        private async Task ProcessControllerInput(WebSocket webSocket, string message)
        {
        #if WINDOWS
            if (message.StartsWith("CONTROLLER:"))
            {
                string input = message.Substring("CONTROLLER:".Length);
                _updateStatusCallback?.Invoke($"Game Controller Input: {input}", true);
                Debug.WriteLine($"🎮 Received Controller Input: {input}");

                await _controllerManager.ProcessControllerInput(input); // No error now
            }
        #endif
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
                        Debug.WriteLine($"❌ Error broadcasting to client: {ex.Message}");
                    }
                }
            }
        }

        private async Task SendMessage(WebSocket client, string message)
        {
            if (client != null && client.State == WebSocketState.Open)
            {
                byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async Task SendChatMessage(string message)
        {
            string chatMessage = CHAT_PREFIX + message;
            await BroadcastMessage(chatMessage, null);
            _updateStatusCallback?.Invoke("Sent chat: " + message, true);
        }

        public void StopServer()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                if (_httpListener != null && _httpListener.IsListening)
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                }

                var clientsCopy = new List<WebSocket>(_clients);
                foreach (var client in clientsCopy)
                {
                    try
                    {
                        if (client != null && client.State == WebSocketState.Open)
                        {
                            string disconnectMsg = "Server Disconnecting";
                            byte[] disconnectBuffer = Encoding.UTF8.GetBytes(disconnectMsg);
                            client.SendAsync(new ArraySegment<byte>(disconnectBuffer), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                            Task.Delay(500).Wait();

                            client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error disconnecting client: {ex.Message}");
                    }
                }

                // Revert network changes
                RunNetshCommand($"http delete urlacl url=http://+:{_port}/");
                RunNetshCommand($"advfirewall firewall delete rule name=\"DigiLimb WebSocket\" protocol=TCP localport={_port}");

                _clients.Clear();

                _updateStatusCallback?.Invoke("Server Session Ended", false);
                Debug.WriteLine("🔴 WebSocket Server stopped.");
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
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",  // Execute netsh directly, avoiding PowerShell
                    Arguments = command,  // Pass the command
                    UseShellExecute = true, // Required for UAC elevation
                    Verb = "runas",  // Requests admin privileges
                    WindowStyle = ProcessWindowStyle.Hidden, // Fully hides the command window
                    CreateNoWindow = true  // Ensures no console window appears
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    process.WaitForExit();
                }

                Debug.WriteLine($"✅ netsh command executed with admin privileges: {command}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ netsh execution failed: {ex.Message}");
            }
        }



        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !ip.ToString().StartsWith("127"))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting local IP: {ex.Message}");
            }
            return "127.0.0.1";
        }
    }
}
