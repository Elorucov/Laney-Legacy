using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class PodcastCover {
        [JsonProperty("sizes")]
        public List<PhotoSizes> Sizes { get; set; }
    }

    public class PodcastInfo {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("plays")]
        public int Plays { get; set; }

        [JsonProperty("cover")]
        public PodcastCover Cover { get; set; } // Какой криворукий сотрудник ВК додумался впихнуть обложки как отдельный вложенный объект, блять?
    }

    public class Podcast : Audio {
        [JsonIgnore]
        public override string ObjectType { get { return "podcast"; } }

        [JsonProperty("podcast_info")]
        public PodcastInfo Info { get; set; }
    }
}