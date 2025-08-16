using Elorucov.Laney.ViewModel;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    class MessageFormTemplateConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is MessageSendRestriction) {
                MessageSendRestriction msr = (MessageSendRestriction)value;
                switch (msr) {
                    case MessageSendRestriction.Banned: return Application.Current.Resources["BanInfoTemplate"];
                    case MessageSendRestriction.None: return Application.Current.Resources["MessageFormTemplate"];
                    default: return Application.Current.Resources["BanInfoTemplate"];
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}
