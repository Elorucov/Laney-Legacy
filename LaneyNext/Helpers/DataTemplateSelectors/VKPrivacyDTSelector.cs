using ELOR.VKAPILib.Objects;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Helpers.DataTemplateSelectors
{
    public class VKPrivacyDTSelector : DataTemplateSelector
    {
        public DataTemplate List { get; set; }
        public DataTemplate Binary { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is PrivacySetting setting)
            {
                switch (setting.Type)
                {
                    case PrivacySettingValueType.List: return List;
                    case PrivacySettingValueType.Binary: return Binary;
                }
            }
            return null;
        }
    }
}
