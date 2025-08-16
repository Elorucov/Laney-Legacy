using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class AudioAlbumThumb {
        [JsonProperty("photo_135")]
        public string Photo135 { get; set; }
    }

    public class AudioAlbum {
        [JsonProperty("thumb")]
        public AudioAlbumThumb Thumb { get; set; }
    }

    public class Audio : AttachmentBase {
        [JsonIgnore]
        public override string ObjectType { get { return "audio"; } }

        [JsonProperty("album")]
        public AudioAlbum Album { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonIgnore]
        public TimeSpan DurationTime { get { return TimeSpan.FromSeconds(Duration); } }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }

        [JsonIgnore]
        public string FullSongName { get { return String.IsNullOrEmpty(Subtitle) ? Title : $"{Title} ({Subtitle})"; } }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { return String.IsNullOrEmpty(Url) ? null : new Uri(Url); } }

        [JsonProperty("content_restricted")]
        public int ContentRestricted { get; set; }

        [JsonProperty("date")]
        public int DateUnix { get; set; }

        [JsonIgnore]
        public DateTime Date { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("thumb")]
        public AudioAlbumThumb Thumb { get; set; }
    }
}