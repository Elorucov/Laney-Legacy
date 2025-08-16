using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Services.Converters {
    public class ReactionImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is int id) {
                if (id == 0) {
                    return Locale.Get("all");
                } else {
                    return new Image {
                        Width = 22, Height = 22,
                        Stretch = Stretch.Uniform,
                        Source = new SvgImageSource {
                            UriSource = Reaction.GetImagePathById(id)
                        }
                    };
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
