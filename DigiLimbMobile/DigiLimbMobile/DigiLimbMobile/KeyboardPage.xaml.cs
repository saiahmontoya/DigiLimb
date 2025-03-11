using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DigiLimbMobile
{
    public partial class KeyboardPage : ContentPage
    {
        private BluetoothManager _bluetoothManager;
        private Dictionary<string, Button> keyButtons = new Dictionary<string, Button>();

        private bool isShiftActive = false;
        private bool isCtrlActive = false;
        private bool isAltActive = false;

        public KeyboardPage()
        {
            InitializeComponent();
            _bluetoothManager = App.BluetoothManager;
            GenerateKeyboard();
        }

        private void GenerateKeyboard()
        {
            Grid keyboardGrid = KeyboardGrid; // Use the Grid from XAML

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
            if (key == "Shift" || key == "Ctrl" || key == "Alt") return Colors.Gray;
            if (key == "Backspace" || key == "Enter") return Colors.Red;
            return Colors.Blue;
        }

        private async void OnKeyPress(string key)
        {
            if (key == "Shift") { isShiftActive = !isShiftActive; UpdateModifierKey("Shift", isShiftActive); return; }
            if (key == "Ctrl") { isCtrlActive = !isCtrlActive; UpdateModifierKey("Ctrl", isCtrlActive); return; }
            if (key == "Alt") { isAltActive = !isAltActive; UpdateModifierKey("Alt", isAltActive); return; }

            key = ApplyShiftModifiers(key);

            string keyData = $"{(isCtrlActive ? "Ctrl+" : "")}{(isAltActive ? "Alt+" : "")}{(isShiftActive ? key.ToUpper() : key)}";

            Console.WriteLine($"📡 Sending Key: {keyData}");
            await SendKeyPress(keyData);

            if (isShiftActive && key.Length == 1)
            {
                isShiftActive = false;
                UpdateModifierKey("Shift", isShiftActive);
            }
        }

        private string ApplyShiftModifiers(string key)
        {
            Dictionary<string, string> shiftMap = new Dictionary<string, string>
            {
                { "1", "!" }, { "2", "@" }, { "3", "#" }, { "4", "$" }, { "5", "%" },
                { "6", "^" }, { "7", "&" }, { "8", "*" }, { "9", "(" }, { "0", ")" },
                { "-", "_" }, { "=", "+" }, { "[", "{" }, { "]", "}" }, { "\\", "|" },
                { ";", ":" }, { "'", "\"" }, { ",", "<" }, { ".", ">" }, { "/", "?" }
            };

            if (isShiftActive)
            {
                if (shiftMap.ContainsKey(key))
                    return shiftMap[key];

                if (key.Length == 1 && char.IsLetter(key[0]))
                    return key.ToUpper();
            }

            return key;
        }

        private void UpdateModifierKey(string key, bool isActive)
        {
            keyButtons[key].BackgroundColor = isActive ? Colors.DarkGray : Colors.Gray;
        }

        private async Task SendKeyPress(string keyData)
        {
            if (_bluetoothManager.keyboardCharacteristic != null)
            {
                List<byte> message = new List<byte> { 0x05 };
                message.AddRange(Encoding.UTF8.GetBytes(keyData));

                await _bluetoothManager.keyboardCharacteristic.WriteAsync(message.ToArray());
                Console.WriteLine($"📡 Sent Key Press: {keyData}");
            }
            else
            {
                Console.WriteLine("❌ No connection to PC.");
            }
        }

        private async void OnCloseKeyboard(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
