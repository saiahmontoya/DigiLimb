using DigiLimbMobile.View;
using System.Net.WebSockets;

namespace DigiLimbMobile
{
    public partial class App : Application
    {
        public static BluetoothManager BluetoothManager { get; private set; }
        public static ClientWebSocket GlobalWebSocket { get; set; } = null; // Global lifetime connection

        public App()
        {
            InitializeComponent();
            BluetoothManager = new BluetoothManager();
            MainPage = new AppShell();
        }
    }
}
