using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.ViewModels;
using System;
using System.Diagnostics;
using VK.VKUI.Controls;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class AudioPlayer : Page
    {
        AudioPlayerViewModel ViewModel { get { return DataContext as AudioPlayerViewModel; } set { DataContext = value; } }
        bool PlaylistVisible = false;
        public AudioPlayer()
        {
            this.InitializeComponent();
            ViewModel = AudioPlayerViewModel.MainInstance;
            ViewModel.PropertyChanged += AudioPlayerPropertyChanged;
            CoverPlaceholderIcon.Id = GetIconForCurrentType(ViewModel.CurrentSong.Type);

            AudioPlayerViewModel.InstancesChanged += AudioPlayerInstancesChanged;
            Window.Current.SizeChanged += Current_SizeChanged;
            CoreApplication.GetCurrentView().CoreWindow.ResizeCompleted += CoreWindow_ResizeCompleted;
            Window.Current.Closed += (a, b) => AudioPlayerViewModel.InstancesChanged -= AudioPlayerInstancesChanged;
        }

        private async void AudioPlayerInstancesChanged(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (AudioPlayerViewModel.MainInstance != null)
                {
                    ViewModel = AudioPlayerViewModel.MainInstance;
                    ViewModel.PropertyChanged += AudioPlayerPropertyChanged;
                }
                else
                {
                    Window.Current.Close();
                }
            });
        }

        private async void AudioPlayerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Sometimes MediaSlider.Position and CoverPlaceholderIcon.ContentTemplate 
                // fails to bind AudioPlayerViewModel.Position and AudioPlayerViewModel.CurrentSong.CoverPlaceholderIcon properties without any reason.
                // So I had to write this crutch...
                if (e.PropertyName == nameof(AudioPlayerViewModel.Position))
                {
                    MediaSlider.Position = ViewModel.Position;
                }
                if (e.PropertyName == nameof(AudioPlayerViewModel.CurrentSong))
                {
                    CoverPlaceholderIcon.Id = GetIconForCurrentType(ViewModel.CurrentSong.Type);
                }
            });
        }

        private VKIconName GetIconForCurrentType(AudioType type)
        {
            switch (type)
            {
                case AudioType.Podcast: return VKIconName.Icon28PodcastOutline;
                case AudioType.VoiceMessage: return VKIconName.Icon28VoiceOutline;
                default: return VKIconName.Icon28SongOutline;
            }
        }

        double oldheight = 0;
        double hdiff = 0;
        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            if (e.Size.Height != oldheight)
            {
                hdiff = e.Size.Height - oldheight;
                oldheight = e.Size.Height;
                Debug.WriteLine($"hdiff: {hdiff} old: {oldheight} new: {e.Size.Height}");
            }
        }

        private void CoreWindow_ResizeCompleted(Windows.UI.Core.CoreWindow sender, object args)
        {
            FixWindowHeight();
        }

        private void FixWindowHeight()
        {
            double w = Window.Current.Bounds.Width;
            double h = Window.Current.Bounds.Height;
            if (h < Constants.AudioPlayerFaceHeight)
            {
                PlaylistVisible = false;
            }
            else if (h > Constants.AudioPlayerFaceHeight && h < Constants.AudioPlayerFullHeight)
            {
                PlaylistVisible = hdiff > 0;
            }
            else if (h > Constants.AudioPlayerFullHeight)
            {
                PlaylistVisible = true;
            }
            ResizeWindowHeightAfterToggle();
        }

        private void ResizeWindowHeightAfterToggle()
        {
            double h = PlaylistVisible ? Constants.AudioPlayerFullHeight : Constants.AudioPlayerFaceHeight;
            ApplicationView.GetForCurrentView().TryResizeView(new Size(Window.Current.Bounds.Width, h));
        }

        private void MediaSlider_PositionChanged(object sender, TimeSpan e)
        {
            ViewModel?.SetPosition(e);
        }

        private void TogglePlaylistVisibility(object sender, RoutedEventArgs e)
        {
            PlaylistVisible = !PlaylistVisible;
            ResizeWindowHeightAfterToggle();
        }
    }
}
