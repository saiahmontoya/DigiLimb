using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
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
        private readonly int _port = 8080; // Fixed port for WebSocket server

        public bool IsRunning { get; private set; } = false;
        public int ConnectedClientCount => _clients.Count;


        // Heartbeat and chat constants
        private const string HEARTBEAT_PING = "PING";
        private const string HEARTBEAT_PONG = "PONG";
        private const string CHAT_PREFIX = "CHAT:";

        public ServerService(Action<string, bool> updateStatusCallback)
        {
            _updateStatusCallback = updateStatusCallback;
            _clients = new List<WebSocket>();
        }

        /// <summary>
        /// Starts the WebSocket server on a fixed port.
        /// </summary>
        public void StartServer()
        {
            if (IsRunning)
            {
                Debug.WriteLine("⚠️ WebSocket Server is already running!");
                return;
            }

            IsRunning = true; // ✅ Now correctly tracks if the server is running
            Task.Run(() => StartListener());
        }

        private async Task StartListener()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _httpListener = new HttpListener();
                string prefix = $"http://+:{_port}/";
                _httpListener.Prefixes.Add(prefix);
                _httpListener.Start();

                string localIP = GetLocalIPAddress();
                _updateStatusCallback?.Invoke($"✅ WebSocket Server Running on {localIP}:{_port}", true);
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
                IsRunning = false; // Reset status
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

        private async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            try
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                WebSocket webSocket = wsContext.WebSocket;
                _clients.Add(webSocket);

                string clientIP = context.Request.RemoteEndPoint.ToString();
                _updateStatusCallback?.Invoke($"🔵 Client Connected: {clientIP}", true);
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
                        _updateStatusCallback?.Invoke("🔴 Client disconnected", true);
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
                        else if (message.StartsWith(CHAT_PREFIX))
                        {
                            string chatText = message.Substring(CHAT_PREFIX.Length);
                            _updateStatusCallback?.Invoke("Chat from client: " + chatText, true);
                            await BroadcastMessage(message, webSocket);
                        }
                        else
                        {
                            await BroadcastMessage(message, webSocket);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in receive loop: {ex.Message}");
                _clients.Remove(webSocket);
            }
        }
        public async Task SendChatMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            string chatMessage = $"CHAT:{message}";
            await BroadcastMessage(chatMessage, null);
            _updateStatusCallback?.Invoke($"Sent chat: {message}", true);
        }


        /// <summary>
        /// Broadcasts a given message to all connected clients (except the optional sender).
        /// </summary>
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

        /// <summary>
        /// Sends a single message to a specific client.
        /// </summary>
        private async Task SendMessage(WebSocket client, string message)
        {
            if (client != null && client.State == WebSocketState.Open)
            {
                byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        /// <summary>
        /// Sends a screen frame safely over WebSocket.
        /// </summary>
        public async Task BroadcastScreenFrame(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image)) return;

            string frameMessage = $"FRAME_START:{base64Image}:FRAME_END";
            byte[] messageBytes = Encoding.UTF8.GetBytes(frameMessage);
            var segment = new ArraySegment<byte>(messageBytes);

            foreach (var client in _clients)
            {
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error broadcasting frame: {ex.Message}");
                    }
                }
            }
        }


        /// <summary>
        /// Stops the server and disconnects all clients.
        /// </summary>
        public void StopServer()
        {
            try
            {
                IsRunning = false; // ✅ Ensure status is reset

                _cancellationTokenSource?.Cancel();
                _httpListener?.Stop();

                foreach (var client in new List<WebSocket>(_clients))
                {
                    if (client.State == WebSocketState.Open)
                    {
                        client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None).Wait();
                    }
                }

                _clients.Clear();
                _updateStatusCallback?.Invoke("🔴 Server stopped", false);
                Debug.WriteLine("🔴 WebSocket Server stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error stopping server: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the local IPv4 address of the current machine.
        /// </summary>
        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }
}
