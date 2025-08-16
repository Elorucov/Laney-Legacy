using Elorucov.Laney.Services.Network;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class AudioUC : UserControl {
        long id = 0;

        public static readonly DependencyProperty AudioProperty = DependencyProperty.Register(
            "Audio", typeof(Audio), typeof(AudioUC), new PropertyMetadata(default(object)));

        public Audio Audio {
            get { return (Audio)GetValue(AudioProperty); }
            set { SetValue(AudioProperty, value); }
        }

        private void SetUp(DependencyObject sender, DependencyProperty dp) {
            new System.Action(async () => { await SetUpAsync((Audio)GetValue(dp)); })();
        }

        public event EventHandler<Audio> IsPlayButtonClicked;

        public AudioUC() {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(AudioProperty, SetUp);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(AudioProperty, id); };
            Loaded += Load;

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) PlayPauseButton.OpacityTransition = new ScalarTransition();
        }

        private void Load(object sender, RoutedEventArgs e) {
            CheckPlayerState();
            AudioPlayerViewModel.InstancesChanged += (a, b) => CheckPlayerState();
        }

        private void CheckPlayerState() {
            AudioPlayerViewModel apvm = AudioPlayerViewModel.MainInstance;
            if (apvm != null) {
                apvm.PlaybackStateChanged += async (c, d) => {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                        CheckIfAudioIsPlaying(apvm.PlaybackState);
                    });
                };
            } else {
                new System.Action(async () => {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                        CheckIfAudioIsPlaying(MediaPlaybackState.None);
                    });
                })();
            }
        }

        private async Task SetUpAsync(Audio a) {
            SongName.Text = a.Title;
            SongArtist.Text = a.Artist;
            SongSubtitle.Text = !string.IsNullOrEmpty(a.Subtitle) ? a.Subtitle : string.Empty;
            duration.Text = a.Duration >= 3600 ? a.DurationTime.ToString("c") : a.DurationTime.ToString(@"m\:ss");

            AudioAlbumThumb thumb = a.Thumb;
            if (thumb == null) thumb = a.Album?.Thumb;
            if (thumb != null && Uri.IsWellFormedUriString(thumb.Photo135, UriKind.Absolute)) {
                BitmapImage bi = new BitmapImage {
                    DecodePixelHeight = 40,
                    DecodePixelWidth = 40,
                    DecodePixelType = DecodePixelType.Logical,
                };
                await bi.SetUriSourceAsync(new Uri(thumb.Photo135));
                Cover.Fill = new ImageBrush {
                    ImageSource = bi,
                };
            }

            if (AudioPlayerViewModel.MainInstance != null) {
                CheckIfAudioIsPlaying(AudioPlayerViewModel.MainInstance.PlaybackState);
            } else {
                CheckIfAudioIsPlaying(MediaPlaybackState.None);
            }
        }

        private void CheckIfAudioIsPlaying(MediaPlaybackState c) {
            if (AudioPlayerViewModel.MainInstance?.CurrentSong?.Id == Audio.Id) {
                PlayPauseButton.Opacity = 1;
                switch (c) {
                    case MediaPlaybackState.Playing:
                        Icon.Id = VK.VKUI.Controls.VKIconName.Icon24Pause;
                        break;
                    case MediaPlaybackState.None:
                    case MediaPlaybackState.Paused:
                        Icon.Id = VK.VKUI.Controls.VKIconName.Icon24Play;
                        break;
                }
            } else {
                PlayPauseButton.Opacity = 0;
                Icon.Id = VK.VKUI.Controls.VKIconName.Icon24Play;
            }
        }

        private void PlayPauseClicked(object sender, RoutedEventArgs e) {
            if (AudioPlayerViewModel.MainInstance?.CurrentSong?.Id == Audio.Id) {
                if (AudioPlayerViewModel.MainInstance.PlaybackState == MediaPlaybackState.Playing) {
                    AudioPlayerViewModel.MainInstance.Pause();
                } else {
                    AudioPlayerViewModel.MainInstance.Play();
                }
            } else {
                IsPlayButtonClicked?.Invoke(this, Audio);
            }
        }

        private void OnPointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            PlayPauseButton.Opacity = 1;
        }

        private void OnPointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            if (Audio == null) {
                PlayPauseButton.Opacity = 0;
                return;
            }

            PlayPauseButton.Opacity = AudioPlayerViewModel.MainInstance?.CurrentSong?.Id == Audio.Id ? 1 : 0;
        }
    }
}
