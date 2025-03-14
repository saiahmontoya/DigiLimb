namespace DigiLimbDesktop;

public partial class ConnectionDashboard : ContentPage
{
    private string _deviceName;
    private string _connectionType;
    private DateTime _connectionStartTime;

    public ConnectionDashboard(string deviceName, string connectionType)
    {
        InitializeComponent();
        _deviceName = deviceName;
        _connectionType = connectionType;
        _connectionStartTime = DateTime.Now;

        lblDeviceName.Text = $"Device: {_deviceName}";
        lblConnectionType.Text = $"Connection Type: {_connectionType}";
        lblCurrentEmulator.Text = "Current Emulator: No emulator running"; // Update dynamically if needed
        UpdateConnectionDuration();
    }

    private void UpdateConnectionDuration()
    {
        TimeSpan duration = DateTime.Now - _connectionStartTime;
        lblConnectionDuration.Text = $"Duration: {duration.Minutes} min {duration.Seconds} sec";
    }

    private async void OnEndConnectionClicked(object sender, EventArgs e)
    {
        // Disconnect logic
        MainPage mainPage = (MainPage)Application.Current.MainPage;
        mainPage._isConnected = false;
        mainPage.UpdateConnectionStatus();

        await DisplayAlert("Connection Ended", "The connection has been terminated.", "OK");
        await Navigation.PopAsync();
    }
}