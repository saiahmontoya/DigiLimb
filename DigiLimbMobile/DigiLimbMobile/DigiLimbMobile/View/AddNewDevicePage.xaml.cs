using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace DigiLimbMobile.View
{
    public partial class AddNewDevicePage : ContentPage
    {
        private readonly BluetoothManager bluetoothManager;

        public AddNewDevicePage()
        {
            InitializeComponent();
            bluetoothManager = new BluetoothManager();
            BindingContext = bluetoothManager;

            DeviceListView.ItemTapped += OnDeviceTapped; // Handle tapping on a device
        }

        private async void OnScanClicked(object sender, EventArgs e)
        {
            await bluetoothManager.ScanForDevices();
        }

        private async void OnDeviceTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is DeviceInfo selectedDevice)
            {
                bool userConfirmed = await DisplayAlert(
                    "Connect to Device",
                    $"Connect to {selectedDevice.Name}?",
                    "Yes",
                    "No"
                );

                if (!userConfirmed)
                    return; // Exit if the user cancels

                var deviceToConnect = bluetoothManager.GetDeviceById(selectedDevice.Id);
                if (deviceToConnect == null)
                {
                    await DisplayAlert("Error", "Device not found in scan results.", "OK");
                    return;
                }

                bool isConnected = await bluetoothManager.ConnectToDevice(deviceToConnect);
                if (isConnected)
                {
                    await DisplayAlert("Success", $"Paired to {selectedDevice.Name}.", "OK");
                    await Shell.Current.GoToAsync("//ConnectionPage"); // Navigate back to ConnectionPage
                }
                else
                {
                    await DisplayAlert("Error", $"Failed to connect to {selectedDevice.Name}.", "OK");
                }
            }
        }
    }
}
