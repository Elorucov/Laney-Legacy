using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    public class ExactlyValueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            bool exact = false;
            string word = parameter.ToString();
            exact = value.ToString() == word;
            if (targetType == typeof(Visibility)) return exact ? Visibility.Visible : Visibility.Collapsed;
            if (targetType == typeof(Nullable<bool>)) {
                bool? nullable = exact;
                return nullable;
            }
            return exact;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}