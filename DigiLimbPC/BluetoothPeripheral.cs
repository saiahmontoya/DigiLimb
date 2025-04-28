using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiLimbDesktop
{
    public class BluetoothPeripheral
    {
        public event EventHandler<bool> DeviceConnectionChanged;
        public event EventHandler<ReceivedDeviceInfo> DeviceInfoReceived;
        public event EventHandler<int> RssiUpdated; // ✅ Needed for RSSI updates

        public void RaiseDeviceConnectionChanged(bool isConnected)
        {
            DeviceConnectionChanged?.Invoke(this, isConnected);
        }

        public void RaiseDeviceInfoReceived(ReceivedDeviceInfo deviceInfo)
        {
            DeviceInfoReceived?.Invoke(this, deviceInfo);
        }

        public void RaiseRssiUpdated(int rssi)
        {
            RssiUpdated?.Invoke(this, rssi);
        }

        public void StopAdvertising()
        {
            // Optional: Stop GATT advertising here
        }

        public void Dispose()
        {
            // Optional: Clean up Bluetooth resources here
        }

        public async Task<int?> GetConnectionStrengthAsync()
        {
            // Simulate getting connection strength RSSI value
            await Task.Delay(50); // Fake async delay
            return -60; // Example RSSI value
        }

        public string GetConnectionStrengthLabel(int rssi)
        {
            return rssi switch
            {
                >= -50 => "Strong",
                >= -75 => "Ight",
                _ => "Weak"
            };
        }

        public async Task SendDisconnectSignalAsync()
        {
            // Simulate sending a disconnect signal to the device
            await Task.Delay(50); // Fake delay
            Debug.WriteLine("📡 Sent disconnect signal to mobile device.");
        }
    }
}

