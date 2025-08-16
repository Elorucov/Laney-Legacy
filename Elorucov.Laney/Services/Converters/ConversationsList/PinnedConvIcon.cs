using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters.ConversationsList {
    public class PinnedConvIcon : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            bool result = false;
            if (value is int majorId) {
                result = majorId != 0 && majorId % 16 == 0;
            }
            if (targetType == typeof(Visibility)) {
                return result ? Visibility.Visible : Visibility.Collapsed;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}