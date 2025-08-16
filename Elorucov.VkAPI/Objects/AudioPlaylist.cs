using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class AudioPlaylist : AttachmentBase {
        [JsonIgnore]
        public override string ObjectType { get { return "audio_playlist"; } }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("photo")]
        public Photo Photo { get; set; }

        [JsonProperty("thumbs")]
        public List<Photo> Thumbs { get; set; }

        [JsonProperty("audios")]
        public List<Audio> Audios { get; set; }

        [JsonProperty("main_color")]
        public string MainColor { get; set; }
    }
}