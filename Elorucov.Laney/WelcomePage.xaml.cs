using Elorucov.Laney.Models;
using Elorucov.Laney.Pages;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.Globalization;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class WelcomePage : Page {
        public WelcomePage() {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            TitleAndStatusBar.ExtendView(true);
            new System.Action(async () => { await Theme.UpdateTitleBarColors(new Windows.UI.ViewManagement.UISettings()); })();

            if (Theme.IsMicaAvailable) Background = null;

            ver.Text = $"v{ApplicationInfo.GetVersion()}";
            langName.Text = Locale.Get("lang_name");
            Loaded += OnLoaded;
        }

        ObservableCollection<VKSession> Sessions;
        VKSession sessionToSwitch;

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if (e.Parameter != null && e.Parameter is VKSession session) {
                sessionToSwitch = session;
            }
        }

        private void Login(object sender, RoutedEventArgs e) {
            if (Sessions != null && Sessions.Count > 0) {
                Frame.Navigate(typeof(DirectAuthPage));
                return;
            }

            new System.Action(async () => {
                ContentDialog dlg = new ContentDialog {
                    Title = Locale.Get("warning"),
                    Content = Locale.Get("direct_auth_notice"),
                    PrimaryButtonText = Locale.Get("understand"),
                    DefaultButton = ContentDialogButton.Primary
                };
                var result = await dlg.ShowAsync();
                if (result == ContentDialogResult.Primary) {
                    Frame.Navigate(typeof(DirectAuthPage));
                }
            })();
        }

        private void ShowQRDialog(object sender, RoutedEventArgs e) {
            QRAuthUI qrDialog = new QRAuthUI();
            qrDialog.Closed += (a, b) => {
                if (b == null) return;
                Tuple<long, string, string> cred = b as Tuple<long, string, string>;
                AppParameters.UserID = cred.Item1;
                AppParameters.WebToken = cred.Item2;
                Frame.Navigate(typeof(DirectAuthPage));
            };
            qrDialog.Show();
        }

        private void ShowLangPicker(object sender, RoutedEventArgs e) {
            List<AppLanguage> langs = new List<AppLanguage>();
            langs.Insert(0, new AppLanguage { LanguageCode = string.Empty, DisplayName = "System" });
            langs.AddRange(AppLanguage.SupportedLanguages);

            // Menu flyout
            var mf = new Windows.UI.Xaml.Controls.MenuFlyout();
            foreach (AppLanguage lang in langs) {
                // Check current lang
                string overridel = ApplicationLanguages.PrimaryLanguageOverride;
                bool isChecked = lang.LanguageCode == overridel;
                RadioMenuFlyoutItem item = new RadioMenuFlyoutItem { GroupName = "lang", Text = lang.DisplayName, IsChecked = isChecked };
                item.Click += (a, b) => {
                    ApplicationLanguages.PrimaryLanguageOverride = lang.LanguageCode;
                    Frame.Navigate(typeof(WelcomePage), null, new SuppressNavigationTransitionInfo());
                };
                mf.Items.Add(item);
            }
            mf.Placement = FlyoutPlacementMode.Top;
            mf.ShowAt((FrameworkElement)sender);
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Loaded -= OnLoaded;

            Tips.Init(LayoutRoot);
            List<UIElement> receivers = new List<UIElement> { this };
            Services.UI.Shadow.TryDrawUsingThemeShadow(LogoEllipse, LogoEllipseShadow, receivers, 16);

            if (Functions.GetOSBuild() >= 16299) {
                KeyboardAccelerator ka = new KeyboardAccelerator {
                    Modifiers = Windows.System.VirtualKeyModifiers.Shift,
                    Key = Windows.System.VirtualKey.F9
                };
                ka.Invoked += async (a, b) => await OpenWTIDOverrideDialogAsync();
                KeyboardAccelerators.Add(ka);

                KeyboardAccelerator kb = new KeyboardAccelerator {
                    Modifiers = Windows.System.VirtualKeyModifiers.Shift,
                    Key = Windows.System.VirtualKey.F8
                };
                kb.Invoked += async (a, b) => await OpenLoginByTokenDialogAsync();
                KeyboardAccelerators.Add(kb);
            }

            new System.Action(async () => {
                if (sessionToSwitch != null) {
                    await Task.Delay(350); // required
                    LoginToSession(sessionToSwitch);
                } else {
                    if (App.IsUpdateIsReadyToApply) {
                        UpdateAvailabilityButton.Visibility = Visibility.Visible;
                    } else {
                        // App.MSStoreUpdater.Downloaded += (j, k) => UpdateAvailabilityButton.Visibility = Visibility.Visible;
                    }

                    var sessions = await VKSessionManager.GetSessionsAsync();
                    if (sessions.Count > 0) {
                        FindName(nameof(SessionsView));
                        NewSessionButtons.Visibility = sessions.Count >= 3 ? Visibility.Collapsed : Visibility.Visible;

                        Sessions = new ObservableCollection<VKSession>(sessions);
                        SessionsList.ItemsSource = Sessions;

                        await Task.Delay(50); // required
                        SessionsList.Focus(FocusState.Programmatic);
                    } else {
                        FindName(nameof(NoSessionsView));

                        await Task.Delay(50); // required
                        MainAuthBtn.Focus(FocusState.Programmatic);
                    }
                }
            })();
        }

        #region Dev and testing feature

        byte num = 0;
        private void OnLogoClicked(object sender, RoutedEventArgs e) {
            num++;
            if (num >= 10) new System.Action(async () => { await OpenWTIDOverrideDialogAsync(); })();
        }

        private async Task OpenWTIDOverrideDialogAsync() {
            // Почему InputScope реализовано так ужасно(((
            var isc = new InputScope();
            isc.Names.Add(new InputScopeName(InputScopeNameValue.Digits));

            TextBox cid = new TextBox { Header = "Client ID:", Text = AppParameters.VKMApplicationID.ToString(), InputScope = isc };
            PasswordBox cs = new PasswordBox { Header = "Client secret:", Margin = new Thickness(0, 12, 0, 0) };

            StackPanel sp = new StackPanel();
            sp.Children.Add(cid);
            sp.Children.Add(cs);

            ContentDialog dlg = new ContentDialog {
                Title = "Override official app credentials",
                Content = sp,
                PrimaryButtonText = "Set",
                SecondaryButtonText = "Reset to default",
                CloseButtonText = Locale.Get("close"),
                DefaultButton = ContentDialogButton.Primary
            };
            dlg.PrimaryButtonClick += (a, b) => {
                b.Cancel = true;
                int id = 0;
                if (!int.TryParse(cid.Text, out id) || id <= 0) {
                    Tips.Show("Incorrect client id!");
                    return;
                }
                if (string.IsNullOrEmpty(cs.Password)) {
                    Tips.Show("Client secret is empty!");
                    return;
                }

                AppParameters.VKMApplicationID = id;
                AppParameters.VKMSecret = cs.Password;
                dlg.Hide();
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Secondary) AppParameters.Reset();
        }


        byte num2 = 0;
        private void OnEClicked(object sender, RoutedEventArgs e) {
            num2++;
            if (num2 >= 5) new System.Action(async () => await OpenLoginByTokenDialogAsync())();
        }

        private async Task OpenLoginByTokenDialogAsync() {
            StackPanel content = new StackPanel();

            PasswordBox at = new PasswordBox {
                Margin = new Thickness(0, 0, 0, 12),
                PlaceholderText = "access_token от официального приложения"
            };

            PasswordBox at3rd = new PasswordBox {
                Margin = new Thickness(0, 0, 0, 12),
                PlaceholderText = $"access_token от приложения с ID {AppParameters.ApplicationID}",
                Visibility = Visibility.Collapsed
            };

            CheckBox checkBox = new CheckBox() {
                Content = $"Указать токен и от приложения с ID {AppParameters.ApplicationID}",
                Margin = new Thickness(0, 0, 0, 12)
            };

            TextBlock err = new TextBlock {
                Foreground = new SolidColorBrush(Colors.Red),
                TextWrapping = TextWrapping.Wrap
            };

            checkBox.Click += (a, b) => {
                at3rd.Visibility = checkBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            };

            content.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Text = "Введите access token от официального приложения ВК (мобильные клиенты либо десктопный VK Мессенджер).\nЕсли галочка «Указать токен и для приложения...» НЕ стоит, то Laney попытается сам получить второй access token." });
            content.Children.Add(new HyperlinkButton { Content = "Получить access_token", Margin = new Thickness(0, 12, 0, 12), NavigateUri = new Uri("https://vkhost.github.io") });
            content.Children.Add(at);
            content.Children.Add(at3rd);
            content.Children.Add(checkBox);
            content.Children.Add(err);

            ContentDialog dlg = new ContentDialog {
                Title = "Auth with token",
                PrimaryButtonText = Locale.Get("continue"),
                SecondaryButtonText = Locale.Get("cancel"),
                DefaultButton = ContentDialogButton.Primary,
                Content = content
            };

            dlg.PrimaryButtonClick += async (a, b) => {
                b.Cancel = true;
                dlg.IsPrimaryButtonEnabled = false;
                dlg.IsSecondaryButtonEnabled = false;
                bool result = await CheckTokenAsync(at.Password, checkBox.IsChecked == true ? at3rd.Password : null, err);
                if (result) {
                    dlg.Hide();
                    Frame.Navigate(typeof(Main));
                } else {
                    dlg.IsPrimaryButtonEnabled = true;
                    dlg.IsSecondaryButtonEnabled = true;
                }
            };

            await dlg.ShowAsync();
        }

        private async Task<bool> CheckTokenAsync(string offClientToken, string appToken, TextBlock err) {
            try {
                err.Text = "Checking access to messages API...";
                VkAPI.API.WebToken = offClientToken;
                object response = await Messages.GetFolders();
                if (response is VKList<Folder>) {
                    err.Text = "Getting user's ID...";
                    object response2 = await Users.Get(accessToken: offClientToken);
                    if (response2 is List<VkAPI.Objects.User> users && users.Count > 0) {
                        if (!string.IsNullOrEmpty(appToken)) {
                            err.Text = "Checking 2-nd token for client...";

                            object appresponse = await Apps.Get(appToken);
                            if (appresponse is VKList<VkAPI.Objects.App> response3) {
                                var app = response3.Items.FirstOrDefault();
                                if (app.Id != AppParameters.ApplicationID) {
                                    err.Text = $"This token gained for \"{app.Title}\" app (id {app.Id}), not for app ({AppParameters.ApplicationID})!";
                                    return false;
                                }
                                AppParameters.UserID = users.FirstOrDefault().Id;
                                AppParameters.AccessToken = appToken;
                                AppParameters.WebToken = offClientToken;
                                Frame.Navigate(typeof(Main));
                                return true;
                            } else if (appresponse is VKError vkerr) {
                                err.Text = $"API error in 2-nd phase! ({vkerr.error_code}): {vkerr.error_msg}";
                            } else if (appresponse is Exception ex) {
                                var e = Functions.GetNormalErrorInfo(response2);
                                err.Text = $"Cannot check app (2-nd phase)! {e.Item1}!\n{e.Item2}";
                            }
                            return false;
                        } else {
                            err.Text = "Getting token for Laney...";
                            AppParameters.UserID = users.FirstOrDefault().Id;
                            AppParameters.WebToken = offClientToken;
                            Frame.Navigate(typeof(DirectAuthPage));
                            return true;
                        }
                    } else {
                        var e = Functions.GetNormalErrorInfo(response2);
                        err.Text = $"Cannot check user id! {e.Item1}\n{e.Item2}";
                        return false;
                    }
                } else {
                    var e = Functions.GetNormalErrorInfo(response);
                    err.Text = $"Cannot check access to messages API: {e.Item1}!\n{e.Item2}";
                    return false;
                }
            } catch (Exception ex) {
                err.Text = $"Exception (0x{ex.HResult.ToString("x8")}): {ex.Message}";
                return false;
            }
        }

        #endregion

        private void LoginToSavedSession(object sender, ItemClickEventArgs e) {
            VKSession session = e.ClickedItem as VKSession;
            if (session == null) return;
            LoginToSession(session);
        }

        private void LoginToSession(VKSession session) {
            AppParameters.UserID = session.Id;
            AppParameters.AccessToken = session.AccessToken;
            AppParameters.WebToken = session.VKMAccessToken;
            AppParameters.WebTokenExpires = session.VKMAccessTokenExpires;
            AppParameters.ExchangeToken = session.VKMExchangeToken;
            AppParameters.UserName = session.Name;
            AppParameters.UserAvatar = session.Avatar;
            AppParameters.Passcode = session.LocalPasscode;

            App.CheckPasscode(Frame, () => Frame.Navigate(typeof(Main)), true);
        }

        private void RemoveSession(object sender, RoutedEventArgs e) {
            FrameworkElement el = sender as FrameworkElement;
            VKSession session = el?.DataContext as VKSession;
            if (session == null) return;

            new System.Action(async () => {
                ContentDialog dlg = new ContentDialog {
                    Content = Locale.Get("wlc_remove_session_confirmation"),
                    PrimaryButtonText = Locale.Get("yes"),
                    SecondaryButtonText = Locale.Get("no"),
                    DefaultButton = ContentDialogButton.Primary
                };
                var result = await dlg.ShowAsync();
                if (result == ContentDialogResult.Primary) {
                    try {
                        ScreenSpinner ssp = new ScreenSpinner();
                        await ssp.ShowAsync(VKSessionManager.DeleteSessionAsync(session.Id));
                        Sessions.Remove(session);

                        NewSessionButtons.Visibility = Sessions.Count >= 3 ? Visibility.Collapsed : Visibility.Visible;
                        if (Sessions.Count == 0) {
                            SessionsView.Visibility = Visibility.Collapsed;
                            FindName(nameof(NoSessionsView));
                        }
                    } catch (Exception ex) {
                        Functions.ShowHandledErrorDialog(ex);
                    }
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
    }
}