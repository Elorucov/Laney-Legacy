using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    // Required for InfoBadge because the type of Value property is f**king Int32!
    internal class Int64ToInt32 : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is long l) {
                if (l > int.MaxValue) return int.MaxValue;
                return System.Convert.ToInt32(l);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}