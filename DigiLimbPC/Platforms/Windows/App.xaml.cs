using System.Threading;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml;
using DigiLimbDesktop;
namespace DigiLimbDesktop.WinUI
{
    public partial class App : MauiWinUIApplication
    {

        public App()
        {
            InitializeComponent();
            LogThreadInfo("Constructor");
        }

        
        protected override MauiApp CreateMauiApp()
        {
            LogThreadInfo("CreateMauiApp");
            Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}, Is UI Thread: {Thread.CurrentThread.IsBackground == false}");

            return MauiProgram.CreateMauiApp();
        }

        

        /// <summary>
        /// Logs the current thread information.
        /// </summary>
        public static void LogThreadInfo(string context)
        {
            string threadInfo = $"Thread ID: {Thread.CurrentThread.ManagedThreadId}, Is UI Thread: {Thread.CurrentThread.IsBackground == false}";
            Console.WriteLine($"[{context}] {threadInfo}");
        }
    }
}
