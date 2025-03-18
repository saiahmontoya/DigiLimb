using System;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace DigiLimbDesktop
{
    public class IpToVisibilityConverter : IValueConverter
    {
        private readonly string _currentDeviceIp;

        public IpToVisibilityConverter(string currentDeviceIp)
        {
            _currentDeviceIp = currentDeviceIp;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is string ipAddress))
                return true; // Default to showing the button

            return ipAddress != _currentDeviceIp; // Hide button if it's the current device
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
