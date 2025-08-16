using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects.Auth {
    public class AnonymToken {
        [JsonProperty("expired_at")]
        public long ExpiresIn { get; private set; }

        [JsonProperty("token")]
        public string Token { get; private set; }
    }
}