using Newtonsoft.Json;

namespace Elorucov.Laney.VKAPIExecute.Objects
{
    public class DeleteConvAndDenyGroupResult
    {
        [JsonProperty("denied")]
        public bool Denied { get; set; }

        [JsonProperty("last_deleted_id")]
        public int LastDeletedId { get; set; }
    }
}