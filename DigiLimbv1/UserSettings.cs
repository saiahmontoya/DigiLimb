using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DigiLimbv1
{
    public partial class UserSettings : Form
    {
        private readonly IMongoCollection<BsonDocument> _usersCollection;
        private string _email;
        private ObjectId? _userID;

        public UserSettings(string email)
        {
            InitializeComponent();

            // Initialize MongoDB connection and collection
            var client = new MongoClient("mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase");
            var database = client.GetDatabase("DigilimbDatabase");
            _usersCollection = database.GetCollection<BsonDocument>("Users");

            // Get user ID by email and load data

            LoadUserAsync(email);
        }

        private async void LoadUserAsync(string email)
        {
            try
            {
                await InitializeUser(email); // Use await instead of .Wait()
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing user: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task InitializeUser(string email)
        {
            _userID = await GetUserIdByEmailAsync(email);
            if (_userID == null)
            {
                MessageBox.Show($"User with email {email} not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadExistingData(_userID.Value); // Load existing data with ObjectId
        }

        public async Task<ObjectId?> GetUserIdByEmailAsync(string email)
        {
            try
            {
                // Create a filter to match the email
                var filter = Builders<BsonDocument>.Filter.Eq("email", email);

                // Query the database for the user document
                var user = await _usersCollection.Find(filter).FirstOrDefaultAsync();

                // Check if the user exists and return the ObjectId
                if (user != null && user.Contains("_id"))
                {
                    return user["_id"].AsObjectId;
                }

                return null; // User not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user ID: {ex.Message}");
                return null;
            }
        }
        private async void LoadExistingData(ObjectId _userID)
        {
            try
            {
                // Find the user's modification record
                var filter = Builders<BsonDocument>.Filter.Eq("userId", _userID);
                var settings = await _usersCollection.Find(filter).FirstOrDefaultAsync();

                if (settings != null)
                {
                    // Pre-fill textboxes with existing data
                    txtName.Text = settings["mouseModifications"]["deviceName"].AsString;
                    txtDPI.Text = settings["mouseModifications"]["dpiLevel"].AsInt32.ToString();
                }
                else
                {
                    // Default values if no record exists
                    txtName.Text = "Default Device";
                    txtDPI.Text = "800";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_userID == null)
                {
                    MessageBox.Show("User ID is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Read input values
                string deviceName = txtName.Text;
                if (!int.TryParse(txtDPI.Text, out int dpiLevel) || dpiLevel < 100 || dpiLevel > 3200)
                {
                    MessageBox.Show("DPI level must be a number between 100 and 3200.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if a record exists for the user
                var filter = Builders<BsonDocument>.Filter.Eq("userId", _userID.Value);
                var existingModification = await _usersCollection.Find(filter).FirstOrDefaultAsync();

                if (existingModification == null)
                {
                    // If no record exists, create a new document
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

                    // Insert the new document
                    await _usersCollection.InsertOneAsync(newModification);
                    MessageBox.Show("New modifications saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // If a record exists, update it
                    var update = Builders<BsonDocument>.Update
                        .Set("mouseModifications.deviceName", deviceName)
                        .Set("mouseModifications.dpiLevel", dpiLevel)
                        .Set("updatedAt", DateTime.UtcNow);

                    var options = new UpdateOptions { IsUpsert = false };
                    await _usersCollection.UpdateOneAsync(filter, update, options);

                    MessageBox.Show("Modifications updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}