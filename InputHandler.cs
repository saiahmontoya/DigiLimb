using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

class InputCapture
{
	private ClientService _client;

	public InputCapture(ClientService client)
	{
		_client = client;
	}

	public async Task OnKeyPress(string key)
	{
		await _client.SendInput($"KEY:{key}");
	}

	public async Task OnMouseMove(int dx, int dy)
	{
		await _client.SendInput($"MOUSE:{dx},{dy}");
	}
}

///InputHandler.cs
using System.Runtime.InteropServices;

#if WINDOWS
class KeyboardSimulator
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    public static void PressKey(string key)
    {
        byte keyCode = (byte)Enum.Parse(typeof(ConsoleKey), key);
        keybd_event(keyCode, 0, 0, 0); // Key Down
        keybd_event(keyCode, 0, 2, 0); // Key Up
    }
}

class MouseSimulator
{
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_MOVE = 0x0001;

    public static void MoveCursor(int dx, int dy)
    {
        mouse_event(MOUSEEVENTF_MOVE, dx, dy, 0, IntPtr.Zero);
    }
}
#endif

///ServerService.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public static class NetworkHelper
{
	public static string GetLocalIPAddress()
	{
		var host = Dns.GetHostEntry(Dns.GetHostName());
		foreach (var ip in host.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4 Only
			{
				return ip.ToString();
			}
		}
		throw new Exception("No network adapters with an IPv4 address found.");
	}
}

public static class UdpBroadcaster
{
	public static async Task BroadcastServerIP(int port = 8888)
	{
		string localIP = NetworkHelper.GetLocalIPAddress();
		using UdpClient udpClient = new UdpClient();
		udpClient.EnableBroadcast = true;

		IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, port);
		byte[] data = Encoding.UTF8.GetBytes(localIP);

		while (true)
		{
			await udpClient.SendAsync(data, data.Length, endPoint);
			await Task.Delay(5000); // Broadcast every 5 seconds
		}
	}
}

public class ServerService
{
	private TcpListener _server;
	private bool _isRunning = false;

	public async Task StartServer(int port = 5000)
	{
		string localIP = NetworkHelper.GetLocalIPAddress();
		Console.WriteLine($"Server started on {localIP}:{port}");
		_server = new TcpListener(IPAddress.Any, port);
		_server.Start();
		_isRunning = true;
		Console.WriteLine("Server started. Waiting for connections...");

		while (_isRunning)
		{
			TcpClient client = await _server.AcceptTcpClientAsync();
			Console.WriteLine("Client connected!");
			_ = Task.Run(() => HandleClient(client));
		}
	}

	private async Task HandleClient(TcpClient client)
	{
		NetworkStream stream = client.GetStream();
		byte[] buffer = new byte[1024];

		while (client.Connected)
		{
			int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			if (bytesRead == 0) break;

			string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			Console.WriteLine($"Received: {data}");
		}

		Console.WriteLine("Client disconnected.");
		client.Close();
	}
}
