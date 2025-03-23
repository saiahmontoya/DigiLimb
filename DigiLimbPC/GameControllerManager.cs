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
                Debug.WriteLine("🧠 REACHED ENUM SWITCH. EXECUTING MAPPING...");

                Xbox360Button? button = cleanInput switch
                {
                    "A" => Xbox360Button.A,
                    "B" => Xbox360Button.B,
                    "X" => Xbox360Button.X,
                    "Y" => Xbox360Button.Y,
                    "⬆" => Xbox360Button.Up,
                    "⬇" => Xbox360Button.Down,
                    "⬅" => Xbox360Button.Left,
                    "➡" => Xbox360Button.Right,
                    _ => null
                };

                if (button != null)
                {
                    var enumType = button.Value.GetType().FullName;
                    Debug.WriteLine($"🎮 Sending button press for: {button.Value} ({(int)button.Value})");
                    Debug.WriteLine($"🧪 Enum type: {enumType}");
                    Debug.WriteLine($"✅ ENUM RESULT: {button.Value}, Raw: {(int)button.Value}");
                    Debug.WriteLine("🧩 Method called from: " + new StackTrace().GetFrame(0)?.GetMethod()?.DeclaringType?.FullName);

                    _controller.SetButtonState(button, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(button, false);
                }
                else
                {
                    Debug.WriteLine($"⚠️ Unknown or unsupported input: {cleanInput}");
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
            Debug.WriteLine($"📥 Raw Message Received: {message}");

            if (message.StartsWith("CONTROLLER:"))
            {
                if (message.Length <= "CONTROLLER:".Length)
                {
                    Debug.WriteLine("⚠️ Malformed controller input: No button value provided.");
                    return;
                }

                string input = message.Substring("CONTROLLER:".Length).Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Debug.WriteLine("⚠️ Empty controller input after parsing.");
                    return;
                }

                _ = ProcessControllerInput(input);
                return;
            }

            try
            {
                var json = JsonSerializer.Deserialize<JoystickPayload>(message);
                if (json?.Type == "joystick")
                {
                    Debug.WriteLine($"🎮 Parsed Joystick Input: X={json.X}, Y={json.Y}, Button={json.ButtonPressed}");
                    SimulateJoystickMovement(json.X, json.Y, json.ButtonPressed);
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"⚠️ Invalid joystick JSON: {ex.Message}");
            }
        }

        private void SimulateJoystickMovement(float x, float y, bool buttonPressed)
        {
#if WINDOWS
            if (!_isInitialized || _controller == null) return;

            short lx = (short)(x * short.MaxValue);
            short ly = (short)(-y * short.MaxValue); // Y is inverted

            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, lx);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, ly);

            Debug.WriteLine($"🕹️ [Mapped] LeftStick X={lx}, Y={ly}");
#endif
        }

        private class JoystickPayload
        {
            public string Type { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public bool ButtonPressed { get; set; }
        }
    }
}
