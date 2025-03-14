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

namespace DigiLimbDesktop
{
    public partial class ConnectionsPage : ContentPage
    {
        // Bluetooth Private Variables
        private readonly IAdapter _adapter;
        private readonly ObservableCollection<BluetoothDeviceInfo> _deviceList;
        private IDevice _selectedDevice;
        private bool _isScanning = false;

        // Reference to the global ServerService
        private ServerService _serverService;
        private bool _isServerRunning = false;   // Tracks server status

#if WINDOWS
        private BluetoothPeripheral _bluetoothPeripheral;
#endif

        public ConnectionsPage()
        {
            InitializeComponent();
            _adapter = CrossBluetoothLE.Current.Adapter;
            _deviceList = new ObservableCollection<BluetoothDeviceInfo>();
            DevicesListView.ItemsSource = _deviceList;

            RequestBluetoothPermissions();

            // Use the global instance of ServerService from App.xaml.cs
            _serverService = App.GlobalServerService;

            // Subscribe to changes in the global log.
            App.GlobalConnectionLog.CollectionChanged += GlobalLog_CollectionChanged;

#if WINDOWS
            _bluetoothPeripheral = new BluetoothPeripheral(_adapter);
            _bluetoothPeripheral.DeviceInfoReceived += OnDeviceInfoReceived;

            // Ensure event subscription is active.
            _bluetoothPeripheral.DeviceConnected -= OnDeviceConnected;
            _bluetoothPeripheral.DeviceConnected += OnDeviceConnected;

            _bluetoothPeripheral.DeviceDisconnected -= OnDeviceDisconnected;
            _bluetoothPeripheral.DeviceDisconnected += OnDeviceDisconnected;

            _bluetoothPeripheral.MonitorDeviceConnections();
            Console.WriteLine("?? Subscribed to BluetoothPeripheral Events.");
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

        private async Task ToggleScan()
        {
            try
            {
                if (_isScanning)
                {
                    await _adapter.StopScanningForDevicesAsync();
                    _isScanning = false;
                    btnScan.Text = "Start Scan";
                    btnAllowIncomingConnection.IsVisible = true;
                    lblDevicesList.IsVisible = false;
                    devicesScrollView.IsVisible = false;
                    _deviceList.Clear();
                    Debug.WriteLine("Scan stopped.");
                    return;
                }

                if (CrossBluetoothLE.Current.State != BluetoothState.On)
                {
                    Debug.WriteLine("Bluetooth is off. Please enable it.");
                    return;
                }

                _deviceList.Clear();
                btnConnect.IsEnabled = false;
                _isScanning = true;
                btnScan.Text = "Stop Scan";
                btnAllowIncomingConnection.IsVisible = false;

                _adapter.DeviceDiscovered -= OnDeviceDiscovered;
                _adapter.DeviceDiscovered += OnDeviceDiscovered;

                lblDevicesList.IsVisible = true;
                devicesScrollView.IsVisible = true;
                Debug.WriteLine("Scanning for Bluetooth devices...");
                await _adapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Scan error: {ex.Message}");
            }
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args)
        {
            var device = args.Device;
            if (device == null)
                return;

            var manufacturerData = device.AdvertisementRecords?
                .FirstOrDefault(record => record.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ManufacturerSpecificData);
            if (manufacturerData == null || manufacturerData.Data.Length < 4)
                return;

            int manufacturerId = manufacturerData.Data[0] | (manufacturerData.Data[1] << 8);
            if (manufacturerId != 0x004C)
                return;

            string deviceType = "Unknown Device";
            byte productId = manufacturerData.Data[2];

            deviceType = productId switch
            {
                0x12 => "Apple iPhone",
                0x19 => "Apple Watch",
                _ => "Unknown Apple Device"
            };

            if (!_deviceList.Any(d => d.DeviceId == device.Id.ToString()))
            {
                var deviceInfo = new BluetoothDeviceInfo
                {
                    DisplayName = deviceType,
                    DeviceId = device.Id.ToString(),
                    ManufacturerData = $"Manufacturer ID: {manufacturerId} - {deviceType}"
                };

                MainThread.BeginInvokeOnMainThread(() => _deviceList.Add(deviceInfo));
                Debug.WriteLine($"Discovered: {deviceType}");
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            await ToggleScan();
        }

        private void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is BluetoothDeviceInfo selected)
            {
                _selectedDevice = _adapter.DiscoveredDevices.FirstOrDefault(d => d.Id.ToString() == selected.DeviceId);
                btnConnect.IsEnabled = _selectedDevice != null;
                Debug.WriteLine($"Selected: {selected.DisplayName}");
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (_selectedDevice == null) return;

            try
            {
                await _adapter.ConnectToDeviceAsync(_selectedDevice);
                Debug.WriteLine($"Connected to {_selectedDevice.Name}");

                lblPairedDevice.Text = $"Connected to: {_selectedDevice.Name}";
                lblPairedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                lblPairedDevice.IsVisible = true;

                lblDevicesList.IsVisible = false;
                devicesScrollView.IsVisible = false;
                btnScan.IsVisible = true;
                btnConnect.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connection failed: {ex.Message}");
            }
        }

        private async void btnBack_Click(object sender, EventArgs e)
        {
            // Do not stop the server here so the connection remains active.
            await Navigation.PopAsync();
        }

        private void btnAllowIncomingConnection_Click(object sender, EventArgs e)
        {
#if WINDOWS
            if (btnAllowIncomingConnection.Text == "Allow Incoming Connection")
            {
                _bluetoothPeripheral.Start();
                Debug.WriteLine("Started BLE Peripheral Mode: Advertising DigiLimb Device.");
                Debug.WriteLine("GATT started");

                btnAllowIncomingConnection.Text = "Cancel";
                lblAwaitingConnection.IsVisible = true;
                btnScan.IsVisible = false;
            }
            else
            {
                _bluetoothPeripheral.StopAdvertising();
                Debug.WriteLine("Stopped BLE Peripheral Mode.");

                btnAllowIncomingConnection.Text = "Allow Incoming Connection";
                lblAwaitingConnection.IsVisible = false;
                btnScan.IsVisible = true;
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
                        mainPage.UpdateConnectionStatus();
                        Debug.WriteLine("Connection status updated and main page found");
                    }
                    else
                    {
                        Debug.WriteLine("❌ Could not find MainPage dynamically.");
                    }


                    await Shell.Current.GoToAsync("//MainPage");

                    /*
                    await Task.Delay(500); // Give UI a little time to update
                    var refreshedMainPage = FindMainPage();
                    if (refreshedMainPage != null)
                    {
                        refreshedMainPage.UpdateConnectionStatus();
                    }
                    */
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
        private MainPage FindMainPage()
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

        private void OnDeviceConnected(object? sender, IDevice device)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (lblConnectedDevice == null)
                {
                    Debug.WriteLine("❌ lblConnectedDevice is null! UI is not ready.");
                    return;
                }

                if (device == null)
                {
                    Debug.WriteLine("❌ Device is null! No valid connection.");
                    return;
                }
                lblConnectedDevice.Text = $"Connected to: {device.Name ?? "Unknown Device"}\nID: {device.Id}";
                lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                lblConnectedDevice.IsVisible = true;

                btnAllowIncomingConnection.IsVisible = false;
                lblAwaitingConnection.IsVisible = false;

                Debug.WriteLine($"✅ Device Connected: {device.Name} (ID: {device.Id})");

                MainPage mainPage = (MainPage)Application.Current.MainPage;
                mainPage._isConnected = true;
                mainPage._deviceName = device.Name ?? "Unknown Device";
                mainPage._connectionType = "Bluetooth"; // Set to "WiFi" if connected via WiFi
                mainPage.UpdateConnectionStatus();

                await DisplayAlert("Paired Successfully", $"Paired to {device.Name}.", "OK");
                await Shell.Current.GoToAsync("//MainPage");
            });
        }

        private void OnDeviceDisconnected(object sender, IDevice device)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblConnectedDevice.Text = "Device Disconnected";
                lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                lblConnectedDevice.IsVisible = false;

                btnAllowIncomingConnection.IsVisible = true;
                lblAwaitingConnection.IsVisible = true;

                MainPage mainPage = (MainPage)Application.Current.MainPage;
                mainPage._isConnected = false;
                mainPage.UpdateConnectionStatus();

                Debug.WriteLine("Device Disconnected.");
            });
            Debug.WriteLine("Device Disconnected.");
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

    public class BluetoothDeviceInfo
    {
        public string DisplayName { get; set; }
        public string DeviceId { get; set; }
        public string ManufacturerData { get; set; }
    }
}
