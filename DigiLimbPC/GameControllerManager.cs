using System.Diagnostics;
using System.Text.Json;
#if WINDOWS
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
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
            if (!_isInitialized) return Task.CompletedTask;

            Debug.WriteLine($"🎮 Processing Controller Input: {input}");

            Xbox360Button? button = input.ToUpper() switch
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
                _controller.SetButtonState(button.Value, true);
                Task.Delay(100).Wait();
                _controller.SetButtonState(button.Value, false);
            }
            else
            {
                Debug.WriteLine($"⚠️ Unknown button: {input}");
            }
#endif
            return Task.CompletedTask;
        }

        public void HandleIncomingMessage(string message)
        {
            Debug.WriteLine($"📥 Raw Message Received: {message}");

            if (message.StartsWith("CONTROLLER:"))
            {
                string input = message.Substring("CONTROLLER:".Length).Trim();
                _ = ProcessControllerInput(input); // trigger button input
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
            if (!_isInitialized) return;

            // Convert float range [-1, 1] to short range [-32768, 32767]
            short lx = (short)(x * short.MaxValue);
            short ly = (short)(-y * short.MaxValue); // Y is inverted in most gamepad APIs

            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, lx);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, ly);

            Debug.WriteLine($"🕹️ [Mapped] LeftStick X={lx}, Y={ly}");

            // Optionally simulate button press if joystick button is flagged
            if (buttonPressed)
            {
                Debug.WriteLine("🎮 Simulating Joystick Button Press: A");
                _controller.SetButtonState(Xbox360Button.A, true);
                Task.Delay(100).Wait();
                _controller.SetButtonState(Xbox360Button.A, false);
            }
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
