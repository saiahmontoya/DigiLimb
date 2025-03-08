using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace DigiLimbDesktop
{
    public partial class EmulationsPage : ContentPage
    {
        public EmulationsPage()
        {
            InitializeComponent();
        }

        private async void OnScreenViewerClicked(object sender, EventArgs e)
        {
            // Navigate to the ScreenViewer page.
            await Navigation.PushAsync(new ScreenViewer());
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
