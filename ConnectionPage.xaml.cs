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
#if WINDOWS
using DigiLimb.Platforms.Windows;
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

#if !WINDOWS
        /// <summary>
        /// Handles navigation to the add new device page in mobile.
        /// </summary>
        private async void OnAddDeviceClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddNewDevicePage());
        }
#endif

#if WINDOWS
        /// <summary>
        /// Toggles Bluetooth scanning for available devices.
        /// </summary>
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
                    Log("Scan stopped.");
                    return;
                }

                if (CrossBluetoothLE.Current.State != BluetoothState.On)
                {
                    Log("Bluetooth is off. Please enable it.");
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
                Log("Scanning for Bluetooth devices...");
                await _adapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                Log($"Scan error: {ex.Message}");
            }
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
        }
#endif
    }
}
