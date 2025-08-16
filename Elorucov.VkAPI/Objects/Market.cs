using Newtonsoft.Json;
using System;

namespace Elorucov.VkAPI.Objects {
    public class MarketPrice {

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class MarketCategory {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Market : AttachmentBase {
        [JsonIgnore]
        public override string ObjectType { get { return "market"; } }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("price")]
        public MarketPrice Price { get; set; }

        [JsonProperty("category")]
        public MarketCategory Category { get; set; }

        [JsonProperty("thumb_photo")]
        public string ThumbPhoto { get; set; }

        [JsonIgnore]
        public Uri ThumbPhotoUri { get { return new Uri(ThumbPhoto); } }

        [JsonProperty("date")]
        public int DateUnix { get; set; }

        [JsonIgnore]
        public DateTime Date { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("availability")]
        public int Availability { get; set; }
    }
}