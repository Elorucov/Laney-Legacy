using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class ReactionAssetLinks {
        [JsonProperty("big_animation")]
        public string BigAnimation { get; private set; }

        [JsonProperty("small_animation")]
        public string SmallAnimation { get; private set; }

        [JsonProperty("static")]
        public string Static { get; private set; }
    }

    public class ReactionAsset {
        [JsonProperty("reaction_id")]
        public int ReactionId { get; private set; }

        [JsonProperty("links")]
        public ReactionAssetLinks Links { get; private set; }
    }
}
