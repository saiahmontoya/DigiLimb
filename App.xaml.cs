using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DigiLimbDesktop
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Startup Error: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
#if WINDOWS
                return new Window(new AppShell()); // Windows Entry Point
#else
                return new Window(new AppShell()); // Mobile Entry Point
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Window Creation Error: {ex.Message}");
                return base.CreateWindow(activationState); // Fallback to default window if error occurs
            }
        }
    }
}
