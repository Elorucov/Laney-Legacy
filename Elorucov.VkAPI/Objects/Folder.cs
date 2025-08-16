using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class FolderCreatedResponse {
        [JsonProperty("folder_id")]
        public int FolderId { get; set; }
    }

    public class Folder {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("random_id")]
        public int RandomId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}