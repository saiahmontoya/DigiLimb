using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using System.Collections.ObjectModel;

namespace DigiLimbMobile.View;

public partial class AddNewDevicePage : ContentPage
{
    private BluetoothManager bluetoothManager;
    public AddNewDevicePage()
	{
		InitializeComponent();
        bluetoothManager = new BluetoothManager();
        DeviceListView.ItemsSource = bluetoothManager.Devices;
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        await bluetoothManager.ScanForDevices();
        DeviceListView.ItemsSource = bluetoothManager.Devices;// Populate list of found devices
    }


    private async void OnConnectClicked(object sender, EventArgs e)
    {
        if (DeviceListView.SelectedItem is IDevice selectedDevice) //If selected item is an IDevice, assign it to selectedDevice
        {
            bool success = await bluetoothManager.ConnectToDevice(selectedDevice);
            if (success)
            {
                await DisplayAlert("Connection", $"Connected to {selectedDevice.Name}", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to connect to device", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Please select a device first", "OK");
        }
    }
}