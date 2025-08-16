using ELOR.VKAPILib.Objects;
using Newtonsoft.Json;

namespace Elorucov.Laney.VKAPIExecute.Objects
{
    public class AddVoteResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("poll")]
        public Poll Poll { get; set; }
    }
}