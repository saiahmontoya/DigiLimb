using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DigiLimbDesktop
{
    public class ServerService
    {
        private TcpListener _server;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Action<string, bool> _updateStatusCallback;
        private UdpClient _udpBroadcaster;
        private const int BROADCAST_PORT = 8890; // 🔥 FIXED BROADCAST PORT
        private int _serverPort;

        public ServerService(Action<string, bool> updateStatusCallback)
        {
            _updateStatusCallback = updateStatusCallback;
        }

        public void StartServer()
        {
            if (_server != null)
            {
                Debug.WriteLine("⚠️ Server is already running!");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    _serverPort = FindOpenPort(); // ✅ Choose a dynamic open port
                    _server = new TcpListener(IPAddress.Any, _serverPort);
                    _server.Start();
                    _cancellationTokenSource = new CancellationTokenSource();

                    _updateStatusCallback?.Invoke($"Server Running on port {_serverPort}", true);
                    Debug.WriteLine($"✅ Server started on port {_serverPort}");

                    // ✅ Start broadcasting the IP & Port
                    StartBroadcasting(_serverPort);
                    PeriodicServerCheck(); // 🔍 Check if the server is actually listening

                    AcceptClientsAsync(_cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Server error: {ex.Message}");
                    _updateStatusCallback?.Invoke("Error starting server", false);
                }
            });
        }
        private async void PeriodicServerCheck()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (_server.Server.IsBound)
                        {
                            Debug.WriteLine($"🟢 Server is actively listening on port {_serverPort}");
                        }
                        else
                        {
                            Debug.WriteLine("🔴 Server is NOT listening. Something went wrong!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Periodic server check failed: {ex.Message}");
                    }

                    Thread.Sleep(5000); // Check every 5 seconds
                }
            });
        }


        private async void AcceptClientsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    Debug.WriteLine($"🔵 Client connected: {clientIP}");

                    _updateStatusCallback?.Invoke($"Client Connected: {clientIP}", true);

                    // ✅ Handle incoming messages
                    HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Client accept error: {ex.Message}");
            }
        }

        private async void HandleClientAsync(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];

                while (true) // ✅ Keep listening for messages
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // 🔴 Client disconnected

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.WriteLine($"📥 Received from client: {message}");

                    // ✅ Respond to PING
                    if (message == "PING")
                    {
                        byte[] response = Encoding.UTF8.GetBytes("PONG");
                        await stream.WriteAsync(response, 0, response.Length);
                        Debug.WriteLine("✅ Sent PONG to client.");
                    }
                }

                Debug.WriteLine("🔴 Client disconnected.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error handling client message: {ex.Message}");
            }
        }

        private void StartBroadcasting(int port)
        {
            Task.Run(async () =>
            {
                try
                {
                    _udpBroadcaster = new UdpClient();
                    _udpBroadcaster.EnableBroadcast = true;

                    string localIP = GetLocalIPAddress();
                    string message = $"{localIP}:{port}";

                    while (true)
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        await _udpBroadcaster.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT));
                        Debug.WriteLine($"📡 Broadcasting server info: {message} on {BROADCAST_PORT}");
                        await Task.Delay(5000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Broadcast error: {ex.Message}");
                }
            });
        }


        private string GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127"))
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        public void StopServer()
        {
            _cancellationTokenSource?.Cancel();
            _server?.Stop();
            _udpBroadcaster?.Close();
            _updateStatusCallback?.Invoke("Server Stopped", false);
            Debug.WriteLine("🔴 Server stopped.");
        }

        private int FindOpenPort()
        {
            int port = 8891; // Hardcoded port to ensure client discovery
            bool isPortAvailable = false;

            while (!isPortAvailable)
            {
                try
                {
                    using (TcpListener listener = new TcpListener(IPAddress.Loopback, port))
                    {
                        listener.Start();
                        isPortAvailable = true;
                    }
                }
                catch (SocketException)
                {
                    Debug.WriteLine($"⚠️ Port {port} is in use. Trying next port...");
                    port++;
                }
            }

            Debug.WriteLine($"✅ Selected open port: {port}");
            return port;
        }

    }
}
