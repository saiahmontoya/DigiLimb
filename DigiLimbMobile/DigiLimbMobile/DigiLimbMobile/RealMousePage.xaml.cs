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
using System.Net.WebSockets;
using Microsoft.Maui.Devices.Sensors;

namespace DigiLimbMobile;
public partial class RealMousePage : ContentPage
{
    private BluetoothManager _bluetoothManager;
    public RealMousePage()
	{
        InitializeComponent();
        _bluetoothManager = App.BluetoothManager;
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {

        SettingsPanel.IsVisible = !SettingsPanel.IsVisible;


    }

    private void OnMouseDoneClicked(object sender, EventArgs e)
    {
        if (double.TryParse(MouseSensitivityEntry.Text, out double val))
        {
            sensitivity = val;
        }
        MouseSensitivityEntry.Unfocus();
    }

    private void OnScrollDoneClicked(object sender, EventArgs e)
    {
        if (int.TryParse(ScrollSensitivityEntry.Text, out int val))
        {
            WHEEL_DELTA = val;
        }
        ScrollSensitivityEntry.Unfocus();
    }
    //--------------------------------------------click events-----------------------------------
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
            isRightPressed = false;
            SendRelease(false, true);
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


        if (App.GlobalWebSocket?.State == WebSocketState.Open)
        {
            var ws = App.GlobalWebSocket;
            byte[] messageArray = message.ToArray();
            if (ws == null || ws.State != WebSocketState.Open)
            {
                Console.WriteLine("Not connected to server.");
                return;
            }
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(messageArray), WebSocketMessageType.Binary, true, CancellationToken.None);
                //Console.WriteLine($"Message sent.{BitConverter.ToString(messageArray)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }
        else if (_bluetoothManager.BluetoothConnectionFlag == true)
        {
            if (_bluetoothManager.mouseCharacteristic != null)
            {
                if (message.Count > 0)
                {
                    await _bluetoothManager.mouseCharacteristic.WriteAsync(message.ToArray());
                    Console.WriteLine($"Sent Press: Left={leftPress}, Right={rightPress}");
                }
            }
        }
        else
        {
            Console.WriteLine("Send failed (scroll): no connection");
        }
    }

    private async void SendRelease(bool leftRelease, bool rightRelease)
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

        if (App.GlobalWebSocket?.State == WebSocketState.Open)
        {
            var ws = App.GlobalWebSocket;
            byte[] messageArray = message.ToArray();
            if (ws == null || ws.State != WebSocketState.Open)
            {
                Console.WriteLine("Not connected to server.");
                return;
            }
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(messageArray), WebSocketMessageType.Binary, true, CancellationToken.None);
                //Console.WriteLine($"Message sent.{BitConverter.ToString(messageArray)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }
        else if (_bluetoothManager.BluetoothConnectionFlag == true)
        {
            if (_bluetoothManager.mouseCharacteristic != null)
            {


                if (message.Count > 0)
                {
                    await _bluetoothManager.mouseCharacteristic.WriteAsync(message.ToArray());
                    Console.WriteLine($"Sent Release: Left={leftRelease}, Right={rightRelease}");
                }
            }
        }
        else
        {
            Console.WriteLine("Send failed (scroll): no connection");
        }
    }

    //-----------------------------------scroll events------------------------------------------------------
    //Handle pan gesture updates
    private double _lastPanY = 0; // Store the last Y position

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastPanY = e.TotalY;
                break;

            case GestureStatus.Running:
                double deltaY = e.TotalY - _lastPanY; // Difference in Y movement
                _lastPanY = e.TotalY;

                // Adjust scroll sensitivity multiplier
                double scrollAmount = deltaY * 3; // Modify multiplier as needed
                // Send scroll event to the PC
                SendScrollEvent(scrollAmount);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _lastPanY = 0; // Reset when gesture ends
                break;
        }
    }


    int WHEEL_DELTA = 30;
    private async void SendScrollEvent(double scrollValue)
    {
        int scrollAmount = (int)(scrollValue * WHEEL_DELTA) * -1;

        if (App.GlobalWebSocket?.State == WebSocketState.Open)
        {
            List<byte> message = new List<byte>();

            // scroll Movement (Header 0x05 + 4 Bytes Integer)
            message.Add(0x05);
            message.AddRange(BitConverter.GetBytes(scrollAmount));
            var ws = App.GlobalWebSocket;
            byte[] messageArray = message.ToArray();
            if (ws == null || ws.State != WebSocketState.Open)
            {
                Console.WriteLine("Not connected to server.");
                return;
            }
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(messageArray), WebSocketMessageType.Binary, true, CancellationToken.None);
                Console.WriteLine($"Message sent.{BitConverter.ToString(messageArray)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }
        else if (_bluetoothManager.BluetoothConnectionFlag == true)
        {
            List<byte> message = new List<byte>();

            // scroll Movement (Header 0x05 + 4 Bytes Integer)
            message.Add(0x05);
            message.AddRange(BitConverter.GetBytes(scrollAmount).Reverse());
            // send scroll data via bluetooth
            if (_bluetoothManager.mouseCharacteristic != null)
            {
                await _bluetoothManager.mouseCharacteristic.WriteAsync(message.ToArray());
                Console.WriteLine($"Message bytes: {BitConverter.ToString(message.ToArray())}");
                //Console.WriteLine($"Sent Mouse Movement: X={x}, Y={y}");
            }
            else
            {
                Console.WriteLine("Send failed (scroll): no connection");
            }
        }
        else
        {
            Console.WriteLine("Send failed (scroll): no connection");
        }

    }

    //----------------------------------------------------------movement events-----------------------------------------------
    private bool _gyroEnabled = false;

    private void OnGyroButtonPressed(object sender, EventArgs e)
    {
        _gyroEnabled = true;
        var button = sender as Button;
        if (button != null)
        {
            button.BackgroundColor = Color.FromArgb("#77B1D4");
        }
        // start reading gyroscope and move mouse
    }

    private void OnGyroButtonReleased(object sender, EventArgs e)
    {
        _gyroEnabled = false;
        var button = sender as Button;
        if (button != null)
        {
            button.BackgroundColor = Color.FromArgb("#d9ecfa");
        }
        // stop moving mouse based on gyro
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartGyroscope();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopGyroscope();
    }

    void StartGyroscope()
    {
        if (Gyroscope.Default.IsSupported)
        {
            Gyroscope.Default.ReadingChanged += Gyroscope_ReadingChanged;
            Gyroscope.Default.Start(SensorSpeed.Game); // Fastest updates
        }
    }

    void StopGyroscope()
    {
        if (Gyroscope.Default.IsSupported)
        {
            Gyroscope.Default.Stop();
            Gyroscope.Default.ReadingChanged -= Gyroscope_ReadingChanged;
        }
    }

    private void Gyroscope_ReadingChanged(object sender, GyroscopeChangedEventArgs e)
    {
        var data = e.Reading;
        // data.AngularVelocity is a Vector3: (X, Y, Z)
        double deltaY = data.AngularVelocity.X * 30.0; //in radians/sec
        double deltaX = data.AngularVelocity.Y * 30.0;

        if (_gyroEnabled)
        {
            if (Math.Abs(deltaX) > 0.01 || Math.Abs(deltaY) > 0.01) // filter tiny noise
            {
                SendMouseMovement(deltaX, deltaY);
            }
            // Use deltaX and deltaY to move the mouse
            Console.WriteLine($"Gyroscope - X: {deltaX}, Y: {deltaY}, Z: {data.AngularVelocity.Z}");
        }
        
    }


    // send movements to PC
    double scaleFactor = 2;
    double sensitivity = 1;
    private async void SendMouseMovement(double x, double y)
    {

        double maxSpeed = 120;

        double speed = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        //Debug.WriteLine(speed);
        double curve = 1.0 + (speed / maxSpeed);
        double scaledX = x * curve;
        double scaledY = y * curve;

        double moveX = scaledX / scaleFactor * sensitivity;
        double moveY = scaledY / scaleFactor * sensitivity;

        if (App.GlobalWebSocket?.State == WebSocketState.Open)
        {
            List<byte> message = new List<byte>();

            message.Add(0x01);
            message.AddRange(BitConverter.GetBytes(moveX));

            message.Add(0x02);
            message.AddRange(BitConverter.GetBytes(moveY));

            var ws = App.GlobalWebSocket;
            byte[] messageArray = message.ToArray();
            if (ws == null || ws.State != WebSocketState.Open)
            {
                Console.WriteLine("Not connected to server.");
                return;
            }
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(messageArray), WebSocketMessageType.Binary, true, CancellationToken.None);
                Console.WriteLine($"Message sent.{BitConverter.ToString(messageArray)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }
        else if (_bluetoothManager.BluetoothConnectionFlag == true)
        {
            if (_bluetoothManager.mouseCharacteristic != null)
            {
                List<byte> message = new List<byte>();

                // X Movement (Header 0x01 + 4 Bytes Integer)
                message.Add(0x01);
                message.AddRange(BitConverter.GetBytes(moveX).Reverse());

                // Y Movement (Header 0x02 + 4 Bytes Integer)
                message.Add(0x02);
                message.AddRange(BitConverter.GetBytes(moveY).Reverse());

                await _bluetoothManager.mouseCharacteristic.WriteAsync(message.ToArray());
                //Console.WriteLine($"Message bytes: {BitConverter.ToString(message.ToArray())}");
                //Console.WriteLine($"Sent Mouse Movement: X={moveX}, Y={moveY}");
            }
            else
            {
                Console.WriteLine("Send failed (mouse): no connection");
            }
        }
        else
        {
            Console.WriteLine("Send failed (mouse): no connection");
        }
    }


}
