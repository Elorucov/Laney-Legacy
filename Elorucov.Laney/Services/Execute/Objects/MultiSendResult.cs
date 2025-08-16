
using Newtonsoft.Json;

namespace Elorucov.Laney.Services.Execute.Objects {
    public class MultiSendResult {

        [JsonProperty("peer_id")]
        public int PeerId { get; set; }

        [JsonProperty("message_id")]
        public int MessageId { get; set; }
    }
}