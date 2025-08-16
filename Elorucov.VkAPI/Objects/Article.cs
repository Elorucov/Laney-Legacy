using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class Article : AttachmentBase {
        [JsonProperty("owner_name")]
        public string OwnerName { get; set; }

        [JsonProperty("owner_photo")]
        public string OwnerPhoto { get; set; }

        [JsonProperty("photo")]
        public Photo Photo { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("view_url")]
        public string ViewUrl { get; set; }
    }
}