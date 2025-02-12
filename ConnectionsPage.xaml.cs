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

namespace DigiLimbDesktop
{
    public partial class ConnectionsPage : ContentPage
    {
        private readonly IAdapter _adapter;
        private readonly ObservableCollection<BluetoothDeviceInfo> _deviceList;
        private IDevice _selectedDevice;
        private bool _isScanning = false;
#if WINDOWS
        private BluetoothAdvertiser _bluetoothAdvertiser;
#endif


        public ConnectionsPage()
        {
            InitializeComponent();
            _adapter = CrossBluetoothLE.Current.Adapter;
            _deviceList = new ObservableCollection<BluetoothDeviceInfo>();
            DevicesListView.ItemsSource = _deviceList;

            RequestBluetoothPermissions();

#if WINDOWS
            _bluetoothAdvertiser = new BluetoothAdvertiser();
            _bluetoothAdvertiser.DeviceConnected += OnDeviceConnected;
#endif
        }

        private async Task ToggleScan()
        {
            try
            {
                if (_isScanning)
                {
                    // Stop scanning
                    await _adapter.StopScanningForDevicesAsync();
                    _isScanning = false;
                    btnScan.Text = "Start Scan";
                    btnAllowIncomingConnection.IsVisible = true; // Show the 'Allow Incoming Connection' button again
                    lblDevicesList.IsVisible = false; // Hide "Discovered Devices" label
                    devicesScrollView.IsVisible = false; // Hide the device list
                    _deviceList.Clear(); // Clear the list when scan stops
                    Log("Scan stopped.");
                    return;
                }

                // Ensure Bluetooth is enabled
                if (CrossBluetoothLE.Current.State != BluetoothState.On)
                {
                    Log("Bluetooth is off. Please enable it.");
                    return;
                }

                // Clear any previous data
                _deviceList.Clear();
                btnConnect.IsEnabled = false;
                _isScanning = true;
                btnScan.Text = "Stop Scan";
                btnAllowIncomingConnection.IsVisible = false; // Hide the 'Allow Incoming Connection' button

                // Prevent duplicate event handlers
                _adapter.DeviceDiscovered -= OnDeviceDiscovered;
                _adapter.DeviceDiscovered += OnDeviceDiscovered;

                lblDevicesList.IsVisible = true; // Show "Discovered Devices" label
                devicesScrollView.IsVisible = true; // Show devices list
                Log("Scanning for Bluetooth devices...");
                await _adapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                Log($"Scan error: {ex.Message}");
            }
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args)
        {
            var device = args.Device;
            if (device == null)
                return;

            // Filter out devices based on manufacturer data or other criteria
            var manufacturerData = device.AdvertisementRecords?.FirstOrDefault(record => record.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ManufacturerSpecificData);
            if (manufacturerData == null || manufacturerData.Data.Length < 4)
                return;

            int manufacturerId = manufacturerData.Data[0] | (manufacturerData.Data[1] << 8);
            if (manufacturerId != 0x004C) // Apple Manufacturer ID (for example)
                return;

            string deviceType = "Unknown Device";
            byte productId = manufacturerData.Data[2]; // Device category (e.g., iPhone, MacBook)

            // Assign device type based on the productId
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
                Log($"Discovered: {deviceType}");
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
                Log($"Selected: {selected.DisplayName}");
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (_selectedDevice == null) return;

            try
            {
                await _adapter.ConnectToDeviceAsync(_selectedDevice);
                Log($"Connected to {_selectedDevice.Name}");

                // Show connected device info and hide device list
                lblPairedDevice.Text = $"Connected to: {_selectedDevice.Name}";
                lblPairedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                lblPairedDevice.IsVisible = true;

                // Hide scan related UI elements
                lblDevicesList.IsVisible = false;
                devicesScrollView.IsVisible = false;
                btnScan.IsVisible = true; // Make sure the "Start Scan" button is still visible
                btnConnect.IsEnabled = false; // Disable the Connect button after successful connection
            }
            catch (Exception ex)
            {
                Log($"Connection failed: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            MainThread.BeginInvokeOnMainThread(() => txtLogs.Text += $"{message}{Environment.NewLine}");
        }

        private async void btnBack_Click(object sender, EventArgs e)
        {
            // Navigate back to the previous page
            await Navigation.PopAsync();
        }

        private void btnAllowIncomingConnection_Click(object sender, EventArgs e)
        {
#if WINDOWS

            if (btnAllowIncomingConnection.Text == "Allow Incoming Connection")
            {
                _bluetoothAdvertiser.StartAdvertising();
                Log("Started advertising DigiLimbDevice.");

                btnAllowIncomingConnection.Text = "Cancel";
                lblAwaitingConnection.IsVisible = true;
                btnScan.IsVisible = false;
            }
            else
            {
                _bluetoothAdvertiser.StopAdvertising();
                Log("Stopped advertising DigiLimbDevice.");

                btnAllowIncomingConnection.Text = "Allow Incoming Connection";
                lblAwaitingConnection.IsVisible = false;
                btnScan.IsVisible = true;
            }
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
                Log("Location permission is required for Bluetooth scanning.");
                return;
            }

            // Check if Bluetooth is enabled
            if (CrossBluetoothLE.Current.State != BluetoothState.On)
            {
                Log("Bluetooth is off. Please enable it.");
            }
        }


            private void OnDeviceConnected(string deviceName, string deviceId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblConnectedDevice.Text = $"Connected to: {deviceName}\nID: {deviceId}";
                lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                lblConnectedDevice.IsVisible = true;

                // Hide other elements
                btnAllowIncomingConnection.IsVisible = false;
                lblAwaitingConnection.IsVisible = false;
            });

            Log($"Device Connected: {deviceName} (ID: {deviceId})");
        }

    }

}

    public class BluetoothDeviceInfo
    {
        public string DisplayName { get; set; }
        public string DeviceId { get; set; }
        public string ManufacturerData { get; set; }
    }


