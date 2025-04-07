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

        public GameControllerPage()
        {
            InitializeComponent();
            SetupJoystick();
        }

        private void OnButtonDown(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            string buttonText = btn.Text;
            if (_buttonTimers.ContainsKey(buttonText))
                return;

            // 🔸 Send the initial DOWN immediately
            _ = SendControllerButtonEvent(buttonText, "DOWN");

            // 🔁 Create and start a timer for repeated sends
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

            // 🔹 Immediately send button release event
            _ = SendControllerButtonEvent(buttonText, "UP");

            // ❌ Stop timer
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
                if (App.GlobalWebSocket == null || App.GlobalWebSocket.State != WebSocketState.Open)
                    return;

                string data = $"CONTROLLER:{button}:{state}";
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                await App.GlobalWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ WebSocket Send Failed: {ex.Message}");
            }
        }

        private async Task SendJoystickData(float x, float y)
        {
            if (App.GlobalWebSocket == null || App.GlobalWebSocket.State != WebSocketState.Open)
            {
                System.Diagnostics.Debug.WriteLine("❌ WebSocket not connected. Cannot send joystick data.");
                return;
            }

            var payload = new
            {
                type = "joystick",
                x,
                y,
                buttonPressed = false
            };

            string json = System.Text.Json.JsonSerializer.Serialize(payload);
            System.Diagnostics.Debug.WriteLine($"📤 Sending Joystick JSON: {json}");

            byte[] messageBuffer = Encoding.UTF8.GetBytes(json);
            await App.GlobalWebSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void SetupJoystick()
        {
            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnJoystickMoved;
            JoystickPad.GestureRecognizers.Add(pan);
        }

        private void OnJoystickMoved(object sender, PanUpdatedEventArgs e)
        {
            if (e.StatusType == GestureStatus.Running)
            {
                double padWidth = JoystickPad.Width;
                double padHeight = JoystickPad.Height;

                float normalizedX = (float)Math.Max(-1, Math.Min(1, e.TotalX / (padWidth / 2)));
                float normalizedY = (float)Math.Max(-1, Math.Min(1, -e.TotalY / (padHeight / 2)));

                _ = SendJoystickData(normalizedX, normalizedY);
            }

            if (e.StatusType == GestureStatus.Completed)
            {
                _ = SendJoystickData(0, 0);
            }
        }
    }

}




