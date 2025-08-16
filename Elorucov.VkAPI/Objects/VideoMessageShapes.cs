using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {

    public class VideoMessageShape {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("raw_path")]
        public string RawPath { get; set; }
    }

    public class VideoMessageShapesResponse {
        [JsonProperty("shape_orders")]
        public List<int> ShapeOrders { get; set; }

        [JsonProperty("shapes")]
        public List<VideoMessageShape> Shapes { get; set; }
    }
}
