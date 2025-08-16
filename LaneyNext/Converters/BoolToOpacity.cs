using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class BoolToOpacity : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return b ? 1 : 0.5;
            }
            return 0.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
