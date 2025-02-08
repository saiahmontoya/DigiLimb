using System;
using System.Linq;
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
        public event Action<string, string> DeviceConnected = delegate { };

        public BluetoothPeripheral()
        {
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
            var deferral = args.GetDeferral(); // Take deferral to process request
            try
            {
                using (var writer = new DataWriter())
                {
                    writer.WriteString("Hello from DigiLimb!");
                    var buffer = writer.DetachBuffer();

                    var request = await args.GetRequestAsync();
                    if (request != null)
                    {
                        request.RespondWithValue(buffer); // Send data to the connected device
                        Console.WriteLine("📡 Device Read Requested: Sent Data.");
                    }
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

        public void OnDeviceConnected(string deviceName, string deviceId)
        {
            // Fire the event
            DeviceConnected?.Invoke(deviceName, deviceId);
        }
    }
}
