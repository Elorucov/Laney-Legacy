using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class Album : AttachmentBase {
        [JsonIgnore]
        public override string ObjectType { get { return "album"; } }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("thumb")]
        public Photo Thumb { get; set; }

        [JsonProperty("thumb_src")]
        public string ThumbSrc { get; set; }

        [JsonIgnore]
        public Uri ThumbUri => Uri.IsWellFormedUriString(ThumbSrc, UriKind.Absolute) ? new Uri(ThumbSrc) : null;
    }
}