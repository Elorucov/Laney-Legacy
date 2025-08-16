using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    public class ValueToBool : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            bool r = false;

            r = value != null && parameter != null && value.ToString() == parameter.ToString();

            if (targetType == typeof(Visibility)) return r ? Visibility.Visible : Visibility.Collapsed;
            return r;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            bool b = (bool)value;
            if (targetType == typeof(int)) {
                int v = 0;
                int.TryParse(parameter.ToString(), out v);
                return b ? v : DependencyProperty.UnsetValue;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}