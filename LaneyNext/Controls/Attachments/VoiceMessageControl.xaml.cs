using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Attachments
{
    public sealed partial class VoiceMessageControl : UserControl
    {

        public VoiceMessageControl(AudioMessage audioMessage)
        {
            this.InitializeComponent();
            AudioMessage = audioMessage;
            Loaded += (a, b) =>
            {
                ViewManagement.UISettings.ColorValuesChanged += async (c, d) =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Setup(audioMessage);
                    });
                };

                AudioPlayerViewModel.InstancesChanged += async (y, z) =>
                {
                    if (Player != null)
                    {
                        Player.PropertyChanged += Player_PropertyChanged;
                    }
                    else
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            CheckIfAudioIsPlaying(MediaPlaybackState.None);
                            ForegroundSoundWave.Clip = new RectangleGeometry { Rect = new Rect(0, 0, WaveContainer.ActualWidth, WaveContainer.ActualHeight) };
                            Duration.Text = AudioMessage.DurationTime.ToString(@"m\:ss");
                        });
                    }
                };

                SizeChanged += (c, d) =>
                {
                    Setup(audioMessage);
                };
                Setup(audioMessage);

            };
        }

        public AudioMessage AudioMessage { get; private set; }
        public event EventHandler<AudioMessage> IsPlayButtonClicked;
        private AudioPlayerViewModel Player { get { return AudioPlayerViewModel.VoiceMessageInstance; } }

        private void ChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            Setup((AudioMessage)GetValue(dp));
        }

        private void Setup(AudioMessage am)
        {
            Duration.Text = am.DurationTime.ToString(@"m\:ss");
            TranscriptButton.Visibility = String.IsNullOrEmpty(am.Transcript) ? Visibility.Collapsed : Visibility.Visible;

            if (am.WaveForm != null && am.WaveForm.Length > 0)
            {
                wmax = am.WaveForm.Max();

                DrawSoundWaveLines(BackgroundSoundWave, (SolidColorBrush)Application.Current.Resources["VKButtonPrimaryBackgroundBrush"], am.WaveForm);
                DrawSoundWaveLines(ForegroundSoundWave, (SolidColorBrush)Application.Current.Resources["VKButtonPrimaryBackgroundBrush"], am.WaveForm);
                ForegroundSoundWave.Clip = new RectangleGeometry { Rect = new Rect(0, 0, WaveContainer.ActualWidth, WaveContainer.ActualHeight) };
            }
        }

        #region Render methods

        int wmax = 0;

        private void DrawSoundWaveLines(Canvas c, SolidColorBrush k, int[] waveForm)
        {
            c.Children.Clear();

            double x = WaveContainer.ActualWidth;
            double y = WaveContainer.ActualHeight;

            if (x > 0 && y > 0)
            {
                List<int> wave = GetWaveForm(waveForm);
                if (wave.Count > 0)
                {
                    c.Children.Clear();
                    for (int i = 0; i < wave.Count; i++)
                    {
                        int num = wave[i];
                        int left = i * 3;
                        double top = (y / 2) - (double)num / 2.0;
                        c.Children.Add(GetWaveformItem(num, left, top, k));
                    }
                }
            }
        }

        public static List<int> Resample(List<int> source, int targetLength)
        {
            if (source == null || source.Count == 0 || source.Count == targetLength)
            {
                return source;
            }
            int[] array = new int[targetLength];
            if (source.Count < targetLength)
            {
                double num = (double)source.Count / (double)targetLength;
                for (int i = 0; i < targetLength; i++)
                {
                    array[i] = source[(int)((double)i * num)];
                }
            }
            else
            {
                double num2 = (double)source.Count / (double)targetLength;
                double num3 = 0.0;
                double num4 = 0.0;
                int i = 0;

                foreach (int current in source)
                {
                    double num5 = Math.Min(num4 + 1.0, num2) - num4;
                    num3 += (double)current * num5;
                    num4 += num5;
                    if (num4 >= num2 - 0.001)
                    {
                        array[i++] = (int)Math.Round(num3 / num2);
                        if (num5 < 1.0)
                        {
                            num4 = 1.0 - num5;
                            num3 = (double)current * num4;
                        }
                        else
                        {
                            num4 = 0.0;
                            num3 = 0.0;
                        }
                    }
                }

                if (num3 > 0.0 && i < targetLength)
                {
                    array[i] = (int)Math.Round(num3 / num2);
                }
            }
            return array.ToList();
        }

        private List<int> GetWaveForm(int[] waveform)
        {
            List<int> list2 = new List<int>();
            List<int> WaveList = waveform.ToList();
            if (waveform != null && waveform.Length > 0)
            {
                int targetLength = (int)(WaveContainer.ActualWidth / 3.0);
                List<int> list = Resample(WaveList, targetLength);
                int num = list.Max();
                foreach (int t in list)
                {
                    int num2 = (int)Math.Round(WaveContainer.ActualHeight * ((double)t * 1.0 / (double)wmax));
                    if (num2 < 2)
                    {
                        num2 = 2;
                    }
                    if (num2 % 2 != 0)
                    {
                        num2++;
                    }
                    list2.Add(num2);
                }
            }
            return list2;
        }

        private Rectangle GetWaveformItem(int waveformItem, int left, double top, SolidColorBrush c)
        {
            Rectangle val = new Rectangle();
            val.Width = 2;
            val.Height = waveformItem;
            val.RadiusX = 0;
            val.RadiusY = 0;
            val.Margin = new Thickness(0, 0, 1, 0);
            val.Fill = c;
            Canvas.SetLeft(val, left);
            Canvas.SetTop(val, top);
            return val;
        }

        private void ChangeWaveClip()
        {
            double w = WaveContainer.ActualWidth / Player.CurrentSong.Duration.TotalMilliseconds * Player.Position.TotalMilliseconds;
            ForegroundSoundWave.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, WaveContainer.ActualHeight) };
            Duration.Text = Player.Position.ToString(@"m\:ss");
        }

        #endregion

        #region Player

        private void CheckIfAudioIsPlaying(MediaPlaybackState c)
        {
            if (Player?.CurrentSong?.Id == AudioMessage.Id)
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

        private async void Player_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Player?.CurrentSong?.Id != AudioMessage.Id) return;
            if (e.PropertyName == nameof(AudioPlayerViewModel.Position))
            {
                ChangeWaveClip();
            }
            else if (e.PropertyName == nameof(AudioPlayerViewModel.PlaybackState))
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    CheckIfAudioIsPlaying(Player.PlaybackState);
                });
            }
        }

        private void PlayPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            if (Player?.CurrentSong?.Id == AudioMessage.Id)
            {
                if (Player.PlaybackState == MediaPlaybackState.Playing)
                {
                    Player.Pause();
                }
                else
                {
                    Player.Play();
                }
            }
            else
            {
                IsPlayButtonClicked?.Invoke(this, AudioMessage);
            }
        }

        private void Seeker_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (Player != null)
            {
                e.Handled = true;
                double x = e.GetCurrentPoint(Seeker).Position.X;
                double w = Seeker.ActualWidth;
                double t = Player.CurrentSong.Duration.TotalMilliseconds / w * x;
                Player.SetPosition(TimeSpan.FromMilliseconds(t));
                ChangeWaveClip();
            }
        }

        #endregion

        private void ShowTranscript(object sender, RoutedEventArgs e)
        {
            VK.VKUI.Popups.Flyout f = new VK.VKUI.Popups.Flyout
            {
                Content = new TextBlock
                {
                    Text = AudioMessage.Transcript,
                    TextWrapping = TextWrapping.Wrap
                }
            };
            f.Placement = FlyoutPlacementMode.Top;
            f.ShowAt(sender as FrameworkElement);
        }
    }
}
