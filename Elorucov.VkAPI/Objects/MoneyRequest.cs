using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects {
    public class Amount {
        [JsonProperty("amount")]
        public int Number { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class MoneyRequest {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("from_id")]
        public long FromId { get; set; }

        [JsonProperty("to_id")]
        public long ToId { get; set; }

        [JsonProperty("init_url")]
        public string InitUrl { get; set; }

        [JsonProperty("amount")]
        public Amount Amount { get; set; }

        [JsonProperty("total_amount")]
        public Amount TotalAmount { get; set; }

        [JsonProperty("transferred_amount")]
        public Amount TransferredAmount { get; set; }

        [JsonProperty("held_amount")]
        public Amount HeldAmount { get; set; }

        [JsonProperty("user_is_owner")]
        public bool UserIsOwner { get; set; }

        [JsonProperty("user_sent")]
        public bool UserSent { get; set; }

        [JsonProperty("users_count")]
        public int UsersCount { get; set; }
    }

    public class MoneyTransfer {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("from_id")]
        public long FromId { get; set; }

        [JsonProperty("to_id")]
        public long ToId { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("amount")]
        public Amount Amount { get; set; }
    }
}