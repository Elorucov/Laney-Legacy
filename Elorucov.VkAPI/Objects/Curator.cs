using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class Curator {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { if (!String.IsNullOrEmpty(Url)) { return new Uri(Url); } else { return null; } } }

        [JsonProperty("photo")]
        public List<PhotoSizes> Photo { get; set; }
    }
}