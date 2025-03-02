using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions.Extensions;
using Plugin.BLE.Abstractions;
using Microsoft.Maui.Devices; // Required for DeviceInfo
using Microsoft.Maui.ApplicationModel;
using System.Text;

namespace DigiLimbMobile;

public partial class MousePage : ContentPage
{
    private BluetoothManager _bluetoothManager;

    public MousePage()
	{
		InitializeComponent();
        _bluetoothManager = App.BluetoothManager;
       
    }

    private bool isLeftPressed = false;
    private bool isRightPressed = false;
    private void LeftPressed(object sender, EventArgs e)
    {
        if (!isLeftPressed) 
        {
            var button = sender as Button;
            if (button != null)
            {
                button.BackgroundColor = Color.FromArgb("#77B1D4");
            }
            ClickLabel.Text = $"Left pressed";
            isLeftPressed = true;
            SendPress(true, false);
        }
    }
    private void RightPressed(object sender, EventArgs e)
    {
        if (!isRightPressed)
        {
            var button = sender as Button;
            if (button != null)
            {
                button.BackgroundColor = Color.FromArgb("#77B1D4");
            }
            ClickLabel.Text = $"Right pressed";
            isRightPressed = true;
            SendPress(false, true);
        }
    }

    private void LeftReleased(object sender, EventArgs e)
    {
        if (isLeftPressed)
        {
            var button = sender as Button;
            if (button != null)
            {
                button.BackgroundColor = Color.FromArgb("#d9ecfa");
            }
            ClickLabel.Text = $"Left released";
            isLeftPressed = false;
            SendRelease(true, false);
        }
        
    }

    private void RightReleased(object sender, EventArgs e)
    {
        if (isRightPressed)
        {
            var button = sender as Button;
            if (button != null)
            {
                button.BackgroundColor = Color.FromArgb("#d9ecfa");
            }
            ClickLabel.Text = $"Right released";
            isRightPressed = false;
            SendRelease(false, true);
        }
    }

    //Handle pan gesture updates
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        // map touch movement to mouse movement
        if (e.StatusType == GestureStatus.Running)
        {
            // label indicating movement
            //MovementFeedbackLabel.Text = $"Touch at X: {e.TotalX}, Y: {e.TotalY}";

            SendMouseMovement(e.TotalX, e.TotalY);
        }
    }

    // send movements to PC
    private async void SendMouseMovement(double x, double y)
    {
        if (_bluetoothManager.mouseCharacteristic != null)
        {
            List<byte> message = new List<byte>();

            // X Movement (Header 0x01 + 4 Bytes Integer)
            message.Add(0x01);
            message.AddRange(BitConverter.GetBytes(x).Reverse());

            // Y Movement (Header 0x02 + 4 Bytes Integer)
            message.Add(0x02);
            message.AddRange(BitConverter.GetBytes(y).Reverse());
             
            await _bluetoothManager.mouseCharacteristic.WriteAsync(message.ToArray());
            //Console.WriteLine($"Message bytes: {BitConverter.ToString(message.ToArray())}");
            //Console.WriteLine($"Sent Mouse Movement: X={x}, Y={y}");
        }
        else
        {
            Console.WriteLine("Send failed (mouse): no connection");
        }
    }

    private void testPress()
    {
        Console.WriteLine("test press");
    }

    private void testRel()
    {
        Console.WriteLine("test rel");
    }

    // send clicks to PC
    private async void SendPress(bool leftPress, bool rightPress)
    {
        if (_bluetoothManager.mouseCharacteristic != null)
        {
            List<byte> message = new List<byte>();

            if (leftPress)
            {
                message.Add(0x03);
                message.Add(1); // 1 = Press
            }

            if (rightPress)
            {
                message.Add(0x04);
                message.Add(1); // 1 = Press
            }

            if (message.Count > 0)
            {
                await _bluetoothManager.mouseCharacteristic.WriteAsync(message.ToArray());
                Console.WriteLine($"Sent Press: Left={leftPress}, Right={rightPress}");
            }
        }
        else
        {
            Console.WriteLine("Send failed (press): no connection");
        }
    }

    private async void SendRelease(bool leftRelease, bool rightRelease)
    {
        if (_bluetoothManager.mouseCharacteristic != null)
        {
            List<byte> message = new List<byte>();

            if (leftRelease)
            {
                message.Add(0x03);
                message.Add(0); // 0 = Release
            }

            if (rightRelease)
            {
                message.Add(0x04);
                message.Add(0); // 0 = Release
            }

            if (message.Count > 0)
            {
                await _bluetoothManager.mouseCharacteristic.WriteAsync(message.ToArray());
                Console.WriteLine($"Sent Release: Left={leftRelease}, Right={rightRelease}");
            }
        }
        else
        {
            Console.WriteLine("Send failed (release): no connection");
        }
    }
}