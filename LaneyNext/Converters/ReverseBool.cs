using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class ReverseBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && targetType == typeof(bool))
            {
                return !b;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && targetType == typeof(bool))
            {
                return !b;
            }
            return false;
        }
    }
}
