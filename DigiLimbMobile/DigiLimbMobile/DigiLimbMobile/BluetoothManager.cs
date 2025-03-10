using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions.Extensions;
using Plugin.BLE.Abstractions;
using Microsoft.Maui.Devices; // Required for DeviceInfo
using Microsoft.Maui.ApplicationModel;
using System.Text;

namespace DigiLimbMobile
{
    public class DeviceInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

    public class BluetoothManager
    {
        private readonly IBluetoothLE bluetoothLE; // Check Bluetooth state
        private readonly IAdapter adapter; // Adapter instance to handle scanning and connections

        public ObservableCollection<DeviceInfo> Devices { get; private set; } = new(); // Now uses DeviceInfo

        public ICharacteristic? mouseCharacteristic { get; private set; }  // Store the characteristic
        public ICharacteristic? keyboardCharacteristic { get; private set; } // Keyboard Characteristic

        public BluetoothManager()
        {
            bluetoothLE = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;

            adapter.DeviceDiscovered += (s, e) =>
            {
                bool isDigiLimb = false; // Flag for identifying DigiLimb Desktop

                Console.WriteLine($"📡 Scanned Device: {e.Device.Name} ({e.Device.Id})");

                // Check advertisement records
                foreach (var record in e.Device.AdvertisementRecords)
                {
                    Console.WriteLine($"🔹 Advertisement Record Type: {record.Type}");
                    Console.WriteLine($"🔹 Data: {BitConverter.ToString(record.Data)}");

                    if (record.Type == AdvertisementRecordType.UuidsComplete16Bit && BitConverter.ToString(record.Data) == "FF-F0")
                    {
                        isDigiLimb = true;
                    }
                }

                string deviceName = isDigiLimb ? "DigiLimb Desktop" : e.Device.Name ?? "Unknown Device";
                var deviceInfo = new DeviceInfo
                {
                    Name = deviceName,
                    Id = e.Device.Id.ToString()
                };

                if (!Devices.Any(d => d.Id == deviceInfo.Id))
                {
                    if(deviceName == "DigiLimb Desktop")
                    {
                        Devices.Add(deviceInfo);
                    }
                    Console.WriteLine($"📡 Found Device: {deviceInfo.Name} ({deviceInfo.Id})");
                }
            };
        }

        public IDevice GetDeviceById(string id)
        {
            return adapter.ConnectedDevices.FirstOrDefault(d => d.Id.ToString() == id)
                ?? adapter.DiscoveredDevices.FirstOrDefault(d => d.Id.ToString() == id);
        }

        // Check if Bluetooth is enabled
        public bool IsBluetoothEnabled => bluetoothLE.State == BluetoothState.On;

        // Scan for devices advertising our service UUID (FFF0)
        public async Task ScanForDevices()
        {
            if (!IsBluetoothEnabled)
            {
                Console.WriteLine("❌ Bluetooth is off. Please enable it.");
                return;
            }

            Devices.Clear();
            await adapter.StartScanningForDevicesAsync();
        }

        // Connect to the selected device
        public async Task<bool> ConnectToDevice(IDevice device)
        {
            if (device == null)
            {
                Console.WriteLine("❌ Invalid device selection.");
                return false;
            }
            try
            {
                await adapter.ConnectToDeviceAsync(device);
                Console.WriteLine($"✅ Connected to device: {device.Name}");

                // ✅ Write a connection signal to GATT characteristic
                var service = await device.GetServiceAsync(Guid.Parse("0000FFF0-0000-1000-8000-00805F9B34FB"));
                if (service != null)
                {
                    var characteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF2-0000-1000-8000-00805F9B34FB"));
                    mouseCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF3-0000-1000-8000-00805F9B34FB"));
                    keyboardCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF4-0000-1000-8000-00805F9B34FB")); // Keyboard Characteristic

                    if (characteristic != null)
                    {
                        string mobileDeviceName = "iPhone";
                        string mobileDeviceId = "12 Pro Max";
                        string manufacturerData = "DigiLimb";
                        string deviceInfoJson = $"{{\"name\":\"{mobileDeviceName}\",\"id\":\"{mobileDeviceId}\",\"manufacturer\":\"{manufacturerData}\"}}";

                        byte[] nameBytes = Encoding.UTF8.GetBytes(mobileDeviceName.PadRight(20)); // 20-byte name
                        byte[] idBytes = Encoding.UTF8.GetBytes(mobileDeviceId.PadRight(20)); // 20-byte ID
                        byte[] manufacturerBytes = Encoding.UTF8.GetBytes(manufacturerData.PadRight(20)); // 20-byte Manufacturer

                        byte[] messageBytes = nameBytes.Concat(idBytes).Concat(manufacturerBytes).ToArray();

                        await characteristic.WriteAsync(messageBytes);
                        Console.WriteLine($"📡 Sent Mobile Device Info to Desktop: {mobileDeviceName}, {mobileDeviceId}, {manufacturerData}");
                    }
                }

                return true;
            }
            catch (DeviceConnectionException e)
            {
                Console.WriteLine($"❌ Could not connect to device: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// ✅ Send Keyboard Input to Windows via Bluetooth
        /// </summary>
        public async Task SendKeyPress(string keyData)
        {
            if (keyboardCharacteristic != null)
            {
                try
                {
                    List<byte> message = new List<byte>
                    {
                        0x05 // ✅ Header for keypress data
                    };
                    message.AddRange(Encoding.UTF8.GetBytes(keyData));

                    await keyboardCharacteristic.WriteAsync(message.ToArray());
                    Console.WriteLine($"📡 Sent Key Press: {keyData}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to send key press: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("❌ No connection to PC (keyboardCharacteristic is null).");
            }
        }
    }
}
