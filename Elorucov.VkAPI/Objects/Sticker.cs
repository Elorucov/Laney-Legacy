using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elorucov.VkAPI.Objects {
    public class StickerImage {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { return new Uri(Url); } }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("theme")]
        public string Theme { get; set; }
    }

    public class StickerRender {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("images")]
        public List<StickerImage> Images { get; set; }

        [JsonProperty("is_stub")]
        public bool IsStub { get; set; }

        [JsonProperty("is_rendering")]
        public bool IsRendering { get; set; }
    }

    public class StickerVmoji {
        [JsonProperty("character_id")]
        public string CharacterId { get; set; }
    }

    public interface ISticker {
        List<StickerImage> Images { get; set; }
    }

    public class Sticker : ISticker {
        [JsonProperty("animation_url")]
        public string AnimationUrl { get; set; }

        [JsonProperty("is_allowed")]
        public bool IsAllowed { get; set; }

        [JsonProperty("product_id")]
        public long ProductId { get; set; }

        [JsonProperty("sticker_id")]
        public long StickerId { get; set; }

        [JsonProperty("images")]
        public List<StickerImage> Images { get; set; }

        [JsonProperty("images_with_background")]
        public List<StickerImage> ImagesWithBackground { get; set; }

        [JsonProperty("render")]
        public StickerRender Render { get; set; }

        [JsonProperty("vmoji")]
        public StickerVmoji Vmoji { get; set; }

        [JsonIgnore]
        public bool IsPartial { get { return Images == null && ImagesWithBackground == null; } }
    }

    [DataContract]
    public enum UGCStickerStatus {
        [EnumMember(Value = "created")]
        Created,

        [EnumMember(Value = "passed")]
        Passed,

        [EnumMember(Value = "in_review")]
        InReview,

        [EnumMember(Value = "banned")]
        Banned,

        [EnumMember(Value = "rejected")]
        Rejected,
    }

    public class UGCSticker : AttachmentBase, ISticker {
        [JsonIgnore]
        public override string ObjectType { get { return "ugc_sticker"; } }

        [JsonProperty("pack_id")]
        public long PackId { get; set; }

        [JsonProperty("images")]
        public List<StickerImage> Images { get; set; }

        [JsonProperty("restrictions")]
        public List<string> Restrictions { get; set; }

        [JsonProperty("active_restriction")]
        public string ActiveRestriction { get; set; }

        [JsonProperty("status")]
        public UGCStickerStatus Status { get; set; }

        [JsonProperty("status_description")]
        public string StatusDescription { get; set; }

        [JsonProperty("is_deleted")]
        public bool IsDeleted { get; set; }

        [JsonProperty("is_claimed")]
        public bool IsClaimed { get; set; }
    }

    public class StickerDictionary {
        [JsonProperty("words")]
        public List<string> Words { get; set; }

        [JsonProperty("user_stickers")]
        public List<Sticker> UserStickers { get; set; }

        [JsonProperty("promoted_stickers")]
        public List<Sticker> PromotedStickers { get; set; }
    }

    public class StickersKeywordsResponse {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("chunks_count")]
        public int ChunksCount { get; set; }

        [JsonProperty("chunks_hash")]
        public string ChunksHash { get; set; }

        [JsonProperty("dictionary")]
        public List<StickerDictionary> Dictionary { get; set; }
    }

    public class StickerPreviewIcon {

        [JsonProperty("base_url")]
        public string BaseUrl { get; set; }
    }

    public class StickerPackPreview {

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("icon")]
        public StickerPreviewIcon Icon { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public Uri Uri { get { return new Uri(Url); } }
    }

    public class UGCStickerPack {
        [JsonProperty("owner_id")]
        public ulong OwnerId { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("is_enabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("stickers")]
        public List<UGCSticker> Stickers { get; set; }
    }
}