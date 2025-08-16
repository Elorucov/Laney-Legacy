using Elorucov.Laney.Core;

namespace Elorucov.Laney.ViewModels.Settings
{
    public class InterfaceViewModel : BaseViewModel
    {
        private int _currentTheme;
        private double _fontSize;

        public int CurrentTheme { get { return _currentTheme; } set { _currentTheme = value; OnPropertyChanged(); } }
        public double FontSize { get { return _fontSize; } set { _fontSize = value; OnPropertyChanged(); } }

        public InterfaceViewModel()
        {
            CurrentTheme = Core.Settings.Theme;
            FontSize = Core.Settings.MessageFontSize;

            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(CurrentTheme):
                        Core.Settings.Theme = CurrentTheme;
                        ThemeManager.ApplyThemeAsync();
                        break;
                    case nameof(FontSize):
                        Core.Settings.MessageFontSize = FontSize;
                        ThemeManager.ChangeMessageFontSize(FontSize);
                        break;
                }
            };
        }
    }
}
