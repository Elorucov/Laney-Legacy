using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using Windows.UI.Xaml;

namespace Elorucov.Laney.DataModels
{
    public enum AudioType
    {
        Audio, Podcast, VoiceMessage
    }

    public class AudioPlayerItem
    {
        public AudioType Type { get; private set; }
        public int Id { get; private set; }
        public string Title { get; private set; }
        public string Performer { get; private set; }
        public TimeSpan Duration { get; private set; }
        public Uri Source { get; private set; }
        public DataTemplate CoverPlaceholderIcon { get; private set; }
        public Uri CoverUrl { get; private set; }

        public AudioPlayerItem(Audio audio)
        {
            Type = AudioType.Audio;
            Id = audio.Id;
            Title = audio.Title;
            if (!String.IsNullOrEmpty(audio.Subtitle)) Title += $" ({audio.Subtitle})";
            Performer = audio.Artist;
            Duration = audio.DurationTime;
            Source = audio.Uri;
            CoverPlaceholderIcon = VK.VKUI.VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28SongOutline);
        }

        public AudioPlayerItem(Podcast podcast)
        {
            Type = AudioType.Podcast;
            Id = podcast.Id;
            Title = podcast.Title;
            Performer = podcast.Artist;
            Duration = podcast.DurationTime;
            Source = podcast.Uri;
            CoverPlaceholderIcon = VK.VKUI.VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28PodcastOutline);
            CoverUrl = podcast.Info.Cover?.Sizes[0].Uri;
        }

        public AudioPlayerItem(AudioMessage audioMessage, string ownerName)
        {
            Type = AudioType.VoiceMessage;
            Id = audioMessage.Id;
            Title = Locale.Get("msg_attachment_audiomsg_nom").Capitalize();
            Performer = ownerName;
            Duration = audioMessage.DurationTime;
            Source = audioMessage.Uri;
            CoverPlaceholderIcon = VK.VKUI.VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28VoiceOutline);
        }

        public override string ToString()
        {
            return Type == AudioType.VoiceMessage ? Performer : $"{Performer} — {Title}";
        }
    }
}
