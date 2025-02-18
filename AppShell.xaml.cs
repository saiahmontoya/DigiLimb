
namespace DigiLimbDesktop
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            ConfigurePlatformPages();
        }

        private void ConfigurePlatformPages()
        {
#if WINDOWS
            LoginShellContent.ContentTemplate = new DataTemplate(typeof(LoginPage));
            ConnectionShellContent.ContentTemplate = new DataTemplate(typeof(ConnectionPage));
            EmulationShellContent.ContentTemplate = new DataTemplate(typeof(EmulationPage));

            SettingsShellContent.IsVisible = true;
            SupportShellContent.IsVisible = true;
#else
            LoginShellContent.ContentTemplate = new DataTemplate(typeof(LoginPage));
            ConnectionShellContent.ContentTemplate = new DataTemplate(typeof(ConnectionPage));
            EmulationShellContent.ContentTemplate = new DataTemplate(typeof(EmulationPage));

            SettingsShellContent.IsVisible = false;
            SupportShellContent.IsVisible = false;
#endif
        }
    }
}
