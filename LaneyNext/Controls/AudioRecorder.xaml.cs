using Elorucov.Laney.Core;
using Elorucov.Laney.Core.Media;
using Elorucov.Laney.Helpers;
using System;
using System.Diagnostics;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls
{
    public sealed partial class AudioRecorder : UserControl
    {
        public bool IsShowing { get; private set; }
        public event EventHandler<StorageFile> OnHide;
        StorageFile file = null;
        MediaPlayer player;
        DispatcherTimer recordTimer = new DispatcherTimer();
        DispatcherTimer playerTimer = new DispatcherTimer();
        Stopwatch stopwatch = new Stopwatch();

        public event EventHandler<TimeSpan> OnRecordTimeChanged;

        public AudioRecorder()
        {
            this.InitializeComponent();
        }
        private void Init()
        {
            recordTimer.Interval = TimeSpan.FromMilliseconds(10);
            playerTimer.Interval = TimeSpan.FromMilliseconds(10);
            recordTimer.Tick += (c, d) =>
            {
                TimeSpan diff = stopwatch.Elapsed;
                Duration.Text = $"{diff.ToString(@"m\:ss")}";
                OnRecordTimeChanged?.Invoke(this, diff);
            };

            player = new MediaPlayer();
            player.CommandManager.IsEnabled = false;
            player.PlaybackSession.PlaybackStateChanged += async (c, d) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    bool p = c.PlaybackState == MediaPlaybackState.Playing;
                    ChangeButton(p ? 3 : 2);
                    if (p) { playerTimer.Start(); } else { playerTimer.Stop(); }
                });
            };
            playerTimer.Tick += (c, d) =>
            {
                Duration.Text = $"{player.PlaybackSession.Position.ToString(@"m\:ss")} / {player.PlaybackSession.NaturalDuration.ToString(@"m\:ss")}";
            };
        }

        #region UI

        private void ChangeButton(int i)
        {
            StopButton.Visibility = i == 1 ? Visibility.Visible : Visibility.Collapsed;
            PlayButton.Visibility = i == 2 ? Visibility.Visible : Visibility.Collapsed;
            PauseButton.Visibility = i == 3 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Show()
        {
            Init();
            IsShowing = true;
            ChangeButton(0);
            PanelShow.Begin();
            StartRecording();
        }

        public async void Hide(bool keepFile = false)
        {
            if (AudioRecorderService.IsRecording)
            {
                StopRecording();
            }
            if (player != null) player.Pause();
            if (!keepFile && file != null)
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                file = null;
            }
            PanelHide.Begin();
        }

        private void PanelHide_Completed(object sender, object e)
        {
            IsShowing = false;
            stopwatch.Reset();
            player.Dispose();
            OnHide?.Invoke(this, file);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();
            player.Source = MediaSource.CreateFromStorageFile(file);
            Status.Text = "";
            TimeSpan diff = stopwatch.Elapsed;
            Duration.Text = $"{player.PlaybackSession.Position.ToString(@"m\:ss")} / {diff.ToString(@"m\:ss")}";
            ChangeButton(2);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            player.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            player.Pause();
        }

        private void Send(object sender, RoutedEventArgs e)
        {
            Hide(true);
        }

        private void HideControl(object sender, RoutedEventArgs e)
        {
            Hide();
        }
        #endregion

        private async void StartRecording()
        {
            Status.Text = Locale.Get("wait");
            file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"laneyrecording_{DateTimeOffset.Now.ToUnixTimeSeconds()}.m4a", CreationCollisionOption.GenerateUniqueName);
            try
            {
                var result = await AudioRecorderService.StartAsync(file);
                switch (result)
                {
                    case Windows.Media.Audio.AudioDeviceNodeCreationStatus.Success:
                        Status.Text = $"{Locale.Get("audiorec_recording")}...";
                        stopwatch.Start();
                        recordTimer.Start();
                        ChangeButton(1);
                        break;
                    case Windows.Media.Audio.AudioDeviceNodeCreationStatus.AccessDenied:
                        Status.Text = Locale.Get("access_denied");
                        break;
                    case Windows.Media.Audio.AudioDeviceNodeCreationStatus.DeviceNotAvailable:
                        Status.Text = Locale.Get("audiorec_nomic");
                        break;
                    default:
                        Status.Text = $"{Locale.Get("error")}: {result}";
                        break;
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync(ex);
                Hide();
            }
        }

        private async void StopRecording()
        {
            Status.Text = Locale.Get("wait");
            ChangeButton(0);
            recordTimer.Stop();
            stopwatch.Stop();
            await AudioRecorderService.StopAsync();
        }
    }
}
