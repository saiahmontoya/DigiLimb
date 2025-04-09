using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MauiTimer = System.Timers.Timer;

namespace DigiLimbMobile
{
    public partial class GameControllerPage : ContentPage
    {
        private readonly Dictionary<string, MauiTimer> _buttonTimers = new();
        private Microsoft.Maui.Controls.View _leftThumb, _rightThumb;
        private AbsoluteLayout _leftJoystickBase, _rightJoystickBase;

        public GameControllerPage()
        {
            InitializeComponent();

#if ANDROID
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;
#endif
            SetupJoysticks();
        }

        private void OnButtonDown(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            string buttonText = btn.Text;
            if (_buttonTimers.ContainsKey(buttonText))
                return;

            _ = SendControllerButtonEvent(buttonText, "DOWN");

            var timer = new MauiTimer(100) { AutoReset = true };
            timer.Elapsed += async (_, _) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await SendControllerButtonEvent(buttonText, "DOWN");
                });
            };

            _buttonTimers[buttonText] = timer;
            timer.Start();
        }

        private void OnButtonUp(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            string buttonText = btn.Text;

            _ = SendControllerButtonEvent(buttonText, "UP");

            if (_buttonTimers.TryGetValue(buttonText, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                _buttonTimers.Remove(buttonText);
            }
        }

        private async Task SendControllerButtonEvent(string button, string state)
        {
            try
            {
                if (App.GlobalWebSocket?.State != WebSocketState.Open) return;

                string data = $"CONTROLLER:{button}:{state}";
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                await App.GlobalWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ WebSocket Send Failed: {ex.Message}");
            }
        }

        private void SetupJoysticks()
        {
            _leftJoystickBase = this.FindByName<AbsoluteLayout>("LeftJoystickBase");
            _rightJoystickBase = this.FindByName<AbsoluteLayout>("RightJoystickBase");

            _leftThumb = this.FindByName<Microsoft.Maui.Controls.View>("LeftThumb");
            _rightThumb = this.FindByName<Microsoft.Maui.Controls.View>("RightThumb");

            var leftPan = new PanGestureRecognizer();
            leftPan.PanUpdated += (s, e) => HandleJoystickPan(e, _leftJoystickBase, _leftThumb, "left");
            _leftJoystickBase.GestureRecognizers.Add(leftPan);

            var rightPan = new PanGestureRecognizer();
            rightPan.PanUpdated += (s, e) => HandleJoystickPan(e, _rightJoystickBase, _rightThumb, "right");
            _rightJoystickBase.GestureRecognizers.Add(rightPan);
        }

        private void HandleJoystickPan(PanUpdatedEventArgs e, AbsoluteLayout baseLayout, Microsoft.Maui.Controls.View thumb, string id)
        {
            double radius = baseLayout.Width / 2;
            double maxDist = radius - (thumb.Width / 2);
            double centerX = radius;
            double centerY = radius;

            if (e.StatusType == GestureStatus.Running)
            {
                double dx = e.TotalX;
                double dy = e.TotalY;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > maxDist)
                {
                    double ratio = maxDist / distance;
                    dx *= ratio;
                    dy *= ratio;
                }

                double newX = centerX + dx;
                double newY = centerY + dy;

                AbsoluteLayout.SetLayoutBounds(thumb, new Rect(newX - thumb.Width / 2, newY - thumb.Height / 2, thumb.Width, thumb.Height));

                float normX = (float)(dx / maxDist);
                float normY = (float)(dy / maxDist);
                _ = SendJoystickData(id, normX, normY);
            }

            if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                AbsoluteLayout.SetLayoutBounds(thumb, new Rect(centerX - thumb.Width / 2, centerY - thumb.Height / 2, thumb.Width, thumb.Height));
                _ = SendJoystickData(id, 0, 0);
            }
        }

        private async Task SendJoystickData(string id, float x, float y)
        {
            if (App.GlobalWebSocket?.State != WebSocketState.Open) return;

            var payload = new
            {
                type = "joystick",
                id,
                x,
                y,
                buttonPressed = false
            };

            string json = System.Text.Json.JsonSerializer.Serialize(payload);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            await App.GlobalWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
