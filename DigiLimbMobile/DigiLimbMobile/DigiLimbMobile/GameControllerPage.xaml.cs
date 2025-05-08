using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MauiTimer = System.Timers.Timer;

namespace DigiLimbMobile
{
    public partial class GameControllerPage : ContentPage
    {
        private readonly Dictionary<string, MauiTimer> _buttonTimers = new();
        private Microsoft.Maui.Controls.View _leftThumb, _rightThumb;
        private AbsoluteLayout _leftJoystickBase, _rightJoystickBase;

        private MauiTimer _joystickSendTimer;
        private float _leftX, _leftY, _rightX, _rightY;

        private bool _isPanning = false;
        private bool _leftStickPressed = false;
        private bool _rightStickPressed = false;
        private const float StickPressThreshold = 0.2f;
        private const float MovementThreshold = 0.01f;

        public GameControllerPage()
        {
            InitializeComponent();

#if ANDROID
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;
#endif
            SetupJoysticks();
        }

        private void OnButtonDown(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            string buttonText = btn.Text.ToUpper();
            if (_buttonTimers.ContainsKey(buttonText)) return;

            if (buttonText == "LT" || buttonText == "RT")
            {
                _ = SendTriggerPressure(buttonText, 255);
            }
            else
            {
                _ = SendControllerButtonEvent(buttonText, "DOWN");
            }

            var timer = new MauiTimer(100) { AutoReset = true };
            timer.Elapsed += async (_, _) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (buttonText == "LT" || buttonText == "RT")
                    {
                        _ = SendTriggerPressure(buttonText, 255);
                    }
                    else
                    {
                        await SendControllerButtonEvent(buttonText, "DOWN");
                    }
                });
            };

            _buttonTimers[buttonText] = timer;
            timer.Start();
        }

        private void OnButtonUp(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            string buttonText = btn.Text.ToUpper();

            if (buttonText == "LT" || buttonText == "RT")
            {
                _ = SendTriggerPressure(buttonText, 0);
            }
            else
            {
                _ = SendControllerButtonEvent(buttonText, "UP");
            }

            if (_buttonTimers.TryGetValue(buttonText, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                _buttonTimers.Remove(buttonText);
            }
        }

        private async Task SendControllerButtonEvent(string button, string state)
        {
            if (App.GlobalWebSocket?.State != WebSocketState.Open) return;

            string data = $"CONTROLLER:{button}:{state}";
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            await App.GlobalWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void SetupJoysticks()
        {
            _leftJoystickBase = this.FindByName<AbsoluteLayout>("LeftJoystickBase");
            _rightJoystickBase = this.FindByName<AbsoluteLayout>("RightJoystickBase");

            _leftThumb = this.FindByName<Microsoft.Maui.Controls.View>("LeftThumb");
            _rightThumb = this.FindByName<Microsoft.Maui.Controls.View>("RightThumb");

            _leftThumb.WidthRequest = 50;
            _leftThumb.HeightRequest = 50;
            _rightThumb.WidthRequest = 50;
            _rightThumb.HeightRequest = 50;

            var leftPan = new PanGestureRecognizer();
            leftPan.PanUpdated += (s, e) => HandleJoystickPan(e, _leftJoystickBase, _leftThumb, "left");
            _leftJoystickBase.GestureRecognizers.Add(leftPan);
            _leftThumb.GestureRecognizers.Add(leftPan);

            var rightPan = new PanGestureRecognizer();
            rightPan.PanUpdated += (s, e) => HandleJoystickPan(e, _rightJoystickBase, _rightThumb, "right");
            _rightJoystickBase.GestureRecognizers.Add(rightPan);
            _rightThumb.GestureRecognizers.Add(rightPan);
        }

        private async void HandleJoystickPan(PanUpdatedEventArgs e, AbsoluteLayout baseLayout, Microsoft.Maui.Controls.View thumb, string id)
        {
            const double ThumbSize = 50;
            if (baseLayout.Width == 0 || baseLayout.Height == 0) return;

            double radius = baseLayout.Width / 2;
            double centerX = radius, centerY = radius;
            double maxDist = radius - (ThumbSize / 2);

            if (e.StatusType == GestureStatus.Started)
            {
                StartJoystickTimer();
                _isPanning = true;
            }

            if (e.StatusType == GestureStatus.Running)
            {
                double dx = e.TotalX, dy = e.TotalY;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > maxDist)
                {
                    double ratio = maxDist / distance;
                    dx *= ratio;
                    dy *= ratio;
                }

                double newX = centerX + dx, newY = centerY + dy;

                AbsoluteLayout.SetLayoutBounds(thumb, new Rect(newX - ThumbSize / 2, newY - ThumbSize / 2, ThumbSize, ThumbSize));

                float normX = (float)(dx / maxDist);
                float normY = (float)(dy / maxDist);

                if (id == "left")
                {
                    _leftX = normX;
                    _leftY = normY;
                    if (Math.Sqrt(normX * normX + normY * normY) < StickPressThreshold)
                    {
                        if (!_leftStickPressed)
                        {
                            _leftStickPressed = true;
                            _ = SendControllerButtonEvent("LS", "DOWN");
                        }
                    }
                    else if (_leftStickPressed)
                    {
                        _leftStickPressed = false;
                        _ = SendControllerButtonEvent("LS", "UP");
                    }
                }
                else
                {
                    _rightX = normX;
                    _rightY = normY;
                    if (Math.Sqrt(normX * normX + normY * normY) < StickPressThreshold)
                    {
                        if (!_rightStickPressed)
                        {
                            _rightStickPressed = true;
                            _ = SendControllerButtonEvent("RS", "DOWN");
                        }
                    }
                    else if (_rightStickPressed)
                    {
                        _rightStickPressed = false;
                        _ = SendControllerButtonEvent("RS", "UP");
                    }
                }
            }

            if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                AbsoluteLayout.SetLayoutBounds(thumb, new Rect(centerX - ThumbSize / 2, centerY - ThumbSize / 2, ThumbSize, ThumbSize));

                if (id == "left")
                {
                    _leftX = 0f;
                    _leftY = 0f;
                    if (_leftStickPressed)
                    {
                        _leftStickPressed = false;
                        _ = SendControllerButtonEvent("LS", "UP");
                    }
                }
                else
                {
                    _rightX = 0f;
                    _rightY = 0f;
                    if (_rightStickPressed)
                    {
                        _rightStickPressed = false;
                        _ = SendControllerButtonEvent("RS", "UP");
                    }
                }

                await SendJoystickData(_leftX, _leftY, _rightX, _rightY);
                await Task.Delay(10);
                await SendJoystickData(_leftX, _leftY, _rightX, _rightY);
                StopJoystickTimer();
                _isPanning = false;
            }
        }

        private void StartJoystickTimer()
        {
            if (_joystickSendTimer != null) return;

            _joystickSendTimer = new MauiTimer(30);
            _joystickSendTimer.Elapsed += async (_, _) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (App.GlobalWebSocket?.State == WebSocketState.Open && _isPanning)
                    {
                        await SendJoystickData(_leftX, _leftY, _rightX, _rightY);
                    }
                });
            };
            _joystickSendTimer.AutoReset = true;
            _joystickSendTimer.Start();
        }

        private void StopJoystickTimer()
        {
            _joystickSendTimer?.Stop();
            _joystickSendTimer?.Dispose();
            _joystickSendTimer = null;
        }

        private async Task SendTriggerPressure(string trigger, byte value)
        {
            if (App.GlobalWebSocket?.State != WebSocketState.Open) return;

            string message = $"CONTROLLER:{trigger}:{value}";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await App.GlobalWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendJoystickData(float leftX, float leftY, float rightX, float rightY)
        {
            if (App.GlobalWebSocket?.State != WebSocketState.Open) return;

            string message = $"CONTROLLER:JOYSTICKS:LX:{leftX}:LY:{leftY}:RX:{rightX}:RY:{rightY}";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await App.GlobalWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}