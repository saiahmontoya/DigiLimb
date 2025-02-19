using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Radios;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Text;

namespace DigiLimbDesktop.Platforms.Windows
{

    public class ReceivedDeviceInfo
    {
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public string Manufacturer { get; set; }
    }

    public class BluetoothPeripheral
    {
        private GattServiceProvider _gattServiceProvider;
        private GattLocalCharacteristic _characteristic;
        private IAdapter _adapter;
        private IDevice _connectedDevice; // Store the connected device

        public event EventHandler<IDevice> DeviceConnected;
        public event EventHandler<IDevice> DeviceDisconnected;

        public BluetoothPeripheral(IAdapter adapter)
        {
            _adapter = adapter;
            

            MainThread.InvokeOnMainThreadAsync(async () => await InitializeBluetoothAsync());
        }
        
        // Establish bluetooth permissions
        private async Task InitializeBluetoothAsync()
        {
            var accessStatus = await Radio.RequestAccessAsync();
            if (accessStatus != RadioAccessStatus.Allowed)
            {
                Debug.WriteLine("❌ Bluetooth access denied. Cannot start GATT server.");
                return;
            }
            Debug.WriteLine("✅ Bluetooth access granted.");
            await StartGattServer();
        }

        // Create new gatt server
        private async Task StartGattServer()
        {
            var serviceUuid = new Guid("0000FFF0-0000-1000-8000-00805F9B34FB"); // Custom service UUID

            var serviceResult = await GattServiceProvider.CreateAsync(serviceUuid);
            if (serviceResult.Error != BluetoothError.Success)
            {
                Debug.WriteLine("❌ Failed to create GATT service.");
                return;
            }

            _gattServiceProvider = serviceResult.ServiceProvider;
            Debug.WriteLine("✅ GATT Server Initialized Successfully.");

            _gattServiceProvider.AdvertisementStatusChanged += (sender, args) =>
            {
                Debug.WriteLine($"🔹 GATT Server Status: {args.Status}");
            };


            // Define characteristic properties 
            var characteristicParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read |
                                   GattCharacteristicProperties.Write |
                                   GattCharacteristicProperties.Notify, // ✅ Enables all three
                ReadProtectionLevel = GattProtectionLevel.Plain,
                WriteProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "Connection Signal with Read, Write & Notify"
            };

            var characteristicResult = await _gattServiceProvider.Service.CreateCharacteristicAsync(
                new Guid("0000FFF2-0000-1000-8000-00805F9B34FB"), // Characteristic UUID
                characteristicParameters
            );

            if (characteristicResult.Error == BluetoothError.Success)
            {
                _characteristic = characteristicResult.Characteristic;
                _characteristic.ReadRequested += OnReadRequested;
                _characteristic.WriteRequested += OnWriteRequested;
                Debug.WriteLine("✅ GATT Characteristic Created.");
            }
            else
            {
                Debug.WriteLine("❌ Failed to create characteristic.");
                return;
            }

            _gattServiceProvider.AdvertisementStatusChanged += (sender, args) =>
            {
                Debug.WriteLine($"🔹 GATT Server Status: {args.Status}");
            };

            // Ensure server can be connected to 
            _gattServiceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true,
                
            });

            Debug.WriteLine("✅ GATT Server Advertising Started.");
        }

        // Send data for mobile device to read
        private async void OnReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            var deferral = args.GetDeferral(); // Take deferral to process the request

            try
            {
                // Create the string message you want to send
                string message = "Hello from DigiLimb!";

                // Convert the message to a byte array (UTF-8 encoding)
                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

                // Convert the byte[] to IBuffer
                IBuffer buffer = messageBytes.AsBuffer();

                var request = await args.GetRequestAsync();
                if (request != null)
                {
                    // Respond with the IBuffer as the value
                    request.RespondWithValue(buffer); // Send data to the connected device
                    Debug.WriteLine("📡 Device Read Requested: Sent Data.");
                }
            }
            finally
            {
                deferral.Complete(); // Complete request handling
            }
        }

        public event EventHandler<ReceivedDeviceInfo> DeviceInfoReceived;

        private async void OnWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            try
            {
                var request = await args.GetRequestAsync();
                if (request == null) return;

                var reader = DataReader.FromBuffer(request.Value);
                byte[] receivedBytes = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(receivedBytes);

                string mobileDeviceName = Encoding.UTF8.GetString(receivedBytes, 0, 20).Trim();
                string mobileDeviceId = Encoding.UTF8.GetString(receivedBytes, 20, 20).Trim();
                string manufacturerData = Encoding.UTF8.GetString(receivedBytes, 40, 20).Trim();

                Debug.WriteLine($"📡 Received Mobile Device Info: {mobileDeviceName}, {mobileDeviceId}, {manufacturerData}");


                // ✅ Parse JSON message
                try
                {
                    ReceivedDeviceInfo deviceInfo = new ReceivedDeviceInfo
                    {
                        DeviceName = mobileDeviceName,
                        DeviceId = mobileDeviceId,
                        Manufacturer = manufacturerData
                    };

                    DeviceInfoReceived?.Invoke(this, deviceInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error parsing device info: {ex.Message}");
                }
            }
            finally
            {
                deferral.Complete();
            }
        }





        public void Start()
        {
            MainThread.InvokeOnMainThreadAsync(async () => await StartGattServer());
        }

        public void StopAdvertising()
        {
            _gattServiceProvider?.StopAdvertising();
            Debug.WriteLine("🔹 GATT Server Stopped Advertising.");
        }

        // ✅ Internal handler for DeviceConnected event
        private void OnDeviceConnectedInternal(object sender, DeviceEventArgs args)
        {
            _connectedDevice = args.Device;
            Debug.WriteLine($"✅ Device Connected: {_connectedDevice.Name}");

            // Debugging check to see if DeviceConnected has subscribers
            if (DeviceConnected == null)
            {
                Debug.WriteLine("❌ DeviceConnected event has NO subscribers!");
            }
            else
            {
                Debug.WriteLine("📡 Raising DeviceConnected event.");
                DeviceConnected?.Invoke(this, _connectedDevice);
            }
        }

        // ✅ Internal handler for DeviceDisconnected event
        private void OnDeviceDisconnectedInternal(object sender, DeviceEventArgs args)
        {
            _connectedDevice = null;
                Debug.WriteLine("❌ Device Disconnected");

            // Debugging check
            if (DeviceDisconnected == null)
            {
                Debug.WriteLine("❌ DeviceDisconnected event has NO subscribers!");
            }
            else
            {
                Debug.WriteLine("📡 Raising DeviceDisconnected event.");
                DeviceDisconnected?.Invoke(this, args.Device);
            }
        }

        // ✅ Ensure the Connection Events are Subscribed
        public void MonitorDeviceConnections()
        {
            _adapter.DeviceConnected -= OnDeviceConnectedInternal; // Remove previous handlers to prevent duplicates
            _adapter.DeviceConnected += OnDeviceConnectedInternal;

            _adapter.DeviceDisconnected -= OnDeviceDisconnectedInternal;
            _adapter.DeviceDisconnected += OnDeviceDisconnectedInternal;
        }
    }
}
