using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters.ConversationsList {
    public class VerifiedUser : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is bool) {
                var c = (bool)value;
                return c ? Visibility.Visible : Visibility.Collapsed;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}