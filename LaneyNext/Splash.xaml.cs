using Elorucov.Laney.Core;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Splash : Page
    {
        private Rect splashImageRect; // Rect to store splash screen image coordinates.
        private SplashScreen splash; // Variable to hold the splash screen object.
        private bool dismissed = false; // Variable to track splash screen dismissal status.
        private Frame rootFrame;

        public Splash(IActivatedEventArgs args)
        {
            this.InitializeComponent();

            ulong build = Helpers.OSHelper.GetBuild();
            if (build < 21332)
            {
                Root.Background = (SolidColorBrush)Application.Current.Resources["SystemControlBackgroundAccentBrush"];
                Root.RequestedTheme = ElementTheme.Dark;
                splashLocalPassContainer.RequestedTheme = ElementTheme.Default;
            }

            var ctb = CoreApplication.GetCurrentView().TitleBar;
            ctb.ExtendViewIntoTitleBar = true;
            splash = args.SplashScreen;

            if (splash != null)
            {
                // Register an event handler to be executed when the splash screen has been dismissed.
                splash.Dismissed += new TypedEventHandler<SplashScreen, Object>(DismissedEventHandler);

                splashImageRect = splash.ImageLocation;
                PositionImage();
                PositionRing();
            }

            SizeChanged += (a, b) =>
            {
                if (splash != null)
                {
                    splashImageRect = splash.ImageLocation;
                    PositionImage();
                    PositionRing();
                }
            };

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(360, 500));

            // Create a Frame to act as the navigation context
            rootFrame = new Frame();
            Window.Current.Activate();
            StartUpAsync(args);
        }

        void PositionImage()
        {
            extendedSplashImage.SetValue(Canvas.LeftProperty, splashImageRect.X);
            extendedSplashImage.SetValue(Canvas.TopProperty, splashImageRect.Y);
            extendedSplashImage.Height = splashImageRect.Height;
            extendedSplashImage.Width = splashImageRect.Width;
        }

        void PositionRing()
        {
            splashProgressRing.SetValue(Canvas.LeftProperty, splashImageRect.X + (splashImageRect.Width * 0.5) - (splashProgressRing.Width * 0.5));
            splashProgressRing.SetValue(Canvas.TopProperty, splashImageRect.Y + splashImageRect.Height + splashImageRect.Height * 0.1 + 48);
            splashLocalPassContainer.SetValue(Canvas.LeftProperty, splashImageRect.X + (splashImageRect.Width * 0.5) - (288 * 0.5));
            splashLocalPassContainer.SetValue(Canvas.TopProperty, splashImageRect.Y + splashImageRect.Height + splashImageRect.Height * 0.1 - 16);
        }

        // Include code to be executed when the system has transitioned from the splash screen to the extended splash screen (application's first view).
        private void DismissedEventHandler(SplashScreen sender, object e)
        {
            dismissed = true;
        }

        void DismissExtendedSplash()
        {
            Window.Current.Content = rootFrame;
            ThemeManager.ApplyThemeAsync();
            ViewManagement.UISettings.ColorValuesChanged += async (a, b) => await rootFrame.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ThemeManager.FixTitleBarButtonsColor();
            });
        }

        #region Load app
        IActivatedEventArgs args;
        string arguments;

        private async void StartUpAsync(IActivatedEventArgs args)
        {
            FixAvatars();
            this.args = args;
            var e = args as LaunchActivatedEventArgs;
            arguments = e.Arguments;

            if (rootFrame == null) rootFrame = new Frame();

            if (rootFrame.Content == null)
            {

                // Check local password
                if (LocalPassword.HavePass())
                {
                    if (Settings.UseWindowsHello)
                    {
                        var r = await UserConsentVerifier.RequestVerificationAsync(String.Empty);
                        if (r != UserConsentVerificationResult.Verified)
                        {
                            ShowPasswordUI();
                            return;
                        }
                    }
                    else
                    {
                        ShowPasswordUI();
                        return;
                    }
                }

                // Check sessions
                CheckSessions(args);
            }
        }

        private async void CheckSessions(IActivatedEventArgs args)
        {
            var e = args as LaunchActivatedEventArgs;

            var sessions = await VKSession.GetSessionsAsync();
            if (sessions.Count > 0)
            {
                VKSession session = sessions.First();
                session.StartSession();
                VKSession.BindSessionToCurrentView(session);
                if (!Core.AppInfo.IsExpired)
                {
                    NavigateToPageByActivation(args);
                    DismissExtendedSplash();
                    await System.Threading.Tasks.Task.Delay(3000);
                }
                else
                {
                    Insider.CheckAsync();
                }
            }
            else
            {
                if (Core.AppInfo.IsExpired)
                {
                    AppInfo.ShowExpiredInfoAsync();
                }
                rootFrame.Navigate(typeof(Views.FirstSetup.WelcomeView), e.Arguments);
                DismissExtendedSplash();
            }
        }

        private void ShowPasswordUI()
        {
            splashProgressRing.Visibility = Visibility.Collapsed;
            splashLocalPassContainer.Visibility = Visibility.Visible;
            PassBox.Focus(FocusState.Keyboard);
        }

        private void NavigateToPageByActivation(IActivatedEventArgs args)
        {
            switch (args.Kind)
            {
                case ActivationKind.Launch:
                    var largs = args as LaunchActivatedEventArgs;
                    rootFrame.Navigate(typeof(Shell), largs.Arguments);
                    break;
            }
        }

        private void FixAvatars()
        {
            // Вместо дефолтных вкшных аватарок отображать первые буквы.
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/camera_50.png"));
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/community_50.png"));
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/icons/im_multichat_50.png"));
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/camera_100.png"));
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/community_100.png"));
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/icons/im_multichat_100.png"));
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/camera_200.png"));
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/community_200.png"));
            Avatar.AddUriForIgnore(new Uri("https://vk.com/images/icons/im_multichat_200.png"));
        }

        #endregion

        private void CheckPassLength(PasswordBox sender, PasswordBoxPasswordChangingEventArgs args)
        {
            EnterButton.IsEnabled = sender.Password.Length >= 4;
        }

        private void CheckEnterButton(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) CheckPassword(sender, e);
        }

        private void CheckPassword(object sender, RoutedEventArgs e)
        {
            if (LocalPassword.Verify(PassBox.Password))
            {
                splashProgressRing.Visibility = Visibility.Visible;
                splashLocalPassContainer.Visibility = Visibility.Collapsed;
                CheckSessions(args);
            }
            else
            {
                WrongPasswordAnimation.Begin();
                PassBox.Password = String.Empty;
            }
        }

        private async void Logout(object sender, RoutedEventArgs e)
        {
            splashProgressRing.Visibility = Visibility.Visible;
            splashLocalPassContainer.Visibility = Visibility.Collapsed;

            HttpBaseProtocolFilter f = new HttpBaseProtocolFilter();
            HttpCookieCollection c = f.CookieManager.GetCookies(new Uri("https://vk.com"));
            foreach (HttpCookie hc in c)
            {
                f.CookieManager.DeleteCookie(hc);
            }

            Log.StopAll();
            await ApplicationData.Current.ClearAsync();

            rootFrame.Navigate(typeof(Views.FirstSetup.WelcomeView), arguments);
            DismissExtendedSplash();
        }
    }
}