using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {

    public class VKResponse<T> {
        [JsonProperty("response")]
        public T Response { get; set; }

        [JsonProperty("error")]
        public VKError Error { get; set; }
    }

    public class VKResult {
        [JsonProperty("result")]
        public int Result { get; set; }
    }
}