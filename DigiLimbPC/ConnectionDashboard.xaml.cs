using DigiLimbDesktop.Platforms.Windows;
using System.Diagnostics;

namespace DigiLimbDesktop;

public partial class ConnectionDashboard : ContentPage
{
    private CancellationTokenSource _strengthTokenSource; // ✅ Separate token for RSSI tracking
    private CancellationTokenSource _durationTokenSource; // ✅ Separate token for connection duration
    private BluetoothPeripheral _bluetoothPeripheral;

    public ConnectionDashboard()
    {
        InitializeComponent();

        _bluetoothPeripheral = App.GlobalBluetoothPeripheral; // ✅ Use global instance
        _bluetoothPeripheral.RssiUpdated += OnRssiUpdated; // ✅ Subscribe to RSSI updates

        lblDeviceName.Text = App.GlobalDeviceName;
        lblConnectionType.Text = App.GlobalConnectionType;
        lblCurrentEmulator.Text = "No emulator running";

        connectionIcon.Source = App.GlobalConnectionType == "Bluetooth" ? "bluetoothicon.png" : "wifiicon.png";

        StartUpdatingConnectionDuration();
        StartUpdatingConnectionStrength(); // ✅ Now uses global BluetoothPeripheral
    }

    private void StartUpdatingConnectionStrength()
    {
        _strengthTokenSource = new CancellationTokenSource(); // ✅ Separate token for RSSI updates
        var token = _strengthTokenSource.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var rssi = await _bluetoothPeripheral?.GetConnectionStrengthAsync();
                if (rssi != null)
                {
                    Debug.WriteLine($"Obtained rssi:{rssi}");

                    string strength = _bluetoothPeripheral.GetConnectionStrengthLabel(rssi.Value);
                    Debug.WriteLine($"Resulting Connection Strength Label: {strength}" );
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        lblConnectionStrength.Text = strength;
                        connectionStrengthIcon.Source = strength switch
                        {
                            "Strong" => "highconn.png",
                            "Ight" => "mediumconn.png",
                            _ => "lowconn.png"
                        };
                        
                    });
                }

                await Task.Delay(5000, token); // ✅ Update every 5 seconds
            }
        }, token);
    }
    private void StartUpdatingConnectionDuration()
    {
        if (!App.GlobalIsConnected || App.GlobalConnectionStartTime == null)
        {
            lblConnectionDuration.Text = "0 min 0 sec";
            Debug.WriteLine($"Setting duration to 0 (Failure to start)");
            return;
        }

        _durationTokenSource = new CancellationTokenSource(); // ✅ Separate token for duration updates
        var token = _durationTokenSource.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(UpdateConnectionDuration);
                await Task.Delay(1000, token); // ✅ Update every second
            }
        }, token);
    }

    private void UpdateConnectionDuration()
    {
        if (!App.GlobalIsConnected || App.GlobalConnectionStartTime == null)
        {
            lblConnectionDuration.Text = "0 min 0 sec";
            Debug.WriteLine($"Setting duration to 0 ");
            return;
        }

        TimeSpan duration = DateTime.Now - App.GlobalConnectionStartTime.Value;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            lblConnectionDuration.Text = $"{duration.Minutes} min {duration.Seconds} sec";
        });
    }

    private void OnRssiUpdated(object sender, int rssi)
    {
        if (!App.GlobalIsConnected)
        { 
        Debug.WriteLine($"Can't update RSSI because of connection issue");

        return; // ✅ Prevents updates if disconnected
    }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            lblConnectionStrength.Text = $"{rssi} dBm";
            connectionStrengthIcon.Source = GetRssiIcon(rssi);
        });
    }


    private string GetRssiIcon(int rssi)
    {
        return rssi switch
        {
            >= -50 => "strongconn.png", // ✅ Strongest signal
            >= -75 => "mediumconn.png", // ✅ Fair signal
            _ => "lowconn.png"         // ✅ Weak signal
        };
    }


    private async void OnEndConnectionClicked(object sender, EventArgs e)
    {
        try
        {
            // ✅ Stop updating RSSI and connection duration
            _strengthTokenSource?.Cancel();
            _durationTokenSource?.Cancel();

            // ✅ Reset global connection state
            App.GlobalIsConnected = false;
            App.GlobalConnectionStartTime = null;

            // ✅ Reset UI
            lblConnectionStrength.Text = "N/A";
            lblConnectionDuration.Text = "0 min 0 sec";

            // ✅ Update MainPage (Prevent Exception)
            var mainPage = ConnectionsPage.FindMainPage(); // ✅ Calling FindMainPage() directly from ConnectionsPage

            if (mainPage != null)
            {
                mainPage.UpdateConnectionStatus();
                Debug.WriteLine("✅ Connection status updated on MainPage.");
            }
            else
            {
                Debug.WriteLine("❌ Could not find MainPage.");
            }

            Debug.WriteLine("ENDING CONNECTION TO DEVICE");
            await DisplayAlert("Connection Ended", "The connection has been terminated.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Exception in OnEndConnectionClicked: {ex.Message}");
            await DisplayAlert("Error", "An issue occurred while ending the connection.", "OK");
        }
    }



    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // ✅ Stop both timers when leaving the page
        _strengthTokenSource?.Cancel();
        _durationTokenSource?.Cancel();
    }
}
