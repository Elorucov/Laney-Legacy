using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects.Upload {
    public class VkUploadServer {
        [JsonProperty("upload_url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { return !String.IsNullOrEmpty(Url) ? new Uri(Url) : null; } }
    }
}
