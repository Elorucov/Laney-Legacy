using Elorucov.VkAPI.Objects;
using System;
using VK.VKUI.Controls;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Services.Converters.MessageItem {
    public class DocsIconConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is Document) {
                Document d = value as Document;
                DisplayInformation di = DisplayInformation.GetForCurrentView();

                Border b = new Border { Width = 48, Height = 48, CornerRadius = new CornerRadius(4) };
                b.Background = DocumentIcons.GetIconBackground(d.Type);
                if (d.Preview != null) {
                    Image icon = new Image();
                    icon.Source = new BitmapImage {
                        DecodePixelType = DecodePixelType.Logical,
                        UriSource = d.Preview.Photo.MinimalSizedPhoto.Uri
                    };
                    icon.Stretch = Stretch.UniformToFill;
                    icon.HorizontalAlignment = HorizontalAlignment.Center;
                    icon.VerticalAlignment = VerticalAlignment.Center;
                    b.Child = icon;
                } else {
                    b.Child = new VKIcon {
                        Width = 28,
                        Height = 28,
                        Padding = new Thickness(10),
                        Id = DocumentIcons.GetIcon(d.Type),
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                }

                return b;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}