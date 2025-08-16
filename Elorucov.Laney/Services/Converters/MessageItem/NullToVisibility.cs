using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters.MessageItem {
    public class NullToVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is string s) {
                return !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;
            } else if (value is IEnumerable<object> ie) {
                return ie.Count() >= 1 ? Visibility.Visible : Visibility.Collapsed;
            } else if (value is bool b) {
                return b ? Visibility.Visible : Visibility.Collapsed;
            } else if (value is int i) {
                return i > 0 ? Visibility.Visible : Visibility.Collapsed;
            } else {
                return value != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }

    public class NullToVisibility2 : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            bool r;
            r = value == null;
            if (value is int i) r = i == 0;
            if (value is bool b) r = !b;
            if (value is string s) r = string.IsNullOrEmpty(s);
            if (value is IEnumerable<object> l) {
                r = l == null || l.Count() == 0;
            }
            if (targetType == typeof(Visibility)) {
                return r ? Visibility.Visible : Visibility.Collapsed;
            } else {
                return r;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}