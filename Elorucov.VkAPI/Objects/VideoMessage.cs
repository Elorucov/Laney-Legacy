using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class VideoMessage : Video {
        [JsonIgnore]
        public override string ObjectType { get { return "video_message"; } }

        [JsonProperty("shape_id")]
        public int ShapeId { get; set; }

        [JsonProperty("transcript")]
        public string Transcript { get; set; }
    }
}