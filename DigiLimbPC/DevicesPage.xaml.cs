using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using DigiLimbDesktop.Models;
using System;

namespace DigiLimbDesktop
{
    using DeviceModel = DigiLimbDesktop.Models.Device;

    public partial class DevicesPage : ContentPage
    {
        private readonly IMongoCollection<DeviceModel> _devicesCollection;
        private readonly string _email;

        public DevicesPage(string email)
        {
            InitializeComponent();
            _email = email;

            // Initialize MongoDB connection
            var client = new MongoClient("mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase");
            var database = client.GetDatabase("DigilimbDatabase");
            _devicesCollection = database.GetCollection<DeviceModel>("Devices");

            // Load devices
            LoadUserDevices();
        }

        private async void LoadUserDevices()
        {
            try
            {
                var userId = await GetUserIdByEmailAsync(_email);
                if (userId == null)
                {
                    Console.WriteLine("DEBUG: User not found in database.");
                    await DisplayAlert("Error", "User not found.", "OK");
                    return;
                }

                Console.WriteLine($"DEBUG: Searching for devices with UserId = {userId}");

                // ✅ Ensure userId is compared correctly as an ObjectId
                var filter = Builders<DeviceModel>.Filter.Eq(d => d.UserId, userId.Value);
                var devices = await _devicesCollection.Find(filter).ToListAsync();

                Console.WriteLine($"DEBUG: Found {devices.Count} devices.");

                if (devices.Count == 0)
                {
                    await DisplayAlert("No Devices", "You have no registered devices.", "OK");
                    return;
                }

                // ✅ Ensure devices have all necessary fields populated
                foreach (var device in devices)
                {
                    device.DeviceModel ??= "Unknown Device";
                    device.Manufacturer ??= "Unknown Manufacturer";
                    device.Platform ??= "Unknown Platform";
                    device.OsVersion ??= "Unknown OS Version";
                    device.DeviceType ??= "Unknown DeviceType";
                    device.CreatedAt = device.CreatedAt == DateTime.MinValue ? DateTime.UtcNow : device.CreatedAt;
                    device.MacAddress ??= "Unknown MAC";
                }

                // ✅ Bind devices to UI ListView
                devicesListView.ItemsSource = devices;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load devices: {ex.Message}", "OK");
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private async Task<ObjectId?> GetUserIdByEmailAsync(string email)
        {
            var client = new MongoClient("mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase");
            var database = client.GetDatabase("DigilimbDatabase");
            var usersCollection = database.GetCollection<BsonDocument>("Users");

            var filter = Builders<BsonDocument>.Filter.Eq("email", email);
            var user = await usersCollection.Find(filter).FirstOrDefaultAsync();

            return user != null ? user["_id"].AsObjectId : (ObjectId?)null;  // ✅ Ensures ObjectId return type
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
