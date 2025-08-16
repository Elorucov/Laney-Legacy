using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class Graffiti : AttachmentBase {
        [JsonIgnore]
        public override string ObjectType { get { return "doc"; } }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("photo_586")] // For old wall graffities
        public string Photo586 { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonIgnore]
        public Uri Uri {
            get {
                if (!String.IsNullOrEmpty(Photo586)) return new Uri(Photo586);
                if (!String.IsNullOrEmpty(Url)) return new Uri(Url);
                return null;
            }
        }
    }
}