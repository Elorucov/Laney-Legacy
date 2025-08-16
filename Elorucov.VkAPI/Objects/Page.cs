using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class WikiPage {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("view_url")]
        public string ViewUrl { get; set; }
    }
}