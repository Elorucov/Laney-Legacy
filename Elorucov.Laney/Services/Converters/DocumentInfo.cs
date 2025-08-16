using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Objects;
using System;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    public class DocumentInfo : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is Document d) {
                return $"{d.Extension} · {Functions.GetFileSize(d.Size)}";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
