using Elorucov.Laney.Services.Common;
using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters.MessageItem {
    public class TimeToString : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is DateTime) {
                return ((DateTime)value).ToString("t");
            } else if (value is TimeSpan) {
                TimeSpan ts = (TimeSpan)value;
                return ts.ToString(ts.Hours > 0 ? @"h\:mm\:ss" : @"m\:ss");
            }
            return "--:--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }

    public class DateTimeToString : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is DateTime) {
                return APIHelper.GetNormalizedTime((DateTime)value, true);
            }
            return "--:--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }

    public class DateTimeToString2 : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is DateTime) {
                return APIHelper.GetNormalizedTime((DateTime)value, false);
            }
            return "--:--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }

    public class DateTimeToCompactString : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is DateTime dateTime) {
                return dateTime.ToShortDateString() + " " + dateTime.ToString("H:mm");
            }
            return Locale.Get("now");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}