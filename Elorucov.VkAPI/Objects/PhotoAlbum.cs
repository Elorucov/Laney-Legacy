using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class PhotoAlbum : Album {

        [JsonProperty("size")]
        public int Size { get; set; }
    }
}