using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DigiLimbMobile.View;

public partial class Login : ContentPage
{
	private MongoClient client;
	private IMongoDatabase database;
	private IMongoCollection<User> userCollection;
	
	public Login()
	{
		InitializeComponent();
		InitializeMongoDbConnection();
	}

	public class User()
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

    private async void InitializeMongoDbConnection()
    {
        try
        {
            string connectionUri = "mongodb://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase-shard-00-00.mneoe.mongodb.net:27017,digilimbdatabase-shard-00-01.mneoe.mongodb.net:27017,digilimbdatabase-shard-00-02.mneoe.mongodb.net:27017/?ssl=true&replicaSet=atlas-brcqsn-shard-0&authSource=admin&retryWrites=true&w=majority&appName=DigilimbDatabase";
            client = new MongoClient(connectionUri);
            database = client.GetDatabase("DigilimbDatabase");
            userCollection = database.GetCollection<User>("Users");
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

    private async void LoginBypass(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage());
    }

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
                await DisplayAlert("Success", "Login Successful!", "OK");

                await Shell.Current.GoToAsync($"{nameof(ConnectionPage)}");

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
        string email = Username.Text.Trim();
        string password = Password.Text;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Registration Error", "Please enter both email and password.", "OK");
            return;
        }
        if (userCollection == null)
        {
            await DisplayAlert("Database Error", "MongoDB connection not initialized.", "OK");
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