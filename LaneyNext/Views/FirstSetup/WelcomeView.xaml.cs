using Elorucov.Laney.Core;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.FirstSetup
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class WelcomeView : Page
    {
        public WelcomeView()
        {
            this.InitializeComponent();

            var version = AppInfo.Version;
            Version.Text = $"{version.Major}.{version.Minor}.{version.Build}";
            if (AppInfo.ReleaseState != AppReleaseState.Release) Version.Text += $" {AppInfo.ReleaseState.ToString().ToUpper()}";
        }

        private async void LogIn(object sender, RoutedEventArgs e)
        {
            LogInButton.Visibility = Visibility.Collapsed;
            Spinner.Visibility = Visibility.Visible;
            VKSession session = await AuthorizationHelper.AuthVKUser();
            if (session == null)
            {
                LogInButton.Visibility = Visibility.Visible;
                Spinner.Visibility = Visibility.Collapsed;
                return;
            }
            VKSession.BindSessionToCurrentView(session);
            Frame.Navigate(typeof(Shell));
            Insider.CheckAsync();
        }
    }
}
