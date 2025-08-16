using Elorucov.Laney.ViewModels;
using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class PlaceholderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PlaceholderViewModel pvm)
            {
                if (pvm.IconTemplate == null) return VK.VKUI.VKUILibrary.GetIconTemplate(pvm.Icon);
                return pvm.IconTemplate;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
