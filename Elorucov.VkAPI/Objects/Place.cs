using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elorucov.VkAPI.Objects {
    public class PlaceCategory {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class Place {

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("category_object")]
        public PlaceCategory Category { get; set; }
    }

    public class PlaceSearchResponse {

        [JsonProperty("distance")]
        public int Distance { get; set; }

        [JsonProperty("place")]
        public Place Place { get; set; }
    }
}