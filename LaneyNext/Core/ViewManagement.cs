using ELOR.VKAPILib;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.Views;
using Elorucov.Laney.Views.Settings;
using Elorucov.Laney.VKAPIExecute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Core
{
    public enum ViewType
    {
        Default = 0, Session = 1, SingleConversation = 2, Landing = 3, Settings = 4, AudioPlayer = 5, VideoPlayer = 6
    }

    public enum SessionCreateMethod
    {
        Unknown, SessionReplaced, NewWindowOpened, SwitchToOpenedWindow, MainWindowRestored
    }

    public class ViewManagement
    {
        public static UISettings UISettings = new UISettings();
        public static ViewType CurrentViewType
        {
            get
            {
                return CoreApplication.GetCurrentView().Properties.ContainsKey(Constants.ViewTypeKey) ?
                    (ViewType)CoreApplication.GetCurrentView().Properties[Constants.ViewTypeKey] : ViewType.Default;
            }
        }

        public static bool IsWindowInForeground { get { return Window.Current.CoreWindow.ActivationMode == Windows.UI.Core.CoreWindowActivationMode.ActivatedInForeground; } }

        public static async Task<bool> OpenNewWindow(Type page, ViewType viewType, string title = null, object data = null, bool fullScreen = false, Action<CoreApplicationView> windowCreatedCallback = null, Action<bool, Frame> windowOpenedCallback = null)
        {
            Log.General.Info(String.Empty, new ValueSet { { "page", page.ToString() }, { "view_type", viewType.ToString() }, { "title", title }, { "fullscreen", fullScreen } });
            var currentAV = ApplicationView.GetForCurrentView();
            Window newWindow = null;
            var newAV = CoreApplication.CreateNewView();
            bool result = false;
            await newAV.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                newWindow = Window.Current;
                                var newAppView = ApplicationView.GetForCurrentView();
                                if (!String.IsNullOrEmpty(title)) newAppView.Title = title;

                                newAppView.Consolidated += (a, b) =>
                                {
                                    newAV.CoreWindow.Close();
                                    Window.Current.Content = null;
                                    CoreApplication.DecrementApplicationUseCount();
                                };
                                newAV.TitleBar.ExtendViewIntoTitleBar = true;
                                CoreApplication.IncrementApplicationUseCount();
                                newAV.Properties[Constants.ViewTypeKey] = viewType;
                                newAppView.SetPreferredMinSize(new Size(360, 500));
                                newWindow.Activated += (a, b) =>
                                {
                                    if (fullScreen) newAppView.TryEnterFullScreenMode();
                                };

                                newWindow.CoreWindow.Activate();
                                await Task.Yield();
                                windowCreatedCallback?.Invoke(newAV);
                                var frame = new Frame();
                                newWindow.Content = frame;
                                ThemeManager.ApplyThemeAsync();
                                frame.Navigate(page, data);

                                result = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id, ViewSizePreference.Custom, currentAV.Id, ViewSizePreference.Custom);
                                if (result) windowOpenedCallback?.Invoke(result, frame);
                            });
            return result;
        }

        public static async Task<bool> OpenCompactOverlayWindow(Type page, ViewType viewType, Size windowSize, string title = null, object data = null, Action<CoreApplicationView> windowCreatedCallback = null, Action<bool, Frame> windowOpenedCallback = null)
        {
            Log.General.Info(String.Empty, new ValueSet { { "page", page.ToString() }, { "view_type", viewType.ToString() }, { "title", title }, { "size", windowSize } });
            ViewModePreferences vmpref = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            vmpref.CustomSize = windowSize;
            var currentAV = ApplicationView.GetForCurrentView();
            Window newWindow = null;
            var newAV = CoreApplication.CreateNewView();
            bool result = false;
            await newAV.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                newWindow = Window.Current;
                                var newAppView = ApplicationView.GetForCurrentView();
                                if (!String.IsNullOrEmpty(title)) newAppView.Title = title;

                                newAppView.Consolidated += (a, b) =>
                                {
                                    newAV.CoreWindow.Close();
                                    CoreApplication.DecrementApplicationUseCount();
                                };
                                newAV.TitleBar.ExtendViewIntoTitleBar = true;
                                CoreApplication.IncrementApplicationUseCount();
                                newAV.Properties[Constants.ViewTypeKey] = viewType;
                                newAppView.SetPreferredMinSize(windowSize);

                                newWindow.CoreWindow.Activate();
                                await Task.Yield();
                                windowCreatedCallback?.Invoke(newAV);
                                var frame = new Frame();
                                newWindow.Content = frame;
                                ThemeManager.ApplyThemeAsync();
                                frame.Navigate(page, data);

                                result = await ApplicationViewSwitcher.TryShowAsViewModeAsync(newAppView.Id, ApplicationViewMode.CompactOverlay, vmpref);
                                if (result) windowOpenedCallback?.Invoke(result, frame);
                            });
            return result;
        }

        public static async void SwitchToView(CoreApplicationView view, System.Action actionAfterSwitch = null)
        {
            var fromView = ApplicationView.GetForCurrentView();
            await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var appView = ApplicationView.GetForCurrentView();
                await ApplicationViewSwitcher.SwitchAsync(appView.Id, fromView.Id);
                if (actionAfterSwitch != null) actionAfterSwitch.Invoke();
            });
        }

        public static async void CloseAllAnotherWindows()
        {
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (!view.IsMain) view.CoreWindow.Close();
                });
            }
        }

        public static async Task<CoreApplicationView> GetViewBySession(VKSession session)
        {
            CoreApplicationView cav = null;
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (CurrentViewType == ViewType.Session && view.Properties.ContainsKey("session"))
                    {
                        VKSession vs = view.Properties["session"] as VKSession;
                        if (VKSession.Compare(vs, session))
                        {
                            cav = view;
                        }
                    }
                });
                if (cav != null) break;
            }
            Log.General.Info($"View found: {cav != null}");
            return cav;
        }

        public static async Task<SessionCreateMethod> OpenSession(VKSession session, bool openInCurrentWindow = false)
        {
            Log.General.Info(String.Empty, new ValueSet { { "session_id", session.Id }, { "open_in_current_window", openInCurrentWindow } });
            CoreApplicationView cav = await GetViewBySession(session);
            if (cav != null)
            {
                bool isVisible = false;
                bool isMainWindowRestored = false;
                bool isRunned = false;

                await cav.Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
                {
                    isVisible = cav.CoreWindow.Visible;
                    if (!isVisible)
                    {
                        if (!openInCurrentWindow)
                        {
                            VKSession.BindSessionToCurrentView(session);
                            await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                        }
                        else
                        {
                            if (cav.IsMain)
                            {
                                bool res = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                                cav.CoreWindow.Activate();
                                Window.Current.Activate();
                                bool s = (Window.Current.Content as Frame).Focus(FocusState.Programmatic);
                                System.Diagnostics.Debug.WriteLine($"View is hidden, but it is main. openInCurrentWindow parameter is ignored!\nTryEnterViewModeAsync Result: {res}. Focus result: {s}");
                                isMainWindowRestored = true;
                            }
                            else
                            {
                                cav.CoreWindow.Close();
                            }
                        }
                    }
                    isRunned = true;
                });

                while (!isRunned)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }

                Log.General.Info(String.Empty, new ValueSet { { "is_visible", isVisible }, { "is_main_window_restored", isMainWindowRestored } });

                if (isVisible)
                {
                    SwitchToView(cav);
                    return SessionCreateMethod.SwitchToOpenedWindow;
                }
                else
                {
                    if (openInCurrentWindow)
                    {
                        if (isMainWindowRestored) return SessionCreateMethod.MainWindowRestored;
                        VKSession.BindSessionToCurrentView(session);
                        (Window.Current.Content as Frame).Navigate(typeof(Shell));
                        return SessionCreateMethod.SessionReplaced;
                    }
                    else
                    {
                        return SessionCreateMethod.NewWindowOpened;
                    }
                }
            }
            else
            {
                if (!openInCurrentWindow)
                {
                    bool ka = await OpenNewWindow(typeof(Shell), ViewType.Session, session.DisplayName, null, false, (appview) =>
                    {
                        VKSession.BindSessionToView(session, appview);
                    });
                    return ka ? SessionCreateMethod.NewWindowOpened : SessionCreateMethod.Unknown;
                }
                else
                {
                    VKSession.BindSessionToCurrentView(session);
                    (Window.Current.Content as Frame).Content = new Shell();
                    return SessionCreateMethod.SessionReplaced;
                }
            }
        }

        internal static async void OpenLandingPage()
        {
            foreach (var view in CoreApplication.Views)
            {
                bool isMainView = false;
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    isMainView = view.IsMain;
                    if (isMainView)
                    {
                        Frame frame = new Frame();
                        frame.Navigate(typeof(Views.FirstSetup.WelcomeView), null);
                        Window.Current.Content = frame;
                    }
                });
                if (isMainView) break;
            }
        }

        #region Standalone conversation views

        private static Dictionary<ConversationViewModel, CoreApplicationView> StandaloneConversations = new Dictionary<ConversationViewModel, CoreApplicationView>();

        public static bool AddToStandaloneConversations(ConversationViewModel cvm)
        {
            if (StandaloneConversations.ContainsKey(cvm)) return false;
            StandaloneConversations.Add(cvm, CoreApplication.GetCurrentView());
            return true;
        }

        public static bool RemoveFromStandaloneConversations(ConversationViewModel cvm)
        {
            if (!StandaloneConversations.ContainsKey(cvm)) return false;
            return StandaloneConversations.Remove(cvm);
        }

        public static bool StandaloneConversationsContains(ConversationViewModel cvm)
        {
            return StandaloneConversations.ContainsKey(cvm);
        }

        public static async Task<bool> SwitchToOpenedStandaloneConversationAsync(ConversationViewModel cvm)
        {
            if (!StandaloneConversations.ContainsKey(cvm)) return false;
            var fromView = ApplicationView.GetForCurrentView();
            bool res = false;
            await StandaloneConversations[cvm].Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var appView = ApplicationView.GetForCurrentView();
                await ApplicationViewSwitcher.SwitchAsync(appView.Id, fromView.Id);
                res = true;
            });
            return res;
        }

        public static ConversationViewModel GetStandaloneConversationByWindow()
        {
            if (CurrentViewType != ViewType.SingleConversation || StandaloneConversations.Count == 0) return null;
            foreach (var k in StandaloneConversations)
            {
                if (k.Value == CoreApplication.GetCurrentView()) return k.Key;
            }
            return null;
        }

        #endregion

        private static void BindVKAPIToView(VKAPI api)
        {
            var props = CoreApplication.GetCurrentView().Properties;
            if (props.ContainsKey("api"))
            {
                props["api"] = api;
            }
            else
            {
                props.Add("api", api);
            }
        }

        public static VKAPI GetVKAPIInstanceForCurrentView()
        {
            var props = CoreApplication.GetCurrentView().Properties;
            if (VKSession.Current != null)
            {
                return VKSession.Current.API;
            }
            else
            {
                return props.ContainsKey("api") ? props["api"] as VKAPI : null;
            }
        }

        public static Execute GetVKAPIExecuteInstanceForCurrentView()
        {
            return GetVKAPIInstanceForCurrentView()?.Execute as Execute;
        }

        #region Additional windows (settings, photo viewer, etc.)

        public static async Task OpenSettingsWindow(VKAPI api)
        {
            CoreApplicationView cav = null;
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (CurrentViewType == ViewType.Settings)
                    {
                        cav = view;
                    }
                });
                if (cav != null) break;
            }
            Log.General.Info("Found another view with Settings view type.");
            if (cav != null)
            {
                bool isVisible = false;
                await cav.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    isVisible = cav.CoreWindow.Visible;
                    if (!isVisible) await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                });
                if (isVisible)
                {
                    SwitchToView(cav);
                }
            }
            else
            {
                await OpenNewWindow(typeof(Page), ViewType.Settings, Locale.Get("settings"), null, false, null,
                (success, frame) =>
                {
                    if (!success) return;
                    CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                    BindVKAPIToView(api);
                    frame.Content = new SettingsView();
                });
            }
            // (Window.Current.Content as Frame).Navigate(typeof(SettingsView));
        }

        public static async void OpenPhotoViewer(List<AttachmentBase> attachments, AttachmentBase selected)
        {
            VKSession caller = VKSession.Current;
            ApplicationView nview = null;
            await OpenNewWindow(typeof(Views.PhotoViewer), ViewType.Default, Locale.Get("photoviewer"), null, true, null,
                (success, frame) =>
                {
                    if (!success) return;
                    (frame.Content as Page).DataContext = new PhotoViewerViewModel(attachments, selected, caller);
                    nview = ApplicationView.GetForCurrentView();
                });
            ApplicationView.GetForCurrentView().Consolidated += async (a, b) =>
            {
                await nview.TryConsolidateAsync();
            };
        }

        private static async Task OpenCompactOverlayWindowEx(Type page, ViewType type, Size size, string title, object data = null, System.Action<CoreApplicationView> createdCallback = null)
        {
            VKSession session = VKSession.Current;

            CoreApplicationView cav = null;
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (CurrentViewType == type)
                    {
                        cav = view;
                    }
                });
                if (cav != null) break;
            }
            Log.General.Info("Found view with same view type.", new ValueSet { { "view_type", type.ToString() } });
            if (cav != null)
            {
                bool isVisible = false;
                await cav.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    isVisible = cav.CoreWindow.Visible;
                    if (!isVisible) await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                });
                if (isVisible)
                {
                    SwitchToView(cav);
                }
            }
            else
            {
                await OpenCompactOverlayWindow(page, type, size, title, data, createdCallback);
            }
        }

        public static async Task OpenAudioPlayer()
        {
            await OpenCompactOverlayWindowEx(typeof(AudioPlayer), ViewType.AudioPlayer, new Size(384, Constants.AudioPlayerFaceHeight), Locale.Get("audioplayer"));
        }

        public static async Task OpenVideoPlayer(Video video)
        {
            VKSession session = VKSession.Current;
            await OpenCompactOverlayWindowEx(typeof(VideoPlayer), ViewType.VideoPlayer, video.GetConstraintSize(480), Locale.Get("videoplayer"), new Tuple<Video, VKAPI>(video, session.API));
        }

        #endregion

        public static void EnableFocusForCurrentWindow()
        {
            FrameworkElement root = Window.Current.Content as FrameworkElement;
            root.IsHitTestVisible = true;
        }

        public static void DisableFocusForCurrentWindow()
        {
            FrameworkElement root = Window.Current.Content as FrameworkElement;
            root.IsHitTestVisible = false;
        }
    }
}