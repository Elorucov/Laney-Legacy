using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elorucov.VkAPI.Objects {
    [DataContract]
    public enum DeactivationState {
        No,

        [EnumMember(Value = "deleted")]
        Deleted,

        [EnumMember(Value = "banned")]
        Banned
    }

    [DataContract]
    public enum Sex {
        No = 0,
        Female = 1,
        Male = 2
    }

    [DataContract]
    public enum FriendStatus {
        None = 0,
        RequestSent = 1,
        InboundRequest = 2,
        IsFriend = 3
    }

    [DataContract]
    public enum UserOccupationType {
        [EnumMember(Value = "work")]
        Work,

        [EnumMember(Value = "school")]
        School,

        [EnumMember(Value = "university")]
        University,
    }

    public class UserLastSeen {
        [JsonProperty("time")]
        public long TimeUnix { get; set; }

        [JsonIgnore]
        public DateTime Time { get { return DateTimeOffset.FromUnixTimeSeconds(TimeUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("platform")]
        public int Platform { get; set; }
    }

    public class UserCountry {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class UserCareer {
        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("company")]
        public string Company { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }
    }

    public class UserOccupation {
        [JsonProperty("type")]
        public UserOccupationType Type { get; set; }

        [JsonProperty("id")]
        public double Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    [DataContract]
    public enum UserOnlineStatus {
        Unknown,

        [EnumMember(Value = "not_show")]
        NotShow,

        [EnumMember(Value = "recently")]
        Recently,

        [EnumMember(Value = "last_week")]
        LastWeek,

        [EnumMember(Value = "last_month")]
        LastMonth,

        [EnumMember(Value = "long_ago")]
        LongAgo,
    }

    public class UserOnlineInfo {
        [JsonProperty("visible")]
        public bool Visible { get; set; }

        [JsonProperty("is_online")]
        public bool isOnline { get; set; }

        [JsonProperty("app_id")]
        public long AppId { get; set; }

        [JsonProperty("is_mobile")]
        public bool IsMobile { get; set; }

        [JsonProperty("last_seen")]
        public long LastSeenUnix { get; set; }

        [JsonIgnore]
        public DateTime LastSeen { get { return DateTimeOffset.FromUnixTimeSeconds(LastSeenUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("status")]
        public UserOnlineStatus Status { get; set; }
    }

    public class User {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("first_name_gen")]
        public string FirstNameGen { get; set; }

        [JsonProperty("first_name_dat")]
        public string FirstNameDat { get; set; }

        [JsonProperty("first_name_acc")]
        public string FirstNameAcc { get; set; }

        [JsonProperty("first_name_ins")]
        public string FirstNameIns { get; set; }

        [JsonProperty("first_name_abl")]
        public string FirstNameAbl { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("last_name_gen")]
        public string LastNameGen { get; set; }

        [JsonProperty("last_name_dat")]
        public string LastNameDat { get; set; }

        [JsonProperty("last_name_acc")]
        public string LastNameAcc { get; set; }

        [JsonProperty("last_name_ins")]
        public string LastNameIns { get; set; }

        [JsonProperty("last_name_abl")]
        public string LastNameAbl { get; set; }

        [JsonIgnore]
        public string FullName { get { return String.Join(" ", FirstName, LastName).Trim(); } }

        [JsonProperty("nickname")]
        public string NickName { get; set; }

        [JsonProperty("is_closed")]
        public bool IsClosed { get; set; }

        [JsonProperty("can_access_closed")]
        public bool CanAccessClosed { get; set; }

        [JsonProperty("deactivated")]
        public DeactivationState Deactivated { get; set; }

        [JsonProperty("friend_status")]
        public FriendStatus FriendStatus { get; set; }

        [JsonProperty("bdate")]
        public string BirthDate { get; set; }

        [JsonProperty("city")]
        public UserCountry City { get; set; }

        [JsonProperty("country")]
        public UserCountry Country { get; set; }

        [JsonProperty("career")]
        public List<UserCareer> Career { get; set; }

        [JsonProperty("occupation")]
        public UserOccupation Occupation { get; set; }

        [JsonProperty("blacklisted")]
        public int Blacklisted { get; set; }

        [JsonProperty("blacklisted_by_me")]
        public int BlacklistedByMe { get; set; }

        [JsonProperty("can_send_friend_request")]
        public int CanSendFriendRequest { get; set; }

        [JsonProperty("can_write_private_message")]
        public int CanWritePrivateMessage { get; set; }

        [JsonProperty("last_seen")]
        public UserLastSeen LastSeen { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("sex")]
        public Sex Sex { get; set; }

        [JsonProperty("has_photo")]
        public bool HasPhoto { get; set; }

        [JsonProperty("photo_50")]
        public string Photo50 { get; set; }

        [JsonProperty("photo_100")]
        public string Photo100 { get; set; }

        [JsonProperty("photo_200")]
        public string Photo200 { get; set; }

        [JsonIgnore]
        public Uri Photo {
            get {
                if (!HasPhoto) return null;
                if (Uri.IsWellFormedUriString(Photo200, UriKind.Absolute)) return new Uri(Photo200);
                if (Uri.IsWellFormedUriString(Photo100, UriKind.Absolute)) return new Uri(Photo100);
                if (Uri.IsWellFormedUriString(Photo50, UriKind.Absolute)) return new Uri(Photo50);
                return null;
            }
        }

        [JsonProperty("followers_count")]
        public long Followers { get; set; }

        [JsonProperty("online")]
        public int Online { get; set; }

        [JsonProperty("online_mobile")]
        public int OnlineMobile { get; set; }

        [JsonProperty("online_app")]
        public int OnlineAppId { get; set; }

        [JsonProperty("online_info")]
        public UserOnlineInfo OnlineInfo { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("site")]
        public string Site { get; set; }

        [JsonProperty("verified")]
        public int Verified { get; set; }

        [JsonProperty("can_post")]
        public bool CanPost { get; set; }
    }
}