using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.MyPeople;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.PushNotifications;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI;
using Microsoft.QueryStringDotNET;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Elorucov.Laney {
    /// <summary>
    /// Обеспечивает зависящее от конкретного приложения поведение, дополняющее класс Application по умолчанию.
    /// </summary>
    sealed partial class App : Application {
        /// <summary>
        /// Инициализирует одноэлементный объект приложения. Это первая выполняемая строка разрабатываемого
        /// кода; поэтому она является логическим эквивалентом main() или WinMain().
        /// </summary>
        public App() {
            InitializeComponent();
            new System.Action(async () => { await Log.InitAsync(); })();
            API.RequestCallback = SendRequestToAPIViaLNetAsync;
            ChangeTheme();
            Suspending += OnSuspending;
            Resuming += OnResuming;
        }

        public static UISettings UISettings = new UISettings();

        public static NavigationTransitionInfo DefaultNavEntranceTransition {
            get {
                return GetDefaultEntranceTransition();
            }
        }

        public static NavigationTransitionInfo DefaultNavTransition {
            get {
                return GetDefaultNavTransition(false);
            }
        }

        public static NavigationTransitionInfo DefaultBackNavTransition {
            get {
                return GetDefaultNavTransition(false); // Не надо прописать true, SlideNavigationTransitionInfo сам чекает, возвращаемся ли мы назад или нет.
            }
        }

        private static NavigationTransitionInfo GetDefaultEntranceTransition() {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5)) {
                return new SlideNavigationTransitionInfo {
                    Effect = SlideNavigationTransitionEffect.FromBottom
                };
            } else {
                return new DrillInNavigationTransitionInfo();
            }
        }

        private static NavigationTransitionInfo GetDefaultNavTransition(bool back) {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5)) {
                return new SlideNavigationTransitionInfo {
                    Effect = back ? SlideNavigationTransitionEffect.FromLeft : SlideNavigationTransitionEffect.FromRight
                };
            } else {
                return new DrillInNavigationTransitionInfo();
            }
        }

        public static bool IsNarrowWindow { get; private set; }

        private bool IsUserLogged = false;
        public static bool Launched { get; private set; } = false;
        public bool MainAppWindowDisplayed { get; private set; } = false;
        public static Timer OnlineTimer { get; set; }
        public static bool IsDefaultTheme { get; private set; } = true;
        // public static MicrosoftStoreUpdater MSStoreUpdater { get; private set; } = new MicrosoftStoreUpdater();
        public static bool IsUpdateIsReadyToApply { get; private set; }

        private void ChangeTheme() {
            switch (AppParameters.Theme) {
                case 1: RequestedTheme = ApplicationTheme.Light; IsDefaultTheme = false; break;
                case 2: RequestedTheme = ApplicationTheme.Dark; IsDefaultTheme = false; break;
            }
        }

        // Грязный хак, но к сожалению,
        // через стили XAML акцентная кнопка не фиксится.
        private void FixStyles() {
            if (IsUserLogged) return;
            try {
                Style accentButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style;
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) {
                    accentButtonStyle.Setters.Add(new Setter {
                        Property = Button.BackgroundSizingProperty,
                        Value = BackgroundSizing.InnerBorderEdge
                    });
                } else if (Functions.GetOSBuild() == 15063) {
                    accentButtonStyle.Setters.Add(new Setter {
                        Property = Button.PaddingProperty,
                        Value = new Thickness(11, 5, 11, 5)
                    });
                }
            } catch { }
        }

        public static void FixMicaBackground() {
            Frame rootFrame = Window.Current.Content as Frame;
            if (Theme.IsMicaAvailable) BackdropMaterial.SetApplyToRootOrPageBackground(rootFrame, true);
        }

        private async Task LaunchThis(IActivatedEventArgs args) {
            CoreApplication.UnhandledErrorDetected += CoreApplication_UnhandledErrorDetected;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            // Windows.Storage.ApplicationData.Current.LocalSettings.Values["version"] = ApplicationInfo.GetVersion(true);
            Log.Info($"{GetType().Name} > LaunchThis: Activated args: Kind={args.Kind}; PrevExecState={args.PreviousExecutionState}.");

            // Emoji font
            bool localAvailable = await EmojiFontManager.CheckAsync();
            if (localAvailable && AppParameters.AlternativeEmojiFont) {
                string emojiFontSource = EmojiFontManager.LocalSource + "#Segoe UI Emoji,Segoe UI Variable,XamlAutoFontFamily";
                Log.Info($"{GetType().Name} > LaunchThis: Applying emoji font.");
                App.Current.Resources["ContentControlThemeFontFamily"] = new FontFamily(emojiFontSource);
            }

            //if (AppParameters.Accent.A == 255) {
            //    Theme.ChangeAccentColor(AppParameters.Accent, true);
            //}
            FixStyles();

            // Migrating old id to long id
            string verstr = (string)ApplicationData.Current.LocalSettings.Values["version"];
            if (!string.IsNullOrEmpty(verstr)) {
                try {
                    int minor = Convert.ToInt32(verstr.Split(".")[1]);
                    int build = Convert.ToInt32(verstr.Split(".")[2]);
                    if (minor < 17 && build < 146) {
                        if (ApplicationData.Current.LocalSettings.Values["id"] != null && ApplicationData.Current.LocalSettings.Values["id"] is int oldId) {
                            AppParameters.UserID = oldId;
                        }
                    }
                } catch { }
            }

            int state = 0;
            state = AppParameters.UserID != 0 && AppParameters.AccessToken != null ? 1 : 0;

            Log.Info($"{GetType().Name} > LaunchThis: State: {state}.");

            // Clear badge and tile
            try {
                BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            } catch (Exception ex) {
                Log.Error($"{GetType().Name} > An error occured when clearing badge & tile! 0x{ex.HResult.ToString("x8")}");
            }

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null) {
                Log.Info($"{GetType().Name} > RootFrame is null.");
                rootFrame = new Frame();

                TitleAndStatusBar.ExtendView(true);

                if (Theme.IsMicaAvailable) BackdropMaterial.SetApplyToRootOrPageBackground(rootFrame, true);

                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(420, 500));
            Window.Current.Activate();
            Window.Current.CoreWindow.ResizeCompleted += CoreWindow_ResizeCompleted;
            IsNarrowWindow = Window.Current.Bounds.Width < 420;

            if ((args.Kind == ActivationKind.Launch && args is LaunchActivatedEventArgs) ||
                args.Kind == ActivationKind.ToastNotification && args is ToastNotificationActivatedEventArgs) {
                LaunchActivatedEventArgs largs = args as LaunchActivatedEventArgs;

                if (Launched) {
                    await Main.GetCurrent()?.ParseArgs(args);
                }

                var cusprop = CoreApplication.GetCurrentView().CoreWindow.CustomProperties;
                if (!cusprop.ContainsKey("type")) cusprop.Add("type", "main");
                if (rootFrame.Content == null) {
                    switch (state) {
                        case 0:
                            Log.Warn($"{GetType().Name} > User not logged.");
                            rootFrame.Navigate(typeof(WelcomePage), largs.Arguments);
                            break;
                        case 1:
                            Log.Info($"{GetType().Name} > User logged: {AppParameters.UserID}");
                            CheckPasscode(rootFrame, () => {
                                IsUserLogged = true;
                                MainAppWindowDisplayed = true;
                                rootFrame.Navigate(typeof(Main), args);
                                APIHelper.SetOnlinePeriodically();
                            }, true);
                            break;
                    }
                }
            } else if (args.Kind == ActivationKind.Protocol) {
                ProtocolActivatedEventArgs a = args as ProtocolActivatedEventArgs;
                Log.Info($"{GetType().Name} > Uri activation: {a.Uri}");
                string scheme = a.Uri.Scheme;

                if (scheme == "lny") {
                    if (state != 1) {
                        if (!Launched) Application.Current.Exit();
                    } else {
                        switch (a.Uri.Host) {
                            case "debug":
                                if (Launched) {
                                    new System.Action(async () => { await OpenDebugWindow(); })();
                                } else {
                                    CheckPasscode(rootFrame, () => {
                                        rootFrame.Navigate(typeof(PreviewDebug.Menu), null);
                                    }, false);
                                }
                                break;
                            case "stats":
                                long peerId = 0;
                                var q = QueryString.Parse(a.Uri.Query.Substring(1));
                                if (q.Contains("peerId") && long.TryParse(q["peerId"], out peerId)) {
                                    if (Launched) {
                                        await ViewManagement.OpenNewWindow(typeof(Pages.StatsPage), Locale.Get("message_stats"), peerId);
                                    } else {
                                        CheckPasscode(rootFrame, () => {
                                            rootFrame.Navigate(typeof(Pages.StatsPage), peerId);
                                        }, false);
                                    }
                                } else {
                                    if (!Launched) Exit();
                                }
                                break;
                            default:
                                if (!Launched) Exit();
                                break;
                        }
                    }
                } else if (scheme == "vk") {
                    if (Launched) {
                        await Main.GetCurrent()?.ParseArgs(args);
                        return;
                    }
                    if (a.Uri.Host != "vk.com") Exit();
                    var match = VKLinks._writeReg.Match(a.Uri.AbsolutePath);
                    if (match.Success) {
                        long id = 0;
                        bool result = long.TryParse(match.Groups[1].Value, out id);
                        if (!result) Exit();
                        Log.Info($"{GetType().Name} > Parsed \"write\" link to id {id}");
                        CheckPasscode(rootFrame, () => {
                            rootFrame.Navigate(typeof(Main), args);
                        }, false);
                    } else {
                        if (!Launched) Exit();
                    }
                }

            } else if (ContactsPanel.IsContactPanelSupported && args.Kind == ActivationKind.ContactPanel) {
                var cpframe = new Frame();
                Window.Current.Content = cpframe;
                if (state == 1) {
                    CheckPasscode(cpframe, () => {
                        cpframe.Navigate(typeof(Pages.EntryPoint.ContactPanelAndShare), args);
                        if (!MainAppWindowDisplayed) APIHelper.SetOnlinePeriodically();
                    }, false);

                }
            } else if (args.Kind == ActivationKind.ShareTarget) {
                var cpframe = new Frame();
                Window.Current.Content = cpframe;

                if (state == 1) {
                    CheckPasscode(cpframe, () => {
                        var a = args as ShareTargetActivatedEventArgs;
                        cpframe.Navigate(typeof(Pages.EntryPoint.ShareTargetNew), args);
                    }, false);
                }
            }

            // check for updates
            //Log.Info($"{GetType()} > Checking for updates...");
            //var hasUpdates = await MSStoreUpdater.CheckForUpdatesAsync();
            //Log.Info($"{GetType()} > Has updates: {hasUpdates}");
            //if (hasUpdates) {
            //    IsUpdateIsReadyToApply = await MSStoreUpdater.TrySilentDownloadAsync();
            //    Log.Info($"{GetType()} > Is update downloaded: {IsUpdateIsReadyToApply}");
            //}

            // Вместо дефолтных вкшных аватарок отображать первые буквы.
            APIHelper.PlaceholderAvatars.ForEach(a => Avatar.AddUriForIgnore(a));

            await Theme.UpdateTitleBarColors(App.UISettings);
            Launched = true;
        }

        private void CoreWindow_ResizeCompleted(Windows.UI.Core.CoreWindow sender, object args) {
            if (sender.Bounds.Height < 548)
                ApplicationView.GetForCurrentView().TryResizeView(new Size(sender.Bounds.Width, 580));
        }

        /// <summary>
        /// Вызывается при обычном запуске приложения пользователем. Будут использоваться другие точки входа,
        /// например, если приложение запускается для открытия конкретного файла.
        /// </summary>
        /// <param name="e">Сведения о запросе и обработке запуска.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            new Action(async () => {
                await LaunchThis(e);
                await Functions.ClearTemporaryFolderAsync();
            })();
        }

        // Toast
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args) {
            var deferral = args.TaskInstance.GetDeferral();
            Log.Info($"{GetType().Name} > Background activation. Task name: {args.TaskInstance.Task.Name}");

            switch (args.TaskInstance.Task.Name) {
                case "PNS":
                    var pdetails = args.TaskInstance.TriggerDetails as Windows.Networking.PushNotifications.RawNotification;
                    RawPushNotificationHandler.ParsePushNotification(pdetails.Content, args.TaskInstance);
                    break;
                case "ToastBackgroundTask":
                    var tdetails = args.TaskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
                    if (tdetails != null) new System.Action(async () => { await ToastBackgroundActivation.ParseAsync(tdetails); })();
                    break;
                case "PushChannelUpdaterBackgroundTask":
                    if (IsUserLogged) new System.Action(async () => { await PushChannelUpdater.UpdateAsync(args.TaskInstance); })();
                    break;
            }

            deferral.Complete();
        }

        private void CoreApplication_UnhandledErrorDetected(object sender, UnhandledErrorDetectedEventArgs e) {
            new System.Action(async () => {
                try {
                    e.UnhandledError.Propagate();
                } catch (Exception ex) {
                    Log.Error($"{GetType().Name} > COREAPPLICATION UNHANDLED ERROR! (0x{ex.HResult.ToString("x8")}).");
                    await Pages.UnhandledErrorPage.ShowErrorPageAsync(0, ex);
                }
            })();
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
            new System.Action(async () => {
                Log.Error($"{GetType().Name} > UNOBSERVED TASK EXCEPTION! ({e.Observed}) (0x{e.Exception.HResult.ToString("x8")}).");
                await Pages.UnhandledErrorPage.ShowErrorPageAsync(2, e.Exception);
            })();
        }

        /// <summary>
        /// Вызывается в случае сбоя навигации на определенную страницу
        /// </summary>
        /// <param name="sender">Фрейм, для которого произошел сбой навигации</param>
        /// <param name="e">Сведения о сбое навигации</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        protected override void OnActivated(IActivatedEventArgs args) {
            base.OnActivated(args);
            new Action(async () => {
                await LaunchThis(args);
            })();
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args) {
            base.OnShareTargetActivated(args);
            new Action(async () => {
                await LaunchThis(args);
            })();
        }

        private async Task OpenDebugWindow() {
            await ViewManagement.OpenNewWindow(typeof(PreviewDebug.Menu), "Debug");
        }

        public static void CheckPasscode(Frame frame, Action afterCheck, bool logoutButtonVisibility) {
            if (string.IsNullOrEmpty(AppParameters.Passcode)) {
                afterCheck.Invoke();
            } else {
                frame.Navigate(typeof(Pages.EnterPasswordView), new Tuple<Action, bool>(afterCheck, logoutButtonVisibility));
            }
        }

        private async Task<HttpResponseMessage> SendRequestToAPIViaLNetAsync(Uri uri, Dictionary<string, string> parameters, Dictionary<string, string> headers) {
            // Кукисы отправляем только для запросов с веб-токеном, иначе будет ошибка 5 "another ip-address",
            // а у обычного токена от Laney с кукисами возникает ошибка 3 у метода queue.subscribe (внутри execute.startup) ¯\_(ツ)_/¯
            bool dontSendCookies = false;
            if (headers.ContainsKey("Authorization")) dontSendCookies = headers["Authorization"].Substring(7) == AppParameters.AccessToken;
            headers.Add("X-Owner", "long");
            return await LNet.PostAsync(uri, parameters, headers, dontSendCookies: dontSendCookies, throwExIfNonSuccessResponse: false);
        }

        /// <summary>
        /// Вызывается при приостановке выполнения приложения.  Состояние приложения сохраняется
        /// без учета информации о том, будет ли оно завершено или возобновлено с неизменным
        /// содержимым памяти.
        /// </summary>
        /// <param name="sender">Источник запроса приостановки.</param>
        /// <param name="e">Сведения о запросе приостановки.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();
            if (OnlineTimer != null) {
                OnlineTimer.Dispose();
                OnlineTimer = null;
            }
            Log.Info($"{GetType().Name} > Suspending... Deadline: {e.SuspendingOperation.Deadline.DateTime.ToString("T")}.");
            Log.UnInit();
            deferral.Complete();
        }

        private void OnResuming(object sender, object e) {
            new System.Action(async () => { await Log.InitAsync(); })();
            Log.Info($"{GetType().Name} > Resuming...");
            APIHelper.SetOnlinePeriodically();
        }
    }
}