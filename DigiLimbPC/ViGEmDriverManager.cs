using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DigiLimbDesktop
{
	public static class ViGEmDriverManager
	{
		private const string ViGEmBusRegistryKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ViGEmBus";
		private const string ViGEmBusDownloadUrl = "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe";
		private const string LocalInstallerPath = "C:\\Temp\\ViGEmBus_Setup.exe";

		public static bool IsViGEmBusInstalled()
		{
			return Registry.GetValue(ViGEmBusRegistryKey, "Start", null) != null;
		}

		public static async Task DownloadViGEmBus()
		{
			if (!Directory.Exists("C:\\Temp"))
			{
				Directory.CreateDirectory("C:\\Temp");
			}

			using (WebClient client = new WebClient())
			{
				Debug.WriteLine("📥 Downloading ViGEmBus...");
				await client.DownloadFileTaskAsync(new Uri(ViGEmBusDownloadUrl), LocalInstallerPath);
				Debug.WriteLine("✅ ViGEmBus download complete.");
			}
		}

		public static void InstallViGEmBus()
		{
			Debug.WriteLine("🚀 Installing ViGEmBus...");
			Process process = new Process();
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = $"/c start /wait \"\" \"{LocalInstallerPath}\" /quiet /norestart";
			process.StartInfo.Verb = "runas"; // Forces admin privileges
			process.StartInfo.UseShellExecute = true;
			process.Start();
			process.WaitForExit();
			Debug.WriteLine("✅ ViGEmBus installation complete.");
		}


		public static async Task EnsureViGEmBusInstalled()
		{
			if (!IsViGEmBusInstalled())
			{
				Debug.WriteLine("⚠️ ViGEmBus not found! Downloading...");
				await DownloadViGEmBus();

				Debug.WriteLine("📥 Installing ViGEmBus...");
				InstallViGEmBus();

				Debug.WriteLine("✅ ViGEmBus installed successfully!");
			}
			else
			{
				Debug.WriteLine("✅ ViGEmBus is already installed.");
			}
		}
	}
}
