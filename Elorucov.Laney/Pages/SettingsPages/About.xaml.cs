using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.SettingsPages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class About : Page {
        public About() {
            this.InitializeComponent();
            package = Package.Current;

            var host = Main.GetCurrent();
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

        Package package;

        private void PageLoaded(object sender, RoutedEventArgs e) {
            List<UIElement> receivers = new List<UIElement> { this };
            Services.UI.Shadow.TryDrawUsingThemeShadow(LogoEllipse, LogoEllipseShadow, receivers, 16);

            AppVersion.Text = $"{ApplicationInfo.GetVersion(true)} {package.Id.Architecture}";
            GetBuildDate();
            thing.Text = $"© {GetCopyrightYear()} {Locale.Get("thgirypoc")}.";
        }

        private void GetBuildDate() {
            DateTimeOffset dto = ApplicationInfo.BuildDate;
            BuildDate.Text = GetNormalDateTime(dto, true).ToUpper();

            if (ApplicationInfo.ReleaseState != ApplicationReleaseState.Release) {
                var a = ApplicationInfo.GetExpirationDate();
                var b = a - DateTime.Now;

                FindName(nameof(NonReleaseInfo));
                NonReleaseInfo.Title = $"This is a {ApplicationInfo.ReleaseState} build.";
                NonReleaseInfo.Message = $"Expires in {GetNormalDateTime(a)} ({b.Days} day(s) left).";
            }
        }

        private string GetNormalDateTime(DateTimeOffset dt, bool ShowTime = false) {
            return ShowTime ? dt.ToString(@"yyyy\.MM\.dd H\:mm\:ss") : dt.ToString(@"yyyy\.MM\.dd");
        }

        private string GetCopyrightYear() {
            DateTimeOffset dto = ApplicationInfo.BuildDate;
            return $"2018 - {dto.Year}";
        }

        private void OpenLink(object sender, RoutedEventArgs e) {
            ButtonBase button = sender as ButtonBase;
            if (button.Tag != null && button.Tag is string link)
                new System.Action(async () => { await Launcher.LaunchUriAsync(new Uri(link)); })();
        }

        private void OpenConvWithLC(object sender, RoutedEventArgs e) {
            Main.GetCurrent().ShowConversationPage(-171015120);
        }

        private void LogoTapped(object sender, TappedRoutedEventArgs e) {
            if (EEAnmation.GetCurrentState() != Windows.UI.Xaml.Media.Animation.ClockState.Active) {
                EEAnmation.Begin();
                FindName(nameof(DeviceFamilyInfo));
                DeviceFamilyInfo.Title = AnalyticsInfo.VersionInfo.DeviceFamily;
            }
        }
    }
}