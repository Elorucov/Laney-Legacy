using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elorucov.VkAPI.Objects {
    public class Widget {
        [JsonProperty("item")]
        public WidgetItem Item { get; private set; }
    }

    public class WidgetItem {
        [JsonProperty("type")]
        public string Type { get; private set; }

        [JsonProperty("payload")]
        public JObject Payload { get; private set; }
    }
}