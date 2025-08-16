using System;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class GeopointToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Geopoint g && targetType == typeof(string))
                return $"Lat: {Math.Round(g.Position.Latitude, 6)}\nLon: {Math.Round(g.Position.Longitude, 6)}";
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
