using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using System.Runtime.InteropServices.WindowsRuntime;

namespace DigiLimbDesktop
{
    public class BluetoothService
    {
        private GattServiceProvider serviceProvider;

        public async void InitializeGattService()
        {
            // Assuming the service UUID has already been defined
            var customServiceUuid = new Guid("0000FFF0-0000-1000-8000-00805F9B34FB");
            var serviceResult = await GattServiceProvider.CreateAsync(customServiceUuid);

            if (serviceResult.Error == BluetoothError.Success)
            {
                serviceProvider = serviceResult.ServiceProvider;
                var customCharacteristicUuid = new Guid("0000FFF1-0000-1000-8000-00805F9B34FB");
                var parameters = new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write | GattCharacteristicProperties.Read,
                    WriteProtectionLevel = GattProtectionLevel.Plain,
                    UserDescription = "Key Input"
                };

                var characteristicResult = await serviceProvider.Service.CreateCharacteristicAsync(customCharacteristicUuid, parameters);
                if (characteristicResult.Error == BluetoothError.Success)
                {
                    var characteristic = characteristicResult.Characteristic;
                    characteristic.WriteRequested += Characteristic_WriteRequested;
                }
            }
        }

        private async void Characteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var request = await args.GetRequestAsync();
            var data = request.Value.ToArray(); // Assuming data is ASCII values of keys pressed

            // Process each key press
            foreach (var byteValue in data)
            {
                char keyChar = Convert.ToChar(byteValue);
                InputSimulator.SimulateKeyPress(keyChar); // Simulate key press
            }

            request.Respond();
        }
    }
}
