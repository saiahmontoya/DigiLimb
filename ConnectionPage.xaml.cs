using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Microsoft.Maui.Dispatching;
using System.Net;
using System.Net.Sockets;
using System.Text;

#if WINDOWS
using DigiLimbDesktop.Platforms.Windows;
using Microsoft.Maui.ApplicationModel;
#endif

namespace DigiLimbDesktop
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
        private bool _isServerRunning = false;
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

        // ---------------------- WINDOWS-ONLY BUTTONS ---------------------- //

#if WINDOWS
        /// <summary>
        /// Handles when a Bluetooth device successfully connects.
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

        /// <summary>
        /// Handles when a Bluetooth device is discovered during scanning.
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
        /// Starts the server when button is clicked (Windows only).
        /// </summary>
        private async void btnStartServer_Click(object sender, EventArgs e)
        {
            if (_isServerRunning)
            {
                Log("⚠️ Server is already running.");
                return;
            }

            try
            {
                int openPort = FindOpenPort(5000, 5100);
                await _serverService.StartServer(openPort);
                _isServerRunning = true;
                btnStartServer.IsEnabled = false;
                btnStopServer.IsEnabled = true;
                Log($"🚀 Server started on port {openPort}.");
            }
            catch (Exception ex)
            {
                Log($"❌ Server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the server when button is clicked (Windows only).
        /// </summary>
        private void btnStopServer_Click(object sender, EventArgs e)
        {
            if (!_isServerRunning)
            {
                Log("⚠️ No server is running.");
                return;
            }

            _serverService.StopServer();
            _isServerRunning = false;
            btnStartServer.IsEnabled = true;
            btnStopServer.IsEnabled = false;
            Log("🛑 Server stopped.");
        }

        /// <summary>
        /// Finds an available port in the given range.
        /// </summary>
        private int FindOpenPort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                try
                {
                    using (TcpListener listener = new TcpListener(IPAddress.Loopback, port))
                    {
                        listener.Start();
                        listener.Stop();
                        return port;
                    }
                }
                catch (SocketException)
                {
                    continue; // Try next port
                }
            }
            throw new Exception("No available ports found.");
        }
#endif

        // ---------------------- MOBILE BUTTON HANDLERS ---------------------- //
#if !WINDOWS
        private void btnStartServer_Click(object sender, EventArgs e)
        {
            Log("⚠️ Starting a server is not supported on mobile.");
        }

        private void btnStopServer_Click(object sender, EventArgs e)
        {
            Log("⚠️ Stopping a server is not supported on mobile.");
        }
        private async void OnAddDeviceClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddNewDevicePage());
        }
#else
         private void OnAddDeviceClicked(object sender, EventArgs e)
        {
            Log("⚠️ Add New Device is not supported on Windows.");
        }
#endif

        // ---------------------- COMMON BUTTON HANDLERS ---------------------- //
        private void btnScan_Click(object sender, EventArgs e)
        {
            Log("🔍 Scanning for devices...");
        }

        private void btnAllowIncomingConnection_Click(object sender, EventArgs e)
        {
            Log("⚠️ Allowing incoming connections is not supported on Windows.");
        }

        private void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
        {
            Log("⚠️ Device selection is not supported on Windows.");
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            Log("⚠️ Connecting to a selected device is not supported on Windows.");
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Log("⚠️ Navigating back is not supported on Windows.");
        }
       

        /// <summary>
        /// Logs messages to the UI.
        /// </summary>
        private void Log(string message)
        {
            MainThread.BeginInvokeOnMainThread(() => txtLogs.Text += $"{message}{Environment.NewLine}");
        }
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
