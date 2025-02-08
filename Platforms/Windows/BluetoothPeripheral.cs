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

namespace DigiLimbDesktop.Platforms.Windows
{
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

        private async Task InitializeBluetoothAsync()
        {
            var accessStatus = await Radio.RequestAccessAsync();
            if (accessStatus != RadioAccessStatus.Allowed)
            {
                Console.WriteLine("❌ Bluetooth access denied. Cannot start GATT server.");
                return;
            }
            Console.WriteLine("✅ Bluetooth access granted.");
            await StartGattServer();
        }

        private async Task StartGattServer()
        {
            var serviceUuid = new Guid("0000FFF0-0000-1000-8000-00805F9B34FB"); // Custom service UUID

            var serviceResult = await GattServiceProvider.CreateAsync(serviceUuid);
            if (serviceResult.Error != BluetoothError.Success)
            {
                Console.WriteLine("❌ Failed to create GATT service.");
                return;
            }

            _gattServiceProvider = serviceResult.ServiceProvider;



            var characteristicParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "DigiLimb Data"
            };

            var characteristicResult = await _gattServiceProvider.Service.CreateCharacteristicAsync(
                new Guid("0000FFF1-0000-1000-8000-00805F9B34FB"), // Characteristic UUID
                characteristicParameters
            );

            if (characteristicResult.Error == BluetoothError.Success)
            {
                _characteristic = characteristicResult.Characteristic;
                _characteristic.ReadRequested += OnReadRequested;
                Console.WriteLine("✅ GATT Characteristic Created.");
            }
            else
            {
                Console.WriteLine("❌ Failed to create characteristic.");
                return;
            }

            _gattServiceProvider.AdvertisementStatusChanged += (sender, args) =>
            {
                Console.WriteLine($"🔹 GATT Server Status: {args.Status}");
            };

            _gattServiceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true,
            });

            Console.WriteLine("✅ GATT Server Advertising Started.");
        }

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
                    Console.WriteLine("📡 Device Read Requested: Sent Data.");
                }
            }
            finally
            {
                deferral.Complete(); // Complete request handling
            }
        }




        public void Start()
        {
            MainThread.InvokeOnMainThreadAsync(async () => await StartGattServer());
        }

        public void StopAdvertising()
        {
            _gattServiceProvider?.StopAdvertising();
            Console.WriteLine("🔹 GATT Server Stopped Advertising.");
        }

        // Internal handler for DeviceConnected event
        private void OnDeviceConnectedInternal(object sender, DeviceEventArgs args)
        {
            _connectedDevice = args.Device;
            Console.WriteLine($"✅ Device Connected: {_connectedDevice.Name}");

            // Raise the DeviceConnected event
            DeviceConnected?.Invoke(this, _connectedDevice);
        }

        // Internal handler for DeviceDisconnected event
        private void OnDeviceDisconnectedInternal(object sender, DeviceEventArgs args)
        {
            _connectedDevice = null;
            Console.WriteLine("❌ Device Disconnected");

            // Raise the DeviceDisconnected event
            DeviceDisconnected?.Invoke(this, args.Device);
        }

        public void MonitorDeviceConnections()
        {
            _adapter.DeviceConnected += OnDeviceConnectedInternal;
            _adapter.DeviceDisconnected += OnDeviceDisconnectedInternal;
        }
    }
}
