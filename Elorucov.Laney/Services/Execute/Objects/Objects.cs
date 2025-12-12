using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using Action = Elorucov.VkAPI.Objects.Action;

namespace Elorucov.Laney.Services.Execute.Objects {
    public class UserOnlineInfoEx : UserOnlineInfo {
        [JsonProperty("app_name")]
        public string AppName { get; set; }
    }

    public class MessagesHistoryResponseEx {
        [JsonProperty("conversation")]
        public Conversation Conversation { get; set; }

        [JsonProperty("messages")]
        public List<Message> Messages { get; set; }

        [JsonProperty("messages_count")]
        public int Count { get; set; }

        [JsonProperty("last_message")]
        public Message LastMessage { get; set; }

        [JsonProperty("last_cmid")]
        public int LastMessageId { get; set; }

        [JsonProperty("is_messages_allowed")]
        public bool IsMessagesAllowed { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }

        [JsonProperty("contacts")]
        public List<Contact> Contacts { get; set; }

        [JsonProperty("members")]
        public List<ChatMember> Members { get; set; }

        [JsonProperty("online_info")]
        public UserOnlineInfoEx OnlineInfo { get; set; }
    }

    public class ChatInfoEx {
        [JsonProperty("chat_id")]
        public int ChatId { get; set; }

        [JsonProperty("peer_id")]
        public long PeerId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("photo")]
        public string Photo { get; set; }

        [JsonIgnore]
        public Uri PhotoUri { get { return Uri.IsWellFormedUriString(Photo, UriKind.Absolute) ? new Uri(Photo) : null; } }

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("is_channel")]
        public bool IsChannel { get; set; }

        [JsonProperty("members_count")]
        public int MembersCount { get; set; }

        [JsonProperty("online_count")]
        public int OnlineCount { get; set; }

        [JsonProperty("push_settings")]
        public PushSettings PushSettings { get; set; }

        [JsonProperty("acl")]
        public ChatACL ACL { get; set; }

        [JsonProperty("permissions")]
        public ChatPermissions Permissions { get; set; }

        [JsonProperty("state")]
        public UserStateInChat State { get; set; }

        [JsonProperty("pinned_message")]
        public Message PinnedMessage { get; set; }

        [JsonProperty("writing_disabled")]
        public WritingDisabledInfo WritingDisabled { get; set; }

        [JsonProperty("last_cmid")]
        public int LastCMID { get; set; }

        [JsonProperty("is_casper_chat")]
        public bool IsCasperChat { get; set; }

        [JsonProperty("disable_service_messages")]
        public bool DisableServiceMessages { get; set; }
    }

    public class SpecialEvent {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("is_internal")]
        public bool IsInternal { get; set; }

        [JsonProperty("is_hash_required")]
        public bool IsHashRequired { get; set; }

        [JsonProperty("need_tooltip")]
        public bool NeedTooltip { get; set; }
    }

    public class StartupInfo {
        [JsonProperty("exchange_token")]
        public string ExchangeToken { get; set; }

        [JsonProperty("ctl_source")]
        public string ChatThemesListSource { get; set; }

        [JsonProperty("trackVisitorResult")]
        public int TrackVisitorResult { get; set; }

        [JsonProperty("setOnlineResult")]
        public int SetOnlineResult { get; set; }

        [JsonProperty("registerDeviceResult")]
        public int RegisterDeviceResult { get; set; }

        [JsonProperty("push_settings")]
        public NotificationSettings PushSettings { get; set; }

        [JsonProperty("longpoll")]
        public LongPollServerInfo LongPoll { get; set; }

        [JsonProperty("users")]
        public List<User> Users { get; set; }

        [JsonProperty("messages_translation_language_pairs")]
        public List<string> MessagesTranslationLanguagePairs { get; set; }

        [JsonProperty("special_events")]
        public List<SpecialEvent> SpecialEvents { get; set; }

        [JsonProperty("sticker_product_ids")]
        public List<long> StickerProductIds { get; set; }

        [JsonProperty("features")]
        public List<int> Features { get; set; }

        [JsonProperty("reactions_assets")]
        public List<ReactionAsset> ReactionsAssets { get; set; }

        [JsonProperty("available_reactions")]
        public List<int> AvailableReactions { get; set; }

        [JsonProperty("queue_config")]
        public QueueSubscribeResponse QueueConfig { get; set; }
    }

    public class RegisterDeviceResult {
        [JsonProperty("unregistered")]
        public int Unregistered { get; set; }

        [JsonProperty("registered")]
        public int Registered { get; set; }
    }

    public class StickersFlyoutRecentItems {
        [JsonProperty("recent_stickers")]
        public List<Sticker> RecentStickers { get; set; }

        [JsonProperty("favorite_stickers")]
        public List<Sticker> FavoriteStickers { get; set; }

        [JsonProperty("graffities")]
        public List<Document> Graffities { get; set; }
    }

    public class UserEx : User {
        [JsonProperty("friends_count")]
        public int FriendsCount { get; set; }

        [JsonProperty("messages_count")]
        public int MessagesCount { get; set; }

        [JsonProperty("last_cmid")]
        public int LastCMID { get; set; }

        [JsonProperty("notifications_disabled")]
        public bool NotificationsDisabled { get; set; }

        [JsonProperty("live_in")]
        public string LiveIn { get; set; }

        [JsonProperty("current_career")]
        public UserCareer CurrentCareer { get; set; }

        [JsonProperty("current_education")]
        public string CurrentEducation { get; set; }

        [JsonProperty("online_info")]
        public new UserOnlineInfoEx OnlineInfo { get; set; }

        [JsonProperty("original_photo")]
        public Photo OriginalPhoto { get; set; }
    }

    public class GroupEx : Group {
        [JsonProperty("messages_count")]
        public int MessagesCount { get; set; }

        [JsonProperty("notifications_disabled")]
        public bool NotificationsDisabled { get; set; }

        [JsonProperty("messages_allowed")]
        public bool MessagesAllowed { get; set; }

        [JsonProperty("last_cmid")]
        public int LastCMID { get; set; }
    }

    public class AddChatUserResponse {
        [JsonProperty("success")]
        public List<string> Success { get; set; }

        [JsonProperty("failed")]
        public List<string> Failed { get; set; }
    }

    public class AddVoteResponse {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("poll")]
        public Poll Poll { get; set; }
    }

    public class AlbumLite {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("thumb")]
        public string Thumb { get; set; }

        [JsonIgnore]
        public Uri ThumbUri => Uri.IsWellFormedUriString(Thumb, UriKind.Absolute) ? new Uri(Thumb) : null;
    }

    public class ConvsAndCountersResponse {
        [JsonProperty("folders")]
        public VKList<Folder> Folders { get; set; }

        [JsonProperty("conversations")]
        public ConversationsResponse Conversations { get; set; }

        [JsonProperty("show_only_not_muted_messages")]
        public bool ShowOnlyNotMutedMessages { get; set; }

        [JsonProperty("counters")]
        public Counters Counters { get; set; }

        [JsonProperty("video_message_shapes")]
        public VideoMessageShapesResponse VideoMessageShapes { get; set; }
    }

    public class UGCStickerPacksResponse {
        [JsonProperty("items")]
        public List<UGCStickerPack> Items { get; set; }

        [JsonProperty("can_hide_keyboard")]
        public bool CanHideKeyboard { get; set; }

        [JsonProperty("is_keyboard_hidden")]
        public bool IsKeyboardHidden { get; set; }
    }

    public class CheckChatRightsResponse {
        [JsonProperty("writing_disabled")]
        public WritingDisabledInfo WritingDisabled { get; set; }

        [JsonProperty("can_write")]
        public CanWrite CanWrite { get; set; }

        [JsonProperty("acl")]
        public ChatACL ACL { get; set; }

        [JsonProperty("permissions")]
        public ChatPermissions Permissions { get; set; }
    }

    public class FeedSourcesResponse {
        [JsonProperty("lists")]
        public List<FeedList> Lists { get; private set; }

        [JsonProperty("friends")]
        public List<User> Friends { get; private set; }

        [JsonProperty("has_more_friends")]
        public bool HasMoreFriends { get; private set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; private set; }

        [JsonProperty("has_more_groups")]
        public bool HasMoreGroups { get; private set; }

        [JsonProperty("admined_groups")]
        public List<Group> AdminedGroups { get; private set; }

        [JsonProperty("has_more_admined_groups")]
        public bool HasMoreAdminedGroups { get; private set; }
    }

    public class UsersAndGroupsList {
        [JsonProperty("users")]
        public List<User> Users { get; private set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; private set; }
    }

    public class StatsPrepareResponse {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("messages_count")]
        public int MessagesCount { get; set; }

        [JsonProperty("first_cmid")]
        public int FirstCMID { get; set; }

        [JsonProperty("first_date")]
        public long FirstDate { get; set; }

        [JsonProperty("last_date")]
        public long LastDate { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("reactions_assets")]
        public List<ReactionAsset> ReactionsAssets { get; set; }
    }

    public class StatsRangeResponse {
        [JsonProperty("messages_count")]
        public int MessagesCount { get; set; }

        [JsonProperty("first_cmid")]
        public int FirstCMID { get; set; }

        [JsonProperty("last_cmid")]
        public int LastCMID { get; set; }
    }

    public class AudiosAndPlaylistsResponse {
        [JsonProperty("playlists")]
        public VKList<AudioPlaylist> Playlists { get; private set; }

        [JsonProperty("audios")]
        public VKList<Audio> Audios { get; private set; }
    }

    #region Need for message stats
    public class VKListLite<T> {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; } // special for messages.whoReadMessage

        [JsonProperty("items")]
        public List<T> Items { get; set; }

        [JsonProperty("profiles")]
        public List<UserLite> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<GroupLite> Groups { get; set; }
    }

    public class MessageLite {
        [JsonIgnore]
        public DateTime DateTime { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }

        [JsonIgnore]
        public DateTime UpdateTime { get { return DateTimeOffset.FromUnixTimeSeconds(UpdateTimeUnix).DateTime.ToLocalTime(); } }

        [JsonIgnore]
        public DateTime PinnedAt { get { return DateTimeOffset.FromUnixTimeSeconds(PinnedAtUnix).DateTime.ToLocalTime(); } }

        //

        [JsonProperty("conversation_message_id")]
        public int ConversationMessageId { get; set; }

        [JsonProperty("date")]
        public long DateUnix { get; set; }

        [JsonProperty("update_time")]
        public long UpdateTimeUnix { get; set; }

        [JsonProperty("pinned_at")]
        public long PinnedAtUnix { get; set; }

        [JsonProperty("peer_id")]
        public long PeerId { get; set; }

        [JsonProperty("from_id")]
        public long FromId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("attachments")]
        public List<AttachmentLite> Attachments { get; set; } = new List<AttachmentLite>();

        [JsonProperty("important")]
        public bool Important { get; set; }

        [JsonProperty("geo")]
        public Geo Geo { get; set; }

        [JsonProperty("action")]
        public Action Action { get; set; }

        [JsonProperty("is_expired")]
        public bool IsExpired { get; set; }

        [JsonProperty("reaction_id")]
        public int ReactionId { get; set; }

        [JsonProperty("reactions")]
        public List<MessageReaction> Reactions { get; set; }

        [JsonProperty("expire_ttl")]
        public int ExpireTTL { get; set; } // returned only in non-casper chats.

        [JsonProperty("ttl")]
        public int TTL { get; set; } // returned only in casper-chats, lol.
    }

    public class AttachmentLite {
        [JsonProperty("type")]
        public string TypeString { get; set; }

        [JsonProperty("audio_message")]
        public AudioMessage AudioMessage { get; set; }

        [JsonProperty("link")]
        public Link Link { get; set; }

        [JsonProperty("sticker")]
        public Sticker Sticker { get; set; }

        [JsonProperty("ugc_sticker")]
        public UGCSticker UGCSticker { get; set; }

        [JsonProperty("gift")]
        public Gift Gift { get; set; }

        [JsonProperty("call")]
        public Call Call { get; set; }
    }

    public class UserLite {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonIgnore]
        public string FullName { get { return String.Join(" ", FirstName, LastName).Trim(); } }

        [JsonProperty("photo_50")]
        public string Photo50 { get; set; }

        [JsonProperty("photo_100")]
        public string Photo100 { get; set; }

        [JsonIgnore]
        public Uri Photo {
            get {
                if (Uri.IsWellFormedUriString(Photo100, UriKind.Absolute)) return new Uri(Photo100);
                if (Uri.IsWellFormedUriString(Photo50, UriKind.Absolute)) return new Uri(Photo50);
                return new Uri("https://vk.com/images/camera_200.png");
            }
        }
    }

    public class GroupLite {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("photo_50")]
        public string Photo50 { get; set; }

        [JsonProperty("photo_100")]
        public string Photo100 { get; set; }

        [JsonIgnore]
        public Uri Photo {
            get {
                if (Uri.IsWellFormedUriString(Photo100, UriKind.Absolute)) return new Uri(Photo100);
                if (Uri.IsWellFormedUriString(Photo50, UriKind.Absolute)) return new Uri(Photo50);
                return new Uri("https://vk.com/images/camera_200.png");
            }
        }
    }

    #endregion
}