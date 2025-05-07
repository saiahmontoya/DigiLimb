using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Advertisement;
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
        private GattLocalCharacteristic _characteristicHeartbeat; // ✅ New characteristic for heartbeats
        private GattLocalCharacteristic _characteristicDeviceInfo;
        private GattLocalCharacteristic _characteristicRssi; // ✅ New RSSI characteristic
        private GattLocalCharacteristic _characteristicMouseData;
        private GattLocalCharacteristic _characteristicKeyboardData; // ✅ New Keyboard Characteristic
        //private IAdapter _adapter;
        //private IDevice _connectedDevice;

        public event EventHandler<IDevice> DeviceConnected;
        public event EventHandler<IDevice> DeviceDisconnected;

        private int? _lastRssi = null; // ✅ Stores last known RSSI value
        private DateTime _lastHeartbeatTime = DateTime.MinValue;
        private CancellationTokenSource? _heartbeatMonitorTokenSource; // ✅ Used to check if heartbeats stop

        
        public event EventHandler<(double x, double y, bool leftClick, bool rightClick)> MouseDataReceived;
        public event EventHandler<string> KeyboardDataReceived; // ✅ New Event for Keyboard Input
        public event EventHandler<ReceivedDeviceInfo> DeviceInfoReceived;

        public event EventHandler<int> RssiUpdated;
        public event EventHandler<bool> DeviceConnectionChanged;

        public BluetoothPeripheral()
        {
            MainThread.InvokeOnMainThreadAsync(async () => await StartGattServer());
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

            await CreateHeartbeatCharacteristic();
            await CreateDeviceInfoCharacteristic();
            await CreateRssiCharacteristic(); // ✅ Create RSSI characteristic
            await CreateMouseDataCharacteristic();
            await CreateKeyboardDataCharacteristic(); // ✅ Added

            _gattServiceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true,
            });

            Debug.WriteLine("✅ GATT Server Advertising Started.");
        }

        private async Task CreateHeartbeatCharacteristic()
        {
            var characteristicParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Write | GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "Heartbeat Signal"
            };

            var characteristicResult = await _gattServiceProvider.Service.CreateCharacteristicAsync(
                new Guid("0000FFF6-0000-1000-8000-00805F9B34FB"), characteristicParameters);

            if (characteristicResult.Error == BluetoothError.Success)
            {
                _characteristicHeartbeat = characteristicResult.Characteristic;
                _characteristicHeartbeat.WriteRequested += OnHeartbeatReceived; // ✅ Listen for heartbeats
                Debug.WriteLine("✅ Heartbeat Characteristic Created.");
            }
        }

        private async void OnHeartbeatReceived(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();
                if (request == null) return;

                var reader = DataReader.FromBuffer(request.Value);
                byte[] receivedBytes = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(receivedBytes);

                string heartbeatSignal = Encoding.UTF8.GetString(receivedBytes).Trim();

                if (heartbeatSignal == "ALIVE")
                {
                    Debug.WriteLine("📡 Received Heartbeat - Mobile App is Still Connected.");
                    _lastHeartbeatTime = DateTime.Now;

                    // ✅ If this is the first heartbeat, start monitoring for disconnection
                    if (_heartbeatMonitorTokenSource == null)
                    {
                        StartHeartbeatMonitor();
                    }

                    DeviceConnectionChanged?.Invoke(this, true);
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void StartHeartbeatMonitor()
        {
            _heartbeatMonitorTokenSource = new CancellationTokenSource();
            var token = _heartbeatMonitorTokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if ((DateTime.Now - _lastHeartbeatTime).TotalSeconds > 10)
                        {
                            Debug.WriteLine("❌ No heartbeat received for 10 seconds. Assuming device disconnected.");
                            DeviceConnectionChanged?.Invoke(this, false);

                            break; // ✅ Exit loop naturally
                        }

                        await Task.Delay(5000, token);
                    }
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("🟡 Heartbeat monitor task cancelled gracefully.");
                }
                finally
                {
                    _heartbeatMonitorTokenSource?.Dispose();
                    _heartbeatMonitorTokenSource = null;
                    Debug.WriteLine("✅ Heartbeat monitor cleaned up.");
                }
            }, token);
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
                _characteristicDeviceInfo.WriteRequested += OnDeviceInfoWriteRequested;
                Debug.WriteLine("✅ Device Info Characteristic Created.");
            }
            else
            {
                Debug.WriteLine("❌ Failed to create Device Info Characteristic.");
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

                // ✅ Extracting device details from the mobile app's data
                string mobileDeviceName = Encoding.UTF8.GetString(receivedBytes, 0, 20).Trim();
                string mobileDeviceId = Encoding.UTF8.GetString(receivedBytes, 20, 20).Trim();
                string manufacturerData = Encoding.UTF8.GetString(receivedBytes, 40, 20).Trim();

                Debug.WriteLine($"📡 Received Mobile Device Info: {mobileDeviceName}, {mobileDeviceId}, {manufacturerData}");

                // ✅ Update the PC's internal records with the mobile device’s info
                DeviceInfoReceived?.Invoke(this, new ReceivedDeviceInfo
                {
                    DeviceName = mobileDeviceName,
                    DeviceId = mobileDeviceId, // ⚠️ This ID is still a GUID, NOT the Windows-assigned `deviceId`
                    Manufacturer = manufacturerData
                });


            }
            finally
            {
                deferral.Complete();
            }
        }

        private async Task CreateRssiCharacteristic()
        {
            var characteristicParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Write | GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "Live RSSI Updates"
            };

            var characteristicResult = await _gattServiceProvider.Service.CreateCharacteristicAsync(
                new Guid("0000FFF4-0000-1000-8000-00805F9B34FB"), characteristicParameters);

            if (characteristicResult.Error == BluetoothError.Success)
            {
                _characteristicRssi = characteristicResult.Characteristic;
                _characteristicRssi.WriteRequested += OnRssiWriteRequested; // ✅ Attach the new RSSI handler
                Debug.WriteLine("✅ RSSI Characteristic Created.");
            }
            else
            {
                Debug.WriteLine("❌ Failed to create RSSI Characteristic.");
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

        private async void OnRssiWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();
                if (request == null) return;

                var reader = DataReader.FromBuffer(request.Value);
                byte[] receivedBytes = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(receivedBytes);

                if (receivedBytes.Length == 4) // ✅ Ensure correct RSSI data size
                {
                    int rssi = BitConverter.ToInt32(receivedBytes, 0);
                    _lastRssi = rssi;
                    Debug.WriteLine($"📡 Received RSSI: {rssi} dBm");

                    RssiUpdated?.Invoke(this, rssi); // ✅ Notify UI of new RSSI value
                }
            }
            finally
            {
                deferral.Complete();
            }
        }



        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        private bool isMoving = false;
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
                int scroll=0;
                bool leftClick = false, rightClick = false;

                while (reader.UnconsumedBufferLength > 0)
                {
                    byte header = reader.ReadByte(); // Read the header

                    switch (header)
                    {
                        case 0x01:
                            x = reader.ReadDouble();
                            break;
                        case 0x02:
                            y = reader.ReadDouble();
                            break;
                        case 0x03:
                            leftClick = reader.ReadByte() != 0;
                            break;
                        case 0x04:
                            rightClick = reader.ReadByte() != 0;
                            break;
                        case 0x05:
                            scroll = reader.ReadInt32();
                            break;
                        default:
                            Debug.WriteLine($"⚠️ Unknown Header: {header}");
                            break;
                    }
                }

                Debug.WriteLine($"🖱️ Mouse Data Received: X={x}, Y={y}, LeftClick={leftClick}, RightClick={rightClick}");
                if (x != 0 || y != 0)
                {
                    MouseEmulator.SimulateMouseMove(x / 2, y / 2);
                }

                if (scroll != 0)
                {
                    Debug.WriteLine("Simulating Scroll");
                    MouseEmulator.SimulateMouseScroll(scroll);
                }

                if (leftClick && !isLeftPressed) // mobile sends TRUE(pressed), and if its currently not being pressed already then perform press
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




        public async Task<int?> GetConnectionStrengthAsync()
        {
            if (_lastRssi == null)
            {
                Debug.WriteLine("❌ No active device connection.");
                return null;
            }
            return _lastRssi;
        }


        public string GetConnectionStrengthLabel(int rssi)
        {
            if (rssi >= -50) return "Strong";  // Strongest signal
            if (rssi >= -75) return "Ight";       // Ight signal
            return "Weak";                         // Poor signal
        }

        public void Dispose()
        {
            try
            {
                Debug.WriteLine("🛑 Disposing BluetoothPeripheral");

               
                // ✅ Dispose of characteristics to free memory
                _characteristicHeartbeat = null;
                _characteristicDeviceInfo = null;
                _characteristicRssi = null;
                _characteristicMouseData = null;

                // ✅ Ensure GATT Service Provider is cleaned up
                _gattServiceProvider = null;

                Debug.WriteLine("✅ BluetoothPeripheral successfully disposed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error disposing BluetoothPeripheral: {ex.Message}");
            }
        }

        public async Task SendDisconnectSignalAsync()
        {
            if (_characteristicDeviceInfo != null)
            {
                try
                {
                    byte[] disconnectMessage = Encoding.UTF8.GetBytes("DISCONNECT");
                    await _characteristicDeviceInfo.NotifyValueAsync(disconnectMessage.AsBuffer());
                    Debug.WriteLine("📡 Sent DISCONNECT signal to mobile.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Failed to send DISCONNECT signal: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("❌ DeviceInfoCharacteristic is null. Cannot send disconnect signal.");
            }
        }



    }
}
