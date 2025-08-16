using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class StickerIdToUri : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int id)
            {
                return new Uri(id <= 0 ? "ms-appx:///Assets/NonVector/StickerKeywordBot.png" : $"https://vk.com/sticker/1-{id}-128b");
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
