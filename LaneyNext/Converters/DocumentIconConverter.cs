using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Helpers;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Converters
{
    public class DocumentIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is Document d)
            {
                Border b = new Border
                {
                    Width = 48,
                    Height = 48,
                    RequestedTheme = Windows.UI.Xaml.ElementTheme.Dark,
                    CornerRadius = new Windows.UI.Xaml.CornerRadius(4)
                };
                b.Background = APIHelper.GetDocumentIconBackground(d.Type);
                if (d.Preview != null)
                {
                    Image img = new Image();
                    img.Source = new BitmapImage
                    {
                        DecodePixelType = DecodePixelType.Logical,
                        UriSource = d.Preview.Photo.MinimalSizedPhoto.Uri
                    };
                    img.Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
                    img.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                    img.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
                    b.Child = img;
                }
                else
                {
                    b.Child = new ContentPresenter
                    {
                        ContentTemplate = APIHelper.GetDocumentIcon(d.Type),
                        Width = 28,
                        Height = 28,
                    };
                }
                return b;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
