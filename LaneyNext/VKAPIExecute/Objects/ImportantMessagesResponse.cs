using ELOR.VKAPILib.Objects;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.Laney.VKAPIExecute.Objects
{
    public class ConversationNames
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class ImportantMessagesResponse
    {
        [JsonProperty("messages")]
        public VKList<Message> Messages { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }

        [JsonProperty("conversations")]
        public List<ConversationNames> Conversations { get; set; }
    }
}
