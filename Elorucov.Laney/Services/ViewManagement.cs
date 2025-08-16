using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Services {
    public enum WindowType { Main, Hosted, ContactPanel, StandaloneConversation }

    public class ViewManagement {
        public static WindowType GetWindowType() {
            var cav = CoreApplication.GetCurrentView();
            var cprop = cav.CoreWindow.CustomProperties;
            if (cprop.ContainsKey("type")) {
                string s = cprop["type"].ToString();
                switch (s) {
                    case "host": return WindowType.Hosted;
                    case "copa": return WindowType.ContactPanel;
                    case "conv": return WindowType.StandaloneConversation;
                    default: return WindowType.Main;
                }
            } else {
                return WindowType.Main;
            }
        }

        public static async Task<bool> OpenNewWindow(Type page, string title, object data = null, bool closeOnMainWindowClosing = false) {
            var currentAV = ApplicationView.GetForCurrentView();
            Window newWindow = null;
            var newAV = CoreApplication.CreateNewView();
            bool result = false;
            await newAV.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            async () => {
                                newWindow = Window.Current;
                                var newAppView = ApplicationView.GetForCurrentView();
                                newAppView.Title = title;

                                newAppView.SetPreferredMinSize(new Size(420, 500));

                                var frame = new Frame();
                                frame.Navigate(page, data);
                                newWindow.Content = frame;
                                newWindow.Activate();

                                //result = await ApplicationViewSwitcher.TryShowAsViewModeAsync(newAppView.Id, compactOverlay ? ApplicationViewMode.CompactOverlay : ApplicationViewMode.Default, pref);
                                result = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id, ViewSizePreference.Custom, currentAV.Id, ViewSizePreference.Custom);

                            });
            if (closeOnMainWindowClosing) currentAV.Consolidated += async (a, b) => {
                await newAV.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { newWindow?.Close(); });
            };
            return result;
        }

        public static async Task<bool> OpenNewCompactOverlayWindow(ViewModePreferences pref, Type page, string title, object data = null, bool closeOnMainWindowClosing = false) {
            var currentAV = ApplicationView.GetForCurrentView();
            Window newWindow = null;
            var newAV = CoreApplication.CreateNewView();
            bool result = false;
            await newAV.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            async () => {
                                newWindow = Window.Current;
                                var newAppView = ApplicationView.GetForCurrentView();
                                newAppView.Title = title;

                                newAppView.SetPreferredMinSize(new Size(320, 130));

                                var frame = new Frame();
                                newWindow.Content = frame;
                                newWindow.Activate();
                                frame.Navigate(page, data);

                                result = AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop" ?
                                await ApplicationViewSwitcher.TryShowAsViewModeAsync(newAppView.Id, ApplicationViewMode.CompactOverlay, pref) :
                                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id, ViewSizePreference.UseMinimum, currentAV.Id, ViewSizePreference.UseNone);
                            });
            if (closeOnMainWindowClosing) currentAV.Consolidated += async (a, b) => {
                await newAV.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { newWindow?.Close(); });
            };
            return result;
        }

        public static async Task CloseAllAnotherWindowsAsync() {
            foreach (var view in CoreApplication.Views) {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    if (!view.IsMain) view.CoreWindow.Close();
                });
            }
        }
    }
}
