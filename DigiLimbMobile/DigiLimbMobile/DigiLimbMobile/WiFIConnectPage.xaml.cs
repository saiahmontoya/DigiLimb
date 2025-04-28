using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using Microsoft.Maui.Controls;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Microsoft.Maui.ApplicationModel;

namespace DigiLimbMobile
{
    public partial class WiFiConnectPage : ContentPage
    {
        private ClientWebSocket _webSocket;
        public bool WiFiConnectionFlag;
        public WiFiConnectPage()
        {
            InitializeComponent();
        }

        private async void OnScanQRCodeClicked(object sender, EventArgs e)
        {
            CameraBarcodeReaderView scannerView = null;
            ContentPage scannerPage = null;
            bool hasScanned = false;

            try
            {
                scannerView = new CameraBarcodeReaderView
                {
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    IsDetecting = true
                };

                scannerPage = new ContentPage
                {
                    Content = new Grid { Children = { scannerView } }
                };

                scannerView.BarcodesDetected += async (s, e) =>
                {
                    if (hasScanned) return;

                    try
                    {
                        if (e?.Results != null && e.Results.Any())
                        {
                            hasScanned = true;
                            var scannedValue = e.Results.FirstOrDefault()?.Value;
                            if (!string.IsNullOrEmpty(scannedValue))
                            {
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    scannerView.IsDetecting = false;
                                    ProcessScannedData(scannedValue);

                                    await Task.Delay(100);
                                    if (Application.Current.MainPage.Navigation.ModalStack.Contains(scannerPage))
                                        await Application.Current.MainPage.Navigation.PopModalAsync();
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error during scanning: {ex.Message}");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            lblStatus.Text = "QR scan error.";
                        });
                    }
                };

                await Navigation.PushModalAsync(scannerPage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to initialize QR scanner: {ex.Message}");
                lblStatus.Text = $"Scanner init error: {ex.Message}";
            }
        }

        private void ProcessScannedData(string scannedData)
        {
            try
            {
                string[] parts = scannedData.Split(':');
                if (parts.Length == 3)
                {
                    entryServerIP.Text = parts[0];
                    entryPort.Text = parts[1];
                    App.GlobalPasskey = parts[2];
                    entryPasskey.Text = App.GlobalPasskey;
                }
                lblStatus.Text = "QR Code Scanned Successfully!";
                Debug.WriteLine($"📤 Scanned Passkey: {App.GlobalPasskey}");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Invalid QR Code format.";
                Debug.WriteLine($"❌ Failed to parse QR code: {ex.Message}");
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (App.GlobalWebSocket != null && App.GlobalWebSocket.State == WebSocketState.Open)
            {
                lblStatus.Text = "Already connected.";
                return;
            }

            string serverIP = entryServerIP.Text?.Trim();
            string port = entryPort.Text?.Trim();
            App.GlobalPasskey = entryPasskey.Text?.Trim();

            if (string.IsNullOrEmpty(serverIP) || string.IsNullOrEmpty(port))
            {
                lblStatus.Text = "Please enter a valid server IP and port.";
                return;
            }

            string url = $"ws://{serverIP}:{port}/";
            Debug.WriteLine($"🌐 Connecting to {url}");

            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                App.GlobalWebSocket = _webSocket;

                Debug.WriteLine("✅ WebSocket connected.");

                if (!string.IsNullOrEmpty(App.GlobalPasskey))
                {
                    byte[] passkeyBuffer = Encoding.UTF8.GetBytes(App.GlobalPasskey);
                    Debug.WriteLine($"📤 Sending passkey: {App.GlobalPasskey}");
                    await _webSocket.SendAsync(new ArraySegment<byte>(passkeyBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                lblStatus.Text = "Connected to server.";
                StartReceiveLoop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Connection failed: {ex.Message}");
                lblStatus.Text = $"Connection failed: {ex.Message}";
                WiFiConnectionFlag = false;
            }
        }

        private async void StartReceiveLoop()
        {
            var buffer = new byte[1024];
            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        lblStatus.Text = "Server closed connection.";
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        if (message == "PING")
                        {
                            byte[] pongBuffer = Encoding.UTF8.GetBytes("PONG");
                            await _webSocket.SendAsync(new ArraySegment<byte>(pongBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        else if (message.StartsWith("CHAT:"))
                        {
                            string chatText = message.Substring("CHAT:".Length);
                            lblReceived.Text += $"\n{chatText}";
                        }
                        else
                        {
                            lblReceived.Text = $"Received: {message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Receive error: {ex.Message}";
            }
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            var ws = App.GlobalWebSocket;
            if (ws == null || ws.State != WebSocketState.Open)
            {
                lblStatus.Text = "Not connected to server.";
                return;
            }

            string message = entrySendMessage.Text;
            if (string.IsNullOrEmpty(message))
            {
                lblStatus.Text = "Enter a message to send.";
                return;
            }

            string chatMessage = "CHAT:" + message;
            byte[] messageBuffer = Encoding.UTF8.GetBytes(chatMessage);

            try
            {
                await ws.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                lblStatus.Text = "Message sent.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Send error: {ex.Message}";
            }
        }
    }
}
