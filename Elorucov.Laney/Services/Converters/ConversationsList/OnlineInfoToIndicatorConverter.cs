using Elorucov.VkAPI.Objects;
using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters.ConversationsList {
    public class OnlineInfoToIndicatorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is UserOnlineInfo uoi && uoi.Visible && uoi.isOnline) {
                return App.Current.Resources[!uoi.IsMobile ? "OnlineIcon" : "OnlineMobileIcon"];
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }

    public class OnlineIndicatorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            int platform = (int)value;
            if (platform == 0) return null;
            return App.Current.Resources[platform == 7 ? "OnlineIcon" : "OnlineMobileIcon"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}