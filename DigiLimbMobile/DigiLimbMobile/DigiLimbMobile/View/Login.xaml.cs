using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.NetworkInformation;
using Microsoft.Maui.Devices;

namespace DigiLimbMobile.View;

public partial class Login : ContentPage
{
    private MongoClient client;
    private IMongoDatabase database;
    private IMongoCollection<User> userCollection;
    private IMongoCollection<Device> deviceCollection;

    public Login()
    {
        InitializeComponent();
        InitializeMongoDbConnection();
    }

    // 📌 User Model
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

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 📌 Device Model (MAC Address Only)
    public class Device
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("deviceModel")]
        public string DeviceModel { get; set; } // We'll hardcode "Unknown Device" or any custom string

        [BsonElement("macAddress")]
        public string MacAddress { get; set; }  // Only MAC Address

        [BsonElement("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    // 📌 Initialize MongoDB
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

    // 📌 Test Connection
    private async Task TestConnectionAsync()
    {
        try
        {
            await database.RunCommandAsync<BsonDocument>("{ ping: 1 }");
            Console.WriteLine("Successfully connected to MongoDB!");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Connection Test Failed", ex.Message, "OK");
        }
    }

    // 📌 Login Process
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string email = Username.Text.Trim();
        string password = Password.Text;
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
                await UpdateDeviceInfo(user.Id);

                await DisplayAlert("Success", "Login Successful!", "OK");
                await Shell.Current.GoToAsync("//ConnectionPage");
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

    // 📌 Update or Insert Device Info (MAC Address Only, No IP or DeviceInfo properties)
    private async Task UpdateDeviceInfo(string userId)
    {
        string macAddress = GetMacAddress();  // Get MAC Address

        var filter = Builders<Device>.Filter.Eq(d => d.UserId, userId);
        var update = Builders<Device>.Update
            .Set(d => d.MacAddress, macAddress)
            .Set(d => d.LastUpdated, DateTime.UtcNow);

        var result = await deviceCollection.UpdateOneAsync(filter, update);

        if (result.MatchedCount == 0)
        {
            // If no existing device record was found, insert a new one
            var newDevice = new Device
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserId = userId,
                DeviceModel = "Unknown Device", // Hardcode or any custom logic
                MacAddress = macAddress,
                LastUpdated = DateTime.UtcNow
            };

            await deviceCollection.InsertOneAsync(newDevice);
        }

        Console.WriteLine($"Updated Device Info - MAC: {macAddress}");
    }

    // 📌 Get MAC Address
    private static string GetMacAddress()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var netInterface in networkInterfaces)
            {
                if (netInterface.OperationalStatus == OperationalStatus.Up &&
                    netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    return BitConverter.ToString(netInterface.GetPhysicalAddress().GetAddressBytes()).Replace("-", ":");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving MAC Address: {ex.Message}");
        }

        return "Unknown MAC";
    }

    // 📌 Hashing Functions
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

    // 📌 Register Process
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string email = Username.Text?.Trim();
        string password = Password.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Registration Error", "Please enter both email and password.", "OK");
            return;
        }

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
}
