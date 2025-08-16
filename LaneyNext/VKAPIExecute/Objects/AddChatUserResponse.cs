using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.Laney.VKAPIExecute.Objects
{
    public class AddChatUserResponse
    {
        [JsonProperty("success")]
        public List<string> Success { get; set; }

        [JsonProperty("failed")]
        public List<string> Failed { get; set; }
    }
}
