using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Pages;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.LongPoll;
using Elorucov.Laney.Services.PushNotifications;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Objects;
using Microsoft.QueryStringDotNET;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Main : Page {
        public double ConversationViewWidth { get { return MainLayout.RightContentWidth; } }
        public bool IsWideMode { get { return MainLayout.IsWideMode; } }
        public bool IsLeftPaneCompact { get { return MainLayout.IsWideMode && MainLayout.LeftPaneIsCompact; } }
        private bool IsInternetAvailable { get { return NetworkInformation.GetInternetConnectionProfile() != null; } }

        public event EventHandler<bool> LeftPaneCompactModeChanged;

        public Main() {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");

            MainLayout.LeftPaneIsCompactChanged += (a, b) => LeftPaneCompactModeChanged?.Invoke(this, b);

            API.Initialize(AppParameters.AccessToken, Locale.Get("lang"), ApplicationInfo.UserAgent, AppParameters.VKMApplicationID, AppParameters.VKMSecret, AppParameters.VkApiDomain);
            API.WebToken = AppParameters.WebToken;
            API.ExchangeToken = AppParameters.ExchangeToken;
            API.WebTokenRefreshed = async (isSuccess, token, expiresIn) => await APIHelper.SaveRefreshedTokenAsync(isSuccess, token, expiresIn);
            API.InvalidSessionErrorReceived += API_InvalidSessionErrorReceived;

            new System.Action(async () => { await SetUpUIAsync(); })();
            Loaded += async (a, b) => {
                Log.Info($"{GetType()} > Loaded: starting.");

                if (App.IsUpdateIsReadyToApply) {
                    UpdateAvailabilityButton.Visibility = Visibility.Visible;
                } else {
                    // App.MSStoreUpdater.Downloaded += (j, k) => UpdateAvailabilityButton.Visibility = Visibility.Visible;
                }

                // Audio player
                AudioPlayerViewModel.InstancesChanged += (c, d) => {
                    AudioMP.DataContext = AudioPlayerViewModel.MainInstance;
                    VoiceMP.DataContext = AudioPlayerViewModel.VoiceMessageInstance;
                    AudioMP.Visibility = AudioPlayerViewModel.MainInstance != null && AudioPlayerViewModel.VoiceMessageInstance == null ? Visibility.Visible : Visibility.Collapsed;
                    VoiceMP.Visibility = AudioPlayerViewModel.VoiceMessageInstance != null ? Visibility.Visible : Visibility.Collapsed;
                };
                await SetUpMenu();
                await ChatThemeService.LoadLocalThemes();

                ConversationView cv = new ConversationView();

                RightFrame.DataContext = new ConversationViewModel();
                // ShowInformation(Locale.Get("msgview_info_empty"));
                ShowInformation(string.Empty);

                RightFrame.Content = cv;

                if (IsInternetAvailable) {
                    StartupTasks();
                } else {
                    await SetTitleText(Locale.Get("status_no_internet"));
                    ShowInformation(Locale.Get("network_error_no_internet"));
                    NetworkInformation.NetworkStatusChanged += TryDoStartupTasks;
                }

                CoreApplication.GetCurrentView().CoreWindow.KeyDown += CoreWindow_KeyDown;

                if (AppParameters.DisplayRamUsage) {
                    DbgRAMContainer.Visibility = Visibility.Visible;
                    DispatcherTimer tmr = new DispatcherTimer();
                    tmr.Interval = TimeSpan.FromSeconds(0.5);
                    tmr.Tick += (c, d) => DbgRAM.Text = $"{Functions.GetMemoryUsageInMb()} Mb";
                    tmr.Start();
                }
            };
            Unloaded += (a, b) => {
                CoreApplication.GetCurrentView().CoreWindow.KeyDown -= CoreWindow_KeyDown;
                API.InvalidSessionErrorReceived -= API_InvalidSessionErrorReceived;
            };
        }

        private void API_InvalidSessionErrorReceived(object sender, EventArgs e) {
            if (AppParameters.DisableAutoLogoff) return;
            API.InvalidSessionErrorReceived -= API_InvalidSessionErrorReceived;
            new System.Action(async () => { await APIHelper.Logout(); })();
        }

        MenuFlyout MainMenu = new MenuFlyout {
            Placement = FlyoutPlacementMode.Bottom
        };

        private async Task SetUpMenu() {
            var favoritesIconXaml = "<PathIcon xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Data='{StaticResource FavoritesIcon}' Height='16' HorizontalAlignment='Center' VerticalAlignment='Center'/>";
            PathIcon favoritesIcon = XamlReader.Load(favoritesIconXaml) as PathIcon;
            favoritesIcon.RenderTransform = new TranslateTransform { X = 2 };

            MenuFlyoutItem important = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("AllConv_Important") };
            MenuFlyoutItem favorites = new MenuFlyoutItem { Icon = favoritesIcon, Text = Locale.Get("AllConv_Favorites") };
            MenuFlyoutItem folders = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("folders") };
            MenuFlyoutItem refresh = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("refresh") };
            MenuFlyoutItem settings = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("AllConv_Settings") };
            MenuFlyoutItem logout = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("AllConv_Logout") };
            MenuFlyoutSubItem switchAcc = new MenuFlyoutSubItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("switch_account") };

            important.Click += ImpMsgsButton;
            favorites.Click += FavMsgsButton;
            folders.Click += OpenFoldersSettingsModal;
            refresh.Click += RefreshPage;
            settings.Click += SettingsButton;
            logout.Click += LogoutButton;

            MainMenu.Items.Add(important);
            MainMenu.Items.Add(favorites);
            MainMenu.Items.Add(folders);
            MainMenu.Items.Add(new MenuFlyoutSeparator());
            MainMenu.Items.Add(refresh);
            MainMenu.Items.Add(settings);

            // Multiacc
            List<VKSession> sessions = await VKSessionManager.GetSessionsAsync();
            foreach (var session in sessions) {
                if (session.Id == AppParameters.UserID) continue;
                MenuFlyoutItem item = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = session.Name };
                item.Click += async (a, b) => {
                    await APIHelper.Logout(session);
                };
                switchAcc.Items.Add(item);
            }

            if (switchAcc.Items.Count > 0) switchAcc.Items.Add(new MenuFlyoutSeparator());
            switchAcc.Items.Add(logout);
            MainMenu.Items.Add(switchAcc);

            if (AppParameters.ShowDebugItemsInMenu) {
                MenuFlyoutItem d0 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "LongPoll simulator" };
                MenuFlyoutItem d1 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Get to peer" };
                MenuFlyoutItem d4 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Set window 360x640" };
                MenuFlyoutItem d5 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Set custom window size" };
                MenuFlyoutItem d2 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Crash app" };
                MenuFlyoutItem d3 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Reset users'n'groups cache" };
                MenuFlyoutItem d6 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Copy device_id for push" };
                MenuFlyoutItem d10 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Copy ChannelUri for push" };
                MenuFlyoutItem d7 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "GC.Collect()" };
                MenuFlyoutItem d8 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Copy access token" };
                MenuFlyoutItem d9 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Copy offclient access token" };
                MenuFlyoutItem d11 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Cached images info" };
                MenuFlyoutItem d12 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Clear cached images" };
                MenuFlyoutItem d13 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Stickers keywords info" };

                d0.Click += async (a, b) => await ShowLPSimulatorDialog();
                d1.Click += async (a, b) => await ShowGetToPeerIdDialog();
                d4.Click += (a, b) => {
                    ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(360, 360));
                    ApplicationView.GetForCurrentView().TryResizeView(new Size(360, 641));
                };
                d5.Click += async (a, b) => await ShowCustomWndSizeDialog();
                d2.Click += (a, b) => throw new Exception("This is a crash. Not bandicoot, but a crash.");
                d3.Click += (a, b) => {
                    AppSession.CachedUsers.Clear();
                    AppSession.CachedGroups.Clear();
                    AppSession.CachedContacts.Clear();
                };
                d6.Click += (a, b) => {
                    DataPackage dp = new DataPackage();
                    dp.RequestedOperation = DataPackageOperation.Copy;
                    dp.SetText(VKNotificationHelper.GetDeviceId());
                    Clipboard.SetContent(dp);
                    Tips.Show("Copied.");
                };
                d10.Click += async (a, b) => {
                    DataPackage dp = new DataPackage();
                    dp.RequestedOperation = DataPackageOperation.Copy;
                    dp.SetText(await VKNotificationHelper.GetChannelUri());
                    Clipboard.SetContent(dp);
                    Tips.Show("Copied.");
                };
                d7.Click += (a, b) => {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                };
                d8.Click += (a, b) => {
                    DataPackage dp = new DataPackage();
                    dp.RequestedOperation = DataPackageOperation.Copy;
                    dp.SetText(AppParameters.AccessToken);
                    Clipboard.SetContent(dp);
                    Tips.Show("Copied. Don't share this token with anyone!!!");
                };
                d9.Click += (a, b) => {
                    DataPackage dp = new DataPackage();
                    dp.RequestedOperation = DataPackageOperation.Copy;
                    dp.SetText(AppParameters.WebToken);
                    Clipboard.SetContent(dp);
                    Tips.Show("Copied. Don't share this token with anyone!!!");
                };

                d11.Click += (a, b) => {
                    var info = Services.Network.LNetExtensions.GetImagesCacheSize();
                    string infotext = $"Count: {info.Item1}\nTotal size: {Functions.GetFileSize(info.Item2)}\nTotal bytes: {info.Item2}";
                    Tips.Show(d11.Text, infotext);
                };
                d12.Click += (a, b) => Services.Network.LNetExtensions.ClearImagesCache();

                d13.Click += (a, b) => {
                    string infotext = $"Total words: {StickersKeywords.WordsCount}\nTotal stickers: {StickersKeywords.StickersCount}\nChunks: {StickersKeywords.Chunks}";
                    Tips.Show(d13.Text, infotext);
                };

                MenuFlyoutSubItem mfdi = new MenuFlyoutSubItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Quick debug" };
                mfdi.Items.Add(d0);
                mfdi.Items.Add(d1);
                mfdi.Items.Add(d4);
                mfdi.Items.Add(d5);
                mfdi.Items.Add(d2);
                mfdi.Items.Add(d3);
                mfdi.Items.Add(d6);
                mfdi.Items.Add(d10);
                mfdi.Items.Add(d7);
                mfdi.Items.Add(d8);
                mfdi.Items.Add(d9);
                mfdi.Items.Add(d11);
                mfdi.Items.Add(d12);
                mfdi.Items.Add(d13);

                MenuFlyoutItem m2 = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Avatar creator" };
                m2.Click += (a, b) => {
                    AvatarCreator ac = new AvatarCreator();
                    ac.Show();
                };

                MainMenu.Items.Add(new MenuFlyoutSeparator());
                MainMenu.Items.Add(mfdi);
                MainMenu.Items.Add(m2);
            }
        }

        private void RefreshPage(object sender, RoutedEventArgs e) {
            if (AppSession.ImViewModel != null) {
                new System.Action(async () => { await AppSession.ImViewModel.RefreshAsync(); })();
            }
        }

        private async Task ShowLPSimulatorDialog() {
            ContentDialog cdlg = new ContentDialog {
                Title = "LongPoll simulator",
                PrimaryButtonText = "Trigger",
                SecondaryButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary
            };

            TextBox tb = new TextBox {
                PlaceholderText = "[[501, 1, \"Test\", 0], [503, 2, \"Hi\"]]"
            };

            StackPanel s = new StackPanel();
            s.Children.Add(new TextBlock { Text = "Здесь Вы можете вызвать события LongPoll сами." });
            s.Children.Add(tb);
            cdlg.Content = s;

            var result = await cdlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                try {
                    await LongPoll.TestLPResponseAsync(tb.Text);
                } catch (Exception ex) {
                    Functions.ShowHandledErrorTip(ex);
                }
            }
        }

        private async Task ShowGetToPeerIdDialog() {
            ContentDialog cdlg = new ContentDialog {
                Title = "Get to peer",
                PrimaryButtonText = "Show",
                SecondaryButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary
            };

            TextBox tb = new TextBox();
            tb.MaxLength = 11;

            StackPanel s = new StackPanel();
            s.Children.Add(new TextBlock { Text = "Enter peer id" });
            s.Children.Add(tb);
            cdlg.Content = s;

            var result = await cdlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                int p = 0;
                if (int.TryParse(tb.Text, out p)) ShowConversationPage(p);
            }
        }

        private async Task ShowCustomWndSizeDialog() {
            var b = Window.Current.Bounds;
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            TextBox ws = new TextBox { Width = 64, Text = b.Width.ToString() };
            TextBox hs = new TextBox { Width = 64, Text = b.Height.ToString() };

            sp.Children.Add(ws);
            sp.Children.Add(new TextBlock { Text = "X", Margin = new Thickness(0, 8, 0, 0) });
            sp.Children.Add(hs);

            ContentDialog dlg = new ContentDialog {
                Title = "Enter window size (min. 360x360)",
                Content = sp,
                PrimaryButtonText = "Resize",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                Double w, h = 0;
                Double.TryParse(ws.Text, out w);
                Double.TryParse(hs.Text, out h);
                if (w == 0 || h == 0) return;
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(360, 360));
                ApplicationView.GetForCurrentView().TryResizeView(new Size(w, h));
            }

        }

        private void TryDoStartupTasks(object sender) {
            if (IsInternetAvailable) {
                NetworkInformation.NetworkStatusChanged -= TryDoStartupTasks;
                StartupTasks();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            new System.Action(async () => {
                await ParseArgs(e.Parameter);
            })();
        }

        public async Task ParseArgs(object args) {
            int tp = AppParameters.Notifications;
            if (tp == 2) {  // После обновления приложения, если у юзера был выбран интерактивный тип, автоматически меняем на "вкл"
                AppParameters.Notifications = 1;
                tp = 1;
            }
            string argument = string.Empty;
            string method = string.Empty;
            try {
                if (args is ToastNotificationActivatedEventArgs t) {
                    method = nameof(ToastNotificationActivatedEventArgs);
                    argument = t.Argument;
                    Log.Info($"{GetType()} > {method}: {argument}");
                    var q = QueryString.Parse(argument);
                    int peer = q.Contains("peerId") ? int.Parse(q["peerId"]) : -1;
                    int msgId = q.Contains("cmid") ? int.Parse(q["cmid"]) : -1;
                    await Task.Delay(350); // Delay
                    if (peer != 0 || msgId != 0) {
                        if (AppParameters.WebTokenSupport) {
                            while (string.IsNullOrEmpty(API.WebToken)) {
                                Log.Warn($"{GetType()} > Web token is null! Waiting 500 ms...");
                                await Task.Delay(500);
                            }
                        }
                        ShowConversationPage(peer, msgId);
                    } else {
                        string info = "The notification doesn't contains peerId or cmid that required to open conversation.\n";
                        info += $"Please send screenshot of this message to developer.\n";
                        await new MessageDialog($"{info}Arg: {t.Argument}\nNotification type: {tp}", Locale.Get("global_error")).ShowAsync();
                    }
                } else if (args is ProtocolActivatedEventArgs pargs) {
                    await CheckLaunchUrl(pargs.Uri);
                }
            } catch (Exception ex) {
                string info = "An error occured when parsing notifcation argument.\n";
                info += $"Please send screenshot of this message to developer.\n";
                info += $"Error code: 0x{ex.HResult.ToString("x8")}\n{ex.Message}\n\nMethod: {method}\nArg: {argument}\nNotification type: {tp}\nType: {args.GetType()}";
                await new MessageDialog(info, Locale.Get("global_error")).ShowAsync();
            }
        }

        private async Task CheckLaunchUrl(Uri uri) {
            string link = $"https://{uri.Host}{uri.AbsolutePath}{uri.Query}";
            await Task.Delay(500); // надо.
            await VKLinks.LaunchLinkAsync(new Uri(link));
        }

        bool startupTasksCompleted = false;
        private void StartupTasks() {
            if (startupTasksCompleted) return;
            // ShowInformation(Locale.Get("msgview_info_empty"));

            try {
                BackgroundExecutionManager.RemoveAccess();
            } catch (Exception ex) {
                Log.Error($"{GetType().Name} > An error occured while executing BackgroundExecutionManager.RemoveAccess()! 0x{ex.HResult.ToString("x8")}");
            }
            new System.Action(async () => {
                bool ta = await ToastBackgroundActivation.TryRegisterBackgroundTaskAsync();
                bool tb = await PushChannelUpdater.TryRegisterBackgroundTaskAsync();
                Log.Info($"{GetType().Name} > ToastBackgroundActivation task: {ta}, PushChannelUpdater task: {tb}");
            })();

            if (AppSession.ImViewModel == null) AppSession.ImViewModel = new ImViewModel();
            Action<FrameworkElement> data = (e) => MainMenu.ShowAt(e);
            ConvsAndFriendsFrame.Navigate(typeof(ImView), data, new SuppressNavigationTransitionInfo());

            new System.Action(async () => {
                await SetTitleText(Locale.Get("status_connecting"));
                Log.Info($"{GetType().Name} > StartupTasks: executing...");
                object st = await Execute.Startup();
                if (st is StartupInfo inf) {
                    AppParameters.ChatThemesListSource = inf.ChatThemesListSource;
                    AppSession.MessagesTranslationLanguagePairs = inf.MessagesTranslationLanguagePairs;
                    AppSession.PushSettings = inf.PushSettings;
                    AppSession.ReactionsAssets = inf.ReactionsAssets;
                    AppSession.AvailableReactions = inf.AvailableReactions;
                    AppSession.AddUsersToCache(inf.Users);

                    var current = inf.Users[0];
                    AppParameters.UserName = current.FullName;
                    AppParameters.UserAvatar = current.Photo?.AbsoluteUri;

                    // Обновляем файл сессий, добавляя или обновляя текущего авторизованного пользователя.
                    // Добавление произойдёт, если юзер обновился из старой версии Laney (где не было поддержи мультиаккаунта),
                    // при условии, что юзер воспользовался прямой авторизацией (т. е. web token НЕ юзается).
                    // Иначе, не будем обновлять файл сессий и не будем "поддержать" мультиаккаунт до того момента,
                    // когда юзер не выйдет из аккаунта.
                    try {
                        var session = new VKSession(AppParameters.UserID, AppParameters.AccessToken, AppParameters.WebToken, AppParameters.WebTokenExpires, AppParameters.ExchangeToken, current.FirstName, current.Photo?.AbsoluteUri);
                        if (!string.IsNullOrEmpty(AppParameters.Passcode)) session.LocalPasscode = AppParameters.Passcode;
                        await VKSessionManager.AddOrUpdateSessionAsync(session);
                    } catch (Exception ex) {
                        Functions.ShowHandledErrorTip(ex);
                    }

                    if (AppSession.ImViewModel != null) {
                        AppSession.ImViewModel.CurrentUserName = current.FullName;
                        AppSession.ImViewModel.CurrentUserAvatar = current.Photo;
                    }

                    Log.Info($"{GetType().Name} > StartupTasks: OK.");
                    if (inf.SetOnlineResult == 1) Application.Current.Suspending += async (a, b) => {
                        Log.Info($"{GetType().Name} > Set offline...");
                        var d = b.SuspendingOperation.GetDeferral();
                        await VkAPI.Methods.Account.SetOffline().ConfigureAwait(false);
                        d.Complete();
                    };
                    if (inf.RegisterDeviceResult == 1) {
                        bool pr = await VKNotificationHelper.RegisterBackgrundTaskAsync();
                        Log.Info($"{GetType().Name} > StartupTasks: Push notification registered: {pr}.");
                    }

                    AppSession.ActiveStickerPacks = inf.StickerProductIds;
                    Log.Info($"{GetType().Name} > StartupTasks: User have {inf.StickerProductIds.Count} active sticker packs.");

                    AddFeedEntryPointInMenu();

                    if (inf.SpecialEvents.Count > 0) {
                        List<SpecialEvent> withTooltips = new List<SpecialEvent>();

                        MainMenu.Items.Add(new MenuFlyoutSeparator());
                        foreach (var se in inf.SpecialEvents) {
                            if (se.NeedTooltip) withTooltips.Add(se);
                            MenuFlyoutItem semfi = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = se.Icon ?? "" }, Text = se.Title };
                            semfi.Click += async (a, b) => {
                                Uri url = se.IsHashRequired ? Functions.BuildSpecEventUri(se.Link, se.Id) : new Uri(se.Link);
                                if (se.IsInternal) {
                                    ModalWebView mvw = new ModalWebView(url, se.Title);
                                    mvw.Show();
                                } else {
                                    await Launcher.LaunchUriAsync(url);
                                }
                            };
                            MainMenu.Items.Add(semfi);
                        }
                        ShowTooltips(withTooltips);
                    }
                    InitLongPoll(inf.LongPoll);
                    if (!VKQueue.IsInitialized) {
                        if (inf.QueueConfig != null) {
                            await VKQueue.InitAsync(inf.QueueConfig, SynchronizationContext.Current);
                        } else { // Unknown method passed from execute
                            var qc = await VkAPI.Methods.Queue.Subscribe($"onlfriends_{AppParameters.UserID}");
                            if (qc is QueueSubscribeResponse qsr) {
                                await VKQueue.InitAsync(qsr, SynchronizationContext.Current);
                            }
                        }
                    }
                } else {
                    Functions.ShowHandledErrorTip(st);
                }
            })();

            startupTasksCompleted = true;
            new System.Action(async () => {
                await ChatThemeService.InitThemes();
                await StickersKeywords.InitAsync();
            })();
        }

        private void AddFeedEntryPointInMenu() {
            MainMenu.Items.Add(new MenuFlyoutSeparator());
            MenuFlyoutItem feedmfi = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("feed") + " (beta)" };
            feedmfi.Click += (a, b) => {
                ConvsAndFriendsFrame.Navigate(typeof(FeedSourcePage), null, App.DefaultNavTransition);
            };
            MainMenu.Items.Add(feedmfi);
        }

        private void ShowTooltips(List<SpecialEvent> events) {
            List<int> ids = new List<int>();
            if (!string.IsNullOrEmpty(AppParameters.SpecialEventTooltips)) {
                var ew = AppParameters.SpecialEventTooltips.Split(',');
                ew.ToList().ForEach(i => ids.Add(int.Parse(i)));
            }

            var newEvents = events.Where(e => !ids.Contains(e.Id)).ToList();
            ShowEventTips(newEvents);
        }

        public HyperlinkButton MenuButton = null;
        private void ShowEventTips(List<SpecialEvent> newEvents) {
            if (newEvents.Count > 0) {
                SpecialEvent fe = newEvents.FirstOrDefault();
                TeachingTip tt = new TeachingTip {
                    Title = fe.Title,
                    PreferredPlacement = TeachingTipPlacementMode.Bottom
                };
                if (MenuButton != null) tt.Target = MenuButton;
                Tips.FixUI(tt);
                LayoutRoot.Children.Add(tt);
                if (MenuButton != null) MenuButton.Click += (a, b) => {
                    newEvents = new List<SpecialEvent>();
                    tt.IsOpen = false;
                    LayoutRoot.Children.Remove(tt);
                };
                tt.Closed += (a, b) => {
                    string ew = AppParameters.SpecialEventTooltips;
                    AppParameters.SpecialEventTooltips = string.IsNullOrEmpty(ew) ?
                        fe.Id.ToString() : ew + $",{fe.Id}";

                    LayoutRoot.Children.Remove(tt);
                    if (newEvents.Count > 0) newEvents.RemoveAt(0);
                    ShowEventTips(newEvents);
                };
                tt.IsOpen = true;
            }
        }

        private void InitLongPoll(LongPollServerInfo info = null) {
            Log.Info($"{GetType().Name} > Init longpoll...");

            new System.Action(async () => {
                LongPoll.StatusChanged += async (a, b) => await SetTitleText(b);
                var ka = await LongPoll.InitLongPoll(info);
                if (!ka) {
                    Log.Warn($"{GetType().Name} > Longpoll initialization error. Reconnect after 5 sec.");

                    await SetTitleText(String.Format(Locale.GetForFormat("status_waiting_reconnect"), 5));
                    DispatcherTimer tmr = new DispatcherTimer();
                    tmr.Interval = TimeSpan.FromSeconds(5);
                    tmr.Tick += async (a, b) => {
                        Log.Info($"{GetType().Name} > Trying again.");
                        if (await LongPoll.InitLongPoll()) {
                            tmr.Stop();
                            await SetTitleText();
                            Log.Info($"{GetType().Name} > LP initialized.");
                        } else {
                            await SetTitleText(String.Format(Locale.GetForFormat("status_waiting_reconnect"), 5));
                        }
                    };
                    tmr.Start();
                } else if (ka) {
                    await SetTitleText();
                    Log.Info($"{GetType().Name} > LP initialized.");
                }
            })();
        }

        #region Static members

        public static Main GetCurrent() {
            Frame frm = Window.Current.Content as Frame;
            if (frm != null && frm.Content is Main) return frm.Content as Main;
            return null;
        }


        #endregion

        #region UI

        private async Task SetUpUIAsync() {
            Log.Info($"{GetType().Name} > SetUpUI.");
            Tips.Init(LayoutRoot);

            var snm = SystemNavigationManager.GetForCurrentView();
            if (AppParameters.BackButtonForNavDebug) snm.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            snm.BackRequested += GoBack;

            if (Theme.IsMicaAvailable) LayoutRoot.Background = null;
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                FindName(nameof(WindowTitleBar));
                CoreApplicationViewTitleBar tb = CoreApplication.GetCurrentView().TitleBar;
                tb.LayoutMetricsChanged += (a, b) => {
                    Log.Info($"{GetType().Name} > TitleBarLayoutMetricsChanged.");
                    ChangeWindowTitleBarLayout(tb);
                };

                // WindowTitleBar.Height = tb.Height;
                WindowTitleBar.Height = 32; // чтобы не прыгало

                await Theme.UpdateTitleBarColors(App.UISettings);
                App.UISettings.ColorValuesChanged += async (a, b) => {
                    Log.Info($"{GetType().Name} > UISettings.ColorValuesChanged.");
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                        await Theme.UpdateTitleBarColors(a);
                        ChatThemeService.UpdateTheme();
                    });
                };
            } else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                double a = Math.Min(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
                DisplayInformation di = DisplayInformation.GetForCurrentView();
                if (a < 480 && di.NativeOrientation == DisplayOrientations.Portrait) {
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                }
                DbgRAMContainer.HorizontalAlignment = HorizontalAlignment.Center;
            }

            Theme.ThemeChanged += (a, b) => {
                ChatThemeService.UpdateTheme();
            };
        }

        private async Task SetTitleText(string txt = null) {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                await TitleAndStatusBar.SetTitleText(txt);
            });
        }

        private void ChangeWindowTitleBarLayout(CoreApplicationViewTitleBar a) {
            WindowTitleBar.Height = a.Height;
            Log.Info($"{GetType().Name} > ChangeWindowTitleBarLayout: Left: {a.SystemOverlayLeftInset}; Right: {a.SystemOverlayRightInset}, Height: {a.Height}");
        }

        private void ShowInformation(string i, bool showTip = false) {
            InfoText.Text = string.IsNullOrEmpty(i) ? "" : i;
            InfoContainer.Visibility = string.IsNullOrEmpty(i) ? Visibility.Collapsed : Visibility.Visible;
            if (showTip && !MainLayout.IsWideMode) {
                Tips.Show(i);
            }
        }

        #endregion

        #region Conversations

        private KeyValuePair<long, List<LMessage>> _forwardingMessages = new KeyValuePair<long, List<LMessage>>(0, null);
        private List<AttachmentBase> _forwardingAttachments;

        public void SwitchToLeftFrame(bool dontShowEmptyConv = false) {
            MainLayout.IsRightPaneShowing = false;
            if (!dontShowEmptyConv) {
                AppSession.CurrentConversationVM = null;
                RightFrame.DataContext = new ConversationViewModel();
                // ShowInformation(Locale.Get("msgview_info_empty"));
                ShowInformation(string.Empty);
            }
        }

        public void StartForwardingMessage(long fromPeerId, List<LMessage> messages) {
            SharingModal sm = new SharingModal(fromPeerId, messages);
            sm.Closed += (a, b) => {
                if (b != null && b is long peerId) {
                    RightFrame.DataContext = new ConversationViewModel();
                    _forwardingMessages = new KeyValuePair<long, List<LMessage>>(fromPeerId, messages);
                    ShowConversationPage(peerId);
                }
            };
            sm.Show();
        }

        public void StartForwardingAttachments(List<AttachmentBase> attachments) {
            SharingModal sm = new SharingModal(attachments);
            sm.Closed += (a, b) => {
                if (b != null && b is long peerId) {
                    RightFrame.DataContext = new ConversationViewModel();
                    _forwardingAttachments = attachments;
                    ShowConversationPage(peerId);
                }
            };
            sm.Show();
        }

        public void ShowConversationPage(ConversationViewModel con, int msgid = -1, bool forceLoad = true, string vkRef = null, string vkRefSource = null) {
            if (con != null) {
                if (AppSession.CurrentConversationVM != null) AppSession.ChatNavigationHistory.Push(AppSession.CurrentConversationVM.ConversationId);
                AppSession.CurrentConversationVM = con;
                AppSession.CurrentConversationVM.MessageFormViewModel.SetRef(vkRef, vkRefSource);

                if (_forwardingMessages.Key != 0 && _forwardingMessages.Value != null && _forwardingMessages.Value.Count > 0) {
                    if (!string.IsNullOrEmpty(con.RestrictionReason)) {
                        Tips.Show(con.RestrictionReason);
                        return;
                    }
                    AppSession.CurrentConversationVM.MessageFormViewModel.AddForwardedMessages(_forwardingMessages.Key, _forwardingMessages.Value);
                }

                if (_forwardingAttachments != null && _forwardingAttachments.Count > 0) {
                    if (!string.IsNullOrEmpty(con.RestrictionReason)) {
                        Tips.Show(con.RestrictionReason);
                        return;
                    }
                    foreach (AttachmentBase ab in _forwardingAttachments) {
                        var oavms = AppSession.CurrentConversationVM.MessageFormViewModel.Attachments;
                        if (oavms.Count > 10) break;
                        oavms.Add(new OutboundAttachmentViewModel(ab));
                    }
                }

                _forwardingMessages = new KeyValuePair<long, List<LMessage>>(0, null);
                _forwardingAttachments = null;
                ShowInformation(null);

                SwitchToConversationPage(msgid, forceLoad);
                ToggleContentLayerVisibility(true);
                ModalsManager.CloseLastOpenedModal();
            }
        }

        public void ShowConversationPage(long peerid, int msgid = -1, bool forceLoad = true, string vkRef = null, string vkRefSource = null) {
            if (peerid != 0) {
                Log.Info($"{GetType().Name} > ShowConversationPage for peer id {peerid}.");

                if (AppSession.CurrentConversationVM != null) AppSession.ChatNavigationHistory.Push(AppSession.CurrentConversationVM.ConversationId);

                var conv = (from b in AppSession.CachedConversations where b.ConversationId == peerid select b).ToList().FirstOrDefault();
                if (conv != null) {
                    AppSession.CurrentConversationVM = conv;
                } else {
                    ConversationViewModel cvm = new ConversationViewModel(peerid, msgid);
                    AppSession.CachedConversations.Add(cvm);
                    AppSession.CurrentConversationVM = cvm;
                }
                AppSession.CurrentConversationVM.MessageFormViewModel.SetRef(vkRef, vkRefSource);

                if (_forwardingMessages.Key != 0 && _forwardingMessages.Value != null && _forwardingMessages.Value.Count > 0) {
                    if (!string.IsNullOrEmpty(AppSession.CurrentConversationVM.RestrictionReason)) {
                        Tips.Show(AppSession.CurrentConversationVM.RestrictionReason);
                        return;
                    }
                    AppSession.CurrentConversationVM.MessageFormViewModel.AddForwardedMessages(_forwardingMessages.Key, _forwardingMessages.Value);
                }

                if (_forwardingAttachments != null && _forwardingAttachments.Count > 0) {
                    if (!string.IsNullOrEmpty(AppSession.CurrentConversationVM.RestrictionReason)) {
                        Tips.Show(AppSession.CurrentConversationVM.RestrictionReason);
                        return;
                    }
                    foreach (AttachmentBase ab in _forwardingAttachments) {
                        var oavms = AppSession.CurrentConversationVM.MessageFormViewModel.Attachments;
                        if (oavms.Count > 10) break;
                        oavms.Add(new OutboundAttachmentViewModel(ab));
                    }
                }

                _forwardingMessages = new KeyValuePair<long, List<LMessage>>(0, null);
                _forwardingAttachments = null;
                ShowInformation(null);
                SwitchToConversationPage(msgid, forceLoad);
            } else {
                RightFrame.DataContext = new ConversationViewModel();
                // ShowInformation(Locale.Get("msgview_info_empty"));
                ShowInformation(string.Empty);
                MainLayout.IsRightPaneShowing = false;
            }
            ToggleContentLayerVisibility(true);
        }

        private void SwitchToConversationPage(int messageId, bool forceLoad) {
            NonConvoFrameContainer.Visibility = Visibility.Collapsed;
            RightFrame.Visibility = Visibility.Visible;
            NavFrame.BackStack.Clear();
            NavFrame.Content = null;

            RightFrame.DataContext = AppSession.CurrentConversationVM;
            if (forceLoad) {
                AppSession.CurrentConversationVM.GoToMessage(messageId);
            } else {
                AppSession.CurrentConversationVM.OnOpened(messageId);
            }

            MainLayout.IsRightPaneShowing = true;
            if (LayoutRoot.Children.Last() is TeachingTip tt) tt.IsOpen = false;

            // Focus to message input
            MessageForm.TryFocusToTextBox();
        }

        public void NavigateToPage(Type pageType, object data = null, bool clearHistory = false) {
            RightFrame.DataContext = new ConversationViewModel();
            MainLayout.IsRightPaneShowing = true;

            RightFrame.Visibility = Visibility.Collapsed;
            NonConvoFrameContainer.Visibility = Visibility.Visible;
            if (clearHistory) {
                NavFrame.Content = null;
                NavFrame.BackStack.Clear();
            }
            NavFrame.Navigate(pageType, data, App.DefaultNavEntranceTransition);
            if (clearHistory && NavFrame.BackStackDepth == 1) {
                NavFrame.BackStack.RemoveAt(0);
            }
        }

        #endregion

        #region Contextmenu buttons

        private void ImpMsgsButton(object sender, RoutedEventArgs e) {
            ImportantMessages imm = new ImportantMessages();
            imm.Closed += (a, b) => {
                if (b is LMessage msg) {
                    ShowConversationPage(msg.PeerId, msg.ConversationMessageId);
                }
            };
            imm.Show();
        }

        private void FavMsgsButton(object sender, RoutedEventArgs e) {
            ShowConversationPage(AppParameters.UserID);
        }

        private void OpenFoldersSettingsModal(object sender, RoutedEventArgs e) {
            new FoldersSettings().Show();
        }

        private void SettingsButton(object sender, RoutedEventArgs e) {
            ConvsAndFriendsFrame.Navigate(typeof(SettingsCategoriesView), null, App.DefaultNavTransition);
        }

        private void LogoutButton(object sender, RoutedEventArgs e) {
            API.InvalidSessionErrorReceived -= API_InvalidSessionErrorReceived;
            new System.Action(async () => { await APIHelper.Logout(); })();
        }

        #endregion

        #region Events

        private void ShowMenu(object sender, RoutedEventArgs e) {
            var hb = sender as FrameworkElement;
            MainMenu.ShowAt(hb);
        }

        private void GoBack(object sender, BackRequestedEventArgs e) {
            GoBack(e);
        }

        public void GoBack(BackRequestedEventArgs e = null) {
            if (ModalsManager.HaveOpenedModals) {
                if (e != null) e.Handled = true;
            } else {
                if (NonConvoFrameContainer.Visibility == Visibility.Visible) {
                    if (NavFrame.CanGoBack) {
                        NavFrame.GoBack(App.DefaultBackNavTransition);
                        if (e != null) e.Handled = true;
                    } else {
                        SwitchToLeftFrame();
                        if (NavFrame.BackStackDepth == 1) {
                            NavFrame.BackStack.RemoveAt(0);
                        }

                        NonConvoFrameContainer.Visibility = Visibility.Collapsed;
                        RightFrame.Visibility = Visibility.Visible;
                        ToggleContentLayerVisibility(true);
                        MainLayout.Focus(FocusState.Programmatic);
                        if (e != null) e.Handled = true;
                    }
                } else {
                    if (MainLayout.IsRightPaneShowing) {
                        if (AppSession.ChatNavigationHistory.Count > 0) {
                            NonConvoFrameContainer.Visibility = Visibility.Collapsed;
                            RightFrame.Visibility = Visibility.Visible;

                            long prev = AppSession.ChatNavigationHistory.Pop(); // remove last (previous) peer id from stack
                            ShowConversationPage(prev, forceLoad: false);
                            AppSession.ChatNavigationHistory.Pop(); // remove last (navigated from) peer id from stack again.
                        } else {
                            SwitchToLeftFrame();
                        }
                        if (e != null) e.Handled = true;
                    } else {
                        if (ConvsAndFriendsFrame.CanGoBack) {
                            ConvsAndFriendsFrame.GoBack(App.DefaultBackNavTransition);
                            if (e != null) e.Handled = true;
                        }
                    }
                }
            }
        }

        public void ToggleContentLayerVisibility(bool visible) {
            MainLayout.IsLayerVisible = visible;
        }

        bool f1InfoModalOpened = false;
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args) {
            new System.Action(async () => {
                bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                if (args.VirtualKey == VirtualKey.F1) {
                    if (f1InfoModalOpened) return;
                    f1InfoModalOpened = true;
                    await new ContentDialog {
                        Title = Locale.Get("hotkeys_title"),
                        Content = new ScrollViewer {
                            Content = new TextBlock {
                                Text = Locale.Get("hotkeys"),
                                TextWrapping = TextWrapping.Wrap,
                            }
                        },
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                    f1InfoModalOpened = false;
                } else if (args.VirtualKey == VirtualKey.Escape) {
                    if (ModalsManager.HaveOpenedModals) {
                        ModalsManager.CloseLastOpenedModal();
                        return;
                    }
                    if (_forwardingAttachments != null || _forwardingMessages.Key != 0) return;
                    SwitchToLeftFrame();
                    // ShowInformation(Locale.Get("msgview_info_empty"));
                    ShowInformation(string.Empty);
                    AppSession.ChatNavigationHistory.Clear();
                } else if (ctrl && args.VirtualKey == VirtualKey.Q) {
                    await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                } else if (!ctrl && args.VirtualKey == VirtualKey.F9) {
                    ShowMenu(MainMenuTarget, null);
                } else if (args.VirtualKey == VirtualKey.F11) {
                    await ChangeViewMode(ctrl);
                }
            })();
        }

        private async Task ChangeViewMode(bool compactOverlay) {
            var view = ApplicationView.GetForCurrentView();
            if (compactOverlay) {
                if (view.ViewMode == ApplicationViewMode.CompactOverlay) {
                    await view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                } else {
                    var vmp = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                    vmp.CustomSize = new Windows.Foundation.Size(360, 500);
                    await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, vmp);
                }
            } else {
                if (view.IsFullScreenMode) {
                    view.ExitFullScreenMode();
                } else {
                    view.TryEnterFullScreenMode();
                }
            }
        }

        #endregion

        private void ShutdownAudioPlayer(object sender, RoutedEventArgs e) {
            AudioPlayerViewModel.CloseMainInstance();
        }

        private void ShutdownVoiceMsgPlayer(object sender, RoutedEventArgs e) {
            AudioPlayerViewModel.CloseVoiceMessageInstance();
        }

        private void OpenAudioPlayerUI(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                bool canShowCompactOverlay = AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop" && !AppParameters.ForceAudioPlayerModal;

                if (canShowCompactOverlay) {
                    if (AudioPlayerView.CurrentView == null) {
                        ViewModePreferences vmpref = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                        vmpref.CustomSize = new Size(360, 358);
                        bool result = await ViewManagement.OpenNewCompactOverlayWindow(vmpref, typeof(AudioPlayerView), "Audio player", null, false);
                        if (!result) Log.Error($"Maybe an audio player window didn't open...");
                    } else {
                        await ApplicationViewSwitcher.SwitchAsync(AudioPlayerView.CurrentView.Id);
                    }
                } else {
                    Modal modal = new Modal {
                        CloseButtonVisibility = Visibility.Visible,
                        Style = App.Current.Resources["ContentUnderTitleBarModalStyle"] as Style,
                        MaxWidth = 420,
                        FullSizeDesired = true,
                        Padding = new Thickness(0),
                        Content = new AudioPlayerView(true)
                    };
                    modal.Show();
                }
            })();
        }

        private void UpdateAvailabilityButton_Click(object sender, RoutedEventArgs e) {
            //new System.Action(async () => {
            //    var result = await App.MSStoreUpdater.TryApplyUpdatesAsync();
            //    if (!result.HasValue || !result.Value) {
            //        await Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?ProductId=9MSPLCXVN1M5"));
            //    }
            //})();
        }

        private void PaneResizerButton_Click(object sender, RoutedEventArgs e) {
            if (MainLayout.LeftPaneIsCompact) {
                MainLayout.LeftPaneIsCompact = false;
                PaneResizerIcon.Glyph = "";
            } else {
                MainLayout.LeftPaneIsCompact = true;
                PaneResizerIcon.Glyph = "";
            }
        }

        private void MainLayout_SizeChanged(object sender, SizeChangedEventArgs e) {
            PaneResizerButton.Visibility = e.NewSize.Width >= 720 && ConvsAndFriendsFrame.Content is ImView ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ConvsAndFriendsFrame_Navigated(object sender, NavigationEventArgs e) {
            MainLayout.IsRightPaneShowing = false;
            if (e.SourcePageType == typeof(ImView)) {
                if (IsWideMode) PaneResizerButton.Visibility = Visibility.Visible;
            } else {
                PaneResizerButton.Visibility = Visibility.Collapsed;
            }
        }
    }
}