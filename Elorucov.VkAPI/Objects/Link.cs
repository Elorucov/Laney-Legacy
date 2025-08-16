using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class LinkButtonAction {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { if (!String.IsNullOrEmpty(Url)) { return new Uri(Url); } else { return null; } } }
    }

    public class LinkButton {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("action")]
        public LinkButtonAction Action { get; set; }
    }

    public class Link {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { if (!String.IsNullOrEmpty(Url) && Uri.IsWellFormedUriString(Url, UriKind.Absolute)) { return new Uri(Url); } else { return null; } } }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("caption")]
        public string Caption { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("photo")]
        public Photo Photo { get; set; }

        [JsonProperty("button")]
        public LinkButton Button { get; set; }

        [JsonProperty("preview_page")]
        public string PreviewPage { get; set; }

        [JsonProperty("preview_url")]
        public string PreviewUrl { get; set; }

        [JsonProperty("image_src")]
        public string ImageSrc { get; set; }

        [JsonIgnore]
        public Uri PreviewUri {
            get {
                if (!String.IsNullOrEmpty(PreviewUrl)) { return new Uri(PreviewUrl); } else {
                    if (!String.IsNullOrEmpty(ImageSrc)) { return new Uri(ImageSrc); } else { return null; }
                }
            }
        }
    }

    public class DonutLinkDonors {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("friends_count")]
        public int FriendsCount { get; set; }

        [JsonProperty("friends")]
        public List<long> Friends { get; set; }
    }

    public class DonutLink {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { if (!String.IsNullOrEmpty(Url) && Uri.IsWellFormedUriString(Url, UriKind.Absolute)) { return new Uri(Url); } else { return null; } } }

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("donors")]
        public DonutLinkDonors Donors { get; set; }

        [JsonProperty("button")]
        public LinkButton Button { get; set; }

        [JsonProperty("action")]
        public LinkButtonAction Action { get; set; }
    }
}
