//using DigiLimbDesktop.Platforms.Windows;
using System.Diagnostics;
#if WINDOWS
using DigiLimbDesktop.Platforms.Windows;
#endif
namespace DigiLimbDesktop;

public partial class ConnectionDashboard : ContentPage
{
    private CancellationTokenSource? _strengthTokenSource; // ✅ Separate token for RSSI tracking
    private CancellationTokenSource? _durationTokenSource; // ✅ Separate token for connection duration
#if WINDOWS
    private BluetoothPeripheral _bluetoothPeripheral;
#endif

    public ConnectionDashboard()
    {
        InitializeComponent();
#if WINDOWS
        _bluetoothPeripheral = App.GlobalBluetoothPeripheral; // ✅ Use global instance
        _bluetoothPeripheral.RssiUpdated += OnRssiUpdated; // ✅ Subscribe to RSSI updates
#endif

  

        lblDeviceName.Text = App.GlobalDeviceName;
        lblConnectionType.Text = App.GlobalConnectionType;
        lblCurrentEmulator.Text = "No emulator running";

        connectionIcon.Source = App.GlobalConnectionType == "Bluetooth" ? "bluetoothicon.png" : "wifiicon.png";

        StartUpdatingConnectionDuration();
        StartUpdatingConnectionStrength(); // ✅ Now uses global BluetoothPeripheral
    }

    private void StartUpdatingConnectionStrength()
    {
        _strengthTokenSource = new CancellationTokenSource();
        var token = _strengthTokenSource.Token;

        Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
#if WINDOWS
                    var rssi = await _bluetoothPeripheral?.GetConnectionStrengthAsync();

                    if (rssi != null)
                    {
                        Debug.WriteLine($"Obtained rssi: {rssi}");

                        string strength = _bluetoothPeripheral.GetConnectionStrengthLabel(rssi.Value);
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

                    await Task.Delay(5000, token); // ✅ Throws if cancelled
#endif
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("🟡 Connection strength task cancelled gracefully.");
            }
            finally
            {
                _strengthTokenSource?.Dispose();
                _strengthTokenSource = null;
                Debug.WriteLine("✅ Connection strength tracking cleaned up.");
            }
        }, token);
    }

    private void StartUpdatingConnectionDuration()
    {
        if (!App.GlobalIsConnected || App.GlobalConnectionStartTime == null)
        {
            lblConnectionDuration.Text = "0 min 0 sec";
            return;
        }

        _durationTokenSource = new CancellationTokenSource();
        var token = _durationTokenSource.Token;

        Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    MainThread.BeginInvokeOnMainThread(UpdateConnectionDuration);
                    await Task.Delay(1000, token);
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("🟡 Connection duration tracking cancelled.");
            }
            finally
            {
                _durationTokenSource?.Dispose();
                _durationTokenSource = null;
                Debug.WriteLine("✅ Duration tracking cleaned up.");
            }
        }, token);
    }


    private void UpdateConnectionDuration()
    {
        if (!App.GlobalIsConnected || App.GlobalConnectionStartTime == null)
        {
            lblConnectionDuration.Text = "0 min 0 sec";
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


            Debug.WriteLine("ENDING CONNECTION TO DEVICE");

            // ✅ Send disconnect signal to mobile
#if WINDOWS
            await _bluetoothPeripheral.SendDisconnectSignalAsync();
#endif
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
        _strengthTokenSource?.Cancel();
        _durationTokenSource?.Cancel();
#if WINDOWS
        _bluetoothPeripheral.RssiUpdated -= OnRssiUpdated;
#endif
    }

}
