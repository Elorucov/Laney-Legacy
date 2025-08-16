using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class ConversationAttachment {
        [JsonProperty("attachment")]
        public Attachment Attachment { get; set; }

        [JsonProperty("cmid")]
        public int MessageId { get; set; }

        [JsonProperty("from_id")]
        public long FromId { get; set; }
    }

    public class ConversationAttachmentsResponse : VKList<ConversationAttachment> {

        [JsonProperty("cmid_next_from")]
        public string NextFrom { get; set; }
    }
}