using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DigiLimbDesktop
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // ✅ Ensure ViGEmBus is installed before the app starts
            Task.Run(async () =>
            {
                Debug.WriteLine("🔍 Checking for ViGEmBus installation...");
                await ViGEmDriverManager.EnsureViGEmBusInstalled();
            }).Wait(); // Block until installation is complete.

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit() // If you're using MAUI Toolkit
                .UseBarcodeReader()  // ✅ Register the barcode reader here
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
