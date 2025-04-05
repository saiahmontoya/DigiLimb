using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;

namespace DigiLimbMobile
{
    public partial class ScreenViewerPage : ContentPage
    {
        private ClientWebSocket _webSocket;
        private bool _isReceiving;
        private CancellationTokenSource _receiveCts;

        public ScreenViewerPage()
        {
            InitializeComponent();
            SetupConnection();
        }

        private void SetupConnection()
        {
            if (App.GlobalWebSocket != null && App.GlobalWebSocket.State == WebSocketState.Open)
            {
                _webSocket = App.GlobalWebSocket;
                lblStatus.Text = "✅ Connected to server. Waiting for screen...";
                StartReceiving();
            }
            else
            {
                lblStatus.Text = "❌ Not connected to server.";
            }
        }

        private void StartReceiving()
        {
            _isReceiving = true;
            _receiveCts = new CancellationTokenSource();
            Task.Run(() => ReceiveScreenFrames(_receiveCts.Token), _receiveCts.Token);
        }

        private async Task ReceiveScreenFrames(CancellationToken token)
        {
            byte[] buffer = new byte[5 * 1024 * 1024];

            try
            {
                while (_isReceiving && _webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            ms.Write(buffer, 0, result.Count);
                        } while (!result.EndOfMessage);

                        byte[] imageData = ms.ToArray();
                        if (imageData.Length > 0)
                        {
                            UpdateScreenImage(imageData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblStatus.Text = $"❌ Receive error: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// Updates the UI with the received screen frame.
        /// </summary>
        private void UpdateScreenImage(byte[] imageData)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    string base64String = Encoding.UTF8.GetString(imageData).Trim();

                    // ✅ Ensure we only decode full frames
                    if (base64String.StartsWith("FRAME_START:") && base64String.EndsWith(":FRAME_END"))
                    {
                        string imageDataBase64 = base64String.Replace("FRAME_START:", "").Replace(":FRAME_END", "").Trim();

                        // ✅ Ensure proper padding
                        while (imageDataBase64.Length % 4 != 0)
                        {
                            imageDataBase64 += "=";
                        }

                        byte[] decodedImage = Convert.FromBase64String(imageDataBase64);
                        imgScreen.Source = ImageSource.FromStream(() => new MemoryStream(decodedImage));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Image decode error: {ex.Message}");
                }
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isReceiving = false;
            _receiveCts?.Cancel();
            lblStatus.Text = "❌ Screen viewing stopped.";
        }
    }
}
