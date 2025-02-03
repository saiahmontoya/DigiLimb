namespace DigiLimbDesktop
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private async void OnConnectionsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConnectionsPage());
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

        private async void OnEmulationClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EmulationPage());
        }
    }

}
