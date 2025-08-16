using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class SituationalTheme {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonIgnore]
        public Uri Uri { get { if (!String.IsNullOrEmpty(Link)) { return new Uri(Link); } else { return null; } } }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("squared_cover_photo")]
        public Photo SquaredCoverPhoto { get; set; }
    }
}