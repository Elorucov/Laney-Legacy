using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class ReactedPeer {
        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("reaction_id")]
        public int ReactionId { get; set; }
    }

    public class GetReactedPeersResponse : VKList {
        [JsonProperty("cmid")]
        public int CMID { get; set; } // Нужет для execute.getReactedPeersMulti

        [JsonProperty("counters")]
        public List<MessageReaction> Counters { get; set; }

        [JsonProperty("reactions")]
        public List<ReactedPeer> Reactions { get; set; }
    }
}
