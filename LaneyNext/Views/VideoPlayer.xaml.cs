using ELOR.VKAPILib;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class VideoPlayer : Page
    {
        public VideoPlayer()
        {
            this.InitializeComponent();
            Loaded += (a, b) => CheckAndShowVideo();
            SizeChanged += VideoPlayer_SizeChanged;
            Window.Current.CoreWindow.Closed += (a, b) =>
            {
                Web.NavigateToString("");
            };
        }

        Video Video;
        VKAPI API;
        List<VideoSource> Sources;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Tuple<Video, VKAPI> data)
            {
                Video = data.Item1;
                API = data.Item2;
            }
        }

        private async void CheckAndShowVideo()
        {
            var response = await API.Video.GetAsync(0, $"{Video.OwnerId}_{Video.Id}");
            Video = response.Items.FirstOrDefault();

            Web.NavigationCompleted += Web_NavigationCompleted;
            Web.NavigationFailed += Web_NavigationFailed;
            Web.ContainsFullScreenElementChanged += Web_ContainsFullScreenElementChanged;
            Web.Navigate(Video.PlayerUri);
        }

        private void ShowError(string error)
        {
            LoadingSpinner.Visibility = Visibility.Collapsed;
            ErrorInfo.Text = error;
        }

        private async void VideoPlayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.ViewMode == ApplicationViewMode.Default && !view.IsFullScreenMode && !view.IsFullScreen)
            {
                await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
            }
        }

        #region VK video

        private void ShowVKVideo(List<VideoSource> sources)
        {
            ErrorInfo.Text = $"Count: {sources.Count}";
            if (sources.Count == 0)
            {
                ErrorInfo.Text = $"Links not found.";
                return;
            }
            Sources = sources;
            LoadingPanel.Visibility = Visibility.Collapsed;
            VKVideo.Visibility = Visibility.Visible;
            VKVideo.Source = MediaSource.CreateFromUri(sources.First().Source);

            AudioPlayerViewModel.MainInstance?.Pause();
            AudioPlayerViewModel.VoiceMessageInstance?.Pause();
        }

        private void VideoMTC_SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            MenuFlyout mf = new MenuFlyout { Placement = FlyoutPlacementMode.Top };

            MenuFlyoutSubItem resolution = new MenuFlyoutSubItem { Text = Locale.Get("resolution") };
            if (Sources.Count == 1 && Sources.First().Resolution == 0)
            { // If video is clip (a. k. a. short_video).
            }
            else
            {
                foreach (var source in Sources)
                {
                    ToggleMenuFlyoutItem r = new ToggleMenuFlyoutItem { Text = $"{source.Resolution}p" };
                    r.IsChecked = ((MediaSource)VKVideo.Source).Uri == source.Source;
                    r.Click += (a, b) =>
                    {
                        ChangeSource(source);
                        mf.Hide();
                    };
                    resolution.Items.Add(r);
                }
            }

            List<double> rates = new List<double> { 0.25, 0.5, 1, 1.5, 2 };
            MenuFlyoutSubItem speed = new MenuFlyoutSubItem { Text = Locale.Get("speed") };
            foreach (double rate in rates)
            {
                ToggleMenuFlyoutItem s = new ToggleMenuFlyoutItem { Text = $"{rate}x" };
                s.IsChecked = VKVideo.MediaPlayer.PlaybackSession.PlaybackRate == rate;
                s.Click += (a, b) =>
                {
                    VKVideo.MediaPlayer.PlaybackSession.PlaybackRate = rate;
                    mf.Hide();
                };
                speed.Items.Add(s);
            }

            if (resolution.Items.Count > 0) mf.Items.Add(resolution);
            mf.Items.Add(speed);
            mf.ShowAt(button);
        }

        private void ChangeSource(VideoSource source)
        {
            var pos = VKVideo.MediaPlayer.PlaybackSession.Position;
            VKVideo.Source = MediaSource.CreateFromUri(source.Source);
            VKVideo.MediaPlayer.PlaybackSession.Position = pos;
            VKVideo.MediaPlayer.Play();
        }

        #endregion

        #region Video from other platform (WebView)

        private void Web_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            Web.Visibility = Visibility.Visible;
        }

        private void Web_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            ShowError($"Player loading error: {e.WebErrorStatus}");
        }

        Size windowSize;
        private async void Web_ContainsFullScreenElementChanged(WebView sender, object args)
        {
            var view = ApplicationView.GetForCurrentView();
            if (sender.ContainsFullScreenElement)
            {
                windowSize = new Size(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
                view.TryEnterFullScreenMode();
            }
            else
            {
                await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                view.TryResizeView(windowSize);
            }
        }

        #endregion
    }
}