using Newtonsoft.Json;

namespace LaneyWidgets.VKAPI {
    public enum Sex {
        Undefined = 0,
        Female = 1,
        Male = 2
    }

    //public enum UserOnlineStatus {
    //    Unknown,

    //    [EnumMember(Value = "not_show")]
    //    NotShow,

    //    [EnumMember(Value = "recently")]
    //    Recently,

    //    [EnumMember(Value = "last_week")]
    //    LastWeek,

    //    [EnumMember(Value = "last_month")]
    //    LastMonth,

    //    [EnumMember(Value = "long_ago")]
    //    LongAgo,
    //}

    internal class UserLastSeen {

        [JsonProperty("time")]
        public long TimeUnix { get; set; }

        [JsonIgnore]
        public DateTime Time { get { return DateTimeOffset.FromUnixTimeSeconds(TimeUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("platform")]
        public int Platform { get; set; }
    }

    internal class UserOnlineInfo {

        [JsonProperty("visible")]
        public bool Visible { get; set; }

        [JsonProperty("is_online")]
        public bool IsOnline { get; set; }

        [JsonProperty("app_id")]
        public int AppId { get; set; }

        [JsonProperty("app_name")]
        public string AppName { get; set; }

        [JsonProperty("is_mobile")]
        public bool IsMobile { get; set; }

        [JsonProperty("last_seen")]
        public long LastSeenUnix { get; set; }

        [JsonIgnore]
        public DateTime LastSeen { get { return DateTimeOffset.FromUnixTimeSeconds(LastSeenUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    internal class User {

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonIgnore]
        public string FullName { get { return $"{FirstName} {LastName}"; } }

        [JsonProperty("sex")]
        public Sex Sex { get; set; }

        [JsonProperty("photo_100")]
        public string Photo100 { get; set; }

        [JsonIgnore]
        public Uri Photo {
            get {
                if (Uri.IsWellFormedUriString(Photo100, UriKind.Absolute)) return new Uri(Photo100);
                return new Uri("https://vk.com/images/camera_200.png");
            }
        }

        [JsonProperty("online_info")]
        public UserOnlineInfo OnlineInfo { get; set; }

        [JsonProperty("verified")]
        public int Verified { get; set; }
    }
}