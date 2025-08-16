using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Objects;
using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    public class VideoAttachmentInfoConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is Video) {
                Video v = value as Video;
                return $"{v.DurationTime.ToString("g")}, {String.Format(Locale.GetDeclensionForFormat(v.Views, "views"), v.Views)}";
            }
            return "unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
