using Elorucov.Laney.Core;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class LocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string resourceId)
            {
                return Locale.Get(resourceId);
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
