using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class VideoPlayerView : Page {
        private static ViewModePreferences vmpref;

        public static async Task Show(long messageId, Video video) {
            if (video.Restriction != null && (!string.IsNullOrEmpty(video.Restriction.Title) || !string.IsNullOrEmpty(video.Restriction.Text))) {
                ContentDialog dlg = new ContentDialog {
                    Title = video.Restriction.Title,
                    Content = video.Restriction.Text,
                    PrimaryButtonText = Locale.Get("close"),
                    DefaultButton = ContentDialogButton.Primary
                };
                if (video.Restriction.CanPlay && video.Restriction.Button.Action == "play") {
                    dlg.SecondaryButtonText = video.Restriction.Button.Title;
                }
                if (await dlg.ShowAsync() != ContentDialogResult.Secondary) return;
            } else if (video.Upcoming == 1) return;

            vmpref = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            vmpref.CustomSize = video.Type == "short_video" ? new Size(270, 480) : new Size(480, 270);
            await ViewManagement.OpenNewCompactOverlayWindow(vmpref, typeof(VideoPlayerView), "Video player", new Tuple<long, Video>(messageId, video));
        }

        Video Video;

        public VideoPlayerView() {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                TitleAndStatusBar.ExtendView(true, true);
            } else {
                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            }

            var info = e.Parameter as Tuple<long, Video>;
            Video = info.Item2;
            Loaded += async (a, b) => await CheckAndShowVideo();
        }

        private async Task CheckAndShowVideo() {
            if (Video == null) {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                return;
            }

            ApplicationView.GetForCurrentView().Consolidated += (a, b) => {
                Web.NavigateToString("");
                VKVideo.MediaPlayer.Pause();
                VKVideo.MediaPlayer.Source = null;
            };

            TitleAndStatusBar.ChangeBackgroundColor(Color.FromArgb(0, 0, 0, 0));
            await TitleAndStatusBar.ChangeColor(Color.FromArgb(255, 255, 255, 255));

            try {
                string accessKey = !string.IsNullOrEmpty(Video.AccessKey) ? $"_{Video.AccessKey}" : string.Empty;
                var response = await Videos.Get(Video.OwnerId, $"{Video.OwnerId}_{Video.Id}{accessKey}");
                if (response is VKList<Video> videos && videos.Count > 0) {
                    Video = videos.Items.FirstOrDefault();
                    if (Video.Restriction != null && !Video.Restriction.CanPlay) {
                        ShowError($"{Video.Restriction.Title}\n{Video.Restriction.Text}");
                        return;
                    }
                    bool needWebView = true;
                    if (string.IsNullOrEmpty(Video.Platform) || Video.Live == 0 || Video.Upcoming == 0) {
                        if (Video.Files != null) {
                            Uri videoUri = null;

                            if (!string.IsNullOrEmpty(Video.Files.MP4p144)) videoUri = new Uri(Video.Files.MP4p144);
                            if (!string.IsNullOrEmpty(Video.Files.MP4p240)) videoUri = new Uri(Video.Files.MP4p240);
                            if (!string.IsNullOrEmpty(Video.Files.MP4p360)) videoUri = new Uri(Video.Files.MP4p360);
                            if (!string.IsNullOrEmpty(Video.Files.MP4p480)) videoUri = new Uri(Video.Files.MP4p480);
                            if (!string.IsNullOrEmpty(Video.Files.MP4p720)) videoUri = new Uri(Video.Files.MP4p720);
                            if (!string.IsNullOrEmpty(Video.Files.MP4p1080)) videoUri = new Uri(Video.Files.MP4p1080);
                            if (!string.IsNullOrEmpty(Video.Files.HLS)) videoUri = new Uri(Video.Files.HLS);
                            if (!string.IsNullOrEmpty(Video.Files.HLSOndemand)) videoUri = new Uri(Video.Files.HLSOndemand);

                            if (videoUri != null) {
                                needWebView = false;
                                LoadingPanel.Visibility = Visibility.Collapsed;
                                VKVideo.Visibility = Visibility.Visible;
                                VKVideo.Source = MediaSource.CreateFromUri(videoUri);
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(Video.Platform) || needWebView) {
                        Web.DOMContentLoaded += Web_DOMContentLoaded;
                        Web.NavigationFailed += Web_NavigationFailed1;
                        Web.Navigate(Video.PlayerUri);
                    }
                } else {
                    Exception ex = response as Exception;
                    ShowError($"{Locale.Get("global_error")} (0x{ex.HResult.ToString("x8")})\n{ex.Message.Trim()}");
                }
            } catch (Exception ex) {
                ShowError($"{Locale.Get("global_error")} (0x{ex.HResult.ToString("x8")})\n{ex.Message.Trim()}");
            }
        }

        private void ShowError(string error) {
            LoadingSpinner.Visibility = Visibility.Collapsed;
            Web.Visibility = Visibility.Collapsed;
            Log.Error($"Error in video player: {error}");
            ErrorInfo.Text = error;
            ProgressText.Text = string.Empty;
        }

        #region Video from other platform (WebView)

        private void Web_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args) {
            LoadingPanel.Visibility = Visibility.Collapsed;
            Web.Visibility = Visibility.Visible;
        }

        private void Web_NavigationFailed1(object sender, WebViewNavigationFailedEventArgs e) {
            ShowError($"Player loading error: {e.WebErrorStatus}");
        }

        #endregion
    }
}