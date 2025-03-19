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

                    // Check if this record contains FFF0 (shown as FF-F0 in logs)
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

                // ✅ Get actual device ID
                string mobileDeviceName = "iPhone"; // 📱 Still hardcoded (iOS 16+ requires entitlement)
                string mobileDeviceId = device.Id.ToString(); // ✅ Use actual Bluetooth device ID
                string manufacturerData = "DigiLimb";

                Console.WriteLine($"📡 Sending Device Info: {mobileDeviceName}, {mobileDeviceId}, {manufacturerData}");

                // ✅ Send the correct device ID through the characteristic
                var service = await device.GetServiceAsync(Guid.Parse("0000FFF0-0000-1000-8000-00805F9B34FB"));
                if (service != null)
                {
                    var characteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF2-0000-1000-8000-00805F9B34FB"));
                    var heartbeatCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF6-0000-1000-8000-00805F9B34FB")); // ✅ Heartbeat characteristic
                    var rssiCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF4-0000-1000-8000-00805F9B34FB")); // ✅ RSSI characteristic
                    mouseCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF3-0000-1000-8000-00805F9B34FB"));

                    if (characteristic != null)
                    {
                        byte[] nameBytes = Encoding.UTF8.GetBytes(mobileDeviceName.PadRight(20));
                        byte[] idBytes = Encoding.UTF8.GetBytes(mobileDeviceId.PadRight(20)); // ✅ Now sending actual device ID
                        byte[] manufacturerBytes = Encoding.UTF8.GetBytes(manufacturerData.PadRight(20));

                        byte[] messageBytes = nameBytes.Concat(idBytes).Concat(manufacturerBytes).ToArray();
                        await characteristic.WriteAsync(messageBytes);

                        Console.WriteLine($"📡 Sent Device Info to PC: {mobileDeviceName}, {mobileDeviceId}, {manufacturerData}");
                    }

                    if (heartbeatCharacteristic != null)
                    {
                        Task.Run(() => StartHeartbeat(device, heartbeatCharacteristic)); // ✅ Runs in background
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Heartbeat characteristic not found.");
                    }
                    // ✅ Start tracking RSSI if a characteristic for it exists
                    if (rssiCharacteristic != null)
                    {
                        StartRssiTracking(device, rssiCharacteristic);
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No RSSI characteristic found on PC GATT Server.");
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

        private async void StartHeartbeat(IDevice device, ICharacteristic heartbeatCharacteristic)
        {
            while (device.State == DeviceState.Connected)
            {
                try
                {
                    byte[] heartbeatSignal = Encoding.UTF8.GetBytes("ALIVE");
                    await heartbeatCharacteristic.WriteAsync(heartbeatSignal);
                    Console.WriteLine("📡 Sent Heartbeat to PC.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Heartbeat Failed: {ex.Message}");
                    break; // ✅ Stop sending if the connection is lost
                }

                await Task.Delay(5000);
            }
        }


        private async void StartRssiTracking(IDevice device, ICharacteristic rssiCharacteristic)
        {
            while (device.State == DeviceState.Connected)
            {
                try
                {
                    await device.UpdateRssiAsync();
                    int rssi = device.Rssi;
                    Console.WriteLine($"📡 RSSI Updated: {rssi} dBm");

                    if (rssiCharacteristic != null) // ✅ Prevents null reference exception
                    {
                        byte[] rssiBytes = BitConverter.GetBytes(rssi);
                        await rssiCharacteristic.WriteAsync(rssiBytes);
                        Console.WriteLine($"📡 Sent RSSI to PC: {rssi} dBm");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error reading RSSI: {ex.Message}");
                }

                await Task.Delay(5000);
            }

        }



        }
}
