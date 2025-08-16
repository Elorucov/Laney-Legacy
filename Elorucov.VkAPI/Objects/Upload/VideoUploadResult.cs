using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects.Upload {
    public class VideoUploadResult {
        [JsonProperty("video_id")]
        public int VideoId { get; set; }

        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("video_hash")]
        public string VideoHash { get; set; }
    }
}
