using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class EnterPasswordView : Page {
        public EnterPasswordView() {
            this.InitializeComponent();
            Loaded += async (a, b) => await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => pass.Focus(FocusState.Keyboard));
        }

        Action afterPass = null;

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            TitleAndStatusBar.ExtendView(true);
            if (Theme.IsMicaAvailable) LayoutRoot.Background = null;

            Tuple<Action, bool> actions = e.Parameter as Tuple<Action, bool>;
            afterPass = actions.Item1;
            outbtn.Visibility = actions.Item2 ? Visibility.Visible : Visibility.Collapsed;

            // Set password to current session
            // (if user updted from old version)
            new System.Action(async () => {
                await RefreshPasscodeForSession();
                await CheckWinHello();
            })();
        }

        private async Task RefreshPasscodeForSession() {
            var sessions = await VKSessionManager.GetSessionsAsync();
            var session = sessions.Where(s => s.Id == AppParameters.UserID).FirstOrDefault();
            if (session == null) return;

            session.LocalPasscode = AppParameters.Passcode;
            await VKSessionManager.AddOrUpdateSessionAsync(session);
        }

        private async Task CheckWinHello() {
            if (AppParameters.WindowsHelloInsteadPasscode) {
                var result = await UserConsentVerifier.RequestVerificationAsync(" ");
                if (result == UserConsentVerificationResult.Verified) {
                    afterPass.Invoke();
                } else {
                    form.Visibility = Visibility.Visible;
                    pass.Focus(FocusState.Keyboard);
                }
            } else {
                form.Visibility = Visibility.Visible;
                pass.Focus(FocusState.Keyboard);
            }
        }

        private void Check(object sender, RoutedEventArgs e) {
            Check();
        }

        private void Logout(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await APIHelper.Logout(); })();
        }

        private void CheckCount(object sender, KeyRoutedEventArgs e) {
            chkbtn.IsEnabled = pass.Password.Length >= 4;
            if (e.Key == Windows.System.VirtualKey.Enter) {
                Check();
            }
        }

        private void Check() {
            if (pass.Password.Length < 4) return;
            if (AppParameters.Passcode == pass.Password) {
                afterPass.Invoke();
            } else {
                pass.Password = string.Empty;
                chkbtn.IsEnabled = false;
                WrongPasswordAnimation.Begin();
                pass.Focus(FocusState.Keyboard);
            }
        }
    }
}