using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class EnumFixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value.GetType().IsEnum && targetType.IsEnum) return value;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
