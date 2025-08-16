using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class VKList<T> {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; } // special for messages.whoReadMessage

        [JsonProperty("items")]
        public List<T> Items { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }

        [JsonProperty("contacts")]
        public List<Contact> Contacts { get; set; }
    }

    public class VKList {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }

        [JsonProperty("contacts")]
        public List<Contact> Contacts { get; set; }
    }

    public class ImportantMessagesResponse : VKList {
        [JsonProperty("conversations")]
        public List<Conversation> Conversations { get; set; }

        [JsonProperty("messages")]
        public VKList<Message> Messages { get; set; }
    }

    public class WallPostPreviewResponse : VKList {
        [JsonProperty("post")]
        public WallPost Post { get; set; }
    }
}