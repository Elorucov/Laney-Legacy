using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects.Upload {
    public class PhotoUploadServer : VkUploadServer {
        [JsonProperty("album_id")]
        public int AlbumId { get; set; }

        [JsonProperty("user_id")]
        public int UserId { get; set; }
    }
}
