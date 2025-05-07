using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace DigiLimbDesktop
{
    public partial class SettingsPage : ContentPage
    {
        private readonly IMongoCollection<BsonDocument> _usersCollection;
        private string _email;
        private ObjectId? _userID;
        private string tempSelect;
        private string _originalTheme;
        private string _selectedTheme = "Light"; // Default theme
        private Button _selectedThemeButton;


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
                    txtName.Text = "Default Device";
                    txtDPI.Text = "800";
                }

                // ✅ Load theme and sync visual state
                // LoadTheme();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error loading data: {ex.Message}", "OK");
                Debug.WriteLine($"❌ LoadExistingData Exception: {ex.Message}");
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
                // Ensure the event is invoked on the main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        _selectedTheme = tempSelect;
                        
                        DeviceNameUpdated?.Invoke(deviceName);

                        Debug.WriteLine($"💾 Saving theme preference: '{_selectedTheme}'");
                        Preferences.Set("AppTheme", _selectedTheme);
                        _originalTheme = _selectedTheme;

                        ApplyTheme(_selectedTheme);

                        Debug.WriteLine("✅ Theme preference saved and applied.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error during theme save/apply: {ex.Message}");
                    }
                });

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error saving data: {ex.Message}", "OK");
            }
        }

        

        private void OnLightModeClicked(object sender, EventArgs e)
        {
            if (_selectedThemeButton != btnLightMode)
            {
                tempSelect = "Light";
                SetSelectedButton(btnLightMode, btnDarkMode);
                Debug.WriteLine("🌞 Light mode selected.");
                ApplyTheme(tempSelect);
            }
        }

        private void OnDarkModeClicked(object sender, EventArgs e)
        {
            if (_selectedThemeButton != btnDarkMode)
            {
                tempSelect = "Dark";
                SetSelectedButton(btnDarkMode, btnLightMode);
                Debug.WriteLine("🌙 Dark mode selected.");
                ApplyTheme(tempSelect);
            }
        }

        private void SetSelectedButton(Button selected, Button unselected)
        {
            Debug.WriteLine($"▶️ Selecting: {selected.Text}");

            // Selected button style
            selected.BackgroundColor = Color.FromArgb("#037ffc");
            selected.TextColor = Colors.White;
            selected.Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0.5f,
                Offset = new Point(4, 4),
                Radius = 12
            };

            // Unselected button style
            unselected.BackgroundColor = Color.FromArgb("#D3D3D3");
            unselected.TextColor = Colors.Black;
            unselected.Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0.5f,
                Offset = new Point(4, 4),
                Radius = 12
            };

            _selectedThemeButton = selected;
        }






        private void LoadTheme()
        {
            _selectedTheme = Preferences.Get("AppTheme", "Light");

            if (_selectedTheme == "Dark")
                SetSelectedButton(btnDarkMode, btnLightMode);
            else
                SetSelectedButton(btnLightMode, btnDarkMode);
        }



        private async void ApplyTheme(string theme)
        {
            try
            {
                var page = this; // The current settings page

                // Animate page background color
                var currentPageColor = page.BackgroundColor;
                var targetPageColor = theme == "Dark" ? Color.FromArgb("#3A3A3A") : Colors.White;
                await page.AnimateColor("BackgroundColor", currentPageColor, targetPageColor);

                // Animate all frames (cards)
                var allBorders = this.FindByName<ScrollView>("MainScroll")
                     ?.FindDescendants().OfType<Border>() ?? Enumerable.Empty<Border>();

                Color targetCardColor = theme == "Dark" ? Colors.DarkGray : Colors.LightBlue;

                foreach (var border in allBorders)
                {
                    Color currentCardColor = border.BackgroundColor;
                    await border.AnimateColor("BackgroundColor", currentCardColor, targetCardColor);
                }


                // Get current values from resource dictionary
                var primaryTextColor = theme == "Dark" ? Colors.White : Color.FromArgb("#2D2D2D");
                var secondaryTextColor = theme == "Dark" ? Colors.LightGray : Colors.DarkGray;

                // Find all labels
                var allLabels = this.FindByName<ScrollView>("MainScroll")?.FindDescendants().OfType<Label>() ?? Enumerable.Empty<Label>();

                // Animate primary labels
                var primaryLabels = allLabels.Where(label =>
                    label.TextColor == (Color?)Application.Current.Resources["PrimaryTextColor"]);

                foreach (var label in primaryLabels)
                {
                    var current = label.TextColor ?? Colors.Transparent;
                    await label.AnimateColor("TextColor", current, primaryTextColor);
                }

                // Animate secondary labels
                var secondaryLabels = allLabels.Where(label =>
                    label.TextColor == (Color?)Application.Current.Resources["SecondaryTextColor"]);

                foreach (var label in secondaryLabels)
                {
                    var current = label.TextColor ?? Colors.Transparent;
                    await label.AnimateColor("TextColor", current, secondaryTextColor);
                }

                var allEntries = this.FindByName<ScrollView>("MainScroll")?.FindDescendants().OfType<Entry>() ?? Enumerable.Empty<Entry>();
                var entryColor = theme == "Dark" ? Color.FromArgb("#787878") : Colors.White;

                foreach (var entry in allEntries)
                {
                    var current = entry.BackgroundColor;
                    await entry.AnimateColor("BackgroundColor", current, entryColor);
                }



                // Still update resource values for new pages to load correctly
                Application.Current.Resources["PageBackgroundColor"] = targetPageColor;
                Application.Current.Resources["CardBackgroundColor"] = targetCardColor;
                Application.Current.Resources["PrimaryTextColor"] = primaryTextColor;
                Application.Current.Resources["SecondaryTextColor"] = secondaryTextColor;
                Application.Current.Resources["EntryBackgroundColor"] = entryColor;
                Application.Current.Resources["ShellColor"] = theme == "Dark"
                ? Color.FromArgb("#2A2A2A")  // Darker shade for dark theme
                : Color.FromArgb("#C0C0C0"); // Light gray for light theme
                var titleColor = (Color)Application.Current.Resources["PrimaryTextColor"];
                var shellColor = (Color)Application.Current.Resources["ShellColor"];
                if (Shell.Current?.CurrentPage is Page currentPage)
                {
                    Shell.SetTitleColor(currentPage, titleColor);
                    Shell.SetBackgroundColor(currentPage, shellColor);
                    // Shell.SetForegroundColor(currentPage, accent);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error applying theme: {ex.Message}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _originalTheme = Preferences.Get("AppTheme", "Light");
            _selectedTheme = _originalTheme; // Set initial selection
            LoadTheme(); // this sets the correct button state
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Debug.WriteLine("Settings page dissapearing");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // If the selected theme was changed but not saved, revert back
                    if (tempSelect != _originalTheme)
                    {
                        Debug.WriteLine($"🌀 Reverting unsaved theme from '{tempSelect}' back to '{_originalTheme}'");
                        ApplyTheme(_originalTheme);
                    }
                    else
                    {
                        Debug.WriteLine($"Selected theme ({tempSelect}) and og theme ({_originalTheme}) are the same ");
                    }
                }
                catch
                {
                    Debug.WriteLine("AHHHHHHHH");
                }
            });
        }



    }
    public static class VisualElementExtensions
    {
        public static async Task AnimateColor(this VisualElement element, string property, Color fromColor, Color toColor, uint duration = 150)
        {
            if (element == null || fromColor == null || toColor == null)
            {
                Debug.WriteLine("⚠️ Skipping animation due to null value.");
                return;
            }

            var animation = new Animation(v =>
            {
                var color = Color.FromRgba(
                    fromColor.Red + (toColor.Red - fromColor.Red) * v,
                    fromColor.Green + (toColor.Green - fromColor.Green) * v,
                    fromColor.Blue + (toColor.Blue - fromColor.Blue) * v,
                    fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * v
                );

                if (property == "BackgroundColor")
                    element.BackgroundColor = color;
                else if (property == "TextColor" && element is Label label)
                    label.TextColor = color;
            });

            animation.Commit(element, "ColorFade", 16, duration, Easing.Linear);
            await Task.Delay((int)duration);
        }
      
        public static IEnumerable<Element> FindDescendants(this Element element)
        {
            if (element == null)
                yield break;

            if (element is IElementController controller)
            {
                foreach (var child in controller.LogicalChildren)
                {
                    yield return child;

                    foreach (var descendant in child.FindDescendants())
                        yield return descendant;
                }
            }
        }
    }

}
