using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class LongPollHistoryResponse {
        [JsonProperty("history")]
        public List<object[]> History { get; set; }

        [JsonProperty("messages")]
        public VKList<Message> Messages { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }

        [JsonProperty("contacts")]
        public List<Contact> Contacts { get; set; }

        [JsonProperty("new_pts")]
        public int NewPTS { get; set; }

        [JsonProperty("more")]
        public bool More { get; set; }

        [JsonProperty("credentials")]
        public LongPollServerInfo Credentials { get; set; }
    }
}