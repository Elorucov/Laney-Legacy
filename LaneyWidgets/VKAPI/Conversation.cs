using Newtonsoft.Json;

namespace LaneyWidgets.VKAPI {

    internal class Peer {

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("local_id")]
        public long LocalId { get; set; }
    }

    internal class SortId {

        [JsonProperty("major_id")]
        public int MajorId { get; set; }

        [JsonProperty("minor_id")]
        public int MinorId { get; set; }
    }

    internal class Conversation {

        [JsonProperty("peer")]
        public Peer Peer { get; set; }

        [JsonProperty("unread_count")]
        public int UnreadCount { get; set; }

        [JsonProperty("is_marked_unread")]
        public bool IsMarkedUnread { get; set; }

        [JsonProperty("sort_id")]
        public SortId SortId { get; set; }

        [JsonProperty("unread_reactions")]
        public List<int> UnreadReactions { get; set; }
    }
}