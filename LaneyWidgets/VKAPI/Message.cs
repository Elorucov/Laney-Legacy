using Newtonsoft.Json;

namespace LaneyWidgets.VKAPI {

    internal class Action {

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("member_id")]
        public long MemberId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("old_text")]
        public string OldText { get; set; }

        [JsonProperty("conversation_message_id")]
        public int ConversationMessageId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }
    }

    internal class Message {

        [JsonIgnore]
        public DateTime DateTime { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }

        [JsonIgnore]
        public DateTime UpdateTime { get { return DateTimeOffset.FromUnixTimeSeconds(UpdateTimeUnix).DateTime.ToLocalTime(); } }

        //

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("conversation_message_id")]
        public int ConversationMessageId { get; set; }

        [JsonProperty("date")]
        public long DateUnix { get; set; }

        [JsonProperty("update_time")]
        public long UpdateTimeUnix { get; set; }

        [JsonProperty("peer_id")]
        public long PeerId { get; set; }

        [JsonProperty("from_id")]
        public long FromId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("attachments")]
        public List<Attachment> Attachments { get; set; }

        [JsonProperty("fwd_messages")]
        public List<Message> ForwardedMessages { get; set; }

        [JsonProperty("action")]
        public Action Action { get; set; }

        [JsonProperty("expire_ttl")]
        public int ExpireTTL { get; set; }

        [JsonProperty("ttl")]
        public int TTL { get; set; }

        [JsonProperty("is_expired")]
        public bool IsExpired { get; set; }

        [JsonProperty("is_silent")]
        public bool IsSilent { get; set; }
    }
}
