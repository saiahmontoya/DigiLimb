using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DigiLimbDesktop
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

#if WINDOWS
            MainPage = new AppShell(); // Desktop entry point
#else
            MainPage = new AppShell(); // Mobile entry point
#endif
        }

#if WINDOWS
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell()); // Only required for desktop
        }
#endif
    }
}
