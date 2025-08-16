using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elorucov.VkAPI.Objects {
    public class FeedList {
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("title")]
        public string Title { get; private set; }
    }

    public class WallPostCounter {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("can_post")]
        public bool CanPost { get; set; }

        [JsonProperty("can_view")]
        public bool CanView { get; set; }
    }

    public class WallPostLikes {
        [JsonProperty("can_like")]
        public bool CanLike { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("user_likes")]
        public bool UserLikes { get; set; }

        [JsonProperty("repost_disabled")]
        public bool RepostDisabled { get; set; }
    }

    public class WallPostCopyright {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    [DataContract]
    public enum PostType {
        [EnumMember(Value = "post")]
        Post,

        [EnumMember(Value = "post_ads")]
        Ads,

        [EnumMember(Value = "copy")]
        Copy,

        [EnumMember(Value = "reply")]
        Reply,

        [EnumMember(Value = "postpone")]
        Postpone,

        [EnumMember(Value = "suggest")]
        Suggest,
    }

    public class WallPostDonutPlaceholder {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class WallPostDonut {
        [JsonProperty("is_donut")]
        public bool IsDonut { get; set; }

        [JsonProperty("placeholder")]
        public WallPostDonutPlaceholder Placeholder { get; set; }
    }

    public class WallPostGeoPlace {
        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class WallPostGeo {
        [JsonProperty("coordinates")]
        public string Coordinates { get; set; }

        [JsonProperty("place")]
        public WallPostGeoPlace Place { get; set; }
    }

    public class WallPostHeaderSource {
        [JsonProperty("source_id")]
        public long SourceId { get; set; }
    }

    public class WallPostHeaderDescriptionText {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class WallPostHeaderDescription {
        [JsonProperty("text")]
        public WallPostHeaderDescriptionText Text { get; set; }
    }

    public class WallPostHeader {
        [JsonProperty("photo")]
        public WallPostHeaderSource Photo { get; set; }

        [JsonProperty("title")]
        public WallPostHeaderSource Title { get; set; }

        [JsonProperty("description")]
        public WallPostHeaderDescription Description { get; set; }

        [JsonProperty("date")]
        public int DateUnix { get; set; }

        [JsonIgnore]
        public DateTime Date { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }
    }

    public class WallPost : AttachmentBase {
        [JsonIgnore]
        public override string ObjectType { get { return "wall"; } }

        [JsonProperty("from_id")]
        public long FromId { get; set; }

        [JsonProperty("to_id")]
        public long ToId { get; set; }

        [JsonIgnore]
        public long OwnerOrToId { get { return OwnerId != 0 ? OwnerId : ToId; } } // #лучшееапивинтернете

        [JsonProperty("date")]
        public int DateUnix { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("post_type")]
        public string PostType { get; set; }

        [JsonIgnore]
        public DateTime Date { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("likes")]
        public WallPostLikes Likes { get; set; }

        [JsonProperty("comments")]
        public WallPostCounter Comments { get; set; }

        [JsonProperty("reposts")]
        public WallPostCounter Reposts { get; set; }

        [JsonProperty("views")]
        public WallPostCounter Views { get; set; }

        [JsonProperty("attachments")]
        public List<Attachment> Attachments { get; set; }

        [JsonProperty("signer_id")]
        public long SignerId { get; set; }

        [JsonProperty("copyright")]
        public WallPostCopyright Copyright { get; set; }

        [JsonProperty("marked_as_ads")]
        public bool MarkedAsAds { get; set; }

        [JsonProperty("donut")]
        public WallPostDonut Donut { get; set; }

        [JsonProperty("geo")]
        public WallPostGeo Geo { get; set; }

        [JsonProperty("copy_history")]
        public List<WallPost> CopyHistory { get; set; }

        [JsonProperty("textlive")]
        public Textlive Textlive { get; set; }

        [JsonProperty("header")]
        public WallPostHeader Header { get; set; }

        [JsonProperty("friends_only")]
        public bool FriendsOnly { get; set; }

        [JsonProperty("best_friends_only")]
        public bool BestFriendsOnly { get; set; }

        [JsonProperty("is_pinned")]
        public bool IsPinned { get; set; }

        [JsonProperty("zoom_text")]
        public bool ZoomText { get; set; }
    }

    public class WallReply : AttachmentBase {
        [JsonIgnore]
        public override string ObjectType { get { return "wall_reply"; } }

        [JsonProperty("from_id")]
        public long FromId { get; set; }

        [JsonProperty("post_id")]
        public long PostId { get; set; }

        [JsonProperty("date")]
        public int DateUnix { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonIgnore]
        public DateTime Date { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }
    }

    public class NewsfeedResponse : VKList<WallPost> {
        [JsonProperty("next_from")]
        public string NextFrom { get; set; }
    }

    public class WallPostResponse {
        [JsonProperty("post_id")]
        public int PostId { get; private set; }
    }
}
