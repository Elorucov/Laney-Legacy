using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class Note : AttachmentBase {
        [JsonIgnore]
        public override string ObjectType { get { return "note"; } }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("view_url")]
        public string ViewUrl { get; set; }
    }
}