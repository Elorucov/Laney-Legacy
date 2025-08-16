using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class CreateChatResponse {
        [JsonProperty("chat_id")]
        public long ChatId { get; set; }

        [JsonProperty("peer_ids")]
        public List<long> PeerIds { get; set; }
    }
}