using Elorucov.Laney.Services.Common;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services.UI {
    public class Theme {
        public static bool IsMicaAvailable {
            get {
                return !AppParameters.ForceAcrylicBackgroundOnWin11 && Functions.IsWin11();
            }
        }

        public static bool IsAnimationsEnabled { get { return new UISettings().AnimationsEnabled; } }

        public static bool IsDarkTheme() {
            bool isDark = true;

            var theme = (Window.Current.Content as Frame).RequestedTheme;
            var appTheme = App.Current.RequestedTheme;
            if (theme == ElementTheme.Default) {
                isDark = appTheme == ApplicationTheme.Dark;
            } else {
                isDark = theme == ElementTheme.Dark;
            }
            return isDark;
        }

        public static void ChangeTheme(ElementTheme theme) {
            bool isDark = true;
            var appTheme = App.Current.RequestedTheme;
            if (theme == ElementTheme.Default) {
                isDark = appTheme == ApplicationTheme.Dark;
            } else {
                isDark = theme == ElementTheme.Dark;
            }

            (Window.Current.Content as Frame).RequestedTheme = theme;

            var ps = VisualTreeHelper.GetOpenPopups(Window.Current);
            foreach (Popup p in ps) {
                (p.Child as FrameworkElement).RequestedTheme = theme;
            }
            ThemeChanged?.Invoke(null, isDark);
        }

        public static event EventHandler<bool> ThemeChanged;

        public static void UpdateTheme(bool needUpdateAccentColor = false) {
            if (new AccessibilitySettings().HighContrast) return;
            if (needUpdateAccentColor) {
                switch (Common.AppParameters.Theme) {
                    case 2: Theme.ChangeTheme(ElementTheme.Light); break;
                    case 1: Theme.ChangeTheme(ElementTheme.Dark); break;
                    case 0: Theme.ChangeTheme(Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Light : ElementTheme.Dark); break;
                }
            }
            switch (Common.AppParameters.Theme) {
                case 0: Theme.ChangeTheme(ElementTheme.Default); break;
                case 1: Theme.ChangeTheme(ElementTheme.Light); break;
                case 2: Theme.ChangeTheme(ElementTheme.Dark); break;
            }
        }

        public static async Task UpdateTitleBarColors(UISettings uis) {
            Color fc = uis.GetColorValue(UIColorType.Foreground);
            switch (AppParameters.Theme) {
                case 1: fc = Color.FromArgb(255, 0, 0, 0); break;
                case 2: fc = Color.FromArgb(255, 255, 255, 255); break;
            }
            await TitleAndStatusBar.ChangeColor(fc);

            Color bc = uis.GetColorValue(UIColorType.Background);
            bc.A = 0;
            switch (AppParameters.Theme) {
                case 1: bc = Color.FromArgb(0, 255, 255, 255); break;
                case 2: bc = Color.FromArgb(0, 0, 0, 0); break;
            }
            TitleAndStatusBar.ChangeBackgroundColor(bc);
        }

        public static Color VKAccent = Color.FromArgb(255, 0x27, 0x87, 0xF5);
        public static Color ParseColor(string v) {
            try {
                string[] a = v.Split(',');
                return Color.FromArgb(255, Byte.Parse(a[0]), Byte.Parse(a[1]), Byte.Parse(a[2]));
            } catch {
                return VKAccent;
            }
        }

        public static void ResetAccent() {
            var uiSettings = new UISettings();
            Application.Current.Resources["SystemAccentColorDark3"] = uiSettings.GetColorValue(UIColorType.AccentDark3);
            Application.Current.Resources["SystemAccentColorDark2"] = uiSettings.GetColorValue(UIColorType.AccentDark2);
            Application.Current.Resources["SystemAccentColorDark1"] = uiSettings.GetColorValue(UIColorType.AccentDark1);
            Application.Current.Resources["SystemAccentColor"] = uiSettings.GetColorValue(UIColorType.Accent);
            Application.Current.Resources["SystemAccentColorLight1"] = uiSettings.GetColorValue(UIColorType.AccentLight1);
            Application.Current.Resources["SystemAccentColorLight2"] = uiSettings.GetColorValue(UIColorType.AccentLight2);
            Application.Current.Resources["SystemAccentColorLight3"] = uiSettings.GetColorValue(UIColorType.AccentLight3);
            UpdateTheme(true);
        }

        public static event EventHandler<double> MessageBubbleFontSizeChanged;
        public static event EventHandler<bool> IsTextSelectionEnabledChanged;
        public static void ChangeMessageBubbleFontSize(double size) {
            MessageBubbleFontSizeChanged?.Invoke(null, size);
        }

        public static void ChangeIsTextSelectionEnabled() {
            IsTextSelectionEnabledChanged?.Invoke(null, AppParameters.IsTextSelectionEnabled);
        }

        public static event EventHandler<bool> FoldersViewChanged;
        public static void ChangeFoldersView(bool isVertical) {
            AppParameters.FoldersPlacement = isVertical;
            FoldersViewChanged?.Invoke(null, isVertical);
        }

        public static event EventHandler<bool> ChatsListItemTemplateChanged;
        public static void ChangeChatsListItemTemplate(bool enabled) {
            AppParameters.ChatsListLines = enabled;
            ChatsListItemTemplateChanged?.Invoke(null, enabled);
        }

        // Default icons font
        public static FontFamily DefaultIconsFont {
            get {
                return App.Current.Resources["SymbolThemeFontFamily"] as FontFamily;
            }
        }
    }
}