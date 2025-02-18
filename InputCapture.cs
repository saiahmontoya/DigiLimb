using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace DigiLimb // Ensure the class is inside the correct namespace
{
    class InputCapture
    {
        private ClientService clientService;  // Renamed from _client to clientService

        public InputCapture(ClientService client)
        {
            clientService = client;
        }

        public async Task OnKeyPress(string key)
        {
            await clientService.SendInput($"KEY:{key}");
        }

        public async Task OnMouseMove(int dx, int dy)
        {
            await clientService.SendInput($"MOUSE:{dx},{dy}");
        }
    }
}
