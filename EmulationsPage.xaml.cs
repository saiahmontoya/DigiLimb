using System;
using Microsoft.Maui.Controls;

namespace DigiLimbDesktop
{
    public partial class EmulationPage : ContentPage
    {
        public EmulationPage()
        {
            InitializeComponent();
        }

        // Handles keyboard input simulation
        private void OnSimulateKeyboardInputClicked(object sender, EventArgs e)
        {
            try
            {
                // Simulate pressing the 'A' key
                InputSimulator.SimulateKeyPress('A');
                DisplayResult("Simulated key press: 'A'");
            }
            catch (Exception ex)
            {
                DisplayResult($"Error simulating key press: {ex.Message}");
            }
        }

        // Displays the results of the simulations
        private void DisplayResult(string message)
        {
            lblStatus.Text = message;
        }
    }
}
