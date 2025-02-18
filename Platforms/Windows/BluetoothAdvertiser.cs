using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Storage.Streams;

namespace DigiLimbDesktop.Platforms.Windows
{
    public class BluetoothAdvertiser
    {
        private readonly BluetoothLEAdvertisementPublisher _publisher;
        public event Action<string, string> DeviceConnected = delegate { };
        // Trigger when connected

        public BluetoothAdvertiser()
        {
            _publisher = new BluetoothLEAdvertisementPublisher();
            MainThread.InvokeOnMainThreadAsync(async () => await RequestBluetoothAccessAsync());
            // Ensure permission before advertising
        }


        // Check bluetooth permissions
        private async Task RequestBluetoothAccessAsync()
        {
            var radios = await Radio.GetRadiosAsync();
            var bluetoothRadio = radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);
            if (bluetoothRadio != null)
            {
                var accessStatus = await Radio.RequestAccessAsync();
                if (accessStatus != RadioAccessStatus.Allowed)
                {
                    Console.WriteLine("❌ Bluetooth access denied. Cannot start advertising.");
                    return;
                }
                Console.WriteLine("✅ Bluetooth access granted.");
                ConfigureAdvertisement();

            }
            else
            {
                Console.WriteLine("❌ No Bluetooth radio found.");
                return;
            }

        }

        private void ConfigureAdvertisement()
        {
            var advertisement = _publisher.Advertisement;

            // ✅ 1. Add a Custom Service UUID (16-bit for compatibility)
            var serviceUuid = new BluetoothLEAdvertisementDataSection
            {
                DataType = (byte)BluetoothLEAdvertisementDataTypes.CompleteService16BitUuids,
                Data = CreateUuidBuffer(0xFFF0) // Example 16-bit UUID
            };
            advertisement.DataSections.Add(serviceUuid);

            // ✅ 2. Add Manufacturer Data
            var manufacturerData = new BluetoothLEManufacturerData(0xFFFF, CreateManufacturerBuffer("DigiLimb"));
            advertisement.ManufacturerData.Add(manufacturerData);
        }

        public void StartAdvertising()
        {
            _publisher.Start();
            Console.WriteLine("🔹 BLE Advertising Started: DigiLimb Device");
        }

        public void StopAdvertising()
        {
            _publisher.Stop();
            Console.WriteLine("🔹 BLE Advertising Stopped.");
        }

        // Buffers to trasnmit bluetooth metadata
        private IBuffer CreateUuidBuffer(ushort uuid)
        {
            var writer = new DataWriter();
            writer.WriteUInt16(uuid);
            return writer.DetachBuffer();
        }

        private IBuffer CreateManufacturerBuffer(string data)
        {
            var writer = new DataWriter();
            writer.WriteString(data);
            return writer.DetachBuffer();
        }

        // ✅ Simulated Connection Trigger
        public void SimulateDeviceConnected()
        {
            // Ensure the event is triggered on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DeviceConnected?.Invoke("Test Device", "00:11:22:33:44:55");
            });
        }

    }
}
