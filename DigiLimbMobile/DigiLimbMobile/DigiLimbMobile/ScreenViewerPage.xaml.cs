using Microsoft.Maui.Controls;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace DigiLimbMobile
{
    public partial class ScreenViewerPage : ContentPage
    {
        // Here we create a new ClientWebSocket; alternatively, you may reuse a shared connection.
        private ClientWebSocket _webSocket;
        private bool _isViewing = false;
        private CancellationTokenSource _receiveCts;

        public ScreenViewerPage()
        {
            InitializeComponent();
            // Optionally, set the connection info if available.
            // In this example, it's hardcoded.
            lblConnectionInfo.Text = "Connected to: 192.168.1.15:8080";
        }

        private async void OnScreenViewerToggled(object sender, ToggledEventArgs e)
        {
            if (e.Value)
            {
                await StartScreenViewer();
            }
            else
            {
                await StopScreenViewer();
            }
        }

        private async Task StartScreenViewer()
        {
            try
            {
                // Establish WebSocket connection if not already connected.
                if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                {
                    _webSocket = new ClientWebSocket();
                    // Use the desktop IP and port as shown in the connection info.
                    string url = "ws://192.168.1.15:8080/";
                    await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                }
                lblStatus.Text = "Requesting screen...";
                // Send command to desktop to start screen capture.
                await SendTextAsync("START_SCREEN");
                _isViewing = true;
                _receiveCts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoop(_receiveCts.Token));
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
            }
        }

        private async Task StopScreenViewer()
        {
            try
            {
                _isViewing = false;
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    // Send command to desktop to stop screen capture.
                    await SendTextAsync("STOP_SCREEN");
                    _receiveCts?.Cancel();
                    lblStatus.Text = "Screen viewer stopped.";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[300_000]; // Adjust the size based on expected frame size.
            try
            {
                while (_isViewing && _webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        lblStatus.Text = "Server closed connection.";
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (message.StartsWith("FRAME:"))
                        {
                            string base64Data = message.Substring("FRAME:".Length);
                            byte[] imageBytes = Convert.FromBase64String(base64Data);
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                imgScreen.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                                lblStatus.Text = "Receiving screen frames...";
                            });
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                lblStatus.Text = $"Received: {message}";
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblStatus.Text = $"Receive error: {ex.Message}";
                });
            }
        }

        private async Task SendTextAsync(string message)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
