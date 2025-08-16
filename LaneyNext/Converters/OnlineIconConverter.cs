using ELOR.VKAPILib.Objects;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class OnlineIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is UserOnlineInfo info)
            {
                if (!info.Visible) return null;
                if (!info.isOnline) return null;
                string icon = info.IsMobile ? "OnlineMobileIcon" : "OnlineIcon";
                return Application.Current.Resources[icon];
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
