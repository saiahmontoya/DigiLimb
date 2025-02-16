using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Microsoft.Maui.Controls;
#if WINDOWS
using DigiLimbDesktop.Platforms.Windows;
#endif
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DigiLimbDesktop
{
    public class BluetoothManager
    {
        private IAdapter _adapter;

        public BluetoothManager(IAdapter adapter)
        {
            _adapter = adapter;
        }

        public async Task<bool> StartScanning(Action<IDevice> deviceDiscoveredCallback)
        {
            try
            {
                _adapter.DeviceDiscovered += (s, e) => deviceDiscoveredCallback(e.Device);
                await _adapter.StartScanningForDevicesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting Bluetooth scan: {ex.Message}");
                return false;
            }
        }

        public async Task StopScanning()
        {
            if (_adapter.IsScanning)
            {
                await _adapter.StopScanningForDevicesAsync();
                _adapter.DeviceDiscovered -= (s, e) => { };
            }
        }

        public async Task ConnectToDevice(IDevice device)
        {
            if (device != null)
            {
                await _adapter.ConnectToDeviceAsync(device);
            }
        }
    }
}
