using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters.ConversationsList {

    // Костыль для рисования сепаратора под последней закреплённой беседой.
    public class PinnedConvSeparator : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            bool result = false;
            if (value is int majorId) {
                result = majorId == 16;
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