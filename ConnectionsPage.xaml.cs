using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Microsoft.Maui.Controls;
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

        public ConnectionsPage()
        {
            InitializeComponent();
            _adapter = CrossBluetoothLE.Current.Adapter;
            _deviceList = new ObservableCollection<BluetoothDeviceInfo>();
            DevicesListView.ItemsSource = _deviceList;
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
                    Log("Scan stopped.");
                }
                else
                {
                    // Start scanning
                    _deviceList.Clear();
                    btnConnect.IsEnabled = false;
                    _isScanning = true;
                    btnScan.Text = "Stop Scan";

                    _adapter.DeviceDiscovered += (s, a) =>
                    {
                        var device = a.Device;
                        if (device == null || _deviceList.Any(d => d.DeviceId == device.Id.ToString())) return;

                        var deviceInfo = new BluetoothDeviceInfo
                        {
                            DisplayName = device.Name ?? "Unknown Device",
                            DeviceId = device.Id.ToString(),
                            ManufacturerData = "Unknown Manufacturer" // Placeholder (modify if needed)
                        };

                        MainThread.BeginInvokeOnMainThread(() => _deviceList.Add(deviceInfo));
                        Log($"Discovered: {deviceInfo.DisplayName}");
                    };

                    Log("Scanning for Bluetooth devices...");
                    await _adapter.StartScanningForDevicesAsync();
                }
            }
            catch (Exception ex)
            {
                Log($"Scan error: {ex.Message}");
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
    }

    public class BluetoothDeviceInfo
    {
        public string DisplayName { get; set; }
        public string DeviceId { get; set; }
        public string ManufacturerData { get; set; }
    }
}
