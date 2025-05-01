using DigiLimbMobile.View;
using System.Net.WebSockets;

namespace DigiLimbMobile
{
    public partial class App : Application
    {
        public static BluetoothManager BluetoothManager { get; private set; }

        public static ClientWebSocket GlobalWebSocket { get; set; } // Global lifetime connection

        public static string GlobalPasskey { get; set; }

        public App()
        {
            InitializeComponent();
            BluetoothManager = new BluetoothManager();
            MainPage = new AppShell();
        }
    }
}
