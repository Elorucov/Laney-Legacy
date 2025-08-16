using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Core
{
    public class ThemeManager
    {
        public static async void ApplyThemeAsync()
        {
            int theme = Settings.Theme;
            Log.General.Info("Applying theme...", new ValueSet { { "theme", theme } });
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    FrameworkElement root = Window.Current.Content as FrameworkElement;
                    switch (theme)
                    {
                        default: root.RequestedTheme = ElementTheme.Default; break;
                        case 2: root.RequestedTheme = ElementTheme.Light; break;
                        case 3: root.RequestedTheme = ElementTheme.Dark; break;
                    }
                    FixTitleBarButtonsColor();
                });
            }
            // TODO: for auto.
        }

        public static void FixTitleBarButtonsColor()
        {
            try
            {
                ApplicationView av = ApplicationView.GetForCurrentView();
                ApplicationViewTitleBar tb = av.TitleBar;

                bool isDark = false;
                switch (Settings.Theme)
                {
                    default: isDark = Application.Current.RequestedTheme == ApplicationTheme.Dark; break;
                    case 2: isDark = false; break;
                    case 3: isDark = true; break;
                }

                Color textPrimary = isDark ? Color.FromArgb(255, 225, 227, 230) : Color.FromArgb(255, 0, 0, 0);
                Color buttonPrimaryBackground = isDark ? Color.FromArgb(255, 225, 227, 230) : Color.FromArgb(255, 49, 134, 204);
                Color buttonPrimaryForeground = isDark ? Color.FromArgb(255, 19, 19, 20) : Color.FromArgb(255, 255, 255, 255);
                Color buttonSecondaryBackground = isDark ? Color.FromArgb(255, 69, 70, 71) : Color.FromArgb(14, 0, 28, 61);
                Color buttonSecondaryForeground = isDark ? Color.FromArgb(255, 225, 227, 230) : Color.FromArgb(255, 63, 138, 224);

                tb.BackgroundColor = tb.InactiveBackgroundColor = Colors.Transparent;
                tb.ButtonBackgroundColor = tb.ButtonInactiveBackgroundColor = Colors.Transparent;
                tb.ButtonForegroundColor = tb.ButtonInactiveForegroundColor = textPrimary;
                tb.ForegroundColor = tb.InactiveForegroundColor = textPrimary;
                tb.ButtonHoverBackgroundColor = buttonSecondaryBackground;
                tb.ButtonHoverForegroundColor = buttonSecondaryForeground;
                tb.ButtonPressedBackgroundColor = buttonPrimaryBackground;
                tb.ButtonPressedForegroundColor = buttonPrimaryForeground;
            }
            catch (Exception ex)
            {
                Log.General.Error($"FixTitleBarButtonsColor failed!", ex);
            }
        }

        public static event EventHandler<double> MessageFontSizeChanged;

        public static void ChangeMessageFontSize(double size)
        {
            MessageFontSizeChanged?.Invoke(null, size);
        }

        #region Resource dictionaries

        public static void UpdateResourceColors(FrameworkElement element)
        {
            element.RequestedTheme = App.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            element.RequestedTheme = ElementTheme.Default;
        }

        public static void UpdateResourceColors(FrameworkElement element, FrameworkElement parent)
        {
            if (parent.RequestedTheme == ElementTheme.Default)
            {
                UpdateResourceColors(element);
                return;
            }
            element.RequestedTheme = parent.RequestedTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            element.RequestedTheme = ElementTheme.Default;
        }

        public static void LoadThemedResourceDictionary(string name, string oldname, FrameworkElement element = null)
        {
            string newlight = $"ms-appx:///Styles/ColorSchemes/{name}Light.xaml";
            string newdark = $"ms-appx:///Styles/ColorSchemes/{name}Dark.xaml";
            string oldlight = $"ms-appx:///Styles/ColorSchemes/{oldname}Light.xaml";
            string olddark = $"ms-appx:///Styles/ColorSchemes/{oldname}Dark.xaml";

            var resources = element == null ? Application.Current.Resources : element.Resources;
            var themeDicts = resources.ThemeDictionaries;

            ReplaceResDictInThemeDicts(themeDicts, "Light", newlight, oldlight);
            ReplaceResDictInThemeDicts(themeDicts, "Default", newdark, olddark);
        }

        private static void ReplaceResDictInThemeDicts(IDictionary<object, object> themeDicts, string key, string newpath, string oldpath)
        {
            ResourceDictionary resDict = !themeDicts.ContainsKey(key) ? new ResourceDictionary() : (ResourceDictionary)themeDicts[key];
            if (!themeDicts.ContainsKey(key)) themeDicts[key] = resDict;
            RemoveResDictBySource(oldpath, resDict.MergedDictionaries);
            resDict.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(newpath) });
        }

        private static void RemoveResDictBySource(string oldname, IList<ResourceDictionary> mergedDictionaries)
        {
            ResourceDictionary r = (from q in mergedDictionaries where q.Source != null && q.Source.ToString() == oldname select q).FirstOrDefault();
            if (r != null) mergedDictionaries.Remove(r);
        }

        #endregion

        #region Chat backgrounds

        public static Dictionary<string, bool> PreInstalledBackgrounds = new Dictionary<string, bool> {
            { "ms-appx:///Assets/ChatBackgrounds/02.jpg", true },
            { "ms-appx:///Assets/ChatBackgrounds/01.png", false }
        };

        public static bool IsPreInstalledChatBackground(string uri)
        {
            return PreInstalledBackgrounds.ContainsKey(uri);
        }

        public static async void ChangeChatBackgroundFromFileAsync(StorageFile file)
        {
            // Delete old imported
            string b = Settings.ChatBackground;
            if (Settings.ChatBackgroundType == 2 && !IsPreInstalledChatBackground(b))
            {
                Log.General.Info("Removing old background", new ValueSet { { "old", b } });
                StorageFile old = await StorageFile.GetFileFromApplicationUriAsync(new Uri(b));
                await old.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            Log.General.Info("Copying file to local storage", new ValueSet { { "path", file.Path } });
            StorageFile copied = await file.CopyAsync(ApplicationData.Current.LocalFolder, $"{Guid.NewGuid()}{file.FileType}");
            ChangeChatBackground(2, $"ms-appdata:///Local/{copied.Name}", true);
        }

        public static event EventHandler ChatBackgroundChanged;

        public static void ResetChatBackground()
        {
            Settings.ChatBackgroundType = 0;
            Settings.ChatBackground = null;
            ChatBackgroundChanged?.Invoke(null, null);
        }

        public static void ChangeChatBackground(int type, string background, bool stretch = false)
        {
            Log.General.Info(String.Empty, new ValueSet { { "type", type }, { "background", background }, { "stretch", stretch } });
            Settings.ChatBackgroundType = type;
            Settings.ChatBackground = background;
            Settings.ChatBackgroundImageStretch = stretch;
            ChatBackgroundChanged?.Invoke(null, null);
        }

        #endregion
    }
}