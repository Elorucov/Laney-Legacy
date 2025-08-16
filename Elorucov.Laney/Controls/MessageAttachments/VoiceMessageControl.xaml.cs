using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class VoiceMessageControl : UserControl {
        public VoiceMessageControl(AudioMessage audioMessage) {
            this.InitializeComponent();
            AudioMessage = audioMessage;
            Loaded += (a, b) => {
                CheckPlayerState();
                AudioPlayerViewModel.InstancesChanged += (y, z) => CheckPlayerState();

                SizeChanged += (c, d) => {
                    Setup(audioMessage);
                };
                Setup(audioMessage);

            };
        }

        private void CheckPlayerState() {
            if (Player != null) {
                Player.PropertyChanged += Player_PropertyChanged;
            } else {
                new System.Action(async () => {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                        CheckIfAudioIsPlaying(MediaPlaybackState.None);
                        ForegroundSoundWave.Clip = new RectangleGeometry { Rect = new Rect(0, 0, WaveContainer.ActualWidth, WaveContainer.ActualHeight) };
                        Duration.Text = AudioMessage.Duration >= 3600 ? AudioMessage.DurationTime.ToString("c") : AudioMessage.DurationTime.ToString(@"m\:ss");
                    });
                })();
            }
        }

        public AudioMessage AudioMessage { get; private set; }
        public event EventHandler<AudioMessage> IsPlayButtonClicked;
        private AudioPlayerViewModel Player { get { return AudioPlayerViewModel.VoiceMessageInstance; } }

        private void ChangedCallback(DependencyObject sender, DependencyProperty dp) {
            Setup((AudioMessage)GetValue(dp));
        }

        private void Setup(AudioMessage am) {
            Duration.Text = am.Duration >= 3600 ? am.DurationTime.ToString("c") : am.DurationTime.ToString(@"m\:ss");
            TranscriptButton.Visibility = string.IsNullOrEmpty(am.Transcript) ? Visibility.Collapsed : Visibility.Visible;
            if (!string.IsNullOrEmpty(am.Transcript)) TranscriptText.Text = am.Transcript;
            if (am.WaveForm != null && am.WaveForm.Length > 0) {
                wmax = am.WaveForm.Max();

                DrawSoundWaveLines(BackgroundSoundWave, "{ThemeResource AlternativeMessageControlForegroundAccentBrush}", am.WaveForm);
                DrawSoundWaveLines(ForegroundSoundWave, "{ThemeResource AlternativeMessageControlForegroundAccentBrush}", am.WaveForm);
                ForegroundSoundWave.Clip = new RectangleGeometry { Rect = new Rect(0, 0, WaveContainer.ActualWidth, WaveContainer.ActualHeight) };
            }
        }

        #region Render methods

        int wmax = 0;

        private void DrawSoundWaveLines(Canvas c, string xamlBrush, int[] waveForm) {
            c.Children.Clear();

            double x = WaveContainer.ActualWidth;
            double y = WaveContainer.ActualHeight;

            if (x > 0 && y > 0) {
                List<int> wave = GetWaveForm(waveForm);
                if (wave.Count > 0) {
                    c.Children.Clear();
                    for (int i = 0; i < wave.Count; i++) {
                        int num = wave[i];
                        int left = i * 3;
                        double top = (y / 2) - (double)num / 2.0;
                        c.Children.Add(GetWaveformItem(num, left, top, xamlBrush));
                    }
                }
            }
        }

        public static List<int> Resample(List<int> source, int targetLength) {
            if (source == null || source.Count == 0 || source.Count == targetLength) {
                return source;
            }
            int[] array = new int[targetLength];
            if (source.Count < targetLength) {
                double num = (double)source.Count / (double)targetLength;
                for (int i = 0; i < targetLength; i++) {
                    array[i] = source[(int)((double)i * num)];
                }
            } else {
                double num2 = (double)source.Count / (double)targetLength;
                double num3 = 0.0;
                double num4 = 0.0;
                int i = 0;

                foreach (int current in source) {
                    double num5 = Math.Min(num4 + 1.0, num2) - num4;
                    num3 += (double)current * num5;
                    num4 += num5;
                    if (num4 >= num2 - 0.001) {
                        array[i++] = (int)Math.Round(num3 / num2);
                        if (num5 < 1.0) {
                            num4 = 1.0 - num5;
                            num3 = (double)current * num4;
                        } else {
                            num4 = 0.0;
                            num3 = 0.0;
                        }
                    }
                }

                if (num3 > 0.0 && i < targetLength) {
                    array[i] = (int)Math.Round(num3 / num2);
                }
            }
            return array.ToList();
        }

        private List<int> GetWaveForm(int[] waveform) {
            List<int> list2 = new List<int>();
            List<int> WaveList = waveform.ToList();
            bool isAllEmpty = WaveList.Where(l => l == 0).Count() == WaveList.Count;
            if (AudioMessage.Id == 628213440) System.Diagnostics.Debugger.Break();
            if ((waveform != null && waveform.Length > 0) && !isAllEmpty) {
                int targetLength = (int)(WaveContainer.ActualWidth / 3.0);
                List<int> list = Resample(WaveList, targetLength);
                int num = list.Max();
                foreach (int t in list) {
                    int num2 = (int)Math.Round(WaveContainer.ActualHeight * ((double)t * 1.0 / (double)wmax));
                    if (num2 < 2) {
                        num2 = 2;
                    }
                    if (num2 % 2 != 0) {
                        num2++;
                    }
                    list2.Add(num2);
                }
            }
            return list2;
        }

        private Rectangle GetWaveformItem(int waveformItem, int left, double top, string xamlBrush) {
            string rectXaml = $"<Rectangle xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" Width=\"2\" Height=\"{waveformItem}\" Margin=\"0,0,1,0\" Fill=\"{xamlBrush}\"></Rectangle>";
            Rectangle rect = XamlReader.Load(rectXaml) as Rectangle;
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            return rect;
        }

        private void ChangeWaveClip() {
            double w = WaveContainer.ActualWidth / Player.CurrentSong.Duration.TotalMilliseconds * Player.Position.TotalMilliseconds;
            ForegroundSoundWave.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, WaveContainer.ActualHeight) };
            Duration.Text = Player.Position.ToString(@"m\:ss");
        }

        #endregion

        #region Player

        private void CheckIfAudioIsPlaying(MediaPlaybackState c) {
            if (Player?.CurrentSong?.Id == AudioMessage.Id) {
                switch (c) {
                    case MediaPlaybackState.Playing:
                        IconPresenter.ContentTemplate = (DataTemplate)Application.Current.Resources["Icon24Pause"];
                        break;
                    case MediaPlaybackState.None:
                    case MediaPlaybackState.Paused:
                        IconPresenter.ContentTemplate = (DataTemplate)Application.Current.Resources["Icon24Play"];
                        break;
                }
            } else {
                IconPresenter.ContentTemplate = (DataTemplate)Application.Current.Resources["Icon24Play"];
            }
        }

        private void Player_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (Player?.CurrentSong?.Id != AudioMessage.Id) return;
            if (e.PropertyName == nameof(AudioPlayerViewModel.Position)) {
                ChangeWaveClip();
            } else if (e.PropertyName == nameof(AudioPlayerViewModel.PlaybackState)) {
                new System.Action(async () => {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                        CheckIfAudioIsPlaying(Player.PlaybackState);
                    });
                })();
            }
        }

        private void PlayPauseButtonClicked(object sender, RoutedEventArgs e) {
            if (Player?.CurrentSong?.Id == AudioMessage.Id) {
                if (Player.PlaybackState == MediaPlaybackState.Playing) {
                    Player.Pause();
                } else {
                    Player.Play();
                }
            } else {
                IsPlayButtonClicked?.Invoke(this, AudioMessage);
            }
        }

        private void Seeker_PointerPressed(object sender, PointerRoutedEventArgs e) {
            if (Player != null) {
                e.Handled = true;
                double x = e.GetCurrentPoint(Seeker).Position.X;
                double w = Seeker.ActualWidth;
                double t = Player.CurrentSong.Duration.TotalMilliseconds / w * x;
                Player.SetPosition(TimeSpan.FromMilliseconds(t));
                ChangeWaveClip();
            }
        }

        #endregion

        private void ShowTranscript(object sender, RoutedEventArgs e) {
            TranscriptContainer.Visibility = TranscriptContainer.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}