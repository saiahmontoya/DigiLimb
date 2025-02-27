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

    //left click functionality
    private void LeftClick(object sender, EventArgs e)
    {
        ClickLabel.Text = $"Left Click";
        SendClick(false, true);
    }

    //right click functionality
    private void RightClick(object sender, EventArgs e)
    {
        ClickLabel.Text = $"Right Click";
        SendClick(false, true);
    }
    //Handle pan gesture updates
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        // map touch movement to mouse movement
        if (e.StatusType == GestureStatus.Running)
        {
            // label indicating movement
            MovementFeedbackLabel.Text = $"Touch at X: {e.TotalX}, Y: {e.TotalY}";

            SendMouseMovement(e.TotalX, e.TotalY);
        }
    }

    // send movements to PC
    private async void SendMouseMovement(double x, double y)
    {
        if (_bluetoothManager.Characteristic != null)
        {
            List<byte> message = new List<byte>();

            // X Movement (Header 0x01 + 4 Bytes Integer)
            message.Add(0x01);
            message.AddRange(BitConverter.GetBytes(x));

            // Y Movement (Header 0x02 + 4 Bytes Integer)
            message.Add(0x02);
            message.AddRange(BitConverter.GetBytes(y));

            await _bluetoothManager.Characteristic.WriteAsync(message.ToArray());
            Console.WriteLine($"Sent Mouse Movement: X={x}, Y={y}");
        }
        else
        {
            Console.WriteLine("Send failed (mouse): no connection");
        }
    }


    // send clicks to PC
    private async void SendClick(bool leftClick, bool rightClick)
    {
        if (_bluetoothManager.Characteristic != null)
        {
            List<byte> message = new List<byte>();

            if (leftClick)
            {
                message.Add(0x03);
                message.Add(1); // 1 = Click, 0 = No Click
            }

            if (rightClick)
            {
                message.Add(0x04);
                message.Add(1); // 1 = Click, 0 = No Click
            }

            if (message.Count > 0)
            {
                await _bluetoothManager.Characteristic.WriteAsync(message.ToArray());
                Console.WriteLine($"Sent Click: Left={leftClick}, Right={rightClick}");
            }
        }
        else
        {
            Console.WriteLine("Send failed (click): no connection");
        }
    }

}