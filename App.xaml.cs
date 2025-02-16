namespace DigiLimbDesktop
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            InitializeBluetoothService();
        }

        private void InitializeBluetoothService()
        {
            BluetoothService bluetoothService = new BluetoothService();
            bluetoothService.InitializeGattService();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
