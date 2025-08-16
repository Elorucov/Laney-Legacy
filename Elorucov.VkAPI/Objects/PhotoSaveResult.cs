using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class PhotoSaveResult {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("pid")]
        public long PhotoId { get; set; }

        [JsonProperty("album_id")]
        public long AlbumId { get; set; }

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("access_key")]
        public string AccessKey { get; set; }

        [JsonProperty("sizes")]
        public List<PhotoSizes> Sizes { get; set; }
    }
}