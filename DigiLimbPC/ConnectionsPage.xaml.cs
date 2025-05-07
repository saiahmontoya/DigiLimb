using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Specialized;
using Microsoft.Maui.Devices;

#if WINDOWS
using DigiLimbDesktop.Platforms.Windows;
#endif


namespace DigiLimbDesktop
{
    public partial class ConnectionsPage : ContentPage
    {

        // Reference to the global ServerService
        private ServerService _serverService;
        private bool _isServerRunning = false;   // Tracks server status
#if WINDOWS
        private BluetoothPeripheral _bluetoothPeripheral;
#endif

        public ConnectionsPage()
        {
            InitializeComponent();


            RequestBluetoothPermissions();

            // Use the global instance of ServerService from App.xaml.cs
            _serverService = App.GlobalServerService;

            // Subscribe to changes in the global log.
            App.GlobalConnectionLog.CollectionChanged += GlobalLog_CollectionChanged;
#if WINDOWS
            if (App.GlobalBluetoothPeripheral != null) // ✅ Only create GATT server if it doesn't exist
            {
                _bluetoothPeripheral = App.GlobalBluetoothPeripheral;
                _bluetoothPeripheral.DeviceInfoReceived -= OnDeviceInfoReceived;
                _bluetoothPeripheral.DeviceInfoReceived += OnDeviceInfoReceived;
                _bluetoothPeripheral.DeviceConnectionChanged -= OnDeviceConnectionChanged;
                _bluetoothPeripheral.DeviceConnectionChanged += OnDeviceConnectionChanged;
                Debug.WriteLine("✅ Subscribed to BluetoothPeripheral Events.");
                if (App.GlobalIsConnected == true)
                {
                    lblAwaitingConnection.IsVisible = false;
                    lblConnectedDevice.Text = $"Connected to: {App.GlobalDeviceName} SWAMP IZZO";
                    lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                    lblConnectedDevice.IsVisible = true;
                    btnAllowIncomingConnection.IsEnabled = false;
                }
                else
                {
                    // This is aimed towards when the user is forced back to connections after a disconnection occurs
                    // I can see how this could possibly get bugged if its back nav'd into. 
                    lblAwaitingConnection.IsVisible = false;
                    lblConnectedDevice.Text = "Lost connection to device.";
                    lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                    lblConnectedDevice.IsVisible = true;
                    btnAllowIncomingConnection.Text = "Cancel";
                }
            }
            else
            {
                Debug.WriteLine("NO GLOBAL GATT");
            }
#endif
        }


        private void GlobalLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Update the chat log text.
                txtLogs.Text = string.Join("\n", App.GlobalConnectionLog);

                // Write each new item to the console.
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        Console.WriteLine(item.ToString());
                    }
                }

                // Update lblPairedDevice with the latest message.
                if (App.GlobalConnectionLog.Any())
                {
                    string lastMsg = App.GlobalConnectionLog.Last().Trim();
                    if (lastMsg.StartsWith("WebSocket Server Running"))
                    {
                        lblPairedDevice.Text = lastMsg;
                        lblPairedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                        lblPairedDevice.FontAttributes = FontAttributes.Bold;
                    }
                    else if (lastMsg == "Server Session Ended")
                    {
                        lblPairedDevice.Text = lastMsg;
                        lblPairedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                        lblPairedDevice.FontAttributes = FontAttributes.Bold;
                    }
                    else
                    {
                        lblPairedDevice.Text = lastMsg;
                        lblPairedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                        lblPairedDevice.FontAttributes = FontAttributes.None;
                    }
                    lblPairedDevice.IsVisible = true;
                }
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Refresh the log area when the page appears.
            txtLogs.Text = string.Join("\n", App.GlobalConnectionLog);
        }



        private async void btnBack_Click(object sender, EventArgs e)
        {
            // Do not stop the server here so the connection remains active.
            await Navigation.PopAsync();
        }

        private void btnAllowIncomingConnection_Click(object sender, EventArgs e)
        {
#if WINDOWS
            if (btnAllowIncomingConnection.Text == "Allow Incoming Bluetooth Connection")
            {

                if (App.GlobalBluetoothPeripheral == null)
                {
                    Debug.WriteLine("✅ Starting GATT Server (Allowing Incoming Connections)");
                    _bluetoothPeripheral = new BluetoothPeripheral();
                    App.GlobalBluetoothPeripheral = _bluetoothPeripheral;
                    _bluetoothPeripheral.DeviceInfoReceived -= OnDeviceInfoReceived;
                    _bluetoothPeripheral.DeviceInfoReceived += OnDeviceInfoReceived;
                    _bluetoothPeripheral.DeviceConnectionChanged -= OnDeviceConnectionChanged;
                    _bluetoothPeripheral.DeviceConnectionChanged += OnDeviceConnectionChanged;
                    

                    Debug.WriteLine("✅ Subscribed to BluetoothPeripheral Events.");

                }
                else
                {
                    Debug.WriteLine("🔄 GATT Server is already running.");
                }


                btnAllowIncomingConnection.Text = "Cancel";
                lblAwaitingConnection.Text = "Awaiting connection...";

            }
            else // User clicks cancel
            {
                if (_bluetoothPeripheral != null)
                {
                    Debug.WriteLine("❌ Stopping GATT Server and Clearing Global Variables");

                    _bluetoothPeripheral.StopAdvertising();
                    _bluetoothPeripheral.Dispose(); // ✅ Fully dispose of the GATT server
                    App.GlobalBluetoothPeripheral = null; // ✅ Clears global instance
                    App.GlobalIsConnected = false;
                    App.GlobalDeviceName = "No Device";
                    App.GlobalConnectionType = "";
                    App.GlobalConnectionStartTime = null;

                    lblAwaitingConnection.Text = "No device paired.";
                    btnAllowIncomingConnection.Text = "Allow Incoming Bluetooth Connection";
                }
            }
#else
            Debug.WriteLine("BLE Peripheral mode not available");
#endif
        }


        private async void RequestBluetoothPermissions()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                Debug.WriteLine("Location permission is required for Bluetooth scanning.");
                return;
            }

            if (CrossBluetoothLE.Current.State != BluetoothState.On)
            {
                Debug.WriteLine("Bluetooth is off. Please enable it.");
            }
        }
        private void OnDeviceConnectionChanged(object sender, bool isConnected)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"📡 Device Connection Status Changed: {(isConnected ? "Connected" : "Disconnected")}");

                if (!isConnected) // ✅ Only update UI when a device disconnects
                {
                    lblConnectedDevice.Text = "Lost connection to device.";
                    lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                    lblConnectedDevice.IsVisible = true;

                    // ✅ Reset global connection state when a device disconnects
                    App.GlobalIsConnected = false;
                    App.GlobalDeviceName = "No Device";
                    App.GlobalConnectionType = "";
                    App.GlobalConnectionStartTime = null;
                }
            });
        }

#if WINDOWS
        private void OnDeviceInfoReceived(object sender, ReceivedDeviceInfo deviceInfo)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (lblConnectedDevice != null)
                {
                    lblConnectedDevice.Text = $"Connected to: {deviceInfo.DeviceName}\nID: {deviceInfo.DeviceId}";
                    lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                    lblConnectedDevice.IsVisible = true;

                    btnAllowIncomingConnection.IsVisible = false;
                    lblAwaitingConnection.IsVisible = false;
                    Debug.WriteLine($"✅ Device Connected: {deviceInfo.DeviceName} (ID: {deviceInfo.DeviceId})");

                    await DisplayAlert("Paired Successfully", $"Paired to {deviceInfo.DeviceName}.", "OK");


                    App.GlobalBluetoothPeripheral = _bluetoothPeripheral;
                    App.GlobalIsConnected = true;
                    App.GlobalDeviceName = deviceInfo.DeviceName ?? "Unknown Device";
                    App.GlobalConnectionType = "Bluetooth"; // Set to "WiFi" if needed
                    App.GlobalConnectionStartTime = DateTime.Now; // Store start time for duration tracking
          
                    Debug.WriteLine("Connection status updated and main page found");

                    await Shell.Current.GoToAsync("//MainPage", true);


                }
                else
                {
                    Debug.WriteLine("❌ lblConnectedDevice is null! UI is not ready.");
                }
            });
            Console.WriteLine($"📡 UI Updated: Connected to {deviceInfo.DeviceName} (ID: {deviceInfo.DeviceId})");
        }
#endif
        private void btnStartServer_Click(object sender, EventArgs e)
        {
            if (!_isServerRunning)
            {
                _serverService.StartServer();
                _isServerRunning = true;
                btnStartServer.IsEnabled = false;
                btnStopServer.IsEnabled = true;
                btnShowQR.IsVisible = true;
            }
        }

        private void btnStopServer_Click(object sender, EventArgs e)
        {
            if (_isServerRunning)
            {
                _serverService.StopServer();
                _isServerRunning = false;
                btnStartServer.IsEnabled = true;
                btnStopServer.IsEnabled = false;
                btnShowQR.IsVisible = false;
            }
        }

        private void OnShowQRCodeClicked(object sender, EventArgs e)
        {
            _serverService?.ShowQRCodePopupAgain();
        }

        private void btnShowQR_Click(object sender, EventArgs e)
        {
            _serverService?.ShowQRCodePopupAgain();

        }

        // New event handler for sending chat messages from desktop to mobile.
        //private async void OnSendChatClicked(object sender, EventArgs e)
        //{
        //    string chatMessage = entryChatMessage.Text;
        //    if (string.IsNullOrWhiteSpace(chatMessage))
        //    {
        //        await DisplayAlert("Error", "Please enter a message.", "OK");
        //        return;
        //    }
        //    try
        //    {
        //        await _serverService.SendChatMessage(chatMessage);
        //        txtLogs.Text += $"\n[Desktop]: {chatMessage}";
        //        entryChatMessage.Text = "";
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"Failed to send chat message: {ex.Message}", "OK");
        //    }
        //}

        // Update UI when the server starts, stops, or a client joins / sends chat messages.
        private void UpdateServerStatus(string message, bool isRunning)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Append the new message to the log.
                txtLogs.Text += $"\n{message}";

                // Display the server status clearly.
                if (message.StartsWith("WebSocket Server Running"))
                {
                    lblPairedDevice.Text = message;
                    lblPairedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                    lblPairedDevice.FontAttributes = FontAttributes.Bold;
                }
                else if (message == "Server Session Ended")
                {
                    lblPairedDevice.Text = message;
                    lblPairedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                    lblPairedDevice.FontAttributes = FontAttributes.Bold;
                }
                else
                {
                    lblPairedDevice.Text = isRunning ? message : "Server Stopped";
                    lblPairedDevice.TextColor = isRunning ? Microsoft.Maui.Graphics.Colors.Green : Microsoft.Maui.Graphics.Colors.Red;
                    lblPairedDevice.FontAttributes = FontAttributes.None;
                }
                lblPairedDevice.IsVisible = true;
            });
            Debug.WriteLine($"📡 Server Status: {message}");
        }

        /// <summary>
        /// Sends a chat message to all connected clients.
        /// </summary>
        //public async Task SendChatMessage(string message)
        //{
        //    if (string.IsNullOrWhiteSpace(message))
        //        return;

        //    string chatMessage = $"CHAT:{message}";
        //    await _serverService.SendChatMessage(chatMessage);
        //}

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
#if WINDOWS
            if (_bluetoothPeripheral != null)
            {
                _bluetoothPeripheral.DeviceInfoReceived -= OnDeviceInfoReceived;
                _bluetoothPeripheral.DeviceConnectionChanged -= OnDeviceConnectionChanged;
            }
        #endif

        }
    }
}
