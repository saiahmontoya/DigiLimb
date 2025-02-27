using DigiLimbMobile.View;

namespace DigiLimbMobile
{
    public partial class App : Application
    {
        public static BluetoothManager BluetoothManager { get; private set; }
        public App()
        {
            InitializeComponent();
            BluetoothManager = new BluetoothManager();
            MainPage = new AppShell();
        }
        /*
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new Login()));
        }*/
    }
}