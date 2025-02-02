using System;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

namespace DigiLimbv1
{
    public partial class Connections : Form
    {
        private BluetoothLEAdvertisementWatcher watcher;
        private HashSet<ulong> discoveredDevices;

        public Connections()
        {
            InitializeComponent();
            discoveredDevices = new HashSet<ulong>();
        }


        private void StartBLEScan()
        {
            watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            watcher.Received += OnAdvertisementReceived;
            watcher.Stopped += OnAdvertisementStopped;

            try
            {
                watcher.Start();
                Log("Scanning for BLE devices...");
            }
            catch (Exception ex)
            {
                Log($"Error starting BLE scan: {ex.Message}");
            }
        }


        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            var ad = args.Advertisement;
            var deviceName = args.Advertisement.LocalName;
            var deviceAddress = args.BluetoothAddress;
            var signalStrength = args.RawSignalStrengthInDBm;  // Use RawSignalStrengthInDBm

            var manufacturerData = args.Advertisement.ManufacturerData;
            var manufacturerDetails = new List<string>();


            foreach (var data in manufacturerData)
            {
                manufacturerDetails.Add($"ID: {data.CompanyId:X4}, Data: {BitConverter.ToString(data.Data.ToArray())}");
            }
            var manufacturerInfo = string.Join("; ", manufacturerDetails);


            if (!discoveredDevices.Contains(deviceAddress))
            {
                discoveredDevices.Add(deviceAddress);

                if (string.IsNullOrEmpty(deviceName))
                {
                    deviceName = "Unnamed Device";
                }

                this.Invoke(new Action(() =>
                {
                    lstDevices.Items.Add($"Name: {deviceName}, Address: {deviceAddress:X}, " +
                        $"RSSI: {signalStrength} dBm, {manufacturerInfo}");
                }));

            }
        }

        private string GetAppearanceType(ushort appearanceValue)
        {
            switch (appearanceValue)
            {
                case 0x0000: return "Unknown";
                case 0x0040: return "Computer";
                case 0x0080: return "Phone";
                case 0x0100: return "Heart Rate Sensor";
                case 0x0140: return "Blood Pressure Monitor";
                case 0x025: return "Wearable Audio Device";
                // Add more cases here based on the Bluetooth SIG Appearance codes
                default: return "Other/Unknown";
            }
        }

        private void OnAdvertisementStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Log("BLE scanning stopped.");
        }

        private async Task ConnectToDevice(ulong deviceAddress)
        {
            try
            {
                var device = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress);
                if (device != null)
                {
                    Log($"Connected to device: {device.Name}");

                    var result = await device.GetGattServicesAsync();
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var service in result.Services)
                        {
                            Log($"Service: {service.Uuid}");

                            var characteristicsResult = await service.GetCharacteristicsAsync();
                            if (characteristicsResult.Status == GattCommunicationStatus.Success)
                            {
                                foreach (var characteristic in characteristicsResult.Characteristics)
                                {
                                    Log($"  Characteristic: {characteristic.Uuid}");

                                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
                                    {
                                        var valueResult = await characteristic.ReadValueAsync();
                                        if (valueResult.Status == GattCommunicationStatus.Success)
                                        {
                                            var reader = Windows.Storage.Streams.DataReader.FromBuffer(valueResult.Value);
                                            byte[] data = new byte[reader.UnconsumedBufferLength];
                                            reader.ReadBytes(data);
                                            Log($"    Value: {BitConverter.ToString(data)}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log("Failed to connect to the device.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error connecting to device: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            this.Invoke(new Action(() =>
            {
                txtLogs.AppendText($"{message}{Environment.NewLine}");
            }));
        }

        private void btnStartScan_Click_1(object sender, EventArgs e)
        {
            StartBLEScan();
        }

        private void Connections_Load(object sender, EventArgs e)
        {

        }
    }
}
