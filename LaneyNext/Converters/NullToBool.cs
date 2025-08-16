using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class NullToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
#if DEBUG
            if (parameter is string ps && ps == "test") Debugger.Break();
#endif
            bool v = false;
            if (value is string s)
            {
                v = !String.IsNullOrEmpty(s);
            }
            else if (value is IEnumerable<object> l)
            {
                v = l.Count() >= 1;
            }
            else if (value is bool b)
            {
                v = b;
            }
            else if (value is int i)
            {
                v = i != 0;
            }
            else
            {
                v = value != null;
            }
            if (targetType == typeof(Visibility))
            {
                return v ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return v;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }

    public class NullToBool2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
#if DEBUG
            if (parameter is string ps && ps == "test") Debugger.Break();
#endif
            bool r;
            r = value == null;
            if (value is int i) r = i == 0;
            if (value is bool b) r = !b;
            if (value is string s) r = String.IsNullOrEmpty(s);
            if (value is IEnumerable<object> l)
            {
                r = l == null || l.Count() == 0;
            }
            if (targetType == typeof(Visibility))
            {
                return r ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return r;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
