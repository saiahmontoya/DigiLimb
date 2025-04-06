using System.Collections.ObjectModel;
using System.Diagnostics;
namespace DigiLimbDesktop
{
    public partial class AppShell : Shell
    {
        // Store the logged-in user's email globally
        public static string UserEmail { get; set; } = "default@example.com";

        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("ConnectionsPage", typeof(ConnectionsPage));
            Routing.RegisterRoute("ConnectionDashboard", typeof(ConnectionDashboard));

            // Re-apply shell appearance whenever navigating to a new page
            this.Navigated += (s, e) =>
            {
                try
                {
                    var titleColor = (Color)Application.Current.Resources["PrimaryTextColor"];
                    var shellColor = (Color)Application.Current.Resources["ShellColor"];
                    // var accent = (Color)Application.Current.Resources["AccentColor"];

                    if (Shell.Current?.CurrentPage is Page currentPage)
                    {
                        Shell.SetTitleColor(currentPage, titleColor);
                        Shell.SetBackgroundColor(currentPage, shellColor);
                        // Shell.SetForegroundColor(currentPage, accent);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error updating Shell appearance on navigation: {ex.Message}");
                }
            };
        }

    }
}
