using ELOR.VKAPILib.Objects;
using Elorucov.Laney.ViewModels;
using System;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Attachments
{
    public sealed partial class AudioControl : UserControl
    {
        long id = 0;

        public static readonly DependencyProperty AudioProperty = DependencyProperty.Register(
            "Audio", typeof(Audio), typeof(AudioControl), new PropertyMetadata(default(object)));

        public Audio Audio
        {
            get { return (Audio)GetValue(AudioProperty); }
            set { SetValue(AudioProperty, value); }
        }

        private void SetUp(DependencyObject sender, DependencyProperty dp)
        {
            SetUp((Audio)GetValue(dp));
        }

        public event EventHandler<Audio> IsPlayButtonClicked;

        public AudioControl(Audio audio)
        {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(AudioProperty, SetUp);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(AudioProperty, id); };
            Audio = audio;
            Loaded += Load;
        }

        public AudioControl()
        {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(AudioProperty, SetUp);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(AudioProperty, id); };
            Loaded += Load;
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            SetUp((Audio)GetValue(AudioProperty));
            AudioPlayerViewModel.InstancesChanged += async (a, b) =>
            {
                AudioPlayerViewModel apvm = AudioPlayerViewModel.MainInstance;
                if (apvm != null)
                {
                    apvm.PlaybackStateChanged += async (c, d) =>
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            CheckIfAudioIsPlaying(apvm.PlaybackState);
                        });
                    };
                }
                else
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        CheckIfAudioIsPlaying(MediaPlaybackState.None);
                    });
                }
            };
        }

        private void SetUp(Audio a)
        {
            SongName.Text = a.Title;
            SongArtist.Text = a.Artist;
            if (!String.IsNullOrEmpty(a.Subtitle)) SongSubtitle.Text = a.Subtitle;
            Duration.Text = a.DurationTime.ToString(@"m\:ss");

            if (AudioPlayerViewModel.MainInstance != null)
            {
                CheckIfAudioIsPlaying(AudioPlayerViewModel.MainInstance.PlaybackState);
            }
            else
            {
                CheckIfAudioIsPlaying(MediaPlaybackState.None);
            }
        }

        private void CheckIfAudioIsPlaying(MediaPlaybackState c)
        {
            if (AudioPlayerViewModel.MainInstance?.CurrentSong?.Id == Audio.Id)
            {
                switch (c)
                {
                    case MediaPlaybackState.Playing:
                        IconPresenter.ContentTemplate = (DataTemplate)Application.Current.Resources["Icon24Pause"];
                        break;
                    case MediaPlaybackState.None:
                    case MediaPlaybackState.Paused:
                        IconPresenter.ContentTemplate = (DataTemplate)Application.Current.Resources["Icon24Play"];
                        break;
                }
            }
            else
            {
                IconPresenter.ContentTemplate = (DataTemplate)Application.Current.Resources["Icon24Play"];
            }
        }

        private void PlayPauseClicked(object sender, RoutedEventArgs e)
        {
            if (AudioPlayerViewModel.MainInstance?.CurrentSong?.Id == Audio.Id)
            {
                if (AudioPlayerViewModel.MainInstance.PlaybackState == MediaPlaybackState.Playing)
                {
                    AudioPlayerViewModel.MainInstance.Pause();
                }
                else
                {
                    AudioPlayerViewModel.MainInstance.Play();
                }
            }
            else
            {
                IsPlayButtonClicked?.Invoke(this, Audio);
            }
        }
    }
}
