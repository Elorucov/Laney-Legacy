using System;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    internal class GeopointToString : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is Geopoint point) {
                var p = point.Position;
                return $"Lat: {Math.Round(p.Latitude, 4)}; Long: {Math.Round(p.Longitude, 4)}";
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}
