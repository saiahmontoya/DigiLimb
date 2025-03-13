using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DigiLimbDesktop
{
    public class ServerService
    {
        private HttpListener _httpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Action<string, bool> _updateStatusCallback;
        private readonly List<WebSocket> _clients;
        private readonly int _port = 8080; // Fixed port for the WebSocket server
#if WINDOWS
        private readonly GameControllerManager _controllerManager;
#endif

        // Heartbeat and chat constants
        private const string HEARTBEAT_PING = "PING";
        private const string HEARTBEAT_PONG = "PONG";
        private const string CHAT_PREFIX = "CHAT:";
        private const string CONTROLLER_PREFIX = "CONTROLLER:";

        public ServerService(Action<string, bool> updateStatusCallback)
        {
            _updateStatusCallback = updateStatusCallback;
            _clients = new List<WebSocket>();
#if WINDOWS
            Task.Run(async () => await ViGEmDriverManager.EnsureViGEmBusInstalled()).Wait();
            _controllerManager = new GameControllerManager();
#endif
        }

        public void StartServer()
        {
            try
            {
                // Ensure admin privileges to modify firewall and HTTP settings
                RunNetshCommand($"http add urlacl url=http://+:{_port}/ user=Everyone");
                RunNetshCommand($"advfirewall firewall add rule name=\"DigiLimb WebSocket\" dir=in action=allow protocol=TCP localport={_port}");

                if (_httpListener != null && _httpListener.IsListening)
                {
                    Debug.WriteLine("⚠️ WebSocket Server is already running!");
                    return;
                }

                Task.Run(() => StartListener());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error configuring network settings: {ex.Message}");
            }
        }

        public async Task StartListener()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _httpListener = new HttpListener();
                string prefix = $"http://+:{_port}/";
                _httpListener.Prefixes.Add(prefix);
                _httpListener.Start();

                string localIP = GetLocalIPAddress();
                _updateStatusCallback?.Invoke($"WebSocket Server Running on {localIP}:{_port}", true);
                Debug.WriteLine($"✅ WebSocket Server started on {localIP}:{_port}");

                Task.Run(() => StartHeartbeatLoop(), _cancellationTokenSource.Token);

                while (_httpListener.IsListening)
                {
                    HttpListenerContext context = await _httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ WebSocket Server error: {ex.Message}");
                _updateStatusCallback?.Invoke("Error starting WebSocket server", false);
            }
        }

        private async Task StartHeartbeatLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(30000, _cancellationTokenSource.Token);
                await BroadcastMessage(HEARTBEAT_PING, null);
                _updateStatusCallback?.Invoke("Sent heartbeat PING", true);
            }
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            try
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                WebSocket webSocket = wsContext.WebSocket;
                _clients.Add(webSocket);

                string clientIP = context.Request.RemoteEndPoint.ToString();
                _updateStatusCallback?.Invoke($"Client Connected: {clientIP}", true);
                Debug.WriteLine($"🔵 Client connected: {clientIP}");

                await ReceiveLoop(webSocket);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error processing WebSocket request: {ex.Message}");
            }
        }

        private async Task ReceiveLoop(WebSocket webSocket)
        {
            var buffer = new byte[1024];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        _clients.Remove(webSocket);
                        _updateStatusCallback?.Invoke("Client disconnected", true);
                        Debug.WriteLine("🔴 Client disconnected.");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.WriteLine($"📥 Received: {message}");

                        if (message == HEARTBEAT_PONG)
                        {
                            _updateStatusCallback?.Invoke("Received heartbeat PONG", true);
                        }
                        else if (message == HEARTBEAT_PING)
                        {
                            await SendMessage(webSocket, HEARTBEAT_PONG);
                        }
                        else if (message.StartsWith(CHAT_PREFIX))
                        {
                            string chatText = message.Substring(CHAT_PREFIX.Length);
                            _updateStatusCallback?.Invoke("Chat from client: " + chatText, true);
                            await BroadcastMessage(message, webSocket);
                        }
                        else if (message.StartsWith(CONTROLLER_PREFIX))
                        {
                            await ProcessControllerInput(webSocket, message);
                        }

                        // Keep existing broadcast functionality
                        await BroadcastMessage(message, webSocket);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in receive loop: {ex.Message}");
                _clients.Remove(webSocket);
            }
        }

        private async Task ProcessControllerInput(WebSocket webSocket, string message)
        {
        #if WINDOWS
            if (message.StartsWith("CONTROLLER:"))
            {
                string input = message.Substring("CONTROLLER:".Length);
                _updateStatusCallback?.Invoke($"Game Controller Input: {input}", true);
                Debug.WriteLine($"🎮 Received Controller Input: {input}");

                await _controllerManager.ProcessControllerInput(input); // No error now
            }
        #endif
        }



        private async Task BroadcastMessage(string message, WebSocket sender)
        {
            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(messageBuffer);

            foreach (var client in _clients)
            {
                if (client != sender && client.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error broadcasting to client: {ex.Message}");
                    }
                }
            }
        }

        private async Task SendMessage(WebSocket client, string message)
        {
            if (client != null && client.State == WebSocketState.Open)
            {
                byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async Task SendChatMessage(string message)
        {
            string chatMessage = CHAT_PREFIX + message;
            await BroadcastMessage(chatMessage, null);
            _updateStatusCallback?.Invoke("Sent chat: " + message, true);
        }

        public void StopServer()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                if (_httpListener != null && _httpListener.IsListening)
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                }

                var clientsCopy = new List<WebSocket>(_clients);
                foreach (var client in clientsCopy)
                {
                    try
                    {
                        if (client != null && client.State == WebSocketState.Open)
                        {
                            string disconnectMsg = "Server Disconnecting";
                            byte[] disconnectBuffer = Encoding.UTF8.GetBytes(disconnectMsg);
                            client.SendAsync(new ArraySegment<byte>(disconnectBuffer), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                            Task.Delay(500).Wait();

                            client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error disconnecting client: {ex.Message}");
                    }
                }

                // Revert network changes
                RunNetshCommand($"http delete urlacl url=http://+:{_port}/");
                RunNetshCommand($"advfirewall firewall delete rule name=\"DigiLimb WebSocket\" protocol=TCP localport={_port}");

                _clients.Clear();

                _updateStatusCallback?.Invoke("Server Session Ended", false);
                Debug.WriteLine("🔴 WebSocket Server stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error stopping server: {ex.Message}");
            }
        }

        private void RunNetshCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"Start-Process 'netsh' -ArgumentList '{command}' -Verb RunAs\"",
                    RedirectStandardOutput = false, // Can't redirect due to UAC prompt
                    RedirectStandardError = false,
                    UseShellExecute = true, // Needed for UAC elevation
                    CreateNoWindow = true // Prevents flashing console window
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    process.WaitForExit();
                }

                Debug.WriteLine($"✅ netsh command executed with admin privileges: {command}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ netsh execution failed: {ex.Message}");
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !ip.ToString().StartsWith("127"))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting local IP: {ex.Message}");
            }
            return "127.0.0.1";
        }
    }
}
