using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Objects;
using System;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Models {
    public enum AudioType {
        Audio, Podcast, VoiceMessage
    }

    public class AudioPlayerItem {
        public AudioType Type { get; private set; }
        public long Id { get; private set; }
        public string Title { get; private set; }
        public string Performer { get; private set; }
        public TimeSpan Duration { get; private set; }
        public Uri Source { get; private set; }
        public DataTemplate CoverPlaceholderIcon { get; private set; }
        public Uri CoverUrl { get; private set; }
        public AttachmentBase Attachment { get; private set; }

        public AudioPlayerItem(Audio audio) {
            Attachment = audio;
            Type = AudioType.Audio;
            Id = audio.Id;
            Title = audio.Title;
            if (!string.IsNullOrEmpty(audio.Subtitle)) Title += $" ({audio.Subtitle})";
            Performer = audio.Artist;
            Duration = audio.DurationTime;
            Source = audio.Uri;
            CoverPlaceholderIcon = VK.VKUI.VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28SongOutline);
            if (Uri.IsWellFormedUriString(audio.Thumb?.Photo135, UriKind.Absolute)) {
                CoverUrl = new Uri(audio.Thumb.Photo135);
            } else if (Uri.IsWellFormedUriString(audio.Album?.Thumb?.Photo135, UriKind.Absolute)) {
                CoverUrl = new Uri(audio.Album.Thumb.Photo135);
            }
        }

        public AudioPlayerItem(Podcast podcast) {
            Attachment = podcast;
            Type = AudioType.Podcast;
            Id = podcast.Id;
            Title = podcast.Title;
            Performer = podcast.Artist;
            Duration = podcast.DurationTime;
            Source = podcast.Uri;
            CoverPlaceholderIcon = VK.VKUI.VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28PodcastOutline);
            CoverUrl = podcast.Info.Cover?.Sizes[0].Uri;
        }

        public AudioPlayerItem(AudioMessage audioMessage, string ownerName) {
            Attachment = audioMessage;
            Type = AudioType.VoiceMessage;
            Id = audioMessage.Id;
            Title = Locale.Get("atch_audio_message_nom").Capitalize();
            Performer = ownerName;
            Duration = audioMessage.DurationTime;
            Source = audioMessage.Uri;
            CoverPlaceholderIcon = VK.VKUI.VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28VoiceOutline);
        }

        public override string ToString() {
            return Type == AudioType.VoiceMessage ? Performer : $"{Performer} — {Title}";
        }
    }
}