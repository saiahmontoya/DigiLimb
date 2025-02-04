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
        private IAdapter _adapter;
        private ObservableCollection<BluetoothDeviceInfo> _deviceList;

        public ConnectionsPage()
        {
            InitializeComponent();
            _deviceList = new ObservableCollection<BluetoothDeviceInfo>();
            lstDevices.ItemsSource = _deviceList; // Bind the ListView to the ObservableCollection
        }

        // Start BLE scanning
        private async Task StartBLEScan()
        {
            _adapter = CrossBluetoothLE.Current.Adapter;

            _adapter.DeviceDiscovered += (s, a) =>
            {
                var device = a.Device;
                var deviceName = device.Name ?? "Unnamed Device";
                var deviceId = device.Id.ToString();
                var manufacturerData = string.Empty;

                // Access manufacturer data if available (plugin doesn't support advertisement directly)
                var manufacturerInfo = "Manufacturer data unavailable";

                // Create BluetoothDeviceInfo object for displaying in ListView
                var deviceInfo = new BluetoothDeviceInfo
                {
                    DeviceName = deviceName,
                    DeviceId = deviceId,
                    ManufacturerData = manufacturerInfo,
                    Model = "Unknown Model"
                };

                // Add the discovered device to the ObservableCollection
                _deviceList.Add(deviceInfo);

                Log($"Discovered: {deviceInfo.DeviceName}");
            };

            try
            {
                Log("Scanning for Bluetooth devices...");
                await _adapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                Log($"Error starting BLE scan: {ex.Message}");
            }
        }

        // Log to the TextBox
        private void Log(string message)
        {
            txtLogs.Text += $"{message}{Environment.NewLine}";
        }

        // Button click event to start scanning
        private async void btnStartScan_Click(object sender, EventArgs e)
        {
            await StartBLEScan();
        }

        // Bluetooth device information
        public class BluetoothDeviceInfo
        {
            public string DeviceName { get; set; }
            public string DeviceId { get; set; }
            public string ManufacturerData { get; set; }
            public string Model { get; set; }
        }
    }
}
