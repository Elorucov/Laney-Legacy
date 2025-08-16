using ELOR.VKAPILib.Objects;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.Laney.VKAPIExecute.Objects
{
    public class StartSessionResponse
    {
        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("group")]
        public Group Group { get; set; }

        [JsonProperty("longpoll")]
        public LongPollServerInfo LongPoll { get; set; }

        [JsonProperty("stickers_count")]
        public int StickersCount { get; set; }

        [JsonProperty("stickers_keywords")]
        public List<StickerDictionary> StickersKeywords { get; set; }
    }
}
