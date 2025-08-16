using Elorucov.Laney.Services.Common;
using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    public class CounterConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            string word = parameter.ToString();
            if (value is int num) {
                return String.Format(Locale.GetDeclensionForFormat(num, word), num);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}