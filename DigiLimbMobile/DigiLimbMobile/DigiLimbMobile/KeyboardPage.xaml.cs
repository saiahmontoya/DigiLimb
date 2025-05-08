using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.WebSockets;

namespace DigiLimbMobile
{
    public partial class KeyboardPage : ContentPage
    {
        private BluetoothManager _bluetoothManager;
        private Dictionary<string, Button> keyButtons = new Dictionary<string, Button>();

        private bool isShiftActive = false;
        private bool isCtrlActive = false;
        private bool isAltActive = false;
        private bool isCapsActive = false; // ✅ Added Caps state

        public KeyboardPage()
        {
            InitializeComponent();
            _bluetoothManager = App.BluetoothManager;
            GenerateKeyboard();
        }
        private void GenerateKeyboard()
        {
            Grid keyboardGrid = KeyboardGrid;

            int maxColumns = 15;
            for (int i = 0; i < maxColumns; i++)
                keyboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            for (int i = 0; i < KeyboardLayout.Layout.Length; i++)
                keyboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int row = 0; row < KeyboardLayout.Layout.Length; row++)
            {
                int col = 0;
                foreach (string keyData in KeyboardLayout.Layout[row])
                {
                    string[] parts = keyData.Split(':');
                    string actualKey = parts[0];
                    int columnSpan = (parts.Length > 1) ? int.Parse(parts[1]) : 1;

                    Button keyButton = new Button
                    {
                        Text = actualKey,
                        Padding = new Thickness(8),
                        BackgroundColor = GetKeyColor(actualKey),
                        FontSize = 14,
                        HeightRequest = 50,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill
                    };

                    keyButton.Clicked += (sender, e) => OnKeyPress(actualKey);

                    keyButtons[actualKey] = keyButton;

                    Grid.SetRow(keyButton, row);
                    Grid.SetColumn(keyButton, col);
                    Grid.SetColumnSpan(keyButton, columnSpan);
                    keyboardGrid.Children.Add(keyButton);

                    col += columnSpan;
                }
            }
        }

        private Color GetKeyColor(string key)
        {
            if (key == "Shift" || key == "Ctrl" || key == "Alt" || key == "Caps" || key == "Tab" || key == "Space") return Color.FromArgb("#77B1D4");
            if (key == "Backspace" || key == "Enter") return Color.FromArgb("#517891");
            return Color.FromArgb("#57b9FF");
        }

        private async void OnKeyPress(string key)
        {
            if (key == "Shift")
            {
                isShiftActive = !isShiftActive;
                UpdateModifierKey("Shift", isShiftActive);
                return;
            }

            if (key == "Ctrl")
            {
                isCtrlActive = !isCtrlActive;
                UpdateModifierKey("Ctrl", isCtrlActive);
                return;
            }

            if (key == "Alt")
            {
                isAltActive = !isAltActive;
                UpdateModifierKey("Alt", isAltActive);
                return;
            }

            if (key == "Caps")
            {
                isCapsActive = !isCapsActive;
                UpdateModifierKey("Caps", isCapsActive);
                Console.WriteLine("📡 Sending Key: Caps");
                await SendKeyPress("Caps");
                return;
            }

            string keyData = $"{(isCtrlActive ? "Ctrl+" : "")}" +
                             $"{(isAltActive ? "Alt+" : "")}" +
                             $"{(isShiftActive ? "Shift+" : "")}" +
                             key.ToLower(); // always lowercase for letter keys

            Console.WriteLine($"📡 Sending Key: {keyData}");
            await SendKeyPress(keyData);

            // Flash the button
            if (keyButtons.TryGetValue(key, out Button btn))
            {
                Color originalColor = btn.BackgroundColor;
                btn.BackgroundColor = Colors.Gray;
                await Task.Delay(150);
                btn.BackgroundColor = originalColor;
            }

            if (isShiftActive && key.Length == 1)
            {
                isShiftActive = false;
                UpdateModifierKey("Shift", isShiftActive);
            }
        }

        private void UpdateModifierKey(string key, bool isActive)
        {
            if (keyButtons.ContainsKey(key))
            {
                keyButtons[key].BackgroundColor = isActive ? Colors.DarkGray : Colors.Gray;
            }
        }

        public async Task SendKeyPress(string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            List<byte> message = new List<byte> { 0x06 }; // 0x06 = header for keyboard input
            message.AddRange(keyBytes);

            // ✅ Try WebSocket first if it's connected
            if (App.GlobalWebSocket?.State == WebSocketState.Open)
            {
                try
                {
                    await App.GlobalWebSocket.SendAsync(
                        new ArraySegment<byte>(message.ToArray()),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);

                    Console.WriteLine($"[WiFi] Sent key '{key}' over WebSocket");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WiFi] Send failed: {ex.Message}");
                }
            }

            // ✅ Fallback to Bluetooth if connected
            if (_bluetoothManager.BluetoothConnectionFlag == true &&
                _bluetoothManager.keyboardCharacteristic != null)
            {
                try
                {
                    await _bluetoothManager.keyboardCharacteristic.WriteAsync(message.ToArray());
                    Console.WriteLine($"[Bluetooth] Sent key '{key}' over BLE");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Bluetooth] Send failed: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("❌ No connection available to send key press.");
            }
        }

        private async void OnCloseKeyboard(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
