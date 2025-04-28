using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiLimbDesktop
{
    public class ReceivedDeviceInfo : EventArgs
    {
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
    }
}

