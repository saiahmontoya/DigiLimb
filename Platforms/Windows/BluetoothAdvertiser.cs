using System;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace DigiLimbDesktop.Platforms.Windows
{
    public class BluetoothAdvertiser
    {
        private readonly BluetoothLEAdvertisementPublisher _publisher;

        // Using the full 128-bit UUID
        private readonly Guid serviceUuid = new Guid("0000FFF0-0000-1000-8000-00805F9B34FB");

        public event Action<string, string> DeviceConnected; // Real implementation needed

        public BluetoothAdvertiser()
        {
            _publisher = new BluetoothLEAdvertisementPublisher();
            ConfigureAdvertisement();
        }

        private void ConfigureAdvertisement()
        {
            var advertisement = _publisher.Advertisement;

            // Using the full UUID for advertisement
            var serviceUuidDataSection = new BluetoothLEAdvertisementDataSection
            {
                DataType = (byte)BluetoothLEAdvertisementDataTypes.CompleteService128BitUuids,
                Data = CreateUuidBuffer(serviceUuid)
            };
            advertisement.DataSections.Add(serviceUuidDataSection);

            // Add Manufacturer Data
            var manufacturerData = new BluetoothLEManufacturerData(0xFFFF, CreateManufacturerBuffer("DigiLimb"));
            advertisement.ManufacturerData.Add(manufacturerData);
        }

        public void StartAdvertising()
        {
            _publisher.Start();
            Console.WriteLine("BLE Advertising Started: DigiLimb Device");
        }

        public void StopAdvertising()
        {
            _publisher.Stop();
            Console.WriteLine("BLE Advertising Stopped.");
        }

        private IBuffer CreateUuidBuffer(Guid uuid)
        {
            var writer = new DataWriter();
            writer.WriteGuid(uuid);
            return writer.DetachBuffer();
        }

        private IBuffer CreateManufacturerBuffer(string data)
        {
            var writer = new DataWriter();
            writer.WriteString(data);
            return writer.DetachBuffer();
        }

        // Real connection handling logic to be implemented
        public void SimulateDeviceConnected()
        {
            // This should eventually be replaced with actual event handling for device connections
            DeviceConnected?.Invoke("Test Device", "00:11:22:33:44:55");
        }
    }
}
