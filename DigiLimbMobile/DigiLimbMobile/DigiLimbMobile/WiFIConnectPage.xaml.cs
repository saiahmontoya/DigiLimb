using ZXing.Net.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DigiLimbMobile
{
    public partial class WiFiConnectPage : ContentPage
    {
        private ClientWebSocket _webSocket;

        public WiFiConnectPage()
        {
            InitializeComponent();
        }

        private async void OnScanQRCodeClicked(object sender, EventArgs e)
        {
            var scannerPage = new ContentPage
            {
                Content = new ZXing.Net.Maui.Controls.CameraBarcodeReaderView
                {
                    BarcodeOptions = new BarcodeReaderOptions
                    {
                        Formats = ZXing.BarcodeFormat.QR_CODE, // Ensure it's a QR Code
                        AutoRotate = true
                    },
                    BarcodeDetected = async (result) =>
                    {
                        if (result.Count > 0)
                        {
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                string scannedData = result[0].Value;
                                ProcessScannedData(scannedData);

                                // Stop scanner & close modal
                                await Navigation.PopModalAsync();
                            });
                        }
                    }
                }
            };

            await Navigation.PushModalAsync(scannerPage);
        }

        private void ProcessScannedData(string scannedData)
        {
            try
            {
                // Expected format: "192.168.1.100:8080:ABC12345"
                string[] parts = scannedData.Split(':');
                if (parts.Length == 3)
                {
                    entryServerIP.Text = parts[0];
                    entryPort.Text = parts[1];
                    App.GlobalPasskey = parts[2]; // Store passkey globally
                }
                lblStatus.Text = "QR Code Scanned Successfully!";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Invalid QR Code format.";
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
            if (string.IsNullOrEmpty(serverIP) || string.IsNullOrEmpty(port))
            {
                lblStatus.Text = "Please enter a valid server IP and port.";
                return;
            }

            string url = $"ws://{serverIP}:{port}/";
            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                App.GlobalWebSocket = _webSocket;

                // Send passkey for authentication
                if (!string.IsNullOrEmpty(App.GlobalPasskey))
                {
                    byte[] passkeyBuffer = Encoding.UTF8.GetBytes(App.GlobalPasskey);
                    await _webSocket.SendAsync(new ArraySegment<byte>(passkeyBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                lblStatus.Text = "Connected to server.";
                StartReceiveLoop();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Connection failed: {ex.Message}";
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
                            // Reply to heartbeat ping
                            byte[] pongBuffer = Encoding.UTF8.GetBytes("PONG");
                            await _webSocket.SendAsync(new ArraySegment<byte>(pongBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        else if (message.StartsWith("CHAT:"))
                        {
                            // Append chat messages to a chat log
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
