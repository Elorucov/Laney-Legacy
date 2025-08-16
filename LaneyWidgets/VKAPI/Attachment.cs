using Newtonsoft.Json;

namespace LaneyWidgets.VKAPI {

    internal class Attachment {

        [JsonProperty("type")]
        public string Type{ get; set; }
    }
}