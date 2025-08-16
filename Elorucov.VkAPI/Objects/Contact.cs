using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class Contact {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("calls_id")]
        public string CallsId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("photo_50")]
        public string Photo50 { get; set; }

        [JsonIgnore]
        public Uri Photo { get { return !String.IsNullOrEmpty(Photo50) ? new Uri(Photo50) : null; } }

        [JsonProperty("can_write")]
        public bool CanWrite { get; set; }
    }
}