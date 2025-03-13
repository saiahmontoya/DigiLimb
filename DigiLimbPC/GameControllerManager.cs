using System.Diagnostics;
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
            if (!_isInitialized) return Task.CompletedTask; // ✅ MUST RETURN A TASK

            Debug.WriteLine($"🎮 Processing Controller Input: {input}");

            switch (input.ToUpper())
            {
                case "A":
                    _controller.SetButtonState(Xbox360Button.A, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(Xbox360Button.A, false);
                    break;
                case "B":
                    _controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(Xbox360Button.B, false);
                    break;
                case "X":
                    _controller.SetButtonState(Xbox360Button.X, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(Xbox360Button.X, false);
                    break;
                case "Y":
                    _controller.SetButtonState(Xbox360Button.Y, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(Xbox360Button.Y, false);
                    break;
                case "⬆":
                    _controller.SetButtonState(Xbox360Button.Up, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(Xbox360Button.Up, false);
                    break;
                case "⬇":
                    _controller.SetButtonState(Xbox360Button.Down, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(Xbox360Button.Down, false);
                    break;
                case "⬅":
                    _controller.SetButtonState(Xbox360Button.Left, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(Xbox360Button.Left, false);
                    break;
                case "➡":
                    _controller.SetButtonState(Xbox360Button.Right, true);
                    Task.Delay(100).Wait();
                    _controller.SetButtonState(Xbox360Button.Right, false);
                    break;
                default:
                    Debug.WriteLine($"⚠️ Unknown button: {input}");
                    break;
            }
#endif
            return Task.CompletedTask; // ✅ MUST RETURN A TASK
        }
    }
}
