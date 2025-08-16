using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class ChatPreview {
        [JsonProperty("admin_id")]
        public long AdminId { get; set; }

        [JsonProperty("members_count")]
        public int MembersCount { get; set; }

        [JsonProperty("members")]
        public List<long> Members { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("photo")]
        public Photo Photo { get; set; }

        [JsonProperty("local_id")]
        public long LocalId { get; set; }

        [JsonProperty("joined")]
        public bool Joined { get; set; }

        [JsonProperty("is_group_channel")]
        public bool IsGroupChannel { get; set; }

        [JsonProperty("is_nft")]
        public bool IsNFT { get; set; }

        [JsonProperty("chat_settings")]
        public ChatSettings ChatSettings { get; set; }

        [JsonProperty("button")]
        public LinkButton Button { get; set; }
    }

    public class ChatPreviewResponse {
        [JsonProperty("preview")]
        public ChatPreview Preview { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }
    }
}