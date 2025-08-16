using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class StoreProduct {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("stickers")]
        public List<Sticker> Stickers { get; set; }

        [JsonProperty("previews")]
        public List<StickerImage> Previews { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class StockItem {
        [JsonProperty("product")]
        public StoreProduct Product { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("photo_140")]
        public string Photo { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }
    }
}