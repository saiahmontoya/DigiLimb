using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Microsoft.Maui.Controls;
#if WINDOWS
using DigiLimbDesktop.Platforms.Windows;
#endif
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Specialized;
using Microsoft.Maui.Devices;

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
            }

            if (App.GlobalIsConnected == true)
            {
                lblAwaitingConnection.IsVisible = false;
                lblConnectedDevice.Text = $"Connected to: {App.GlobalDeviceName} SWAMP IZZO";
                lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                lblConnectedDevice.IsVisible = true;
                btnAllowIncomingConnection.IsEnabled = false;
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

                    lblAwaitingConnection.Text = "No device paired.";
                    btnAllowIncomingConnection.Text = "Allow Incoming Bluetooth Connection";
                }
            }
#else
            Debug.WriteLine("BLE Peripheral Mode is not available on this platform.");
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

#if WINDOWS
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
#endif



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

                    // ✅ Find MainPage dynamically
                    var mainPage = FindMainPage();
                    if (mainPage != null)
                    {
                        App.GlobalIsConnected = true;
                        App.GlobalDeviceName = deviceInfo.DeviceName ?? "Unknown Device";
                        App.GlobalConnectionType = "Bluetooth"; // Set to "WiFi" if needed
                        App.GlobalConnectionStartTime = DateTime.Now; // Store start time for duration tracking
                        mainPage.UpdateConnectionStatus();
                        Debug.WriteLine("Connection status updated and main page found");
                    }
                    else
                    {
                        Debug.WriteLine("❌ Could not find MainPage dynamically.");
                    }


                    await Shell.Current.GoToAsync("//MainPage");


                }
                else
                {
                    Debug.WriteLine("❌ lblConnectedDevice is null! UI is not ready.");
                }
            });
            Console.WriteLine($"📡 UI Updated: Connected to {deviceInfo.DeviceName} (ID: {deviceInfo.DeviceId})");
        }
#endif
        /// <summary>
        /// Dynamically finds and returns MainPage from the application's navigation structure.
        /// </summary>
        public static MainPage FindMainPage()
        {
            // 1️⃣ Check if MainPage is the current visible page
            if (Application.Current.MainPage is MainPage directMainPage)
                return directMainPage;

            // 2️⃣ Check if MainPage is wrapped in a NavigationPage
            if (Application.Current.MainPage is NavigationPage navPage)
            {
                if (navPage.RootPage is MainPage mainPage)
                    return mainPage;
            }

            // 3️⃣ Check if Shell contains MainPage (dealing with deep nesting)
            if (Application.Current.MainPage is Shell shell)
            {
                foreach (var item in shell.Items) // Iterate through ShellItems
                {
                    if (item is ShellItem shellItem)
                    {
                        foreach (var section in shellItem.Items) // Look in ShellSections
                        {
                            if (section is ShellSection shellSection)
                            {
                                foreach (var content in shellSection.Items) // Look in ShellContent
                                {
                                    if (content is ShellContent shellContent && shellContent.Content is MainPage foundMainPage)
                                    {
                                        return foundMainPage;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 4️⃣ LAST RESORT: Search through ALL pages in the Navigation Stack (in case it's been pushed)
            foreach (var page in Application.Current.MainPage.Navigation.NavigationStack)
            {
                if (page is MainPage foundMainPage)
                    return foundMainPage;
            }

            Debug.WriteLine("❌ MainPage STILL not found in navigation structure.");
            return null;
        }


        private void btnStartServer_Click(object sender, EventArgs e)
        {
            if (!_isServerRunning)
            {
                _serverService.StartServer();
                _isServerRunning = true;
                btnStartServer.IsEnabled = false;
                btnStopServer.IsEnabled = true;
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
            }
        }

        // New event handler for sending chat messages from desktop to mobile.
        private async void OnSendChatClicked(object sender, EventArgs e)
        {
            string chatMessage = entryChatMessage.Text;
            if (string.IsNullOrWhiteSpace(chatMessage))
            {
                await DisplayAlert("Error", "Please enter a message.", "OK");
                return;
            }
            try
            {
                await _serverService.SendChatMessage(chatMessage);
                txtLogs.Text += $"\n[Desktop]: {chatMessage}";
                entryChatMessage.Text = "";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to send chat message: {ex.Message}", "OK");
            }
        }

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
    }

}