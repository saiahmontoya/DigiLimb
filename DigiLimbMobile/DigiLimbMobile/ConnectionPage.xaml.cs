using DigiLimbMobile.View;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace DigiLimbMobile
{
    public partial class ConnectionPage : ContentPage
    {
        private ClientService _clientService;
        private bool _isConnected = false;

        public ConnectionPage()
        {
            InitializeComponent();

            // ✅ Initialize ClientService (without an IP yet, since we will discover it)
            _clientService = new ClientService(UpdateClientStatus);
        }

        private async void OnAddDeviceClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddNewDevicePage());
        }

        private async void btnConnectServer_Click(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                Debug.WriteLine("🔍 Discovering server...");
                await _clientService.DiscoverServer(); // ✅ Find the server before connecting

                Debug.WriteLine("🔗 Attempting to connect...");
                await _clientService.ConnectToServer();

                _isConnected = true;
                btnConnectServer.IsEnabled = false;
                btnDisconnectServer.IsEnabled = true;
                btnSendMessage.IsEnabled = true;
                UpdateConnectionStatus("Connected", Colors.Green);
            }
        }

        private async void btnSendMessage_Click(object sender, EventArgs e)
        {
            await _clientService.SendMessage("Hello from mobile!");
        }

        private void btnDisconnectServer_Click(object sender, EventArgs e)
        {
            _clientService.Disconnect();
            _isConnected = false;
            btnConnectServer.IsEnabled = true;
            btnDisconnectServer.IsEnabled = false;
            btnSendMessage.IsEnabled = false;
            UpdateConnectionStatus("Not Connected", Colors.Red);
        }
        
        // ✅ Function to Update Connection Status in UI
        private void UpdateConnectionStatus(string message, Color color)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblConnectionStatus.Text = message;
                lblConnectionStatus.TextColor = color;
                Debug.WriteLine($"📡 Client Status: {message}");
            });
        }
        private void UpdateClientStatus(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"📡 Client Status: {message}");
                lblConnectionStatus.Text = message;
            });
        }
    }
}
