using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public static class UdpListener
{
    public static async Task<string> DiscoverServerIP(int port = 8888)
    {
        using UdpClient udpClient = new UdpClient(port);
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

        Console.WriteLine("Listening for server broadcasts...");
        UdpReceiveResult result = await udpClient.ReceiveAsync();

        string serverIP = Encoding.UTF8.GetString(result.Buffer);
        Console.WriteLine($"Discovered server IP: {serverIP}");

        return serverIP;
    }
}
public class ClientService
{
    private TcpClient _client;
    private NetworkStream _stream;

    public async Task ConnectToServer(string serverIp, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(serverIp, port);
            _stream = _client.GetStream();
            Console.WriteLine("Connected to Server.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }
    }

    public async Task SendInput(string data)
    {
        if (_stream == null) return;

        byte[] buffer = Encoding.UTF8.GetBytes(data);
        await _stream.WriteAsync(buffer, 0, buffer.Length);
    }
}