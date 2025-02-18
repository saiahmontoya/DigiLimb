using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Exceptions;

namespace DigiLimbMobile
{
    public class BluetoothManager
    {
        private readonly IBluetoothLE bluetoothLE; // Check bluetooth state
        private readonly IAdapter adapter; // Adapter instance to handle scanning and connections
        public ObservableCollection<IDevice> Devices { get; private set; } = new(); // Store devices

        public BluetoothManager()
        {
            bluetoothLE = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;

            // When device found, add to list if not already in it
            adapter.DeviceDiscovered += (s, e) =>
            {
                if (!Devices.Contains(e.Device))
                    Devices.Add(e.Device);
            };
        }

        // Check if bluetooth enabled
        public bool IsBluetoothEnabled => bluetoothLE.State == BluetoothState.On;

        // Scan
        public async Task ScanForDevices()
        {
            if (!IsBluetoothEnabled)
            {
                Console.WriteLine("Bluetooth is off. Please enable it.");
                return;
            }

            Devices.Clear();
            await adapter.StartScanningForDevicesAsync();
        }

        public async Task<bool> ConnectToDevice(IDevice device)
        {
            if (device == null)
            {
                Console.WriteLine("Invalid device selection.");
                return false;
            }
            try
            {
                await adapter.ConnectToDeviceAsync(device);
                Console.WriteLine($"Connected to device: {device.Name}");
                return true;
            }
            catch(DeviceConnectionException e)
            {
                Console.WriteLine($"Could not connect to device: {e.Message}");
                return false;
            }
        }
    }

}
