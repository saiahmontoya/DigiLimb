using System.Collections.ObjectModel;

namespace DigiLimbDesktop
{
    public partial class App : Application
    {
        // Global instance of the ServerService to maintain connection state.
        public static ServerService GlobalServerService { get; private set; }

        // Global variables to check connection
        public static bool GlobalIsConnected { get; set; } = false;
        public static string GlobalDeviceName { get; set; } = "";
        public static string GlobalConnectionType { get; set; } = "";

        // Global log collection for server connection messages.
        public static ObservableCollection<string> GlobalConnectionLog { get; private set; } = new ObservableCollection<string>();

        public App()
        {
            InitializeComponent();

            // Initialize the global server service with a callback that adds messages to the global log.
            GlobalServerService = new ServerService((message, isRunning) =>
            {
                GlobalConnectionLog.Add(message);
            });
        }

        [STAThread]
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell()); // This ensures AppShell is the entry point
        }
    }
}
