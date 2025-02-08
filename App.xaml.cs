
namespace DigiLimbDesktop
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();
           
        }

        [STAThread]
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell()); // This ensures AppShell is the entry point
        }
    }
}
