using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Controls;
using DigiLimbDesktop.Models;
using System.Linq;
using System.Net.NetworkInformation;
using DeviceModel = DigiLimbDesktop.Models.Device;

namespace DigiLimbDesktop
{
    public partial class LoginPage : ContentPage
    {
        private MongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<User> userCollection;
        private IMongoCollection<DeviceModel> deviceCollection;

        public LoginPage()
        {
            InitializeComponent();
            InitializeMongoDbConnection();
        }

        private async void InitializeMongoDbConnection()
        {
            try
            {
                string connectionUri = "mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase";
                client = new MongoClient(connectionUri);
                database = client.GetDatabase("DigilimbDatabase");
                userCollection = database.GetCollection<User>("Users");
                deviceCollection = database.GetCollection<DeviceModel>("Devices");

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
            string email = txtEmail.Text?.Trim();
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
                    // Get MAC Address
                    string macAddress = GetMacAddress();

                    // Store device info in database
                    var existingDevice = await deviceCollection.Find(d => d.MacAddress == macAddress).FirstOrDefaultAsync();
                    if (existingDevice == null)
                    {
                        var newDevice = new DeviceModel
                        {
                            UserId = ObjectId.Parse(user.Id),  // ✅ Convert user.Id (string) to ObjectId
                            DeviceModel = DeviceInfo.Model,
                            Manufacturer = DeviceInfo.Manufacturer,
                            Platform = DeviceInfo.Platform.ToString(),  // ✅ Correct: Convert enum to string
                            OsVersion = DeviceInfo.VersionString,
                            DeviceType = DeviceInfo.DeviceType.ToString(),
                            MacAddress = macAddress,
                            CreatedAt = DateTime.UtcNow
                        };


                        await deviceCollection.InsertOneAsync(newDevice);
                    }

                    // Store user email globally
                    AppShell.UserEmail = email;

                    // Navigate to MainPage
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        //DisplayAlert("Success", "Login Successful!", "OK");
                        Navigation.PushAsync(new MainPage());
                        Debug.WriteLine("Successfully logged in.");
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

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string email = txtEmail.Text?.Trim();
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
        private string GetMacAddress()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault() ?? "Unknown";
        }

    



    }
}
