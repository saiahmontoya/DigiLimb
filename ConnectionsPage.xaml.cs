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
#if __ANDROID__
using Android.Bluetooth;
#endif

namespace DigiLimbDesktop
{
    public partial class ConnectionsPage : ContentPage
    {
        private BluetoothManager _bluetoothManager;
        private readonly ObservableCollection<BluetoothDeviceInfo> _deviceList;
        private IDevice _selectedDevice;
        private bool _isScanning = false;

#if WINDOWS
        private BluetoothAdvertiser _bluetoothAdvertiser;
#endif

        public ConnectionsPage(IAdapter adapter)
        {
            InitializeComponent();
            _bluetoothManager = new BluetoothManager(adapter);
            _deviceList = new ObservableCollection<BluetoothDeviceInfo>();
            DevicesListView.ItemsSource = _deviceList;
            SetupBluetoothConnections();
        }

        private void SetupBluetoothConnections()
        {
#if WINDOWS
            _bluetoothAdvertiser = new BluetoothAdvertiser();
            _bluetoothAdvertiser.DeviceConnected += OnDeviceConnected;
#endif
            RequestBluetoothPermissions();
        }

        private void OnDeviceConnected(object sender, DeviceEventArgs e)
        {
            if (e.Device != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblPairedDevice.Text = $"Connected to: {e.Device.Name}";
                    lblPairedDevice.IsVisible = true;
                });
                Log($"Device Connected: {e.Device.Name}");
            }
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
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (_isScanning)
            {
                await _bluetoothManager.StopScanning();
                UpdateUIForScanStopped();
                return;
            }

            var canScan = await _bluetoothManager.StartScanning(OnDeviceDiscovered);
            if (canScan)
            {
                UpdateUIForScanStarted();
            }
            else
            {
                Log("Bluetooth is off or unavailable. Please enable it to scan.");
            }
        }

        private void UpdateUIForScanStarted()
        {
            _isScanning = true;
            btnScan.Text = "Stop Scan";
            btnAllowIncomingConnection.IsVisible = false;
            lblDevicesList.IsVisible = true;
            devicesScrollView.IsVisible = true;
            Log("Scanning for Bluetooth devices...");
        }

        private void UpdateUIForScanStopped()
        {
            _isScanning = false;
            btnScan.Text = "Start Scan";
            btnAllowIncomingConnection.IsVisible = true;
            lblDevicesList.IsVisible = false;
            devicesScrollView.IsVisible = false;
            _deviceList.Clear();
            Log("Scan stopped.");
        }

        private void OnDeviceDiscovered(IDevice device)
        {
            var deviceInfo = new BluetoothDeviceInfo
            {
                DisplayName = $"{device.Name} ({device.Id})",
                DeviceId = device.Id.ToString()
            };
            MainThread.BeginInvokeOnMainThread(() => _deviceList.Add(deviceInfo));
            Log($"Discovered: {deviceInfo.DisplayName}");
        }

        private void Log(string message)
        {
            MainThread.BeginInvokeOnMainThread(() => txtLogs.Text += $"{message}\n");
        }
    }

    public class BluetoothDeviceInfo
    {
        public string DisplayName { get; set; }
        public string DeviceId { get; set; }
    }
}