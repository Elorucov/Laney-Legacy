using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class Textlive {
        [JsonProperty("attach_url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { if (!String.IsNullOrEmpty(Url)) { return new Uri(Url); } else { return null; } } }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("cover_photo")]
        public Photo CoverPhoto { get; set; }
    }
}