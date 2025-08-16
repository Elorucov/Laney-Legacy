﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elorucov.VkAPI.Objects {
    [DataContract]
    public enum GroupState {
        Open = 0,
        Closed = 1,
        Private = 2
    }

    [DataContract]
    public enum AdminLevel {
        Moderator = 1,
        Editor = 2,
        Administrator = 3
    }

    [DataContract]
    public enum GroupType {
        [EnumMember(Value = "group")]
        Group,

        [EnumMember(Value = "page")]
        Page,

        [EnumMember(Value = "event")]
        Event
    }

    public class GroupCoverImage {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { return new Uri(Url); } }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class GroupCover {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("images")]
        public List<GroupCoverImage> Images { get; set; }
    }

    public class Donut {
        [JsonProperty("is_don")]
        public bool IsDon { get; set; }
    }

    public class Group {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("is_closed")]
        public GroupState State { get; set; }

        [JsonProperty("deactivated")]
        public DeactivationState Deactivated { get; set; }

        [JsonProperty("is_admin")]
        public bool IsAdmin { get; set; }

        [JsonProperty("verified")]
        public int Verified { get; set; }

        // admin level

        [JsonProperty("is_member")]
        public bool IsMember { get; set; }

        [JsonProperty("type")]
        public GroupType Type { get; set; }

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
                if (Uri.IsWellFormedUriString(Photo200, UriKind.Absolute)) return new Uri(Photo200);
                if (Uri.IsWellFormedUriString(Photo100, UriKind.Absolute)) return new Uri(Photo100);
                if (Uri.IsWellFormedUriString(Photo50, UriKind.Absolute)) return new Uri(Photo50);
                return new Uri("https://vk.com/images/camera_200.png");
            }
        }

        [JsonProperty("activity")]
        public string Activity { get; set; }

        [JsonProperty("can_message")]
        public bool CanMessage { get; set; }

        [JsonProperty("can_post")]
        public bool CanPost { get; set; }

        [JsonProperty("can_suggest")]
        public bool CanSuggest { get; set; }

        [JsonProperty("donut")]
        public Donut Donut { get; set; }

        [JsonProperty("city")]
        public UserCountry City { get; set; }

        [JsonProperty("country")]
        public UserCountry Country { get; set; }

        [JsonProperty("cover")]
        public GroupCover Cover { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("members_count")]
        public long Members { get; set; }

        [JsonProperty("wall")]
        public int Wall { get; set; }

        [JsonProperty("site")]
        public string Site { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    // Event object in messages attachments
    public class Event {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("friends")]
        public List<long> Friends { get; set; }

        [JsonProperty("button_text")]
        public string ButtonText { get; set; }
    }
}