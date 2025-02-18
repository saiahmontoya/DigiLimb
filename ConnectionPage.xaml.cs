using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Microsoft.Maui.Dispatching;
using DigiLimbMobile.View;
using System.Net;
using System.Net.Sockets;
using System.Text;

#if WINDOWS
using DigiLimbDesktop.Platforms.Windows;
using Microsoft.Maui.ApplicationModel;
#endif

namespace DigiLimb
{
    public partial class ConnectionPage : ContentPage
    {
#if WINDOWS
        private readonly IAdapter _adapter;
        private readonly ObservableCollection<BluetoothDeviceInfo> _deviceList;
        private IDevice _selectedDevice;
        private bool _isScanning = false;
        private BluetoothAdvertiser _bluetoothAdvertiser;
        private ServerService _serverService;
#endif

        public ConnectionPage()
        {
            InitializeComponent();
            ConfigurePlatformUI();

#if WINDOWS
            _adapter = CrossBluetoothLE.Current.Adapter;
            _deviceList = new ObservableCollection<BluetoothDeviceInfo>();
            DevicesListView.ItemsSource = _deviceList;

            _bluetoothAdvertiser = new BluetoothAdvertiser();
            _bluetoothAdvertiser.DeviceConnected += OnDeviceConnected;

            _serverService = new ServerService();
            StartServerAndBroadcast();

            RequestBluetoothPermissions();
#endif
        }

        /// <summary>
        /// Configures UI elements visibility based on platform.
        /// </summary>
        private void ConfigurePlatformUI()
        {
#if WINDOWS
            Title = "Bluetooth Device Scanner";
            DesktopView.IsVisible = true;
            MobileView.IsVisible = false;
#else
            Title = "Connect";
            MobileView.IsVisible = true;
            DesktopView.IsVisible = false;
#endif
        }

#if WINDOWS
        /// <summary>
        /// Requests Bluetooth permissions at runtime.
        /// </summary>
        private async void RequestBluetoothPermissions()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                Log("⚠️ Location permission is required for Bluetooth scanning.");
                return;
            }

            if (CrossBluetoothLE.Current.State != BluetoothState.On)
            {
                Log("⚠️ Bluetooth is off. Please enable it.");
            }
        }

        /// <summary>
        /// Starts the server and begins broadcasting.
        /// </summary>
        private async void StartServerAndBroadcast()
        {
            try
            {
                Log("🚀 Starting server...");
                await _serverService.StartServer(5000);
                Log("✅ Server started.");

                Log("📡 Starting broadcast...");
                _ = Task.Run(() => UdpBroadcaster.BroadcastServerIP());
            }
            catch (Exception ex)
            {
                Log($"❌ Server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops broadcasting when the page is destroyed.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Log("🛑 Stopping server and broadcasting...");
            _serverService?.StopServer();
        }

        /// <summary>
        /// Logs messages to the UI.
        /// </summary>
        private void Log(string message)
        {
            MainThread.BeginInvokeOnMainThread(() => txtLogs.Text += $"{message}{Environment.NewLine}");
        }

        /// <summary>
        /// Allows incoming Bluetooth connections by starting advertising.
        /// </summary>
        private void btnAllowIncomingConnection_Click(object sender, EventArgs e)
        {
            if (btnAllowIncomingConnection.Text == "Allow Incoming Connection")
            {
                _bluetoothAdvertiser.StartAdvertising();
                Log("📡 Started advertising DigiLimbDevice.");
                btnAllowIncomingConnection.Text = "Cancel";
                lblAwaitingConnection.IsVisible = true;
                btnScan.IsVisible = false;
            }
            else
            {
                _bluetoothAdvertiser.StopAdvertising();
                Log("📴 Stopped advertising DigiLimbDevice.");
                btnAllowIncomingConnection.Text = "Allow Incoming Connection";
                lblAwaitingConnection.IsVisible = false;
                btnScan.IsVisible = true;
            }
        }

        /// <summary>
        /// Handles the event when a Bluetooth device is discovered.
        /// </summary>
        private void OnDeviceDiscovered(object sender, DeviceEventArgs args)
        {
            var device = args.Device;
            if (device == null) return;

            var manufacturerData = device.AdvertisementRecords?.FirstOrDefault(record => record.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ManufacturerSpecificData);
            if (manufacturerData == null || manufacturerData.Data.Length < 4) return;

            int manufacturerId = manufacturerData.Data[0] | (manufacturerData.Data[1] << 8);
            string deviceType = "Unknown Device";
            byte productId = manufacturerData.Data[2];

            deviceType = productId switch
            {
                0x12 => "Apple iPhone",
                0x19 => "Apple Watch",
                _ => "Unknown Device"
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
                Log($"🔎 Discovered: {deviceType}");
            }
        }

        /// <summary>
        /// Handles the event when a Bluetooth device is successfully connected.
        /// </summary>
        private void OnDeviceConnected(string deviceName, string deviceId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblConnectedDevice.Text = $"✅ Connected to: {deviceName}\nID: {deviceId}";
                lblConnectedDevice.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                lblConnectedDevice.IsVisible = true;

                btnAllowIncomingConnection.IsVisible = false;
                lblAwaitingConnection.IsVisible = false;
            });

            Log($"🔗 Device Connected: {deviceName} (ID: {deviceId})");
        }
#endif
    }

    /// <summary>
    /// Represents a discovered Bluetooth device.
    /// </summary>
    public class BluetoothDeviceInfo
    {
        public string DisplayName { get; set; }
        public string DeviceId { get; set; }
        public string ManufacturerData { get; set; }
    }
}
