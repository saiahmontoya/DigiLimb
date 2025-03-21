using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace DigiLimbMobile
{
    public partial class GameControllerPage : ContentPage
    {
        public GameControllerPage()
        {
            InitializeComponent();
            SetupJoystick();
        }

        private async void OnButtonPress(object sender, EventArgs e)
        {
            if (App.GlobalWebSocket == null || App.GlobalWebSocket.State != WebSocketState.Open)
            {
                await DisplayAlert("Error", "Not connected to the server.", "OK");
                return;
            }

            Button button = sender as Button;
            string buttonData = $"CONTROLLER:{button.Text}";
            byte[] messageBuffer = Encoding.UTF8.GetBytes(buttonData);
            await App.GlobalWebSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async void SendJoystickData(float x, float y)
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
                float normalizedY = (float)Math.Max(-1, Math.Min(1, -e.TotalY / (padHeight / 2))); // invert Y

                _ = SendJoystickData(normalizedX, normalizedY);
            }

            if (e.StatusType == GestureStatus.Completed)
            {
                _ = SendJoystickData(0, 0);
            }
        }
    }
}
