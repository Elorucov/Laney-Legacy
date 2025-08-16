using Elorucov.Laney.Helpers;
using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class DateTimeToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dt)
            {
                if (parameter == null) return dt.ToTimeOrDate();
                switch (parameter.ToString())
                {
                    case "t": return dt.ToString(@"H\:mm");
                    case "d": return dt.ToHumanizedDate();
                    case "a": return dt.ToString(@"yyy\.MM\.dd HH\:mm\:ss");
                    case "dt": return dt.ToTimeAndDate();
                    default: return dt.ToTimeOrDate();
                }
            }
            else if (value is TimeSpan tm)
            {
                return tm.Hours > 0 ? tm.ToString(@"h\:mm\:ss") : tm.ToString(@"mm\:ss");
            }
            return "--:--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
