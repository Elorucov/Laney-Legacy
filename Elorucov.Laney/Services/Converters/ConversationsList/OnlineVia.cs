using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters.ConversationsList {
    public class OnlineVia : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is int) {
                var c = (int)value;
                return VKClientsHelper.GetAppIconByLPResponse(c);
            } else {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}