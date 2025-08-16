using System;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class VKPrivacyCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string category)
            {
                var ctg = ViewModels.Settings.PrivacyViewModel.PrivacyCategories.Where(c => c.Value == category).FirstOrDefault();
                return ctg != null ? ctg.Title : String.Empty;
            }
            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
