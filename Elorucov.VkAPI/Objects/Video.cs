using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Elorucov.VkAPI.Objects {
    public class VideoFiles {
        [JsonProperty("external")]
        public string External { get; set; }

        [JsonProperty("hls")]
        public string HLS { get; set; }

        [JsonProperty("hls_ondemand")]
        public string HLSOndemand { get; set; }

        [JsonProperty("mp4_144")]
        public string MP4p144 { get; set; }

        [JsonProperty("mp4_240")]
        public string MP4p240 { get; set; }

        [JsonProperty("mp4_360")]
        public string MP4p360 { get; set; }

        [JsonProperty("mp4_480")]
        public string MP4p480 { get; set; }

        [JsonProperty("mp4_720")]
        public string MP4p720 { get; set; }

        [JsonProperty("mp4_1080")]
        public string MP4p1080 { get; set; }
    }

    public class VideoRestrictionButton {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }
    }

    public class VideoRestriction {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("button")]
        public VideoRestrictionButton Button { get; set; }

        [JsonProperty("blur")]
        public bool Blur { get; set; }

        [JsonProperty("can_play")]
        public bool CanPlay { get; set; }

        [JsonProperty("can_preview")]
        public bool CanPreview { get; set; }
    }

    public class Video : AttachmentBase, IPreview {
        [JsonIgnore]
        public override string ObjectType { get { return "video"; } }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("is_private")]
        public bool IsPrivate { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonIgnore]
        public TimeSpan DurationTime { get { return TimeSpan.FromSeconds(Duration); } }

        [JsonProperty("image")]
        public List<PhotoSizes> Image { get; set; }

        [JsonIgnore]
        public Uri Photo { get { return GetPreviewWithoutPadding()?.Uri; } } // Deprecated

        [JsonIgnore]
        public Uri PreviewImageUri { get { return GetPreviewWithoutPadding()?.Uri; } }

        [JsonIgnore]
        public Size PreviewImageSize { get { return SafeGetSize(); } }

        [JsonProperty("date")]
        public int DateUnix { get; set; }

        [JsonIgnore]
        public DateTime Date { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("adding_date")]
        public int AddingDateUnix { get; set; }

        [JsonIgnore]
        public DateTime AddingDate { get { return DateTimeOffset.FromUnixTimeSeconds(AddingDateUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("files")]
        public VideoFiles Files { get; set; }

        [JsonProperty("player")]
        public string Player { get; set; }

        [JsonIgnore]
        public Uri PlayerUri { get { return new Uri(Player); } }

        [JsonProperty("first_frame")]
        public List<PhotoSizes> FirstFrame { get; set; }

        [JsonIgnore]
        public PhotoSizes FirstFrameForStory { get { return GetFirstFrame(248); } }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("processing")]
        public int Processing { get; set; }

        [JsonProperty("live")]
        public int Live { get; set; }

        [JsonProperty("upcoming")]
        public int Upcoming { get; set; }

        [JsonProperty("views")]
        public int Views { get; set; }

        [JsonProperty("restriction")]
        public VideoRestriction Restriction { get; set; }

        private PhotoSizes GetFirstFrame(double maxWidth) {
            if (FirstFrame == null && FirstFrame.Count == 0) return null;
            PhotoSizes cps = null;
            foreach (PhotoSizes ps in FirstFrame) {
                if (cps == null) cps = ps;
                if (cps.Width > maxWidth) return cps;
                if (cps.Width < ps.Width) cps = ps;
            }
            return cps;
        }

        private PhotoSizes GetPreviewWithoutPadding() {
            if (Image == null) return null;
            if (Image.Count > 1) {
                return Image[Image.Count - 2];
            }
            return null;
        }

        private Size SafeGetSize() {
            var p = GetPreviewWithoutPadding();
            if (p != null) return p.Size;
            return new Size(3, 2);
        }

        public override string ToString() {
            string ak = String.IsNullOrEmpty(AccessKey) ? "" : $"_{AccessKey}";
            return $"video{OwnerId}_{Id}{ak}";
        }
    }
}