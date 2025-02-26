using DigiLimbMobile.View;
namespace DigiLimbMobile;

public partial class ConnectionPage : ContentPage
{
	public ConnectionPage()
	{
		InitializeComponent();
	}

	private async void OnAddDeviceClicked(object sender, EventArgs e)
	{
        await Navigation.PushAsync(new AddNewDevicePage());
    }
}