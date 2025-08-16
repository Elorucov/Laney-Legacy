using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class UnreadMessagesCounterToTemplate : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is bool isMuted)
            {
                if (!isMuted)
                {
                    return Application.Current.Resources["UnreadCounterUnmutedTemplate"] as DataTemplate;
                }
            }
            return Application.Current.Resources["UnreadCounterMutedTemplate"] as DataTemplate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
