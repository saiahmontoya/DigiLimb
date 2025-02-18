using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class NetworkHelper
{
    /// <summary>
    /// Retrieves the local IP address of the machine.
    /// </summary>
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

public class ServerService
{
    private TcpListener _server;
    private bool _isRunning = false;
    private int _serverPort;
    private CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Starts the server on an available port.
    /// </summary>
    public async Task StartServer(int startPort = 5000, int endPort = 6000)
    {
        if (_isRunning)
        {
            Console.WriteLine("⚠️ Server is already running.");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _cancellationTokenSource.Token;

        string localIP = NetworkHelper.GetLocalIPAddress();
        _serverPort = FindAvailablePort(startPort, endPort);

        Console.WriteLine($"🚀 Attempting to start server on {localIP}:{_serverPort}");

        try
        {
            _server = new TcpListener(IPAddress.Any, _serverPort);
            _server.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _server.Start();
            _isRunning = true;

            Console.WriteLine($"✅ Server started on port {_serverPort}. Waiting for connections...");

            // Run the listener loop in a background task
            _ = Task.Run(async () => await AcceptConnectionsAsync(token), token);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"❌ Server start failed: {ex.Message}");
            _isRunning = false;
        }
    }

    /// <summary>
    /// Accepts client connections asynchronously.
    /// </summary>
    private async Task AcceptConnectionsAsync(CancellationToken token)
    {
        try
        {
            while (_isRunning)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("⚠️ Server is stopping, canceling new connections.");
                    return;
                }

                try
                {
                    Console.WriteLine("🔍 Waiting for client connections...");

                    // Accept client with a timeout of 10 seconds to avoid hanging
                    TcpClient client = await Task.Run(() => _server.AcceptTcpClientAsync(), token);

                    if (!_isRunning || token.IsCancellationRequested)
                    {
                        client.Close();
                        return;
                    }

                    Console.WriteLine("🔗 Client connected!");
                    _ = Task.Run(() => HandleClient(client, token));
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("⚠️ Server stopped while waiting for a connection.");
                    return;
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("⚠️ Server listener disposed.");
                    return;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"❌ SocketException while accepting client: {ex.Message}");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AcceptConnectionsAsync Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds an available port within the given range.
    /// </summary>
    private int FindAvailablePort(int startPort, int endPort)
    {
        for (int port = startPort; port <= endPort; port++)
        {
            if (IsPortAvailable(port))
                return port;
        }
        throw new Exception("❌ No available ports found.");
    }

    /// <summary>
    /// Checks if a port is available.
    /// </summary>
    private bool IsPortAvailable(int port)
    {
        try
        {
            using (Socket testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                testSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                return true;
            }
        }
        catch (SocketException)
        {
            return false; // Port is in use
        }
    }

    /// <summary>
    /// Handles a connected client by reading incoming messages.
    /// </summary>
    private async Task HandleClient(TcpClient client, CancellationToken token)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (client.Connected && !token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0) break;

                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"📩 Received: {data}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⚠️ Client operation canceled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Client error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("🔌 Client disconnected.");
            client.Close();
        }
    }

    /// <summary>
    /// Stops the server and releases the port immediately.
    /// </summary>
    public void StopServer()
    {
        if (_isRunning)
        {
            _isRunning = false;

            try
            {
                _cancellationTokenSource?.Cancel();

                // Small delay to ensure all async tasks exit
                Task.Delay(500).Wait();

                _server?.Stop();
                _server = null;
                Console.WriteLine("🛑 Server stopped. Port released.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error stopping server: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ No server is running.");
        }
    }
}
