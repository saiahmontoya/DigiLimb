using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace DigiLimbMobile;

public partial class PresentationMode : ContentPage
{
    public PresentationMode()
    {
        InitializeComponent();
    }

    private async void SendCommandAsync(string command)
    {
        var socket = App.GlobalWebSocket;

        if (socket == null || socket.State != WebSocketState.Open)
        {
            await DisplayAlert("Connection Error", "Not connected to desktop. Please connect first.", "OK");
            return;
        }

        var json = new
        {
            type = "presentation",
            command = command
        };

        string message = JsonSerializer.Serialize(json);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        try
        {
            await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Send Failed", ex.Message, "OK");
        }
    }

    private void OnStartClicked(object sender, EventArgs e) => SendCommandAsync("start");
    private void OnPreviousClicked(object sender, EventArgs e) => SendCommandAsync("previous");
    private void OnNextClicked(object sender, EventArgs e) => SendCommandAsync("next");
    private void OnPlayMediaClicked(object sender, EventArgs e) => SendCommandAsync("playMedia");
    private void OnPauseMediaClicked(object sender, EventArgs e) => SendCommandAsync("pauseMedia");
    private void OnFullscreenMediaClicked(object sender, EventArgs e) => SendCommandAsync("fullscreenMedia");
    private void OnExitFullscreenClicked(object sender, EventArgs e) => SendCommandAsync("exitFullscreen");


}
