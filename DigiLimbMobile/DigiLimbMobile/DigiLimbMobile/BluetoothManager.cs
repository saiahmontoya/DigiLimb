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
using Plugin.BLE.Abstractions.EventArgs;


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

        private IDevice _connectedDevice;
        private ICharacteristic? deviceInfoCharacteristic;
        private ICharacteristic? heartbeatCharacteristic;
        private ICharacteristic? rssiCharacteristic;
        public ICharacteristic? mouseCharacteristic { get; private set; }  // Store the characteristic
        public bool BluetoothConnectionFlag; // flag to determine whether to send via bluetooth
        private bool _stopDataSending = false;


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
                    // Console.WriteLine($"🔹 Advertisement Record Type: {record.Type}");
                    // Console.WriteLine($"🔹 Data: {BitConverter.ToString(record.Data)}");

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

                _connectedDevice = device;
                // ✅ Get actual device ID
                string mobileDeviceName = "iPhone"; // 📱 Still hardcoded (iOS 16+ requires entitlement)
                string mobileDeviceId = device.Id.ToString(); // ✅ Use actual Bluetooth device ID
                string manufacturerData = "DigiLimb";

                Console.WriteLine($"📡 Sending Device Info: {mobileDeviceName}, {mobileDeviceId}, {manufacturerData}");

                // ✅ Send the correct device ID through the characteristic
                var service = await device.GetServiceAsync(Guid.Parse("0000FFF0-0000-1000-8000-00805F9B34FB"));
                if (service != null)
                {
                    deviceInfoCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF2-0000-1000-8000-00805F9B34FB"));
                    heartbeatCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF6-0000-1000-8000-00805F9B34FB")); // ✅ Heartbeat characteristic
                    rssiCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF4-0000-1000-8000-00805F9B34FB")); // ✅ RSSI characteristic
                    mouseCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("0000FFF3-0000-1000-8000-00805F9B34FB"));

                    if (deviceInfoCharacteristic != null)
                    {
                        byte[] nameBytes = Encoding.UTF8.GetBytes(mobileDeviceName.PadRight(20));
                        byte[] idBytes = Encoding.UTF8.GetBytes(mobileDeviceId.PadRight(20)); // ✅ Now sending actual device ID
                        byte[] manufacturerBytes = Encoding.UTF8.GetBytes(manufacturerData.PadRight(20));

                        byte[] messageBytes = nameBytes.Concat(idBytes).Concat(manufacturerBytes).ToArray();
                        await deviceInfoCharacteristic.WriteAsync(messageBytes);
                        await deviceInfoCharacteristic.StartUpdatesAsync();
                        deviceInfoCharacteristic.ValueUpdated += OnDeviceInfoUpdated;

                        BluetoothConnectionFlag = true;

                        Console.WriteLine($"📡 Sent Device Info to PC: {mobileDeviceName}, {mobileDeviceId}, {manufacturerData}");
                    }

                    // Start background loops
                    _stopDataSending = false;

                    if (heartbeatCharacteristic != null)
                    {
                        Task.Run(() => StartHeartbeat()); // ✅ Runs in background
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Heartbeat characteristic not found.");
                    }
                    // ✅ Start tracking RSSI if a characteristic for it exists
                    if (rssiCharacteristic != null)
                    {
                        StartRssiTracking();
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
                BluetoothConnectionFlag = false;
                return false;
            }
        }

        private async void StartHeartbeat()
        {
            while (!_stopDataSending && _connectedDevice?.State == DeviceState.Connected)
            {
                try
                {
                    if (heartbeatCharacteristic != null)
                    {
                        byte[] signal = Encoding.UTF8.GetBytes("ALIVE");
                        await heartbeatCharacteristic.WriteAsync(signal);
                        Console.WriteLine("📡 Sent Heartbeat to PC.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Heartbeat failed: {ex.Message}");
                    break;
                }

                await Task.Delay(5000);
            }
        }


        private async void StartRssiTracking()
        {
            while (!_stopDataSending && _connectedDevice?.State == DeviceState.Connected)
            {
                try
                {
                    await _connectedDevice.UpdateRssiAsync();
                    int rssi = _connectedDevice.Rssi;

                    if (rssiCharacteristic != null)
                    {
                        byte[] rssiBytes = BitConverter.GetBytes(rssi);
                        await rssiCharacteristic.WriteAsync(rssiBytes);
                        Console.WriteLine($"📡 Sent RSSI to PC: {rssi} dBm");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ RSSI update failed: {ex.Message}");
                }

                await Task.Delay(5000);
            }
        }
        private void OnDeviceInfoUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Characteristic.Value).Trim();

            if (message == "DISCONNECT")
            {
                Console.WriteLine("📴 Received DISCONNECT from PC");
                BluetoothConnectionFlag = false;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    StopAllDataSending();
                    await App.Current.MainPage.DisplayAlert("Disconnected", "The PC ended the connection.", "OK");
                    await Shell.Current.GoToAsync("//ConnectionPage");
                });
            }
        }

        public void StopAllDataSending()
        {
            _stopDataSending = true;
            Console.WriteLine("🛑 Data sending stopped.");
              
            if (deviceInfoCharacteristic != null)
            {
                deviceInfoCharacteristic.ValueUpdated -= OnDeviceInfoUpdated;
                deviceInfoCharacteristic.StopUpdatesAsync();
            }

            _connectedDevice = null;
            mouseCharacteristic = null;
            heartbeatCharacteristic = null;
            rssiCharacteristic = null;
        }



    }
}
