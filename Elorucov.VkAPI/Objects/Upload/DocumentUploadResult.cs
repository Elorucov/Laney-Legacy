using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects.Upload {
    public class DocumentUploadResult {
        [JsonProperty("file")]
        public string File { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_descr")]
        public string ErrorDescription { get; set; }
    }
}
