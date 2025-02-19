using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Diagnostics;


using System.Text.Json;
using Microsoft.Maui.Devices;

namespace DigiLimbDesktop
{
    public partial class LoginPage : ContentPage
    {
        private MongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<User> userCollection;
        private IMongoCollection<Device> deviceCollection;

        public LoginPage()
        {
            InitializeComponent();
            InitializeMongoDbConnection();
        }

        // 📌 User Schema
        public class User
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            [BsonElement("email")]
            public string Email { get; set; }

            [BsonElement("passwordHash")]
            public string PasswordHash { get; set; }

            [BsonElement("salt")]
            public string Salt { get; set; }

            [BsonElement("deviceIds")]
            public List<string> DeviceIds { get; set; } = new List<string>();

            [BsonElement("createdAt")]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        // 📌 Device Schema
        public class Device
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            [BsonElement("userId")]
            [BsonRepresentation(BsonType.ObjectId)]
            public string UserId { get; set; }

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
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        private async void InitializeMongoDbConnection()
        {
            try
            {
                string connectionUri = "mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase";
                client = new MongoClient(connectionUri);
                database = client.GetDatabase("DigilimbDatabase");
                userCollection = database.GetCollection<User>("Users");
                deviceCollection = database.GetCollection<Device>("Devices");

                await TestConnectionAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Database Error", $"Connection Error: {ex.Message}", "OK");
            }
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                await database.RunCommandAsync((MongoDB.Driver.Command<BsonDocument>)"{ping:1}");
                Console.WriteLine("Successfully connected to MongoDB!");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Connection Test Failed", ex.Message, "OK");
            }
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Login Error", "Please enter valid credentials.", "OK");
                return;
            }

            try
            {
                var user = await userCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

                if (user == null)
                {
                    await DisplayAlert("Login Error", "User not found.", "OK");
                    return;
                }

                if (VerifyPassword(password, user.PasswordHash, user.Salt))
                {
                    // 📌 Check if the device is already registered
                    await CheckAndSaveDevice(user.Id);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayAlert("Success", "Login Successful!", "OK");
                        Navigation.PushAsync(new MainPage(email));
                        Debug.WriteLine("Succesfully logged in.");
                    });
                }
                else
                {
                    await DisplayAlert("Login Error", "Invalid password.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Login Error", ex.Message, "OK");
            }
        }

        private async Task CheckAndSaveDevice(string userId)
        {
            var deviceInfo = new Device
            {
                UserId = userId,
                DeviceModel = DeviceInfo.Current.Model,
                Manufacturer = DeviceInfo.Current.Manufacturer,
                Platform = DeviceInfo.Current.Platform.ToString(),
                OsVersion = DeviceInfo.Current.VersionString,
                Idiom = DeviceInfo.Current.Idiom.ToString(),
                DeviceType = DeviceInfo.Current.DeviceType.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // 📌 Check if the device already exists
            var existingDevice = await deviceCollection.Find(d =>
                d.UserId == userId &&
                d.DeviceModel == deviceInfo.DeviceModel &&
                d.Platform == deviceInfo.Platform &&
                d.OsVersion == deviceInfo.OsVersion).FirstOrDefaultAsync();

            if (existingDevice == null)
            {
                // 📌 Save new device to database
                await deviceCollection.InsertOneAsync(deviceInfo);

                // 📌 Update the user with the new device ID
                var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
                var update = Builders<User>.Update.Push(u => u.DeviceIds, deviceInfo.Id);
                await userCollection.UpdateOneAsync(filter, update);

                Console.WriteLine("New device registered!");
            }
            else
            {
                Console.WriteLine("Device already registered!");
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Registration Error", "Please enter both email and password.", "OK");
                return;
            }

            try
            {
                var existingUser = await userCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    await DisplayAlert("Registration Error", "User already exists.", "OK");
                    return;
                }

                string salt = GenerateSalt();
                string passwordHash = HashPassword(password, salt);

                var newUser = new User
                {
                    Email = email,
                    PasswordHash = passwordHash,
                    Salt = salt,
                    CreatedAt = DateTime.UtcNow
                };

                await userCollection.InsertOneAsync(newUser);

                await DisplayAlert("Success", "User registered successfully!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Registration Error", ex.Message, "OK");
            }
        }

        private static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private static string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), Convert.FromBase64String(salt), 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(20);
                return Convert.ToBase64String(hash);
            }
        }

        private static bool VerifyPassword(string password, string storedHash, string salt)
        {
            string hashedPassword = HashPassword(password, salt);
            return hashedPassword == storedHash;
        }
    }
}
