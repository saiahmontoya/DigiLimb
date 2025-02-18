using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace DigiLimbDesktop
{
    public partial class MainPage : ContentPage
    {
        private string _email; // Store user's email
        private readonly IMongoCollection<BsonDocument> _usersCollection;
        private ObjectId? _userID;

        public MainPage(string email) // Constructor receives email
        {
            InitializeComponent();
            _email = email; // Store it for later use

            // Initialize MongoDB connection
            var client = new MongoClient("mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase");
            var database = client.GetDatabase("DigilimbDatabase");
            _usersCollection = database.GetCollection<BsonDocument>("Users");

            // Load user data by email
            LoadUserDeviceNameAsync(email);
        }

        
        // Navigate to Connections Page
        private async void OnConnectionsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConnectionsPage());
        }

        // Navigate to Modifications (Settings) Page
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            var settingsPage = new SettingsPage(_email);

            // Subscribe to the DeviceNameUpdated event to update the label
            settingsPage.DeviceNameUpdated += OnDeviceNameUpdated;

            await Navigation.PushAsync(settingsPage);
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

        private async void LoadUserDeviceNameAsync(string email)
        {
            try
            {
                var userID = await GetUserIdByEmailAsync(email);
                if (userID == null)
                {
                    lblDeviceName.Text = "DigiLimb Dashboard";  // Default if no user found
                    return;
                }

                var deviceName = await GetUserDeviceNameAsync(userID.Value);
                lblDeviceName.Text = deviceName + "'s Dashboard" ?? "DigiLimb Dashboard";  // Display the device name or default
            }
            catch (Exception ex)
            {
                lblDeviceName.Text = "DigiLimb Dashboard";  // Fallback in case of error
                Console.WriteLine($"Error loading user device name: {ex.Message}");
            }
        }

        private async Task<ObjectId?> GetUserIdByEmailAsync(string email)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("email", email);
            var user = await _usersCollection.Find(filter).FirstOrDefaultAsync();
            return user?["_id"].AsObjectId;
        }

        private async Task<string> GetUserDeviceNameAsync(ObjectId userID)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("userId", userID);
            var userSettings = await _usersCollection.Find(filter).FirstOrDefaultAsync();
            return userSettings?["mouseModifications"]?["deviceName"].AsString;
        }

        private void OnDeviceNameUpdated(string updatedDeviceName)
        {
            // Update the label text when the device name is updated in the settings page
            lblDeviceName.Text = updatedDeviceName + "'s Dashboard" ?? "DigiLimb Dashboard";
        }
    }
}
