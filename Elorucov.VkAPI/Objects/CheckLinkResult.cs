using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace Elorucov.VkAPI.Objects {

    [DataContract]
    public enum CheckLinkStatus {
        [EnumMember(Value = "not_banned")]
        NotBanned,

        [EnumMember(Value = "banned")]
        Banned,

        [EnumMember(Value = "processing")]
        Processing
    }
    public class CheckLinkResult {
        [JsonProperty("status")]
        public CheckLinkStatus Status { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonIgnore]
        public Uri Uri { get { return new Uri(Link); } }
    }
}