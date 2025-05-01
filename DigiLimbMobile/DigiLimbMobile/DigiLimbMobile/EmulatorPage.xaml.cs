namespace DigiLimbMobile
{
    public partial class EmulatorPage : ContentPage
    {
        public EmulatorPage()
        {
            InitializeComponent();
        }

        // New event handler for the Screen Viewer button
        private async void OnScreenViewerClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ScreenViewerPage());
        }

        private async void OnTrackClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MousePage());
        }
        private async void OnMouseClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RealMousePage());
        }

        private async void OnPresentationClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PresentationMode());
        }
        private async void OnGameControllerClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GameControllerPage());
        }
    }
}
