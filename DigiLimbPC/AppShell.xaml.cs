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
        }
    }
}
