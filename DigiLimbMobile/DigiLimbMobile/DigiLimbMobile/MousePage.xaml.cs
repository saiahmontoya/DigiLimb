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
       
    // ------------------------ mouse movement---------------------------------------
    
    //Handle Pointer gesture updates
    private int activeTouchPoints = 0;
    private Point lastPosition;


    private void OnPointerPressed(object sender, PointerEventArgs e)
    {
        activeTouchPoints++;
        if (activeTouchPoints == 1)
        {
            Point? currentPositionNullable = e.GetPosition(sender as Element);
            if (currentPositionNullable.HasValue)
            {
                lastPosition = currentPositionNullable.Value;
            }
        }
        Console.WriteLine($"Pointer pressed. Active touches: {activeTouchPoints}");
    }
    private void OnPointerReleased(object sender, PointerEventArgs e)
    {
        activeTouchPoints = Math.Max(0, activeTouchPoints - 1); // prevent negative touch points
        if (activeTouchPoints < 2)
        {
            lastPosition = default(Point);  // Reset last position if scrolling is over
        }

        Console.WriteLine($"Pointer released. Active touches: {activeTouchPoints}");
    }
    
    private void OnPointerUpdated(object sender, PointerEventArgs e)
    {
        Point? currentPositionNullable = e.GetPosition(sender as Element);

        if (!currentPositionNullable.HasValue)
        {
            return; // exit early if position is null
        }

        Point currentPosition = currentPositionNullable.Value;
        // map touch movement to mouse movement
        if (activeTouchPoints == 1)
        {
            if (lastPosition == default(Point) || Math.Abs(lastPosition.X - currentPosition.X) > 100 || Math.Abs(lastPosition.Y - currentPosition.Y) > 100)
            {
                lastPosition = currentPosition;
                return;
            }
            double dx = currentPosition.X - lastPosition.X;
            double dy = currentPosition.Y - lastPosition.Y;

            if (Math.Abs(dx) > 1 || Math.Abs(dy) > 1)
            {
                SendMouseMovement(dx, dy);
            }
            lastPosition = currentPosition;
            // label indicating movement
            Console.WriteLine($"Touch at X: {dx}, Y: {dy}");
        }
        //handle two finger inputs
        else if(activeTouchPoints == 2)
        {
            double scrollAmount = currentPosition.Y - lastPosition.Y;

            if (Math.Abs(scrollAmount) > 1) //ignor small movements
            {
                SendScrollEvent(scrollAmount); 
            }

            lastPosition = currentPosition; 
        }
    }
    
    // ----------------------------scroll----------------------------------------------------------
    private async void SendScrollEvent(double scrollAmount)
    {
        // send scroll data
        if (_bluetoothManager.mouseCharacteristic != null)
        {
            List<byte> message = new List<byte>();

            // scroll Movement (Header 0x08 + 4 Bytes Integer)
            message.Add(0x05);
            message.AddRange(BitConverter.GetBytes(scrollAmount).Reverse());

            await _bluetoothManager.mouseCharacteristic.WriteAsync(message.ToArray());
            //Console.WriteLine($"Message bytes: {BitConverter.ToString(message.ToArray())}");
            //Console.WriteLine($"Sent Mouse Movement: X={x}, Y={y}");
        }
        else
        {
            Console.WriteLine("Send failed (scroll): no connection");
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
            Console.WriteLine($"Message bytes: {BitConverter.ToString(message.ToArray())}");
            Console.WriteLine($"Sent Mouse Movement: X={x}, Y={y}");
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