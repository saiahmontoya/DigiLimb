using System.Collections.ObjectModel;
using System.Diagnostics;
using DigiLimbDesktop.Platforms.Windows;

namespace DigiLimbDesktop
{
    public partial class App : Application
    {
        // Global instance of the ServerService to maintain connection state.
        public static ServerService GlobalServerService { get; private set; }


        private static BluetoothPeripheral? _globalPeripheral;
        public static BluetoothPeripheral? GlobalBluetoothPeripheral
        {
            get => _globalPeripheral;
            set
            {
                if (_globalPeripheral is not null)
                    _globalPeripheral!.DeviceConnectionChanged -= OnGlobalDeviceConnectionChanged;

                _globalPeripheral = value;

                if (_globalPeripheral is not null)
                    _globalPeripheral!.DeviceConnectionChanged += OnGlobalDeviceConnectionChanged;
            }
        }





        // Global variables to check connection
        public static bool GlobalIsConnected { get; set; } = false;
        public static string GlobalDeviceName { get; set; } = "";
        public static string GlobalConnectionType { get; set; } = "";

        public static DateTime? GlobalConnectionStartTime { get; set; } = null;

        // Global log collection for server connection messages.
        public static ObservableCollection<string> GlobalConnectionLog { get; private set; } = new ObservableCollection<string>();

        public App()
        {
            InitializeComponent();

           

            if (GlobalBluetoothPeripheral is not null)
            {
                GlobalBluetoothPeripheral!.DeviceConnectionChanged += OnGlobalDeviceConnectionChanged;
            }

            // Initialize the global server service with a callback that adds messages to the global log.
            GlobalServerService = new ServerService((message, isRunning) =>
            {
                GlobalConnectionLog.Add(message);
            });
        }

        private static void OnGlobalDeviceConnectionChanged(object sender, bool isConnected)
        {
            if (!isConnected)
            {
                Debug.WriteLine("📡 Global disconnection detected. Navigating to connection page...");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    GlobalIsConnected = false;
                    GlobalConnectionStartTime = null;
                    GlobalDeviceName = "No Device";
                    GlobalConnectionType = "";

                    if (GlobalBluetoothPeripheral is not null)
                    {
                        Debug.WriteLine("the gatt still there, you good.");
                    }

                    var navStack = Shell.Current?.Navigation?.NavigationStack;
                    var currentPage = navStack?.LastOrDefault();

                    if (currentPage != null)
                    {
                        Debug.WriteLine($"🔍 Current visible page: {currentPage.GetType().Name}");
                    }
                    else
                    {
                        Debug.WriteLine("❌ Could not determine current visible page. Navigation stack was empty or null.");
                    }
                    // ✅ If we're on ConnectionDashboard, navigate away
                    if (currentPage is ConnectionDashboard)
                    {
                        Debug.WriteLine("📦 User is on ConnectionDashboard, navigating to ConnectionsPage.");
                        await Shell.Current.GoToAsync("//MainPage", true);
                        await Task.Delay(100);
                        await Shell.Current.GoToAsync("//ConnectionsPage", true); // true = reset nav stack
                    }
                    else
                    {
                        // If not on dashboard, still navigate to keep user flow consistent
                        await Shell.Current.GoToAsync("ConnectionsPage");
                        Debug.WriteLine("AHHHHHHHHHHHHHHHHH");
                    }

                    try
                    {
                        await Current.MainPage.DisplayAlert("Disconnected", "The mobile device has disconnected.", "OK");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ DisplayAlert failed: {ex.Message}");
                    }
                });
            }
        }

        public static void ApplyThemeToPage(Page page)
        {
            try
            {
                string theme = Preferences.Get("AppTheme", "Light");
                Debug.WriteLine($"🌗 Applying theme on app startup: {theme}");

                Application.Current.Resources["PageBackgroundColor"] = theme == "Dark" ? Color.FromArgb("#3A3A3A") : Colors.White;
                Application.Current.Resources["CardBackgroundColor"] = theme == "Dark" ? Colors.DarkGray : Colors.LightBlue;
                Application.Current.Resources["PrimaryTextColor"] = theme == "Dark" ? Colors.White : Color.FromArgb("#2D2D2D");
                Application.Current.Resources["SecondaryTextColor"] = theme == "Dark" ? Colors.LightGray : Colors.DarkGray;
                Application.Current.Resources["EntryBackgroundColor"] = theme == "Dark" ? Color.FromArgb("#787878") : Colors.White;
                Application.Current.Resources["ShellColor"] = theme == "Dark"
                ? Color.FromArgb("#2A2A2A")  // Darker shade for dark theme
                : Color.FromArgb("#C0C0C0"); // Light gray for light theme



            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error applying theme to page: {ex.Message}");
            }
        }





        [STAThread]
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();

            // ✅ Apply theme here — safe because AppShell is now constructed
            ApplyThemeToPage(shell);

           
            return new Window(shell);
        }

    }
}
