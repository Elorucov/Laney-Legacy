using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Elorucov.VkAPI.Objects {
    public class PhotoSizes {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; }

        [JsonIgnore]
        public Uri Uri { get { return GetUri(); } }

        [JsonProperty("width")]
        public double Width { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }

        [JsonProperty("with_padding")]
        public bool WithPadding { get; set; }

        [JsonIgnore]
        public Size Size { get { return new Size(Width, Height); } }

        private Uri GetUri() {
            if (String.IsNullOrEmpty(Url) && String.IsNullOrEmpty(Src)) return null; // ВК. 👍🏻
            return String.IsNullOrEmpty(Url) ? new Uri(Src) : new Uri(Url);
        }

        public override string ToString() => $"{Type}:{Width}x{Height}";
    }

    public class Photo : AttachmentBase, IPreview {
        [JsonIgnore]
        public override string ObjectType { get { return "photo"; } }

        [JsonProperty("album_id")]
        public long AlbumId { get; set; }

        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("date")]
        public int DateUnix { get; set; }

        [JsonIgnore]
        public DateTime Date { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("sizes")]
        public List<PhotoSizes> Sizes { get; set; }

        [JsonIgnore]
        public PhotoSizes MaximalSizedPhoto { get { return GetMaximalSizedPhoto(); } }

        [JsonIgnore]
        public PhotoSizes MinimalSizedPhoto { get { return GetMinimalSizedPhoto(); } }

        [JsonProperty("width")]
        public double Width { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }

        [JsonProperty("photo_50")]
        public string Photo50Url { get; set; }

        [JsonIgnore]
        public Uri Photo50 { get { return new Uri(Photo50Url); } }

        [JsonProperty("photo_100")]
        public string Photo100Url { get; set; }

        [JsonIgnore]
        public Uri Photo100 { get { return new Uri(Photo100Url); } }

        [JsonProperty("photo_200")]
        public string Photo200Url { get; set; }

        [JsonIgnore]
        public Uri Photo200 { get { return new Uri(Photo200Url); } }

        [JsonProperty("photo_300")]
        public string Photo300Url { get; set; } // For audio playlist

        [JsonIgnore]
        public Uri Photo300 { get { return new Uri(Photo300Url); } }

        [JsonIgnore]
        public Uri PreviewImageUri { get { return GetSizedPhotoForThumbnail()?.Uri; } }

        [JsonIgnore]
        public Size PreviewImageSize { get { return GetSizedPhotoForThumbnail().Size; } }

        //

        private PhotoSizes GetMaximalSizedPhoto() {
            if (Sizes == null || Sizes.Count == 0) return new PhotoSizes {
                Width = 0, Height = 0
            };
            PhotoSizes p = null;
            long max = 0;
            foreach (PhotoSizes s in Sizes) {
                if (s.Width == 0 && s.Height == 0) {
                    p = Sizes.Last();
                } else {
                    if (s.Width * s.Height > max) {
                        max = (long)(s.Width * s.Height);
                        p = s;
                    }
                }
            }
            return p;
        }

        private PhotoSizes GetMinimalSizedPhoto() {
            if (Sizes == null || Sizes.Count == 0) return new PhotoSizes {
                Width = 0, Height = 0
            };
            PhotoSizes ps = null;
            foreach (PhotoSizes s in Sizes) {
                switch (s.Type) {
                    case "m": ps = s; break;
                    case "s": ps = s; break;
                }
            }
            return ps;
        }

        private PhotoSizes cachedPreviewSize;
        private PhotoSizes GetSizedPhotoForThumbnail() {
            if (cachedPreviewSize != null) return cachedPreviewSize;

            if (Sizes == null || Sizes.Count == 0) return new PhotoSizes {
                Width = 0, Height = 0
            };
            PhotoSizes size = new PhotoSizes {
                Width = 0, Height = 0
            };
            var a = Sizes.Where(ps => ps.Type == "s" || ps.Type == "m" || ps.Type == "x"
                                   || ps.Type == "y" || ps.Type == "z" || ps.Type == "o").OrderBy(o => o.Width);
            foreach (var b in a) {
                if (b.Width == 0 && b.Height == 0) return b;
                if (size == null || size.Width < b.Width) size = b;
                if (size.Width >= 600) break;
            }
            cachedPreviewSize = size;
            return size;
        }

        public override string ToString() {
            string ak = String.IsNullOrEmpty(AccessKey) ? "" : $"_{AccessKey}";
            return $"photo{OwnerId}_{Id}{ak}";
        }
    }
}