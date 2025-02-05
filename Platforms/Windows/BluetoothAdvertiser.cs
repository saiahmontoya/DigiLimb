using System;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace DigiLimbDesktop.Platforms.Windows
{
    public class BluetoothAdvertiser
    {
        private readonly BluetoothLEAdvertisementPublisher _publisher;
        public event Action<string, string> DeviceConnected; // Trigger when connected

        public BluetoothAdvertiser()
        {
            _publisher = new BluetoothLEAdvertisementPublisher();
            ConfigureAdvertisement();
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
            DeviceConnected?.Invoke("Test Device", "00:11:22:33:44:55");
        }
    }
}
