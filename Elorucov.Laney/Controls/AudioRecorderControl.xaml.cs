using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Media;
using System;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class AudioRecorderControl : UserControl {
        bool isShowing = false;
        static StorageFile file;
        static DispatcherTimer playerTimer = new DispatcherTimer();
        static DispatcherTimer recordTimer = new DispatcherTimer();
        static TimeSpan startTime;
        static MediaPlayer player;
        DateTime lastTimeUserIsRecordingWasCalled = DateTime.Now;

        public AudioRecorderControl() {
            this.InitializeComponent();
        }

        public event EventHandler<StorageFile> DoneButtonClicked;
        public event EventHandler SendRecordingActivityRequested;

        static bool isInitialized = false;
        private void Init() {
            if (isInitialized) return;
            try {
                recordTimer.Interval = TimeSpan.FromMilliseconds(10);
                playerTimer.Interval = TimeSpan.FromMilliseconds(10);
                recordTimer.Tick += (c, d) => {
                    TimeSpan diff = DateTime.Now.TimeOfDay - startTime;
                    RecInfo.Text = $"{Locale.Get("audiorec_recording")}...   {diff.ToString(@"m\:ss")}.{diff.Milliseconds}";
                    SendRecordingActivity();
                };
                player = new MediaPlayer();
                player.CommandManager.IsEnabled = false;
                player.PlaybackSession.PlaybackStateChanged += async (c, d) => {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                        bool p = c.PlaybackState == MediaPlaybackState.Playing;
                        pauseBtn.Visibility = p ? Visibility.Visible : Visibility.Collapsed;
                        playBtn.Visibility = !p ? Visibility.Visible : Visibility.Collapsed;
                        if (p) { playerTimer.Start(); } else { playerTimer.Stop(); }
                    });
                };
                playerTimer.Tick += (c, d) => {
                    RecInfo.Text = $"{player.PlaybackSession.Position.ToString(@"m\:ss")} / {player.PlaybackSession.NaturalDuration.ToString(@"m\:ss")}";
                };
                isInitialized = true;
            } catch (Exception ex) {
                Functions.ShowHandledErrorDialog(ex);
                Hide();
            }
        }

        public void Show() {
            Init();
            if (!isInitialized) {
                return;
            }

            doneBtn.IsEnabled = false;
            ChangeButton(0);
            Visibility = Visibility.Visible;
            isShowing = true;
            RecInfo.Text = $"{Locale.Get("wait")}...";
            PanelShow.Begin();
            StartRecord();
        }

        private void Hide() {
            doneBtn.IsEnabled = false;
            ChangeButton(0);
            PlayerContainer.Visibility = Visibility.Collapsed;
            Visibility = Visibility.Collapsed;
            isShowing = false;
        }

        private void StartRecord() {
            new Action(async () => {
                try {
                    file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"laneyrecording_{DateTimeOffset.Now.ToUnixTimeSeconds()}.mp3", CreationCollisionOption.GenerateUniqueName);
                    await AudioRecorder2.StartAsync(file);

                    startTime = DateTime.Now.TimeOfDay;
                    recordTimer.Start();
                    ChangeButton(1);
                    doneBtn.IsEnabled = true;
                } catch (UnauthorizedAccessException) {
                    RecInfo.Text = Locale.Get("access_denied");
                } catch (NotSupportedException) {
                    RecInfo.Text = Locale.Get("audiorec_nomic");
                } catch (Exception ex) {
                    RecInfo.Text = $"{Locale.Get("global_error")} (0x{ex.HResult.ToString("x8")}): {ex.Message.Trim()}";
                }
            })();
        }

        private void StopRecord() {
            new System.Action(async () => {
                try {
                    TimeSpan diff = DateTime.Now.TimeOfDay - startTime;
                    RecInfo.Text = Locale.Get("savingfile");
                    recordTimer.Stop();
                    ChangeButton(0);
                    await AudioRecorder2.StopAsync();

                    player.Source = MediaSource.CreateFromStorageFile(file);
                    PlayerContainer.Visibility = Visibility.Visible;
                    RecInfo.Text = $"{player.PlaybackSession.Position.ToString(@"m\:ss")} / {diff.ToString(@"m\:ss")}";
                    ChangeButton(2);
                } catch (Exception ex) {
                    RecInfo.Text = $"{Locale.Get("global_error")} (0x{ex.HResult.ToString("x8")}): {ex.Message.Trim()}";
                }
            })();
        }

        private void StopRecord(object sender, RoutedEventArgs e) {
            StopRecord();
        }

        private void Close(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                if (AudioRecorder2.IsRecording) StopRecord();
                if (player != null) {
                    player.Source = null;
                    playerTimer.Stop();
                }
                if (file != null) {
                    try {
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    } catch (Exception ex) {
                        if (ex.HResult == -2147024894) return; // File not found
                        Functions.ShowHandledErrorDialog(ex);
                    }
                }
                PanelHide.Begin();
            })();
        }

        private void Done(object sender, RoutedEventArgs e) {
            if (AudioRecorder2.IsRecording) StopRecord();
            if (player != null) {
                player.Source = null;
                playerTimer.Stop();
            }
            DoneButtonClicked?.Invoke(this, file);
            PanelHide.Begin();
        }

        private void ChangeButton(int i) {
            stopBtn.Visibility = i == 1 ? Visibility.Visible : Visibility.Collapsed;
            playBtn.Visibility = i == 2 ? Visibility.Visible : Visibility.Collapsed;
            pauseBtn.Visibility = i == 3 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PlayButton(object sender, RoutedEventArgs e) {
            if (player != null) player.Play();
        }

        private void PauseButton(object sender, RoutedEventArgs e) {
            if (player != null) player.Pause();
        }

        private void PanelHide_Completed(object sender, object e) {
            Hide();
        }

        private void SendRecordingActivity() {
            if ((DateTime.Now - lastTimeUserIsRecordingWasCalled).TotalSeconds <= 10) return;
            SendRecordingActivityRequested?.Invoke(this, new EventArgs());
            lastTimeUserIsRecordingWasCalled = DateTime.Now;
        }
    }
}