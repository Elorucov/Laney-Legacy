using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects.Upload {
    public class PhotoUploadResult {
        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("photo")]
        public string Photo { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_descr")]
        public string ErrorDescription { get; set; }
    }
}
