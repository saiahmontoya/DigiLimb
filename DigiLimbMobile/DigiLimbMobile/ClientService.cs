using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DigiLimbMobile
{
    public class ClientService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private string _serverIp;
        private int _serverPort;
        private readonly Action<string> _updateStatusCallback;
        private const int DISCOVERY_PORT = 8890;
        private bool _isConnected = false;
        private CancellationTokenSource _cancellationTokenSource;

        public ClientService(Action<string> updateStatusCallback)
        {
            _updateStatusCallback = updateStatusCallback;
        }

        /// <summary>
        /// Listens for server broadcasts and retrieves the IP and port.
        /// </summary>
        public async Task DiscoverServer()
        {
            try
            {
                // Manually set the server IP and port for testing
                _serverIp = "172.24.240.1";  // 🔥 Replace with your actual server IP
                _serverPort = 8891;          // 🔥 Ensure this matches the server port

                Debug.WriteLine($"✅ Manually set Server IP: {_serverIp}:{_serverPort}");
                _updateStatusCallback?.Invoke($"Manually Set Server: {_serverIp}:{_serverPort}");

                return; // 🔥 Skip the UDP discovery process and use manual settings
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Discovery error: {ex.Message}");
                _updateStatusCallback?.Invoke("Server not found.");
            }
        }



        /// <summary>
        /// Connects to the discovered server.
        /// </summary>
        public async Task<bool> ConnectToServer()
        {
            if (string.IsNullOrEmpty(_serverIp))
            {
                await DiscoverServer();
            }

            if (!string.IsNullOrEmpty(_serverIp))
            {
                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_serverIp, _serverPort);
                    _stream = _client.GetStream();

                    // ✅ Send "PING" to verify connection
                    byte[] pingMessage = Encoding.UTF8.GetBytes("PING");
                    await _stream.WriteAsync(pingMessage, 0, pingMessage.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (response == "PONG")
                    {
                        _updateStatusCallback?.Invoke("Connected to server!");
                        Debug.WriteLine("✅ Connection confirmed with server.");
                        return true;
                    }
                    else
                    {
                        _updateStatusCallback?.Invoke("Failed to verify server connection.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Connection error: {ex.Message}");
                    _updateStatusCallback?.Invoke("Connection failed.");
                    return false;
                }
            }
            return false;
        }


        /// <summary>
        /// Listens for messages from the server continuously.
        /// </summary>
        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) break; // 🔴 Server closed connection

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.WriteLine($"📥 Received: {message}");
                    _updateStatusCallback?.Invoke($"Received: {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Receive error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        public async Task SendMessage(string message)
        {
            if (_client == null || !_client.Connected)
            {
                Debug.WriteLine("❌ Not connected to the server.");
                return;
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(buffer, 0, buffer.Length);
                Debug.WriteLine($"📤 Sent: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Send error: {ex.Message}");
                _updateStatusCallback?.Invoke("Failed to send message.");
            }
        }

        /// <summary>
        /// Disconnects from the server and stops listening for messages.
        /// </summary>
        public void Disconnect()
        {
            _isConnected = false;
            _cancellationTokenSource?.Cancel();
            _client?.Close();
            _client = null;
            _updateStatusCallback?.Invoke("Disconnected.");
            Debug.WriteLine("🔴 Client disconnected.");
        }
    }
}
