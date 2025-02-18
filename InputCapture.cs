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