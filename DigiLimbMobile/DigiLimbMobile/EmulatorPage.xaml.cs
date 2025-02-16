using System;
using Microsoft.Maui.Controls;

namespace DigiLimbMobile
{
    public partial class EmulatorPage : ContentPage
    {
        public EmulatorPage()
        {
            InitializeComponent();
        }

        private void OnKeyClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string key = button.Text; // This captures the text of the button, which corresponds to a keyboard key.
                SendKey(key); // This method would handle sending the key data.
            }
        }

        private void SendKey(string key)
        {
            // Code to send the key over Bluetooth or another connection protocol would go here.
            // For demonstration, let's just print to the console.
            Console.WriteLine($"Key {key} pressed and ready to send.");
            // Later, you would integrate with your BluetoothManager to send this data.
        }
    }
}
