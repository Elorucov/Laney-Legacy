using Elorucov.Laney.Core;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class UserGroupToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is SessionType type)
            {
                if (parameter != null && parameter.ToString() == "g")
                {
                    return type == SessionType.VKGroup ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    return type == SessionType.VKGroup ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
