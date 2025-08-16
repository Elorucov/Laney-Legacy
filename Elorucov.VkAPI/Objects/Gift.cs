using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class Gift {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("thumb_256")]
        public string Thumb { get; set; }

        [JsonIgnore]
        public Uri ThumbUri { get { return new Uri(Thumb); } }
    }
}