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

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
