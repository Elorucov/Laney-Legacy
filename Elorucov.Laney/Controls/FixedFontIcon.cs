using Elorucov.Laney.Services.UI;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Controls {
    public class FixedFontIcon : FontIcon {
        public FixedFontIcon() {
            SetValue(FontFamilyProperty, Theme.DefaultIconsFont);
            SetValue(FontSizeProperty, 16);
        }
    }
}