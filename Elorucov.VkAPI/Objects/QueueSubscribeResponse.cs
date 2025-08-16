using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class Queue {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("timestamp")]
        public int Timestamp { get; set; }
    }

    public class QueueSubscribeResponse {
        [JsonProperty("base_url")]
        public string BaseUrl { get; set; }

        [JsonProperty("queues")]
        public List<Queue> Queues { get; set; }
    }

    // Base response from queue server

    public class QueueResponse {
        [JsonProperty("ts")]
        public string Timestamp { get; set; }

        [JsonProperty("events")]
        public List<QueueEvent> Events { get; set; }
    }

    public class QueueEvent {
        [JsonProperty("entity_type")]
        public string EntityType { get; set; }

        [JsonProperty("entity_id")]
        public int EntityId { get; set; }

        [JsonProperty("data")]
        public JObject Data { get; set; }
    }

    // Response from queue server for "online"

    public class OnlineQueueEvent {
        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("online")]
        public bool Online { get; set; }

        [JsonProperty("platform")]
        public int Platform { get; set; }

        [JsonProperty("app_id")]
        public long AppId { get; set; }

        [JsonProperty("last_seen")]
        public int LastSeenUnix { get; set; }

        [JsonIgnore]
        public DateTime LastSeen { get { return DateTimeOffset.FromUnixTimeSeconds(LastSeenUnix).DateTime.ToLocalTime(); } }
    }
}