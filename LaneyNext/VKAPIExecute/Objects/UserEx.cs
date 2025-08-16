using ELOR.VKAPILib.Objects;
using Newtonsoft.Json;

namespace Elorucov.Laney.VKAPIExecute.Objects
{
    public class UserOnlineInfoEx : UserOnlineInfo
    {
        [JsonProperty("app_name")]
        public string AppName { get; set; }
    }

    public class UserEx : User
    {
        [JsonProperty("live_in")]
        public string LiveIn { get; set; }

        [JsonProperty("current_career")]
        public UserCareer CurrentCareer { get; set; }

        [JsonProperty("current_education")]
        public string CurrentEducation { get; set; }

        [JsonProperty("online_info")]
        public UserOnlineInfoEx OnlineInfo { get; set; }

        [JsonProperty("unavailable_reason")]
        public int UnavailableReason { get; set; }
    }
}
