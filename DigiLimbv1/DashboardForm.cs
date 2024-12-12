using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DigiLimbv1
{
    public partial class DashboardForm : Form
    {
        public DashboardForm()
        {
            InitializeComponent();
            CustomizeDashboard(); // Apply sci-fi customization
        }

        private void CustomizeDashboard()
        {
            this.Text = "Dashboard";
            this.Size = new Size(800, 600);
            this.BackColor = Color.Black;

            // Background Image Debug
            try
            {
                this.BackgroundImage = Image.FromFile("F:\\Users\\thego\\source\\repos\\DigiLimbv1\\DigiLimbv1\\Resources\\scifi_background.jpg");
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading background image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Create a panel for the 3D title
            Panel titlePanel = new Panel
            {
                Size = new Size(800, 100), // Full width of the form, 100px height
                Location = new Point(0, 0),
                BackColor = Color.Transparent // Transparent for custom painting
            };

            // Add custom 3D painting to the title panel
            titlePanel.Paint += (sender, e) =>
            {
                try
                {
                    string titleText = "DigiLimb Control Panel";
                    Font titleFont = new Font("Segoe UI", 36, FontStyle.Bold);

                    // Measure the size of the text to center it
                    SizeF textSize = e.Graphics.MeasureString(titleText, titleFont);
                    float xPosition = (titlePanel.Width - textSize.Width) / 2;
                    float yPosition = (titlePanel.Height - textSize.Height) / 2;

                    // Draw shadow (for 3D effect)
                    using (Brush shadowBrush = new SolidBrush(Color.Black))
                    {
                        e.Graphics.DrawString(titleText, titleFont, shadowBrush, new PointF(xPosition + 4, yPosition + 4)); // Shadow offset
                    }

                    // Draw main text with gradient
                    using (Brush textBrush = new LinearGradientBrush(
                        new RectangleF(0, 0, titlePanel.Width, titlePanel.Height),
                        Color.Cyan,
                        Color.DeepSkyBlue,
                        LinearGradientMode.Vertical))
                    {
                        e.Graphics.DrawString(titleText, titleFont, textBrush, new PointF(xPosition, yPosition));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error in title rendering: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Add the title panel to the form
            this.Controls.Add(titlePanel);

            // Add buttons or other UI elements below the title
            AddButtons();
        }

        private void AddButtons()
        {
            // Button properties
            Font buttonFont = new Font("Segoe UI", 12, FontStyle.Bold);
            Size buttonSize = new Size(200, 60);

            // Create buttons
            Button btnConnections = CreateSciFiButton("Connections", buttonFont, buttonSize, new Point(300, 150));
            Button btnModifications = CreateSciFiButton("Modifications", buttonFont, buttonSize, new Point(300, 250));
            Button btnSupport = CreateSciFiButton("Support", buttonFont, buttonSize, new Point(300, 350));

            // Create Quit Button with custom size and red border
            Button btnQuit = new Button
            {
                Text = "Quit",
                Font = buttonFont,
                Size = new Size(180, 50), // Smaller size
                Location = new Point(310, 450), // Centered
                ForeColor = Color.White,
                BackColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnQuit.FlatAppearance.BorderSize = 2;
            btnQuit.FlatAppearance.BorderColor = Color.Red; // Always red border

            // Disable hover effects for the Quit button
            btnQuit.MouseEnter += (sender, e) =>
            {
                btnQuit.FlatAppearance.BorderColor = Color.Red; // Keep the border red on hover
            };
            btnQuit.MouseLeave += (sender, e) =>
            {
                btnQuit.FlatAppearance.BorderColor = Color.Red; // Reset to red after hover
            };

            btnQuit.Click += (sender, e) =>
            {
                var result = MessageBox.Show("Are you sure you want to quit?", "Confirm Quit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Application.Exit(); // Quit the application
                }
            };

            // Add buttons to the form
            if (btnConnections != null) this.Controls.Add(btnConnections);
            if (btnModifications != null) this.Controls.Add(btnModifications);
            if (btnSupport != null) this.Controls.Add(btnSupport);
            if (btnQuit != null) this.Controls.Add(btnQuit); // Add Quit button
        }

        private Button CreateSciFiButton(string text, Font font, Size size, Point location)
        {
            try
            {
                Button button = new Button
                {
                    Text = text,
                    Font = font,
                    Size = size,
                    Location = location,
                    ForeColor = Color.White,
                    BackColor = Color.Black,
                    FlatStyle = FlatStyle.Flat
                };

                // Basic border styling
                button.FlatAppearance.BorderColor = Color.Cyan;
                button.FlatAppearance.BorderSize = 2;

                // Hover effects
                button.MouseEnter += (sender, e) =>
                {
                    if (button != null && !button.IsDisposed)
                        button.FlatAppearance.BorderColor = Color.Lime;
                };

                button.MouseLeave += (sender, e) =>
                {
                    if (button != null && !button.IsDisposed)
                        button.FlatAppearance.BorderColor = Color.Cyan;
                };

                // Remove custom painting temporarily to debug
                // If this resolves the issue, reintroduce custom painting carefully
                // Add a log for debugging
                return button;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating button '{text}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null; // Return null if creation fails
            }
        }

    }
}
