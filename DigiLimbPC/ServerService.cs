#if WINDOWS
using DigiLimbDesktop.Platforms.Windows;
#endif
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
        private readonly int _port = 8080; // Fixed port for the WebSocket server

        // Heartbeat and chat constants
        private const string HEARTBEAT_PING = "PING";
        private const string HEARTBEAT_PONG = "PONG";
        private const string CHAT_PREFIX = "CHAT:";

        // Screen capture fields (optional, can be removed if not needed)
        private bool _isCapturing = false;
        private CancellationTokenSource _captureCts;

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
            if (_httpListener != null && _httpListener.IsListening)
            {
                Debug.WriteLine("⚠️ WebSocket Server is already running!");
                return;
            }
            Task.Run(() => StartListener());
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

                // Get the local IP address
                string localIP = GetLocalIPAddress();
                // Update UI: send a message that the server is running.
                _updateStatusCallback?.Invoke($"WebSocket Server Running on {localIP}:{_port}", true);
                Debug.WriteLine($"✅ WebSocket Server started on {localIP}:{_port}");

                // Start the heartbeat loop in parallel.
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

        /// <summary>
        /// Sends "PING" messages to connected clients every 30 seconds to check connectivity.
        /// </summary>
        private async Task StartHeartbeatLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(30000, _cancellationTokenSource.Token); // 30-second interval
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
                            // A client responded to our ping
                            _updateStatusCallback?.Invoke("Received heartbeat PONG", true);
                        }
                        else if (message == HEARTBEAT_PING)
                        {
                            // A client is pinging us; respond with a pong
                            await SendMessage(webSocket, HEARTBEAT_PONG);
                        }
                        else if (message.StartsWith(CHAT_PREFIX))
                        {
                            // A chat message from client
                            string chatText = message.Substring(CHAT_PREFIX.Length);
                            _updateStatusCallback?.Invoke("Chat from client: " + chatText, true);

                            // Optionally broadcast the chat to all clients (except sender)
                            await BroadcastMessage(message, webSocket);
                        }
                        else
                        {
                            // For any other text message, just broadcast to all connected clients
                            await BroadcastMessage(message, webSocket);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        byte[] rawBytes = buffer.Take(result.Count).ToArray();
                        //Debug.WriteLine($"received binary: {BitConverter.ToString(rawBytes)}");

                        HandleMouseData(rawBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in receive loop: {ex.Message}");
                _clients.Remove(webSocket);
            }
        }

        //Handle received mouse data

        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        private bool isMoving = false;

        private void HandleMouseData(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            double x = 0, y = 0;
            int scroll = 0;
            bool leftClick = false, rightClick = false;

            while (stream.Position < stream.Length)
            {
                byte header = reader.ReadByte();
                //Debug.WriteLine($"reader val {header:X2}");

                switch (header)
                {
                    case 0x01:
                        x = reader.ReadDouble();
                        break;
                    case 0x02:
                        y = reader.ReadDouble();
                        break;
                    case 0x03:
                        leftClick = reader.ReadByte() != 0;
                        break;
                    case 0x04:
                        rightClick = reader.ReadByte() != 0;
                        break;
                    case 0x05:
                        scroll = reader.ReadInt32();
                        //Debug.WriteLine($"scroll detected: {scroll}");
                        break;
                    default:
                        Debug.WriteLine($"⚠️ Unknown header: {header}");
                        break;
                }
            }

            if (x != 0 || y != 0)
            {
                if (!isMoving)
                {
                    isMoving = true;
#if WINDOWS
                    MouseEmulator.StartMouseMovement();
#endif
                }

                double scaleFactor = 1;
                double moveX = x / scaleFactor;
                double moveY = y / scaleFactor;
#if WINDOWS
                MouseEmulator.SimulateMouseMove(moveX, moveY);
#endif
            }
            else if (isMoving)
            {
                isMoving = false;
#if WINDOWS
                MouseEmulator.StopMouseMovement();
#endif
            }

            if (scroll != 0)
            {
                //Debug.WriteLine("scrolling...");
#if WINDOWS
                //Debug.WriteLine($"windows scroll {scroll}");
                MouseEmulator.SimulateMouseScroll(scroll);
#endif
            }

            if (leftClick && !isLeftPressed)
            {
#if WINDOWS
                MouseEmulator.SimulateLeftPress();
#endif
                isLeftPressed = true;
            }
            else if (!leftClick && isLeftPressed)
            {
#if WINDOWS
                MouseEmulator.SimulateLeftRelease();
#endif
                isLeftPressed = false;
            }

            if (rightClick && !isRightPressed)
            {
#if WINDOWS
                MouseEmulator.SimulateRightPress();
#endif
                isRightPressed = true;
            }
            else if (!rightClick && isRightPressed)
            {
#if WINDOWS
                MouseEmulator.SimulateRightRelease();
#endif
                isRightPressed = false;
            }
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
        /// Public helper to broadcast a chat message from the desktop side.
        /// </summary>
        public async Task SendChatMessage(string message)
        {
            string chatMessage = CHAT_PREFIX + message;
            await BroadcastMessage(chatMessage, null);
            _updateStatusCallback?.Invoke("Sent chat: " + message, true);
        }

        /// <summary>
        /// Stops the server, cancels all active tasks, and closes any connected clients.
        /// </summary>
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

                // Create a copy of the clients list so that we can iterate without modifying the original.
                var clientsCopy = new List<WebSocket>(_clients);
                foreach (var client in clientsCopy)
                {
                    try
                    {
                        if (client != null && client.State == WebSocketState.Open)
                        {
                            // Send a disconnect message so the client knows to disconnect.
                            string disconnectMsg = "Server Disconnecting";
                            byte[] disconnectBuffer = Encoding.UTF8.GetBytes(disconnectMsg);
                            client.SendAsync(new ArraySegment<byte>(disconnectBuffer), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                            // Optional: wait briefly to allow the client to process the disconnect message.
                            Task.Delay(500).Wait();

                            // Now close the client connection gracefully.
                            client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error disconnecting client: {ex.Message}");
                    }
                }
                _clients.Clear();

                // Update the UI to indicate the server session has ended.
                _updateStatusCallback?.Invoke("Server Session Ended", false);
                Debug.WriteLine("🔴 WebSocket Server stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error stopping server: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to get a non-loopback IPv4 address for the current machine.
        /// </summary>
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
