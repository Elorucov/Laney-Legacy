using Elorucov.Laney.Helpers;
using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ulong s = 0;
            return UInt64.TryParse(value.ToString(), out s) ? ((decimal)s).ToFileSize() : "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
