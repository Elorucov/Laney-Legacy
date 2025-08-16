using Elorucov.Laney.Models;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services.Converters.ConversationsList {
    public class UnreadMessagesCounterColor : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is bool) {
                var c = (bool)value;
                return c ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush(Color.FromArgb(255, 81, 129, 184));
            } else {
                return new SolidColorBrush(Color.FromArgb(255, 81, 129, 184));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }

    public class UnreadMessagesCounterVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is int) {
                var c = (int)value;
                return c > 0 ? Visibility.Visible : Visibility.Collapsed;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }

    public class ReadIndicatorVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is SentMessageState state) {
                switch (state) {
                    case SentMessageState.Loading: return (DataTemplate)Application.Current.Resources["LoadingIcon"];
                    case SentMessageState.Unread: return (DataTemplate)Application.Current.Resources["DeliveredCheckIcon"];
                    case SentMessageState.Read: return (DataTemplate)Application.Current.Resources["ReadCheckIcon"];
                    case SentMessageState.Deleted: return (DataTemplate)Application.Current.Resources["DeletedIcon"];
                    default: return null;
                }
            } else {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}