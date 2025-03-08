namespace DigiLimbMobile
{
    public partial class EmulatorPage : ContentPage
    {
        public EmulatorPage()
        {
            InitializeComponent();
        }

        private async void OnMouseClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MousePage());
        }

        // New event handler for the Screen Viewer button
        private async void OnScreenViewerClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ScreenViewerPage());
        }
    }
}
