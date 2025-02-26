using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MongoDB.Bson.Serialization.Attributes;

namespace DigiLimbDesktop
{
    public partial class DevicesPage : ContentPage
    {
        private readonly IMongoCollection<Device> _devicesCollection;
        private string _email;

        public DevicesPage(string email)
        {
            InitializeComponent();
            _email = email;

            // Initialize MongoDB connection
            var client = new MongoClient("mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase");
            var database = client.GetDatabase("DigilimbDatabase");
            _devicesCollection = database.GetCollection<Device>("Devices");

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

                var filter = Builders<Device>.Filter.Eq(d => d.UserId, userId.Value);
                var devices = await _devicesCollection.Find(filter).ToListAsync();

                Console.WriteLine($"DEBUG: Found {devices.Count} devices.");

                if (devices.Count == 0)
                {
                    await DisplayAlert("No Devices", "You have no registered devices.", "OK");
                    return;
                }

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
            return user?["_id"].AsObjectId;
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var deviceId = (ObjectId)button.CommandParameter;

            await DisplayAlert("Connect", $"Attempting to connect to device: {deviceId}", "OK");
            Console.WriteLine($"DEBUG: Connect button clicked for device {deviceId}");
        }

        public class Device
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public ObjectId Id { get; set; }

            [BsonElement("userId")]
            [BsonRepresentation(BsonType.ObjectId)]
            public ObjectId UserId { get; set; }

            [BsonElement("deviceModel")]
            public string DeviceModel { get; set; }

            [BsonElement("manufacturer")]
            public string Manufacturer { get; set; }

            [BsonElement("platform")]
            public string Platform { get; set; }

            [BsonElement("osVersion")]
            public string OsVersion { get; set; }

            [BsonElement("idiom")]
            public string Idiom { get; set; }

            [BsonElement("deviceType")]
            public string DeviceType { get; set; }

            [BsonElement("createdAt")]
            public DateTime CreatedAt { get; set; }
        }

    }
}
