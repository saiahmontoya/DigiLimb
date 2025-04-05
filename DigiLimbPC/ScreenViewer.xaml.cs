using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using Imaging = System.Drawing.Imaging;
using System.Text;
using Size = System.Drawing.Size;

namespace DigiLimbDesktop
{
    public partial class ScreenViewer : ContentPage
    {
        private readonly ServerService _serverService;
        private bool _isStreaming;
        private CancellationTokenSource _captureCts;

        public ScreenViewer()
        {
            InitializeComponent();
            _serverService = App.GlobalServerService;
        }

        private async void OnStartScreenViewerClicked(object sender, EventArgs e)
        {
            if (!_isStreaming)
            {
                await StartScreenStream();
            }
        }

        private void OnStopScreenViewerClicked(object sender, EventArgs e)
        {
            StopScreenStream();
        }

        private async Task StartScreenStream()
        {
            try
            {
                if (_serverService == null || !_serverService.IsRunning)
                {
                    Debug.WriteLine("❌ Server not running.");
                    StatusLabel.Text = "❌ Server not running.";
                    return;
                }

                _isStreaming = true;
                _captureCts = new CancellationTokenSource();
                UpdateButtonState();
                await CaptureAndSendScreen(_captureCts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting screen stream: {ex.Message}");
                _isStreaming = false;
                UpdateButtonState();
            }
        }

        private void StopScreenStream()
        {
            _isStreaming = false;
            _captureCts?.Cancel();
            UpdateButtonState();
        }

        private string CaptureWindowsScreen()
        {
            try
            {
                int screenWidth = GetSystemMetrics(0);
                int screenHeight = GetSystemMetrics(1);

                // Set a reasonable streaming resolution
                int targetWidth = 960;
                int targetHeight = (int)(screenHeight * (960.0 / screenWidth)); // Maintain aspect ratio

                using Bitmap original = new Bitmap(screenWidth, screenHeight);
                using Graphics g = Graphics.FromImage(original);
                g.CopyFromScreen(0, 0, 0, 0, original.Size);

                using Bitmap resized = new Bitmap(original, new Size(targetWidth, targetHeight));
                using MemoryStream ms = new MemoryStream();
                resized.Save(ms, Imaging.ImageFormat.Jpeg); // Use JPEG for smaller payloads

                byte[] imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Windows screen capture error: {ex.Message}");
                return null;
            }
        }

        [DllImport("User32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private async Task<string> CaptureScreenBase64Async()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return CaptureWindowsScreen();
                }
                else
                {
                    var screenImage = await Screenshot.CaptureAsync();
                    if (screenImage == null) return null;

                    using MemoryStream ms = new MemoryStream();
                    using var stream = await screenImage.OpenReadAsync();
                    await stream.CopyToAsync(ms);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Async capture error: {ex.Message}");
                return null;
            }
        }

        private async Task CaptureAndSendScreen(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_serverService.ConnectedClientCount == 0)
                    {
                        Debug.WriteLine("⚠️ No clients connected.");
                        await Task.Delay(500);
                        continue;
                    }

                    string base64Image = await CaptureScreenBase64Async();
                    if (!string.IsNullOrEmpty(base64Image))
                    {
                        await _serverService.BroadcastScreenFrame(base64Image); // ✅ No "FRAME:" prefix here
                    }

                    await Task.Delay(30);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Streaming error: {ex.Message}");
            }
        }

        private void UpdateButtonState()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StartScreenViewerButton.IsVisible = !_isStreaming;
                StopScreenViewerButton.IsVisible = _isStreaming;
                StatusLabel.Text = _isStreaming ? "📡 Streaming..." : "Waiting for connection...";
            });
        }
    }
}
