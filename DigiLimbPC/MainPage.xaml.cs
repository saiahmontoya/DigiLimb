using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Plugin.BLE.Abstractions;
using System.Diagnostics;
using DigiLimbDesktop.Platforms.Windows;

namespace DigiLimbDesktop
{
    public partial class MainPage : ContentPage
    {
        private string _email; // Store user's email
        private readonly IMongoCollection<BsonDocument> _usersCollection;
        private ObjectId? _userID;

        private BluetoothPeripheral _bluetoothPeripheral; // ✅ Reference to BluetoothPeripheral

        private bool _isConnectedValue = false;

        public bool _isConnected
        {
            get => _isConnectedValue;
            set
            {
                if (_isConnectedValue != value) // ✅ Only trigger updates when value changes
                {
                    Debug.WriteLine($"🔍 Changing _isConnected: {_isConnectedValue} ➡ {value}");
                    _isConnectedValue = value;
                    App.GlobalIsConnected = value; // ✅ Sync with global state
                    UpdateConnectionStatus();
                }
            }
        }

        public string _deviceName = "";
        public string _connectionType = ""; // "Bluetooth" or "WiFi"

        public MainPage() // ✅ No longer requires email in constructor
        {
            InitializeComponent();

            // ✅ Retrieve email from AppShell global state
            _email = AppShell.UserEmail;



            // ✅ Subscribe to Bluetooth connection updates
            // ✅ Prevent event duplication & ensure object is not null
            // ✅ Prevent event duplication & ensure object is not null
            if (_bluetoothPeripheral != null)
            {
                // ✅ Retrieve global BluetoothPeripheral instance
                _bluetoothPeripheral = App.GlobalBluetoothPeripheral;
                _bluetoothPeripheral.DeviceConnectionChanged -= OnDeviceConnectionChanged;
                _bluetoothPeripheral.DeviceConnectionChanged += OnDeviceConnectionChanged;
            }
            else
            {
                Debug.WriteLine("⚠️ BluetoothPeripheral is still null after initialization.");
            }

            // Initialize MongoDB connection
            var client = new MongoClient("mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase");
            var database = client.GetDatabase("DigilimbDatabase");
            _usersCollection = database.GetCollection<BsonDocument>("Users");

            // Load user data
            LoadUserDeviceNameAsync(_email);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            Debug.WriteLine($"📌 MainPage appeared. GlobalIsConnected: {App.GlobalIsConnected}");

            // ✅ Ensure `_isConnected` syncs with `GlobalIsConnected`
            _isConnected = App.GlobalIsConnected;

            UpdateConnectionStatus();
        }





        // Navigate to Connections Page
        private async void OnConnectionsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConnectionsPage());
        }

        // Navigate to Devices Page
        private async void OnDevicesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DevicesPage(_email));
        }

        // Navigate to Modifications (Settings) Page
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            var settingsPage = new SettingsPage(_email);

            // Subscribe to the DeviceNameUpdated event to update the label
            settingsPage.DeviceNameUpdated += OnDeviceNameUpdated;

            await Navigation.PushAsync(settingsPage);
        }

        // Navigate to Support Page
        private async void OnSupportClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SupportPage());
        }

        // Navigate to Emulation Page
        private async void OnEmulationClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EmulationsPage());
        }

        // Quit the Application
        private async void OnQuitClicked(object sender, EventArgs e)
        {
            bool confirmExit = await DisplayAlert("Confirm Quit", "Are you sure you want to quit?", "Yes", "No");
            if (confirmExit)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill(); // Force quit
            }
        }

        private async void LoadUserDeviceNameAsync(string email)
        {
            try
            {
                var userID = await GetUserIdByEmailAsync(email);
                if (userID == null)
                {
                    lblDeviceName.Text = "DigiLimb Dashboard";  // Default if no user found
                    return;
                }

                var deviceName = await GetUserDeviceNameAsync(userID.Value);
                lblDeviceName.Text = deviceName + "'s Dashboard" ?? "DigiLimb Dashboard";  // Display the device name or default
            }
            catch (Exception ex)
            {
                lblDeviceName.Text = "DigiLimb Dashboard";  // Fallback in case of error
                Console.WriteLine($"Error loading user device name: {ex.Message}");
            }
        }

        private async Task<ObjectId?> GetUserIdByEmailAsync(string email)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("email", email);
            var user = await _usersCollection.Find(filter).FirstOrDefaultAsync();
            return user?["_id"].AsObjectId;
        }

        private async Task<string> GetUserDeviceNameAsync(ObjectId userID)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("userId", userID);
            var userSettings = await _usersCollection.Find(filter).FirstOrDefaultAsync();
            return userSettings?["mouseModifications"]?["deviceName"].AsString;
        }

        private void OnDeviceNameUpdated(string updatedDeviceName)
        {
            // Update the label text when the device name is updated in the settings page
            lblDeviceName.Text = updatedDeviceName + "'s Dashboard" ?? "DigiLimb Dashboard";
        }

        private async void OnViewConnectionDashboardClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConnectionDashboard());
        }


        private void OnDeviceConnectionChanged(object sender, bool isConnected)
        {
            Debug.WriteLine($"📡 MainPage Detected Connection Change: {(isConnected ? "Connected" : "Disconnected")}");

            _isConnected = isConnected;

            UpdateConnectionStatus();
        }

        public void UpdateConnectionStatus()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"🔄 Updating Connection Status: isConnected = {_isConnected}");

                _isConnected = App.GlobalIsConnected;
                _deviceName = App.GlobalDeviceName;
                _connectionType = App.GlobalConnectionType;
                if (_isConnected)
                {
                    Debug.WriteLine("✅ Connection detected, showing connectionStatusBox.");

                    connectionStatusBox.IsVisible = true; // Make the box visible
                    lblPairedDevice.Text = _deviceName;
                    connectionIcon.Source = _connectionType == "Bluetooth" ? "bluetoothIcon.png" : "wifiIcon.png";
                }
                else
                {
                    Debug.WriteLine("❌ No connection, hiding connectionStatusBox.");
                    connectionStatusBox.IsVisible = false;
                }
            });
        }


    }

}