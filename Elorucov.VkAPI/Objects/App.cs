using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class App {

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("icon_278")]
        public string IconUrl { get; set; }

        [JsonIgnore]
        public Uri Icon { get { return new Uri(IconUrl); } }
    }

    public class MiniApp {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("app")]
        public App App { get; set; }
    }
}
