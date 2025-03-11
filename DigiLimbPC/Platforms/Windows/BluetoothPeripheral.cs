using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Radios;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Text;
using DigiLimbDesktop.Platforms.Windows; // Import MouseEmulator


namespace DigiLimbDesktop.Platforms.Windows
{

    public class ReceivedDeviceInfo
    {
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public string Manufacturer { get; set; }
    }
    public class BluetoothPeripheral
    {
        private GattServiceProvider _gattServiceProvider;
        private GattLocalCharacteristic _characteristicDeviceInfo;
        private GattLocalCharacteristic _characteristicMouseData;
        private GattLocalCharacteristic _characteristicKeyboardData; // ✅ New Keyboard Characteristic
        private IAdapter _adapter;
        private IDevice _connectedDevice;

        public event EventHandler<IDevice> DeviceConnected;
        public event EventHandler<IDevice> DeviceDisconnected;
        public event EventHandler<(double x, double y, bool leftClick, bool rightClick)> MouseDataReceived;
        public event EventHandler<string> KeyboardDataReceived; // ✅ New Event for Keyboard Input
        public event EventHandler<ReceivedDeviceInfo> DeviceInfoReceived;

        public BluetoothPeripheral(IAdapter adapter)
        {
            _adapter = adapter;
            MainThread.InvokeOnMainThreadAsync(async () => await InitializeBluetoothAsync());
        }

        private async Task InitializeBluetoothAsync()
        {
            var accessStatus = await Radio.RequestAccessAsync();
            if (accessStatus != RadioAccessStatus.Allowed)
            {
                Debug.WriteLine("❌ Bluetooth access denied. Cannot start GATT server.");
                return;
            }
            Debug.WriteLine("✅ Bluetooth access granted.");
            await StartGattServer();
        }

        private async Task StartGattServer()
        {
            var serviceUuid = new Guid("0000FFF0-0000-1000-8000-00805F9B34FB");
            var serviceResult = await GattServiceProvider.CreateAsync(serviceUuid);
            if (serviceResult.Error != BluetoothError.Success)
            {
                Debug.WriteLine("❌ Failed to create GATT service.");
                return;
            }

            _gattServiceProvider = serviceResult.ServiceProvider;
            Debug.WriteLine("✅ GATT Server Initialized Successfully.");

            await CreateDeviceInfoCharacteristic();
            await CreateMouseDataCharacteristic();
            await CreateKeyboardDataCharacteristic(); // ✅ Added

            _gattServiceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true,
            });

            Debug.WriteLine("✅ GATT Server Advertising Started.");
        }

        private async Task CreateDeviceInfoCharacteristic()
        {
            var characteristicParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read |
                                           GattCharacteristicProperties.Write |
                                           GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain,
                WriteProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "Connection Signal with Read, Write & Notify"
            };

            var characteristicResult = await _gattServiceProvider.Service.CreateCharacteristicAsync(
                new Guid("0000FFF2-0000-1000-8000-00805F9B34FB"), characteristicParameters);

            if (characteristicResult.Error == BluetoothError.Success)
            {
                _characteristicDeviceInfo = characteristicResult.Characteristic;
                _characteristicDeviceInfo.ReadRequested += OnReadRequested;
                _characteristicDeviceInfo.WriteRequested += OnDeviceInfoWriteRequested;
                Debug.WriteLine("✅ Device Info Characteristic Created.");
            }
            else
            {
                Debug.WriteLine("❌ Failed to create Device Info Characteristic.");
            }
        }

        private async Task CreateMouseDataCharacteristic()
        {
            var characteristicParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read |
                                           GattCharacteristicProperties.Write |
                                           GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain,
                WriteProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "Mouse Emulation Data"
            };

            var characteristicResult = await _gattServiceProvider.Service.CreateCharacteristicAsync(
                new Guid("0000FFF3-0000-1000-8000-00805F9B34FB"), characteristicParameters);

            if (characteristicResult.Error == BluetoothError.Success)
            {
                _characteristicMouseData = characteristicResult.Characteristic;
                _characteristicMouseData.WriteRequested += OnMouseDataWriteRequested;
                Debug.WriteLine("✅ Mouse Data Characteristic Created.");
            }
            else
            {
                Debug.WriteLine("❌ Failed to create Mouse Data Characteristic.");
            }
        }

        private async Task CreateKeyboardDataCharacteristic()
        {
            var characteristicParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Write |
                                           GattCharacteristicProperties.Notify,
                WriteProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "Keyboard Emulation Data"
            };

            var characteristicResult = await _gattServiceProvider.Service.CreateCharacteristicAsync(
                new Guid("0000FFF4-0000-1000-8000-00805F9B34FB"), characteristicParameters);

            if (characteristicResult.Error == BluetoothError.Success)
            {
                _characteristicKeyboardData = characteristicResult.Characteristic;
                _characteristicKeyboardData.WriteRequested += OnKeyboardDataWriteRequested; // ✅ Added Keyboard Handler
                Debug.WriteLine("✅ Keyboard Data Characteristic Created.");
            }
            else
            {
                Debug.WriteLine("❌ Failed to create Keyboard Data Characteristic.");
            }
        }

        private async void OnReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                string message = "Hello from DigiLimb!";
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                IBuffer buffer = messageBytes.AsBuffer();
                var request = await args.GetRequestAsync();
                if (request != null)
                {
                    request.RespondWithValue(buffer);
                    Debug.WriteLine("📡 Device Read Requested: Sent Data.");
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnDeviceInfoWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();
                if (request == null) return;

                var reader = DataReader.FromBuffer(request.Value);
                byte[] receivedBytes = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(receivedBytes);

                string mobileDeviceName = Encoding.UTF8.GetString(receivedBytes, 0, 20).Trim();
                string mobileDeviceId = Encoding.UTF8.GetString(receivedBytes, 20, 20).Trim();
                string manufacturerData = Encoding.UTF8.GetString(receivedBytes, 40, 20).Trim();

                Debug.WriteLine($"📡 Received Mobile Device Info: {mobileDeviceName}, {mobileDeviceId}, {manufacturerData}");

                DeviceInfoReceived?.Invoke(this, new ReceivedDeviceInfo
                {
                    DeviceName = mobileDeviceName,
                    DeviceId = mobileDeviceId,
                    Manufacturer = manufacturerData
                });
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnMouseDataWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();
                if (request == null) return;

                var reader = DataReader.FromBuffer(request.Value);
                byte[] receivedBytes = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(receivedBytes);

                // Process mouse input asynchronously to prevent blocking keyboard
                Task.Run(() =>
                {
                    ProcessMouseInput(receivedBytes);
                });
            }
            finally
            {
                deferral.Complete();
            }
        }

        // ✅ New helper function to process mouse data separately
        private void ProcessMouseInput(byte[] receivedBytes)
        {
            try
            {
                var reader = DataReader.FromBuffer(receivedBytes.AsBuffer());

                double x = 0, y = 0;
                bool leftClick = false, rightClick = false;

                while (reader.UnconsumedBufferLength > 0)
                {
                    byte header = reader.ReadByte(); // Read the header

                    switch (header)
                    {
                        case 0x01:
                            x = reader.ReadInt32();
                            break;
                        case 0x02:
                            y = reader.ReadInt32();
                            break;
                        case 0x03:
                            leftClick = reader.ReadByte() != 0;
                            break;
                        case 0x04:
                            rightClick = reader.ReadByte() != 0;
                            break;
                        default:
                            Debug.WriteLine($"⚠️ Unknown Header: {header}");
                            break;
                    }
                }

                if (x != 0 || y != 0)
                {
                    MouseEmulator.SimulateMouseMove(x / 2, y / 2);
                }

                if (leftClick)
                {
                    MouseEmulator.SimulateLeftPress();
                }
                else
                {
                    MouseEmulator.SimulateLeftRelease();
                }

                if (rightClick)
                {
                    MouseEmulator.SimulateRightPress();
                }
                else
                {
                    MouseEmulator.SimulateRightRelease();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Mouse Input Processing Error: {ex.Message}");
            }
        }

        private async void OnKeyboardDataWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();
                if (request == null) return;

                var reader = DataReader.FromBuffer(request.Value);
                byte[] receivedBytes = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(receivedBytes);

                if (receivedBytes.Length < 2)
                {
                    Debug.WriteLine("❌ Received incomplete keyboard data.");
                    return;
                }

                // ✅ Strip the first byte (header 0x05)
                string keyData = Encoding.UTF8.GetString(receivedBytes, 1, receivedBytes.Length - 1).Trim();
                Debug.WriteLine($"⌨️ [DEBUG] Extracted Keyboard Input: '{keyData}'");

                if (string.IsNullOrEmpty(keyData))
                {
                    Debug.WriteLine("❌ Received empty keyboard input.");
                    return;
                }

                // ✅ Ensure mouse input did not override keyboard input
                Debug.WriteLine($"🖥️ Keyboard Input Processed: '{keyData}'");

                // ✅ Send to Keyboard Emulator
                KeyboardEmulator.ProcessKeyPress(keyData);
            }
            finally
            {
                deferral.Complete();
            }
        }

        public void Start()
        {
            MainThread.InvokeOnMainThreadAsync(async () => await StartGattServer());
        }

        public void StopAdvertising()
        {
            _gattServiceProvider?.StopAdvertising();
            Debug.WriteLine("🔹 GATT Server Stopped Advertising.");
        }

        // ✅ Internal handler for DeviceConnected event
        private void OnDeviceConnectedInternal(object sender, DeviceEventArgs args)
        {
            _connectedDevice = args.Device;
            Debug.WriteLine($"✅ Device Connected: {_connectedDevice.Name}");

            // Debugging check to see if DeviceConnected has subscribers
            if (DeviceConnected == null)
            {
                Debug.WriteLine("❌ DeviceConnected event has NO subscribers!");
            }
            else
            {
                Debug.WriteLine("📡 Raising DeviceConnected event.");
                DeviceConnected?.Invoke(this, _connectedDevice);
            }
        }

        // ✅ Internal handler for DeviceDisconnected event
        private void OnDeviceDisconnectedInternal(object sender, DeviceEventArgs args)
        {
            _connectedDevice = null;
            Debug.WriteLine("❌ Device Disconnected");

            // Debugging check
            if (DeviceDisconnected == null)
            {
                Debug.WriteLine("❌ DeviceDisconnected event has NO subscribers!");
            }
            else
            {
                Debug.WriteLine("📡 Raising DeviceDisconnected event.");
                DeviceDisconnected?.Invoke(this, args.Device);
            }
        }

        // ✅ Ensure the Connection Events are Subscribed
        public void MonitorDeviceConnections()
        {
            _adapter.DeviceConnected -= OnDeviceConnectedInternal; // Remove previous handlers to prevent duplicates
            _adapter.DeviceConnected += OnDeviceConnectedInternal;

            _adapter.DeviceDisconnected -= OnDeviceDisconnectedInternal;
            _adapter.DeviceDisconnected += OnDeviceDisconnectedInternal;
        }
    }
}
