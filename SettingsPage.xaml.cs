using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Xml.Linq;

namespace DigiLimbDesktop
{
    public partial class SettingsPage : ContentPage
    {
        private readonly IMongoCollection<BsonDocument> _usersCollection;
        private string _email;
        private ObjectId? _userID;

        public event Action<string> DeviceNameUpdated;
        public SettingsPage(string email)
        {
            InitializeComponent();
            _email = email;

            // Initialize MongoDB connection
            var client = new MongoClient("mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase");
            var database = client.GetDatabase("DigilimbDatabase");
            _usersCollection = database.GetCollection<BsonDocument>("Users");

            // Load user data by email
            LoadUserAsync(email);
        }

        private async void LoadUserAsync(string email)
        {
            try
            {
                await InitializeUser(email);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error initializing user: {ex.Message}", "OK");
            }
        }

        private async Task InitializeUser(string email)
        {
            _userID = await GetUserIdByEmailAsync(email);
            if (_userID == null)
            {
                await DisplayAlert("Error", $"User with email {email} not found.", "OK");
                return;
            }

            // Load existing data
            await LoadExistingData(_userID.Value);
        }

        private async Task<ObjectId?> GetUserIdByEmailAsync(string email)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("email", email);
                var user = await _usersCollection.Find(filter).FirstOrDefaultAsync();

                if (user != null && user.Contains("_id"))
                {
                    return user["_id"].AsObjectId;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user ID: {ex.Message}");
                return null;
            }
        }

        private async Task LoadExistingData(ObjectId userID)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("userId", userID);
                var settings = await _usersCollection.Find(filter).FirstOrDefaultAsync();

                if (settings != null)
                {
                    txtName.Text = settings["mouseModifications"]["deviceName"].AsString;
                    txtDPI.Text = settings["mouseModifications"]["dpiLevel"].AsInt32.ToString();
                }
                else
                {
                    // Default values
                    txtName.Text = "Default Device";
                    txtDPI.Text = "800";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error loading data: {ex.Message}", "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (_userID == null)
                {
                    await DisplayAlert("Error", "User ID is not initialized.", "OK");
                    return;
                }

                string deviceName = txtName.Text;
                if (!int.TryParse(txtDPI.Text, out int dpiLevel) || dpiLevel < 100 || dpiLevel > 3200)
                {
                    await DisplayAlert("Validation Error", "DPI level must be a number between 100 and 3200.", "OK");
                    return;
                }

                // Check if a record exists
                var filter = Builders<BsonDocument>.Filter.Eq("userId", _userID.Value);
                var existingModification = await _usersCollection.Find(filter).FirstOrDefaultAsync();

                if (existingModification == null)
                {
                    // Create a new document if none exists
                    var newModification = new BsonDocument
            {
                { "userId", _userID.Value },
                { "mouseModifications", new BsonDocument
                    {
                        { "deviceName", deviceName },
                        { "dpiLevel", dpiLevel }
                    }
                },
                { "createdAt", DateTime.UtcNow },
                { "updatedAt", DateTime.UtcNow }
            };

                    await _usersCollection.InsertOneAsync(newModification);
                    await DisplayAlert("Success", "New modifications saved successfully.", "OK");
                }
                else
                {
                    // Update the existing document
                    var update = Builders<BsonDocument>.Update
                        .Set("mouseModifications.deviceName", deviceName)
                        .Set("mouseModifications.dpiLevel", dpiLevel)
                        .Set("updatedAt", DateTime.UtcNow);

                    var options = new UpdateOptions { IsUpsert = false };
                    await _usersCollection.UpdateOneAsync(filter, update, options);
                    await DisplayAlert("Success", "Modifications updated successfully.", "OK");
                }

                // Trigger the event to notify MainPage about the update
                DeviceNameUpdated?.Invoke(deviceName);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error saving data: {ex.Message}", "OK");
            }
        }
    }
}
