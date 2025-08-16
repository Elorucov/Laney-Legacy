using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Elorucov.Laney.DataModels
{
    [DataContract]
    public enum LPBotCallbackActionType
    {
        [EnumMember(Value = "show_snackbar")]
        ShowSnackbar,

        [EnumMember(Value = "open_link")]
        OpenLink,

        [EnumMember(Value = "open_app")]
        OpenApp
    }

    public class LPBotCallbackAction
    {
        [JsonProperty("type")]
        public LPBotCallbackActionType Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("app_id")]
        public int AppId { get; set; }

        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }
    }

    public class LPBotCallback
    {
        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("peer_id")]
        public int PeerId { get; set; }

        [JsonProperty("event_id")]
        public string EventId { get; set; }

        [JsonProperty("action")]
        public LPBotCallbackAction Action { get; set; }
    }
}
