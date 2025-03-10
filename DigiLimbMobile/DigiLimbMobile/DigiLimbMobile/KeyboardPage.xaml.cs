using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DigiLimbMobile;

public partial class KeyboardPage : ContentPage
{
    private BluetoothManager _bluetoothManager;
    private Dictionary<string, Button> keyButtons = new Dictionary<string, Button>();

    private bool isShiftActive = false;
    private bool isCtrlActive = false;
    private bool isAltActive = false;

    public KeyboardPage()
    {
        _bluetoothManager = App.BluetoothManager;
        GenerateKeyboard();
    }

    private void GenerateKeyboard()
    {
        Grid keyboardGrid = new Grid
        {
            ColumnSpacing = 5, // ✅ Add spacing between keys
            RowSpacing = 5,
            Padding = new Thickness(10)
        };

        int maxColumns = 15; // ✅ Ensures layout consistency

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
                    HeightRequest = 50, // ✅ Only set height, let width auto-adjust
                    HorizontalOptions = LayoutOptions.Fill, // ✅ Fixes CS0618 warning
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

        Content = new ScrollView { Content = keyboardGrid };
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

        // Apply Shift Modifier
        key = ApplyShiftModifiers(key);

        // Build key string with modifiers
        string keyData = $"{(isCtrlActive ? "Ctrl+" : "")}{(isAltActive ? "Alt+" : "")}{(isShiftActive ? key.ToUpper() : key)}";

        Console.WriteLine($"📡 Sending Key: {keyData}");
        await _bluetoothManager.SendKeyPress(keyData);

        // If Shift was active for ONE key, disable it after sending
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
                return shiftMap[key]; // Convert to shifted symbol

            if (key.Length == 1 && char.IsLetter(key[0]))
                return key.ToUpper(); // Capitalize letters
        }

        return key;
    }

    private void UpdateModifierKey(string key, bool isActive)
    {
        keyButtons[key].BackgroundColor = isActive ? Colors.DarkGray : Colors.Gray;
    }
}
