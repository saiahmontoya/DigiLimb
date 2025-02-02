using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Text;
using System.Threading.Tasks;

namespace DigiLimbv1
{
    public partial class LoginForm : Form
    {
        private MongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<User> userCollection;

        private TextBox txtEmail;
        private TextBox txtPassword;

        public LoginForm()
        {
            InitializeComponent();
            InitializeMongoDbConnection();
            InitializeLoginBox();
            this.Load += LoginForm_Load; // Subscribe to the Load event
        }

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

        private async void InitializeMongoDbConnection()
        {
            try
            {
                string connectionUri = "mongodb+srv://saiahmontoya01:AQfSCJE5bfDnhYSh@digilimbdatabase.mneoe.mongodb.net/?authSource=admin&w=majority&appName=DigilimbDatabase";
                client = new MongoClient(connectionUri);
                database = client.GetDatabase("DigilimbDatabase");
                userCollection = database.GetCollection<User>("Users");

                await TestConnectionAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Connection Error: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                Console.WriteLine("Successfully connected to MongoDB!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection Test Failed: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeLoginBox()
        {
            this.Text = "Login";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;


            Panel titlePanel = new Panel
            {
                Size = new Size(800, 100),
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            };

            titlePanel.Paint += (sender, e) =>
            {
                string titleText = "DigiLimb";
                Font titleFont = new Font("Segoe UI", 36, FontStyle.Bold);

                using (Brush shadowBrush = new SolidBrush(Color.Black))
                {
                    e.Graphics.DrawString(titleText, titleFont, shadowBrush, new PointF(5, 5));
                }

                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.DrawString(titleText, titleFont, textBrush, new PointF(0, 0));
                }
            };

            Panel loginPanel = new Panel
            {
                Size = new Size(350, 300),
                Location = new Point((this.Width - 350) / 2, (this.Height - 300) / 2 + 40),
                BackColor = Color.FromArgb(128, 0, 0, 0)
            };

            loginPanel.Paint += (sender, e) =>
            {
                using (GraphicsPath path = GetRoundedRectanglePath(loginPanel.ClientRectangle, 20))
                {
                    loginPanel.Region = new Region(path);
                }
            };

            Label lblSubtitle = new Label
            {
                Text = "Login",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 40
            };

            txtEmail = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                Size = new Size(300, 30),
                Location = new Point(25, 70),
                ForeColor = Color.Gray,
                Text = "Email"
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                Size = new Size(300, 30),
                Location = new Point(25, 120),
                ForeColor = Color.Gray,
                Text = "Password",
                UseSystemPasswordChar = false
            };

            // Login Button
            Button btnLogin = new Button
            {
                Text = "Login",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(140, 40),
                Location = new Point(25, 170),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.FlatAppearance.BorderSize = 0;

            // Link the event handler
            btnLogin.Click += btnLogin_Click;

            // Register Button
            Button btnRegister = new Button
            {
                Text = "Register",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(140, 40),
                Location = new Point(185, 170),
                BackColor = Color.FromArgb(50, 180, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRegister.FlatAppearance.BorderSize = 0;

            // Link the event handler
            btnRegister.Click += btnRegister_Click;

            // Add controls to the panel
            loginPanel.Controls.Add(btnLogin);
            loginPanel.Controls.Add(btnRegister);

            loginPanel.Controls.Add(lblSubtitle);
            loginPanel.Controls.Add(txtEmail);
            loginPanel.Controls.Add(txtPassword);
            loginPanel.Controls.Add(btnLogin);
            loginPanel.Controls.Add(btnRegister);

            this.Controls.Add(titlePanel);
            this.Controls.Add(loginPanel);
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            AddPlaceholderBehavior(txtEmail, "Email");
            AddPlaceholderBehavior(txtPassword, "Password");
        }

        private void AddPlaceholderBehavior(TextBox textBox, string placeholderText)
        {
            textBox.GotFocus += (sender, e) =>
            {
                if (textBox.Text == placeholderText)
                {
                    textBox.Text = "";
                    textBox.ForeColor = Color.Black;
                    if (placeholderText == "Password")
                        textBox.UseSystemPasswordChar = true;
                }
            };

            textBox.LostFocus += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholderText;
                    textBox.ForeColor = Color.Gray;
                    if (placeholderText == "Password")
                        textBox.UseSystemPasswordChar = false;
                }
            };
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (email == "Email" || password == "Password" || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter valid credentials.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var user = await userCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

                if (user == null)
                {
                    MessageBox.Show("User not found.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (VerifyPassword(password, user.PasswordHash, user.Salt))
                {
                    MessageBox.Show("Login Successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Navigate to dashboard without disposing LoginForm
                    this.Hide();
                    DashboardForm dashboardForm = new DashboardForm(email);
                    dashboardForm.ShowDialog();
                    this.Show(); // Show LoginForm again if returning
                }
                else
                {
                    MessageBox.Show("Invalid password.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void btnRegister_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both email and password.", "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var existingUser = await userCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    MessageBox.Show("User with this email already exists.", "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                MessageBox.Show("User registered successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Registration Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from event handlers
                this.Load -= LoginForm_Load;

                // Dispose controls
                txtEmail?.Dispose();
                txtPassword?.Dispose();

                // Dispose other components
                components?.Dispose();
            }
            base.Dispose(disposing);
        }



        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Unsubscribe placeholder behaviors
            txtEmail.GotFocus -= (s, args) => { /* Placeholder logic */ };
            txtEmail.LostFocus -= (s, args) => { /* Placeholder logic */ };
            txtPassword.GotFocus -= (s, args) => { /* Placeholder logic */ };
            txtPassword.LostFocus -= (s, args) => { /* Placeholder logic */ };
        }

    }
}
