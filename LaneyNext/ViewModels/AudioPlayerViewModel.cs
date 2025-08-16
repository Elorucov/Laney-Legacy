using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Elorucov.Laney.ViewModels
{
    public class AudioPlayerViewModel : BaseViewModel
    {
        private string _name;
        private ThreadSafeObservableCollection<AudioPlayerItem> _songs;
        private AudioPlayerItem _currentSong;
        private int _currentSongIndex;
        private TimeSpan _position;
        private MediaPlaybackState _playbackState;
        private bool _isPlaying;

        private RelayCommand _playPauseCommand;
        private RelayCommand _getPreviousCommand;
        private RelayCommand _getNextCommand;
        private RelayCommand _repeatCommand;

        public string Name { get { return _name; } set { _name = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<AudioPlayerItem> Songs { get { return _songs; } private set { _songs = value; OnPropertyChanged(); } }
        public AudioPlayerItem CurrentSong { get { return _currentSong; } set { _currentSong = value; OnPropertyChanged(); } }
        public int CurrentSongIndex { get { return _currentSongIndex; } private set { _currentSongIndex = value; OnPropertyChanged(); } }
        public TimeSpan Position { get { return _position; } private set { _position = value; OnPropertyChanged(); } }
        public MediaPlaybackState PlaybackState { get { return _playbackState; } private set { _playbackState = value; OnPropertyChanged(); } }
        public bool RepeatOneSong { get { return Player.IsLoopingEnabled; } set { Player.IsLoopingEnabled = value; OnPropertyChanged(); } }
        public bool IsPlaying { get { return _isPlaying; } private set { _isPlaying = value; OnPropertyChanged(); } }

        public RelayCommand PlayPauseCommand { get { return _playPauseCommand; } private set { _playPauseCommand = value; OnPropertyChanged(); } }
        public RelayCommand GetPreviousCommand { get { return _getPreviousCommand; } set { _getPreviousCommand = value; OnPropertyChanged(); } }
        public RelayCommand GetNextCommand { get { return _getNextCommand; } set { _getNextCommand = value; OnPropertyChanged(); } }
        public RelayCommand RepeatCommand { get { return _repeatCommand; } set { _repeatCommand = value; OnPropertyChanged(); } }

        public event EventHandler<MediaPlaybackState> PlaybackStateChanged;
        AudioType Type;

        public Guid Id { get; private set; }

        #region Private fields

        private MediaPlayer Player;
        private SystemMediaTransportControls SMTC { get { return Player.SystemMediaTransportControls; } }

        #endregion

        private AudioPlayerViewModel(List<Audio> songs, Audio currentSong, string name)
        {
            Log.General.Info("Audios", new ValueSet { { "count", songs.Count }, { "current", currentSong.Id } });
            Type = AudioType.Audio;
            Songs = new ThreadSafeObservableCollection<AudioPlayerItem>();
            Name = name;
            songs.ForEach(song =>
            {
                AudioPlayerItem api = new AudioPlayerItem(song);
                Songs.Add(api);
                if (song == currentSong) CurrentSong = api;
            });
            Initialize();

            SwitchSong();
            PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(CurrentSong)) SwitchSong();
                if (b.PropertyName == nameof(PlaybackState))
                {
                    PlaybackStateChanged?.Invoke(this, PlaybackState);
                }
                ;
            };
        }

        private AudioPlayerViewModel(List<Podcast> podcasts, Podcast currentPodcast, string name)
        {
            Log.General.Info("Podcasts", new ValueSet { { "count", podcasts.Count }, { "current", currentPodcast.Id } });
            Type = AudioType.Podcast;
            Songs = new ThreadSafeObservableCollection<AudioPlayerItem>();
            Name = name;
            podcasts.ForEach(podcast =>
            {
                AudioPlayerItem api = new AudioPlayerItem(podcast);
                Songs.Add(api);
                if (podcast == currentPodcast) CurrentSong = api;
            });
            Initialize();

            SwitchSong();
            PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(CurrentSong)) SwitchSong();
                if (b.PropertyName == nameof(PlaybackState))
                {
                    PlaybackStateChanged?.Invoke(this, PlaybackState);
                }
                ;
            };
        }

        private AudioPlayerViewModel(List<AudioMessage> messages, AudioMessage currentMessage, string ownerName)
        {
            Log.General.Info("Voice messages", new ValueSet { { "count", messages.Count }, { "current", currentMessage.Id } });
            Type = AudioType.VoiceMessage;
            Songs = new ThreadSafeObservableCollection<AudioPlayerItem>();
            Name = ownerName;
            messages.ForEach(message =>
            {
                AudioPlayerItem api = new AudioPlayerItem(message, ownerName);
                Songs.Add(api);
                if (message == currentMessage) CurrentSong = api;
            });
            Initialize();

            SwitchSong();
            PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(CurrentSong)) SwitchSong();
                if (b.PropertyName == nameof(RepeatOneSong)) Core.Settings.AudioPlayerIsLoopingEnabled = RepeatOneSong;
            };
        }

        private void Initialize()
        {
            Player = new MediaPlayer();
            Player.AudioCategory = MediaPlayerAudioCategory.Media;
            Player.AutoPlay = true;
            Player.CommandManager.IsEnabled = false;
            Player.PlaybackSession.PlaybackStateChanged += (a, b) =>
            {
                switch (a.PlaybackState)
                {
                    case MediaPlaybackState.Playing: SMTC.PlaybackStatus = MediaPlaybackStatus.Playing; IsPlaying = true; break;
                    case MediaPlaybackState.Paused: SMTC.PlaybackStatus = MediaPlaybackStatus.Paused; IsPlaying = false; break;
                }
                PlaybackState = a.PlaybackState;
            };
            Player.PlaybackSession.PositionChanged += (a, b) => Position = a.Position;
            Player.MediaEnded += (a, b) =>
            {
                if (!RepeatOneSong && Type == AudioType.Audio) PlayNext();
            };
            RepeatOneSong = Core.Settings.AudioPlayerIsLoopingEnabled;
            Log.General.Info("Player initialized", new ValueSet { { "repeat_one", RepeatOneSong } });

            SMTC.IsEnabled = true;
            SMTC.IsNextEnabled = true;
            SMTC.IsPauseEnabled = true;
            SMTC.IsPlayEnabled = true;
            SMTC.IsPreviousEnabled = true;
            SMTC.ButtonPressed += SMTC_ButtonPressed;

            PlayPauseCommand = new RelayCommand(o =>
            {
                switch (PlaybackState)
                {
                    case MediaPlaybackState.Playing: Pause(); break;
                    case MediaPlaybackState.Paused: Play(); break;
                }
            });
            GetPreviousCommand = new RelayCommand(o => PlayPrevious());
            GetNextCommand = new RelayCommand(o => PlayNext());
            RepeatCommand = new RelayCommand(o => RepeatOneSong = !RepeatOneSong);
        }

        private void Uninitialize()
        {
            Log.General.Info("Player uninitialized", new ValueSet { { "type", Type.ToString() } });
            Player.Dispose();
            Player = null;
        }
        private void SwitchSong()
        {
            if (CurrentSong == null) return;
            CurrentSongIndex = Songs.IndexOf(CurrentSong) + 1;
            Log.General.Info(String.Empty, new ValueSet { { "index", CurrentSongIndex } });
            Player.Source = MediaSource.CreateFromUri(CurrentSong.Source);
            Position = TimeSpan.FromMilliseconds(0);
            ChangeSMTCDisplayUpdater(CurrentSong);
            Play();
        }

        private void ChangeSMTCDisplayUpdater(AudioPlayerItem a)
        {
            SystemMediaTransportControlsDisplayUpdater du = SMTC.DisplayUpdater;
            du.AppMediaId = "Elorucov.Laney.Audio";
            du.Type = MediaPlaybackType.Music;
            du.MusicProperties.Artist = a.Performer;
            du.MusicProperties.Title = a.Title;
            du.Thumbnail = a.CoverUrl != null ? RandomAccessStreamReference.CreateFromUri(a.CoverUrl) : null;
            du.Update();
        }

        private void SMTC_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play: Player.Play(); break;
                case SystemMediaTransportControlsButton.Pause: Player.Pause(); break;
                case SystemMediaTransportControlsButton.Next: PlayNext(); break;
                case SystemMediaTransportControlsButton.Previous: PlayPrevious(); break;
            }
        }

        #region Controls

        public void SetPosition(TimeSpan position)
        {
            Player.PlaybackSession.Position = position;
        }

        public void Play()
        {
            if (Type != AudioType.VoiceMessage && VoiceMessageInstance != null) CloseVoiceMessageInstance();
            Player.Play();
        }

        public void Pause()
        {
            Player.Pause();
        }

        public void PlayNext()
        {
            int i = Songs.IndexOf(CurrentSong);
            if (i >= Songs.Count - 1)
            {
                CurrentSong = Songs[0];
            }
            else
            {
                CurrentSong = Songs[i + 1];
            }
        }

        public void PlayPrevious()
        {
            int i = Songs.IndexOf(CurrentSong);
            if (i <= 0)
            {
                CurrentSong = Songs[Songs.Count - 1];
            }
            else
            {
                CurrentSong = Songs[i - 1];
            }
        }

        #endregion

        #region Static members

        public static AudioPlayerViewModel MainInstance { get; private set; }
        public static AudioPlayerViewModel VoiceMessageInstance { get; private set; }

        public static event EventHandler InstancesChanged;
        public static void PlaySong(List<Audio> songs, Audio selectedSong, string name)
        {
            CloseVoiceMessageInstance();
            if (MainInstance != null)
            {
                MainInstance.Uninitialize();
            }
            MainInstance = new AudioPlayerViewModel(songs, selectedSong, name);
            InstancesChanged?.Invoke(null, null);
        }

        public static void PlayPodcast(List<Podcast> podcasts, Podcast currentPodcast, string name)
        {
            CloseVoiceMessageInstance();
            if (MainInstance != null)
            {
                MainInstance.Uninitialize();
            }
            MainInstance = new AudioPlayerViewModel(podcasts, currentPodcast, name);
            InstancesChanged?.Invoke(null, null);
        }

        public static void PlayVoiceMessage(List<AudioMessage> messages, AudioMessage selectedMessage, string ownerName)
        {
            CloseVoiceMessageInstance();
            if (MainInstance != null)
            {
                MainInstance.Pause();
            }
            VoiceMessageInstance = new AudioPlayerViewModel(messages, selectedMessage, ownerName);
            InstancesChanged?.Invoke(null, null);
        }

        public static void CloseMainInstance()
        {
            if (MainInstance != null)
            {
                MainInstance.Uninitialize();
                MainInstance = null;
                InstancesChanged?.Invoke(null, null);
            }
        }

        public static void CloseVoiceMessageInstance()
        {
            if (VoiceMessageInstance != null)
            {
                VoiceMessageInstance.Uninitialize();
                VoiceMessageInstance = null;
                InstancesChanged?.Invoke(null, null);
            }
        }
        #endregion
    }
}
