using System.Diagnostics;
using System.Text.Json;
#if WINDOWS
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Xbox360Button = Nefarius.ViGEm.Client.Targets.Xbox360.Xbox360Button;
#endif

namespace DigiLimbDesktop
{
    public class GameControllerManager
    {
#if WINDOWS
        private ViGEmClient _viGEmClient;
        private IXbox360Controller _controller;
        private bool _isInitialized = false;

        private short _leftX = 0;
        private short _leftY = 0;
        private short _rightX = 0;
        private short _rightY = 0;
#endif

        public GameControllerManager()
        {
#if WINDOWS
            if (ViGEmDriverManager.IsViGEmBusInstalled())
            {
                try
                {
                    _viGEmClient = new ViGEmClient();
                    _controller = _viGEmClient.CreateXbox360Controller();
                    _controller.Connect();
                    _isInitialized = true;
                    Debug.WriteLine("✅ ViGEm Xbox 360 controller initialized and connected.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Failed to initialize ViGEmClient: {ex.Message}");
                    _isInitialized = false;
                }
            }
            else
            {
                Debug.WriteLine("⚠️ ViGEmBus is not installed. Skipping GameControllerManager initialization.");
                _isInitialized = false;
            }
#endif
        }

        public Task ProcessControllerInput(string input)
        {
#if WINDOWS
            if (!_isInitialized || _controller == null) return Task.CompletedTask;

            try
            {
                string cleanInput = input.ToUpper().Trim();
                Debug.WriteLine($"🧼 Sanitized input: '{cleanInput}'");

                if (cleanInput.StartsWith("JOYSTICKS:"))
                {
                    var parts = cleanInput.Substring("JOYSTICKS:".Length).Split(':');
                    if (parts.Length < 8)
                    {
                        Debug.WriteLine("⚠️ Incomplete joystick data.");
                        return Task.CompletedTask;
                    }

                    float lx = 0, ly = 0, rx = 0, ry = 0;

                    for (int i = 0; i < parts.Length - 1; i += 2)
                    {
                        if (parts[i] == "LX") float.TryParse(parts[i + 1], out lx);
                        else if (parts[i] == "LY") float.TryParse(parts[i + 1], out ly);
                        else if (parts[i] == "RX") float.TryParse(parts[i + 1], out rx);
                        else if (parts[i] == "RY") float.TryParse(parts[i + 1], out ry);
                    }

                    SimulateJoystickMovement(lx, ly, "left");
                    SimulateJoystickMovement(rx, ry, "right");
                    return Task.CompletedTask;
                }

                var partsAlt = cleanInput.Split(':');
                if (partsAlt.Length < 2)
                {
                    Debug.WriteLine("⚠️ Invalid input format.");
                    return Task.CompletedTask;
                }

                string type = partsAlt[0];

                if (type == "LT" || type == "RT")
                {
                    if (byte.TryParse(partsAlt[1], out byte triggerValue))
                    {
                        SimulateTriggerPressure(type, triggerValue);
                        Debug.WriteLine($"🎯 {type} Trigger Pressure: {triggerValue}");
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ Invalid trigger value for {type}: {partsAlt[1]}");
                    }
                    return Task.CompletedTask;
                }
                else if ((type == "LEFT" || type == "RIGHT") && partsAlt.Length >= 4)
                {
                    float x = 0, y = 0;
                    for (int i = 1; i < partsAlt.Length - 1; i += 2)
                    {
                        if (partsAlt[i] == "X" && float.TryParse(partsAlt[i + 1], out float parsedX))
                            x = parsedX;
                        else if (partsAlt[i] == "Y" && float.TryParse(partsAlt[i + 1], out float parsedY))
                            y = parsedY;
                    }
                    SimulateJoystickMovement(x, y, id: type.ToLower());
                    Debug.WriteLine($"🎯 {type} joystick X={x}, Y={y}");
                    return Task.CompletedTask;
                }
                else
                {
                    if (partsAlt.Length != 2)
                    {
                        Debug.WriteLine("⚠️ Invalid button format.");
                        return Task.CompletedTask;
                    }

                    string buttonStr = partsAlt[0];
                    string stateStr = partsAlt[1];

                    Xbox360Button? button = buttonStr switch
                    {
                        "A" => Xbox360Button.A,
                        "B" => Xbox360Button.B,
                        "X" => Xbox360Button.X,
                        "Y" => Xbox360Button.Y,
                        "UP" => Xbox360Button.Up,
                        "DOWN" => Xbox360Button.Down,
                        "LEFT" => Xbox360Button.Left,
                        "RIGHT" => Xbox360Button.Right,
                        "START" => Xbox360Button.Start,
                        "BACK" => Xbox360Button.Back,
                        "LB" => Xbox360Button.LeftShoulder,
                        "RB" => Xbox360Button.RightShoulder,
                        "LS" => Xbox360Button.LeftThumb,
                        "RS" => Xbox360Button.RightThumb,
                        _ => null
                    };

                    if (button == null)
                    {
                        Debug.WriteLine($"⚠️ Unknown or unsupported button: {buttonStr}");
                        return Task.CompletedTask;
                    }

                    bool isPressed = stateStr == "DOWN";
                    _controller.SetButtonState(button, isPressed);
                    Debug.WriteLine($"🎮 Button {button.Value}: {(isPressed ? "Pressed" : "Released")}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in ProcessControllerInput: {ex.Message}");
            }
#endif
            return Task.CompletedTask;
        }

        public void HandleIncomingMessage(string message)
        {
#if WINDOWS
            Debug.WriteLine($"📥 Raw Message Received: {message}");

            if (message.StartsWith("CONTROLLER:"))
            {
                string input = message.Substring("CONTROLLER:".Length).Trim();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    _ = ProcessControllerInput(input);
                }
                else
                {
                    Debug.WriteLine("⚠️ Empty controller input after parsing.");
                }
            }
#endif
        }

        private void SimulateJoystickMovement(float x, float y, string id = "left")
        {
#if WINDOWS
            if (!_isInitialized || _controller == null) return;

            short axisX = (short)(x * short.MaxValue);
            short axisY = (short)(-y * short.MaxValue);

            if (id == "right")
            {
                _rightX = axisX;
                _rightY = axisY;
            }
            else
            {
                _leftX = axisX;
                _leftY = axisY;
            }

            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, _leftX);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, _leftY);
            _controller.SetAxisValue(Xbox360Axis.RightThumbX, _rightX);
            _controller.SetAxisValue(Xbox360Axis.RightThumbY, _rightY);
#endif
        }

        private void SimulateTriggerPressure(string triggerId, byte value)
        {
#if WINDOWS
            if (!_isInitialized || _controller == null) return;

            if (triggerId == "LT")
            {
                _controller.SetSliderValue(Xbox360Slider.LeftTrigger, value);
            }
            else if (triggerId == "RT")
            {
                _controller.SetSliderValue(Xbox360Slider.RightTrigger, value);
            }
#endif
        }
    }
}