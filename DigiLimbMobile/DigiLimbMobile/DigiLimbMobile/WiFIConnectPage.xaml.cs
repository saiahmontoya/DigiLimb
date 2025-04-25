using Microsoft.Maui.Controls;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DigiLimbMobile
{
    public partial class WiFiConnectPage : ContentPage
    {
        // We'll still use a private variable for local reference,
        // but also assign it to the global variable.
        private ClientWebSocket _webSocket;
        public bool WiFiConnectionFlag;
        public WiFiConnectPage()
        {
            InitializeComponent();
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            // If a connection already exists globally, avoid reconnecting.
            if (App.GlobalWebSocket != null && App.GlobalWebSocket.State == WebSocketState.Open)
            {
                lblStatus.Text = "Already connected.";
                return;
            }

            string serverIP = entryServerIP.Text?.Trim();
            if (string.IsNullOrEmpty(serverIP))
            {
                lblStatus.Text = "Please enter a valid server IP address.";
                return;
            }
            string port = entryPort.Text;
            string url = $"ws://{serverIP}:{port}/";

            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                // Save connection globally
                App.GlobalWebSocket = _webSocket;
                Console.WriteLine("Connected to server.");
                WiFiConnectionFlag = true; 
                Console.WriteLine("connection flag set");
                StartReceiveLoop();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Connection failed: {ex.Message}";
                WiFiConnectionFlag = false;
            }
        }

        private async void StartReceiveLoop()
        {
            var buffer = new byte[1024];
            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        lblStatus.Text = "Server closed connection.";
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (message == "PING")
                        {
                            // Reply to heartbeat ping
                            byte[] pongBuffer = Encoding.UTF8.GetBytes("PONG");
                            await _webSocket.SendAsync(new ArraySegment<byte>(pongBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        else if (message.StartsWith("CHAT:"))
                        {
                            // Append chat messages to a chat log
                            string chatText = message.Substring("CHAT:".Length);
                            lblReceived.Text += $"\n{chatText}";
                        }
                        else
                        {
                            lblReceived.Text = $"Received: {message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Receive error: {ex.Message}";
            }
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            // Use the global connection variable for sending messages.
            var ws = App.GlobalWebSocket;
            if (ws == null || ws.State != WebSocketState.Open)
            {
                lblStatus.Text = "Not connected to server.";
                return;
            }
            string message = entrySendMessage.Text;
            if (string.IsNullOrEmpty(message))
            {
                lblStatus.Text = "Enter a message to send.";
                return;
            }
            // Prefix the message as a chat message.
            string chatMessage = "CHAT:" + message;
            byte[] messageBuffer = Encoding.UTF8.GetBytes(chatMessage);
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                lblStatus.Text = "Message sent.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Send error: {ex.Message}";
            }
        }
    }
}
