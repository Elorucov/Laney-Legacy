using Newtonsoft.Json;

namespace Elorucov.Laney.VKAPIExecute.Objects
{
    public class LeaveAndDeleteChatResult
    {
        [JsonProperty("remove_result")]
        public bool RemoveResult { get; set; }

        [JsonProperty("last_deleted_id")]
        public int LastDeletedId { get; set; }
    }
}