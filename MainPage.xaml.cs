using System;
using Microsoft.Maui.Controls;

namespace DigiLimbDesktop
{
    public partial class MainPage : ContentPage
    {
        private string _email; // Store user's email

        public MainPage(string email) // Constructor receives email
        {
            InitializeComponent();
            _email = email; // Store it for later use
        }

        // Click Counter Logic
        private int _clickCount = 0;
        private void OnCounterClicked(object sender, EventArgs e)
        {
            _clickCount++;
            CounterBtn.Text = $"Clicked {_clickCount} times!";
        }

        // Navigate to Connections Page
        private async void OnConnectionsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConnectionsPage());
        }

        // Navigate to Modifications (Settings) Page
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage()); // Pass email
        }

        
        // Navigate to Support Page
        private async void OnSupportClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SupportPage());
        }
        

        // Navigate to Emulation Page
        private async void OnEmulationClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EmulationPage());
        }

        // Quit the Application
        private async void OnQuitClicked(object sender, EventArgs e)
        {
            bool confirmExit = await DisplayAlert("Confirm Quit", "Are you sure you want to quit?", "Yes", "No");
            if (confirmExit)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill(); // Force quit
            }
        }
    }
}
