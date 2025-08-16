using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services.UI {
    public class TitleAndStatusBar {
        static Dictionary<int, Popup> WindowCustomTitleBars = new Dictionary<int, Popup>();
        static Dictionary<int, Popup> WindowCustomTitleBarsGarland = new Dictionary<int, Popup>();

        static TitleAndStatusBar() {
            new System.Action(async () => { await SetupChimeSound(); })();
        }

        private static async Task SetupChimeSound() {
            // Chime sound
            if (Functions.IsHoliday) {
                try {
                    chime = new Windows.Media.Playback.MediaPlayer {
                        AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.Alerts
                    };
                    chime.CommandManager.IsEnabled = false;
                    var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/chime.mp3"));
                    chime.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);
                } catch (Exception ex) {
                    Logger.Log.Warn($"Failed to initialize media player for chime sound! 0x{ex.HResult.ToString("x8")}");
                }
            }
        }

        public static bool ExtendView(bool extend, bool dontAddCustomTitlebar = false) {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                ApplicationViewTitleBar tb = ApplicationView.GetForCurrentView().TitleBar;
                bool res = CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = extend;
                if (!dontAddCustomTitlebar) SetupCustomTitleBar(extend);
                return res;
            } else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                return ApplicationView.GetForCurrentView().SetDesiredBoundsMode(extend ? ApplicationViewBoundsMode.UseCoreWindow : ApplicationViewBoundsMode.UseVisible);
            } else {
                return false;
            }
        }

        public static async Task ChangeColor(Color? color) {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                ApplicationViewTitleBar tb = ApplicationView.GetForCurrentView().TitleBar;
                tb.ForegroundColor = color;
                tb.InactiveForegroundColor = color;
                tb.ButtonForegroundColor = color;
                tb.ButtonInactiveForegroundColor = color;
                await ChangeCustomTitlebarColor(color);
            } else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                StatusBar sb = StatusBar.GetForCurrentView();
                sb.ForegroundColor = color;
            }
        }

        public static void ChangeBackgroundColor(Color? color) {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                ApplicationViewTitleBar tb = ApplicationView.GetForCurrentView().TitleBar;
                tb.BackgroundColor = color;
                tb.InactiveBackgroundColor = color;
                tb.ButtonBackgroundColor = color;
                tb.ButtonInactiveBackgroundColor = color;
            } else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                StatusBar sb = StatusBar.GetForCurrentView();
                sb.BackgroundOpacity = 0;
                sb.BackgroundColor = color;
            }
        }

        public static async Task SetTitleText(string txt = null) {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                try {
                    if (string.IsNullOrEmpty(txt)) txt = Package.Current.DisplayName;
                    // WindowName.Text = txt;

                    var id = ApplicationView.GetForCurrentView().Id;
                    if (WindowCustomTitleBars.ContainsKey(id)) {
                        Popup p = WindowCustomTitleBars[id];

                        TextBlock tb = (p.Child as FrameworkElement).FindControlByName<TextBlock>("WindowName");
                        if (tb != null) tb.Text = txt;
                    }
                } catch (Exception ex) {
                    // Редко, но ApplicationView.GetForCurrentView() кидает исключение. Да, системное блять API кидает непонятное сука исключение.
                    Log.Error($"An error occured in TitleAndStatusBar.SetTitleText! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                }
            } else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                StatusBarProgressIndicator sbpi = StatusBar.GetForCurrentView().ProgressIndicator;
                sbpi.ProgressValue = 0;
                if (string.IsNullOrEmpty(txt)) {
                    await sbpi.HideAsync();
                } else {
                    sbpi.Text = txt;
                    await sbpi.ShowAsync();
                }
            }
        }

        static Windows.Media.Playback.MediaPlayer chime;

        public static async Task ShowGarland() {
            var id = ApplicationView.GetForCurrentView().Id;
            if (!WindowCustomTitleBarsGarland.ContainsKey(id)) {
                chime?.Play();
                Popup p = CreateCustomTitleBarGarlandPopup();
                p.IsOpen = true;
                WindowCustomTitleBarsGarland.Add(id, p);

                SetupCustomTitleBar(false);
                await Task.Delay(16);
                SetupCustomTitleBar(true);
            }
        }

        private static void SetupCustomTitleBar(bool isVisible) {
            var id = ApplicationView.GetForCurrentView().Id;
            if (!WindowCustomTitleBars.ContainsKey(id)) {
                Popup p = CreateCustomTitleBarPopup();
                p.IsOpen = isVisible;
                WindowCustomTitleBars.Add(id, p);
            } else {
                WindowCustomTitleBars[id].IsOpen = isVisible;
            }
        }

        private static Popup CreateCustomTitleBarPopup() {
            var sysTB = CoreApplication.GetCurrentView().TitleBar;
            var view = CoreApplication.GetCurrentView();

            ContentPresenter b = new ContentPresenter {
                Background = new SolidColorBrush(Colors.Transparent),
                ContentTemplate = (DataTemplate)App.Current.Resources["TitlebarContent"],
            };

            var popup = new Popup {
                Child = b,
            };

            FixSize(b, popup, sysTB, Window.Current.Bounds.Width);

            sysTB.LayoutMetricsChanged += async (x, y) => {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FixSize(b, popup, x, Window.Current.Bounds.Width));
            };
            Window.Current.SizeChanged += async (x, y) => {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    var tb = CoreApplication.GetCurrentView().TitleBar;
                    FixSize(b, popup, tb, Window.Current.Bounds.Width);
                });
            };
            Window.Current.SetTitleBar(b);

            // Надо, чтобы titlebar над модалкой отрисовывался. 
            ModalsManager.NewModalOpened += async (v, m) => {
                try {
                    await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                        if (popup.IsOpen) {
                            popup.IsOpen = false;
                            await Task.Delay(16);
                            popup.IsOpen = true;
                        }
                    });
                } catch (Exception ex) {
                    Logger.Log.Error($"Cannot fix custom titlebar popup! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                }
            };

            return popup;
        }

        private static Popup CreateCustomTitleBarGarlandPopup() {
            var sysTB = CoreApplication.GetCurrentView().TitleBar;
            var view = CoreApplication.GetCurrentView();

            ContentPresenter b = new ContentPresenter {
                Background = new SolidColorBrush(Colors.Transparent),
                ContentTemplate = (DataTemplate)App.Current.Resources["TitlebarGarland"],
            };

            var popup = new Popup {
                Child = b,
                Height = 22,
                IsHitTestVisible = false
            };

            FixSizeGarland(b, popup, Window.Current.Bounds.Width);

            Window.Current.SizeChanged += async (x, y) => {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    var tb = CoreApplication.GetCurrentView().TitleBar;
                    FixSizeGarland(b, popup, Window.Current.Bounds.Width);
                });
            };

            // Надо, чтобы titlebar над модалкой отрисовывался. 
            ModalsManager.NewModalOpened += async (v, m) => {
                try {
                    await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                        if (popup.IsOpen) {
                            popup.IsOpen = false;
                            await Task.Delay(20);
                            popup.IsOpen = true;
                        }
                    });
                } catch (Exception ex) {
                    Logger.Log.Error($"Cannot fix custom titlebar (garland) popup! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                }
            };

            return popup;
        }

        private static void FixSize(FrameworkElement c, Popup p, CoreApplicationViewTitleBar tb, double width) {
            var cw = width - tb.SystemOverlayLeftInset - tb.SystemOverlayRightInset;
            var pw = width - tb.SystemOverlayRightInset;
            c.Margin = new Thickness(tb.SystemOverlayLeftInset, 0, 0, 0);
            c.Width = cw; c.Height = tb.Height;
            p.Width = pw; c.Height = tb.Height;
        }

        private static void FixSizeGarland(FrameworkElement c, Popup p, double width) {
            c.Width = width + 24;
            p.Width = width + 24;
        }

        private static async Task ChangeCustomTitlebarColor(Color? color) {
            var id = ApplicationView.GetForCurrentView().Id;
            if (WindowCustomTitleBars.ContainsKey(id)) {
                Popup p = WindowCustomTitleBars[id];

                TextBlock tb = (p.Child as FrameworkElement).FindControlByName<TextBlock>("WindowName");
                Viewbox icon = (p.Child as FrameworkElement).FindControlByName<Viewbox>("WindowIcon");
                if (color != null) {
                    Color c = color.Value;
                    if (tb != null) tb.Foreground = new SolidColorBrush(c);
                    // Т. к. цвет кнопок в системном titlebar мы всегда меняем на белый в просмотрщике фото и историй,
                    // то цвет лого будем менять так:
                    if (icon != null) icon.RequestedTheme = c.B == 255 && c.G == 255 && c.R == 255 ? ElementTheme.Dark : ElementTheme.Default;
                } else {
                    // Заново рендерим titlebar, чё делать-то...
                    string title = tb.Text;

                    p.Child = new ContentPresenter {
                        Background = new SolidColorBrush(Colors.Transparent),
                        ContentTemplate = (DataTemplate)App.Current.Resources["TitlebarContent"],
                    };
                    await SetTitleText(title);
                }
            }
        }
    }
}