using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace Elorucov.VkAPI.Objects {
    [DataContract]
    public enum AttachmentType {
        Unknown,

        [EnumMember(Value = "photo")]
        Photo,

        [EnumMember(Value = "album")]
        Album,

        [EnumMember(Value = "video")]
        Video,

        [EnumMember(Value = "video_message")]
        VideoMessage,

        [EnumMember(Value = "audio")]
        Audio,

        [EnumMember(Value = "audio_message")]
        AudioMessage,

        [EnumMember(Value = "podcast")]
        Podcast,

        [EnumMember(Value = "doc")]
        Document,

        [EnumMember(Value = "graffiti")]
        Graffiti,

        [EnumMember(Value = "link")]
        Link,

        [EnumMember(Value = "donut_link")]
        DonutLink,

        [EnumMember(Value = "poll")]
        Poll,

        [EnumMember(Value = "page")]
        Page,

        [EnumMember(Value = "market")]
        Market,

        [EnumMember(Value = "wall")]
        Wall,

        [EnumMember(Value = "wall_reply")]
        WallReply,

        [EnumMember(Value = "sticker")]
        Sticker,

        [EnumMember(Value = "story")]
        Story,

        [EnumMember(Value = "gift")]
        Gift,

        [EnumMember(Value = "call")]
        Call,

        [EnumMember(Value = "group_call_in_progress")]
        GroupCallInProgress,

        [EnumMember(Value = "event")]
        Event,

        [EnumMember(Value = "curator")]
        Curator,

        [EnumMember(Value = "widget")]
        Widget,

        [EnumMember(Value = "note")]
        Note,

        [EnumMember(Value = "pretty_cards")]
        PrettyCards,

        [EnumMember(Value = "situational_theme")]
        SituationalTheme,

        [EnumMember(Value = "textlive")]
        Textlive,

        [EnumMember(Value = "textpost_publish")]
        TextpostPublish,

        [EnumMember(Value = "narrative")]
        Narrative,

        [EnumMember(Value = "audio_playlist")]
        AudioPlaylist,

        [EnumMember(Value = "artist")]
        Artist,

        [EnumMember(Value = "mini_app")]
        MiniApp,

        [EnumMember(Value = "article")]
        Article,

        [EnumMember(Value = "money_request")]
        MoneyRequest,

        [EnumMember(Value = "money_transfer")]
        MoneyTransfer,

        [EnumMember(Value = "ugc_sticker")]
        UGCSticker,

        [EnumMember(Value = "sticker_pack_preview")]
        StickerPackPreview
    }

    public class AttachmentBase {
        [JsonIgnore]
        public virtual string ObjectType { get; internal set; }

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("access_key")]
        public string AccessKey { get; set; }

        public override string ToString() {
            string ak = String.IsNullOrEmpty(AccessKey) ? "" : $"_{AccessKey}";
            return $"{ObjectType}{OwnerId}_{Id}{ak}";
        }
    }

    public class Attachment {
        [JsonProperty("type")]
        public string TypeString { get; set; }

        [JsonIgnore]
        public AttachmentType Type { get { return GetAttachmentEnum(TypeString); } }

        [JsonProperty("photo")]
        public Photo Photo { get; set; }

        [JsonProperty("video")]
        public Video Video { get; set; }

        [JsonProperty("video_message")]
        public VideoMessage VideoMessage { get; set; }

        [JsonProperty("audio")]
        public Audio Audio { get; set; }

        [JsonProperty("audio_message")]
        public AudioMessage AudioMessage { get; set; }

        [JsonProperty("podcast")]
        public Podcast Podcast { get; set; }

        [JsonProperty("doc")]
        public Document Document { get; set; }

        [JsonProperty("link")]
        public Link Link { get; set; }

        [JsonProperty("donut_link")]
        public DonutLink DonutLink { get; set; }

        [JsonProperty("market")]
        public Market Market { get; set; }

        [JsonProperty("wall")]
        public WallPost Wall { get; set; }

        [JsonProperty("wall_reply")]
        public WallReply WallReply { get; set; }

        [JsonProperty("sticker")]
        public Sticker Sticker { get; set; }

        [JsonProperty("sticker_pack_preview")]
        public StickerPackPreview StickerPackPreview { get; set; }

        [JsonProperty("ugc_sticker")]
        public UGCSticker UGCSticker { get; set; }

        [JsonProperty("graffiti")]
        public Graffiti Graffiti { get; set; }

        [JsonProperty("story")]
        public Story Story { get; set; }

        [JsonProperty("gift")]
        public Gift Gift { get; set; }

        [JsonProperty("call")]
        public Call Call { get; set; }

        [JsonProperty("group_call_in_progress")]
        public GroupCallInProgress GroupCallInProgress { get; set; }

        [JsonProperty("poll")]
        public Poll Poll { get; set; }

        [JsonProperty("event")]
        public Event Event { get; set; }

        [JsonProperty("curator")]
        public Curator Curator { get; set; }

        [JsonProperty("widget")]
        public Widget Widget { get; set; }

        [JsonProperty("page")]
        public WikiPage Page { get; set; }

        [JsonProperty("note")]
        public Note Note { get; set; }

        [JsonProperty("album")]
        public Album Album { get; set; }

        [JsonProperty("situational_theme")]
        public SituationalTheme SituationalTheme { get; set; }

        [JsonProperty("textlive")]
        public Textlive Textlive { get; set; }

        [JsonProperty("textpost_publish")]
        public TextpostPublish TextpostPublish { get; set; }

        [JsonProperty("narrative")]
        public Narrative Narrative { get; set; }

        [JsonProperty("audio_playlist")]
        public AudioPlaylist AudioPlaylist { get; set; }

        [JsonProperty("artist")]
        public Artist Artist { get; set; }

        [JsonProperty("mini_app")]
        public MiniApp MiniApp { get; set; }

        [JsonProperty("article")]
        public Article Article { get; set; }

        [JsonProperty("money_request")]
        public MoneyRequest MoneyRequest { get; set; }

        [JsonProperty("money_transfer")]
        public MoneyTransfer MoneyTransfer { get; set; }

        public static AttachmentType GetAttachmentEnum(string typeString) {
            switch (typeString) {
                case "photo": return AttachmentType.Photo;
                case "album": return AttachmentType.Album;
                case "video": return AttachmentType.Video;
                case "video_message": return AttachmentType.VideoMessage;
                case "audio": return AttachmentType.Audio;
                case "audio_message": return AttachmentType.AudioMessage;
                case "podcast": return AttachmentType.Podcast;
                case "doc": return AttachmentType.Document;
                case "graffiti": return AttachmentType.Graffiti;
                case "link": return AttachmentType.Link;
                case "donut_link": return AttachmentType.DonutLink;
                case "poll": return AttachmentType.Poll;
                case "page": return AttachmentType.Page;
                case "market": return AttachmentType.Market;
                case "wall": return AttachmentType.Wall;
                case "wall_reply": return AttachmentType.WallReply;
                case "sticker": return AttachmentType.Sticker;
                case "sticker_pack_preview": return AttachmentType.StickerPackPreview;
                case "ugc_sticker": return AttachmentType.UGCSticker;
                case "story": return AttachmentType.Story;
                case "event": return AttachmentType.Event;
                case "gift": return AttachmentType.Gift;
                case "call": return AttachmentType.Call;
                case "group_call_in_progress": return AttachmentType.GroupCallInProgress;
                case "curator": return AttachmentType.Curator;
                case "widget": return AttachmentType.Widget;
                case "note": return AttachmentType.Note;
                case "pretty_cards": return AttachmentType.PrettyCards;
                case "situational_theme": return AttachmentType.SituationalTheme;
                case "textlive": return AttachmentType.Textlive;
                case "textpost_publish": return AttachmentType.TextpostPublish;
                case "narrative": return AttachmentType.Narrative;
                case "audio_playlist": return AttachmentType.AudioPlaylist;
                case "artist": return AttachmentType.Artist;
                case "mini_app": return AttachmentType.MiniApp;
                case "article": return AttachmentType.Article;
                case "money_request": return AttachmentType.MoneyRequest;
                case "money_transfer": return AttachmentType.MoneyTransfer;
                default: return AttachmentType.Unknown;
            }
        }
    }
}