using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class NotificationSettings {
        [JsonProperty("msg")]
        public List<string> Message { get; set; }

        [JsonProperty("chat")]
        public List<string> Chat { get; set; }

        [JsonProperty("chat_mention")]
        public List<string> ChatMention { get; set; }
    }
}