using Elorucov.Laney.Models.UI;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.SettingsPages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Interface : Page {
        bool IsXbox = AnalyticsInfo.VersionInfo.DeviceFamily.ToLower().Contains("xbox");

        public Interface() {
            this.InitializeComponent();
            if (!IsXbox) SettingItems.Items.Remove(xb);

            Loaded += (a, b) => LoadSettings();

            var host = Main.GetCurrent();
            if (host.ActualWidth >= 420) {
                double bottom = demoUI.Margin.Bottom;
                demoUI.Margin = new Thickness(24, 0, 24, bottom);
            }

            BackButton.Visibility = host.IsWideMode ? Visibility.Collapsed : Visibility.Visible;
            host.SizeChanged += Host_SizeChanged;
            Unloaded += (a, b) => host.SizeChanged -= Host_SizeChanged;
        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e) {
            BackButton.Visibility = Main.GetCurrent().IsWideMode ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            Main.GetCurrent().GoBack();
        }

        private void LoadSettings() {
            if (IsXbox) {
                p01.IsOn = !ApplicationViewScaling.DisableLayoutScaling;
                p02.IsOn = ApplicationView.GetForCurrentView().DesiredBoundsMode == ApplicationViewBoundsMode.UseCoreWindow ? true : false;
            }

            p03.IsOn = AppParameters.AlternativeEmojiFont;
            p04.SelectedIndex = AppParameters.Theme;
            p06.Value = AppParameters.MessageFontSize;
            p01.Toggled += ts01;
            p02.Toggled += ts02;
            p03.Toggled += ts03;
            p04.SelectionChanged += ts04;
            p06.ValueChanged += ts06;

            // Check emoji font availability
            altEmojiFontSetting.IsEnabled = EmojiFontManager.IsAvailable;
            p03.IsEnabled = EmojiFontManager.IsAvailable;

            // CTE style and loading sample messages
            var cvm = ViewModel.ConversationViewModel.GetSampleConversation();
            demoUI.DataContext = cvm;

            ChatThemeService.RegisterBackgroundElement(ChatBackground);
            ChatThemeService.RegisterChatRootElement(demoUI);

            List<string> styles = new List<string> {
                Locale.Get($"default"), Locale.Get($"custom")
            };

            foreach (var theme in ChatThemeService.PrebuiltStyles) {
                styles.Add(theme.Name);
            }

            p07.ItemsSource = styles;

            var currentTheme = ChatThemeService.PrebuiltStyles.Where(t => t.Id == AppParameters.CTEStyle).FirstOrDefault();
            int index = currentTheme != null ? ChatThemeService.PrebuiltStyles.IndexOf(currentTheme) + 2 : 0;

            if (!string.IsNullOrEmpty(AppParameters.ChatBackground) || !string.IsNullOrEmpty(AppParameters.CTEAccent)) {
                index = 1;
                BackgroundsExpander.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
                ColorsExpander.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
                CheckCustomStyle();
            }

            p07.SelectedIndex = index;
            cvm.Style = AppParameters.CTEStyle;

            p07.SelectionChanged += ts07;

            p08.IsOn = AppParameters.CTEIgnoreChatTheme;
            p08.Toggled += ts08;

            p09.SelectedIndex = AppParameters.ChatsListLines ? 1 : 0;
            p09.SelectionChanged += ts09;

            p10.IsOn = AppParameters.LastMessagePreview;
            p10.Toggled += ts10;

            p11.SelectedIndex = AppParameters.ThumbsPosition ? 1 : 0;
            p11.SelectionChanged += ts11;
        }

        private void ts01(object sender, RoutedEventArgs e) {
            ToggleSwitch ts = sender as ToggleSwitch;
            if (!ts.IsEnabled) return;
            ts.IsEnabled = false;
            bool result = ApplicationViewScaling.TrySetDisableLayoutScaling(!ts.IsOn);
            if (!result) {
                ts.IsOn = !ts.IsOn;
            }
            ts.IsEnabled = true;
        }

        private void ts02(object sender, RoutedEventArgs e) {
            ToggleSwitch ts = sender as ToggleSwitch;
            if (!ts.IsEnabled) return;
            ts.IsEnabled = false;
            bool result = ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ts.IsOn ? ApplicationViewBoundsMode.UseCoreWindow : ApplicationViewBoundsMode.UseVisible);
            if (!result) {
                ts.IsOn = !ts.IsOn;
            }
            ts.IsEnabled = true;
        }

        private void ts03(object sender, RoutedEventArgs e) {
            AppParameters.AlternativeEmojiFont = p03.IsOn;
            altEmojiFontSetting.Description = Locale.Get("restart_required");
        }

        private void ts04(object sender, SelectionChangedEventArgs e) {
            ComboBox cb = sender as ComboBox;
            if (cb.SelectedIndex >= 0) AppParameters.Theme = cb.SelectedIndex;
            switch (cb.SelectedIndex) {
                case 0: Theme.ChangeTheme(ElementTheme.Default); break;
                case 1: Theme.ChangeTheme(ElementTheme.Light); break;
                case 2: Theme.ChangeTheme(ElementTheme.Dark); break;
            }
            new System.Action(async () => { await Theme.UpdateTitleBarColors(new UISettings()); })();
            if (!App.IsDefaultTheme) theme.Description = Locale.Get("restart_required");
        }

        private void ts06(NumberBox sender, NumberBoxValueChangedEventArgs e) {
            AppParameters.MessageFontSize = e.NewValue;
            Theme.ChangeMessageBubbleFontSize(e.NewValue);
        }

        private void ts07(object sender, SelectionChangedEventArgs e) {
            int index = p07.SelectedIndex;
            string key = null;

            BackgroundsExpander.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            ColorsExpander.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            if (index < 2) {
                AppParameters.CTEStyle = null;
                ApplicationData.Current.LocalSettings.Values.Remove("uis_accent");
                if (index == 1) { // Custom
                    CheckCustomStyle();
                } else {
                    AppParameters.ChatBackground = null;
                    AppParameters.CTEAccent = null;
                    Theme.ResetAccent();
                }
            } else {
                AppParameters.ChatBackground = null;
                AppParameters.CTEAccent = null;
                var theme = ChatThemeService.PrebuiltStyles[index - 2];
                AppParameters.CTEStyle = theme.Id;
                key = theme.Id;
            }

            new Action(async () => {
                await System.Threading.Tasks.Task.Delay(32);

                var cvm = demoUI.DataContext as ViewModel.ConversationViewModel;
                cvm.Style = key;
                ChatThemeService.UpdateTheme();
            })();
        }

        private void ts09(object sender, SelectionChangedEventArgs e) {
            int index = p09.SelectedIndex;
            Theme.ChangeChatsListItemTemplate(index == 1);
        }

        private void ts11(object sender, SelectionChangedEventArgs e) {
            int index = p11.SelectedIndex;
            AppParameters.ThumbsPosition = index == 1;

            // Reload sample messages
            var cvm = ViewModel.ConversationViewModel.GetSampleConversation();
            demoUI.DataContext = cvm;
        }

        bool eventRegistered = false;
        private void CheckCustomStyle() {
            var backgrounds = ChatThemeService.Backgrounds.OrderBy(b => b.Sort).ToList();
            backgrounds.Insert(0, null);
            BackgroundsList.ItemsSource = backgrounds;

            string custom = AppParameters.ChatBackground;
            if (!string.IsNullOrEmpty(custom)) {
                var b = backgrounds.Where(u => u?.Id == custom).FirstOrDefault();
                int index = backgrounds.IndexOf(b);
                BackgroundsList.SelectedIndex = b != null && index > 0 ? index : -1;
            } else {
                BackgroundsList.SelectedIndex = 0;
            }

            if (!eventRegistered) {
                eventRegistered = true;
                BackgroundsList.SelectionChanged += BackgroundsList_SelectionChanged;
            }

            List<Appearance> appearances = ChatThemeService.Appearances.OrderBy(a => a.Id).ToList();
            appearances.Insert(0, null);
            ColorsList.ItemsSource = appearances;

            string appearanceId = AppParameters.CTEAccent;
            if (!string.IsNullOrEmpty(appearanceId)) {
                var appearance = appearances.Where(a => a?.Id == appearanceId).FirstOrDefault();
                int index = appearances.IndexOf(appearance);
                ColorsList.SelectedIndex = index > 0 ? index : 0;
            } else {
                ColorsList.SelectedIndex = 0;
            }

            ColorsList.SelectionChanged += ColorsList_SelectionChanged;
        }

        private void BackgroundsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (BackgroundsList.SelectedIndex < 0) return;
            Background background = BackgroundsList.SelectedItem as Background;
            if (background == null) {
                AppParameters.ChatBackground = null;
                ChatThemeService.UpdateTheme();
                return;
            }
            AppParameters.ChatBackground = background.Id;
            ChatThemeService.UpdateTheme();
        }

        private void ColorsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ColorsList.SelectedIndex < 0) return;

            Appearance appearance = ColorsList.SelectedItem as Appearance;
            if (appearance == null) {
                AppParameters.CTEAccent = null;
            } else {
                AppParameters.CTEAccent = appearance.Id;
            }
            ChatThemeService.UpdateTheme();
        }


        private void ChooseFileForBackground(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                FileOpenPicker fop = new FileOpenPicker();
                fop.FileTypeFilter.Add(".jpg");
                fop.FileTypeFilter.Add(".png");
                fop.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                fop.ViewMode = PickerViewMode.Thumbnail;

                StorageFile file = await fop.PickSingleFileAsync();
                if (file != null) {
                    await ChangeChatBackgroundFromFileAsync(file);
                }
            })();
        }

        private async Task ChangeChatBackgroundFromFileAsync(StorageFile file) {
            // Delete old imported
            string b = AppParameters.ChatBackground;
            if (!string.IsNullOrEmpty(b) && b.StartsWith($"ms-appdata:///{ChatThemeService.ChatThemesCacheFolder}/")) {
                Log.Info($"Interface.ChangeChatBackgroundFromFileAsync: Removing old background file: {b}");
                StorageFile old = await StorageFile.GetFileFromApplicationUriAsync(new Uri(b));
                try {
                    await old.DeleteAsync(StorageDeleteOption.PermanentDelete);
                } catch (Exception ex) {
                    Log.Error($"ChangeChatBackgroundFromFileAsync: Failed to delete old background! HR: 0x{ex.HResult.ToString("x8")}, file: {old.Path}");
                }
            }

            Log.Info($"Interface.ChangeChatBackgroundFromFileAsync: Copying file to local storage: {file.Path}");
            StorageFile copied = await file.CopyAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync(ChatThemeService.ChatThemesCacheFolder, CreationCollisionOption.OpenIfExists), $"{Guid.NewGuid()}{file.FileType}");
            AppParameters.ChatBackground = $"ms-appdata:///Local/{ChatThemeService.ChatThemesCacheFolder}/{copied.Name}";
            ChatThemeService.UpdateTheme();
        }

        private void DrawGradient(FrameworkElement sender, DataContextChangedEventArgs args) {
            Border root = sender as Border;
            if (args.NewValue == null) {
                root.Background = new SolidColorBrush(Colors.Transparent);
                ToolTipService.SetToolTip(root, null);
                return;
            }

            Appearance appearance = args.NewValue as Appearance;
            ToolTipService.SetToolTip(root, appearance.Id);
            var themed = Theme.IsDarkTheme() ? appearance.Dark : appearance.Light;
            var gradient = themed.BubbleGradient.Colors;
            LinearGradientBrush lgb = new LinearGradientBrush() {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            double diff = (double)1 / (gradient.Count - 1);
            for (int i = 0; i < gradient.Count; i++) {
                double stop = i * diff;
                lgb.GradientStops.Add(new GradientStop() { Color = ChatThemeService.ParseHex(gradient[i]), Offset = stop });
            }

            root.Background = lgb;
        }


        private void ts08(object sender, RoutedEventArgs e) {
            AppParameters.CTEIgnoreChatTheme = !AppParameters.CTEIgnoreChatTheme;
            ChatThemeService.UpdateTheme();
        }

        private void ts10(object sender, RoutedEventArgs e) {
            AppParameters.LastMessagePreview = !AppParameters.LastMessagePreview;
        }
    }
}