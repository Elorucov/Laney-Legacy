using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects.Upload {
    public class UploadError {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_code")]
        public int ErrorCode { get; set; }

        [JsonProperty("error_descr")]
        public string ErrorDescription { get; set; }
    }
}
