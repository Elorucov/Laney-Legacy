using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elorucov.VkAPI.Objects {
    public class ConversationsResponse {
        [JsonProperty("items")]
        public List<ConversationItem> Items { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }

        [JsonProperty("contacts")]
        public List<Contact> Contacts { get; set; }
    }

    public class ConversationItem {
        [JsonProperty("conversation")]
        public Conversation Conversation { get; set; }

        [JsonProperty("last_message")]
        public Message LastMessage { get; set; }
    }

    [DataContract]
    public enum PeerType {
        [EnumMember(Value = "user")]
        User,

        [EnumMember(Value = "chat")]
        Chat,

        [EnumMember(Value = "group")]
        Group,

        [EnumMember(Value = "email")]
        Email,

        [EnumMember(Value = "contact")]
        Contact
    }

    [DataContract]
    public enum UserStateInChat {
        [EnumMember(Value = "in")]
        In,

        [EnumMember(Value = "kicked")]
        Kicked,

        [EnumMember(Value = "left")]
        Left,

        [EnumMember(Value = "out")]
        Out,
    }

    [DataContract]
    public enum ChatSettingsChangers {
        [EnumMember(Value = "owner")]
        Owner,

        [EnumMember(Value = "owner_and_admins")]
        OwnerAndAdmins,

        [EnumMember(Value = "all")]
        All,
    }

    public class Peer {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("type")]
        public PeerType Type { get; set; }

        [JsonProperty("local_id")]
        public long LocalId { get; set; }
    }

    public class PushSettings {
        [JsonProperty("disabled_until")]
        public int DisabledUntil { get; set; }

        [JsonProperty("disabled_forever")]
        public bool DisabledForever { get; set; }

        [JsonProperty("no_sound")]
        public bool NoSound { get; set; }

        [JsonProperty("disabled_mentions")]
        public bool DisabledMentions { get; set; }

        [JsonProperty("disabled_mass_mentions")]
        public bool DisabledMassMentions { get; set; }
    }

    public class CanWrite {
        [JsonProperty("allowed")]
        public bool Allowed { get; set; }

        [JsonProperty("reason")]
        public int Reason { get; set; }

        [JsonProperty("until")]
        public long Until { get; set; }
    }

    public class WritingDisabledInfo {
        [JsonProperty("value")]
        public bool Value { get; set; }

        [JsonProperty("until_ts")]
        public long UntilTS { get; set; }
    }

    public class ChatPhoto {
        [JsonProperty("is_default_photo")]
        public bool IsDefaultPhoto { get; set; }

        [JsonProperty("photo_50")]
        public string Small { get; set; }

        [JsonProperty("photo_100")]
        public string Medium { get; set; }

        [JsonProperty("photo_200")]
        public string Big { get; set; }
    }

    public class ChatACL {
        [JsonProperty("can_change_info")]
        public bool CanChangeInfo { get; set; }

        [JsonProperty("can_change_invite_link")]
        public bool CanChangeInviteLink { get; set; }

        [JsonProperty("can_change_pin")]
        public bool CanChangePin { get; set; }

        [JsonProperty("can_invite")]
        public bool CanInvite { get; set; }

        [JsonProperty("can_promote_users")]
        public bool CanPromoteUsers { get; set; }

        [JsonProperty("can_see_invite_link")]
        public bool CanSeeInviteLink { get; set; }

        [JsonProperty("can_change_style")]
        public bool CanChangeStyle { get; set; }

        [JsonProperty("can_send_reactions")]
        public bool CanSendReactions { get; set; }
    }

    public class ChatPermissions {
        [JsonProperty("invite")]
        public ChatSettingsChangers Invite { get; set; }

        [JsonProperty("change_info")]
        public ChatSettingsChangers ChangeInfo { get; set; }

        [JsonProperty("change_pin")]
        public ChatSettingsChangers ChangePin { get; set; }

        [JsonProperty("use_mass_mentions")]
        public ChatSettingsChangers UseMassMentions { get; set; }

        [JsonProperty("see_invite_link")]
        public ChatSettingsChangers SeeInviteLink { get; set; }

        [JsonProperty("call")]
        public ChatSettingsChangers Call { get; set; }

        [JsonProperty("change_admins")]
        public ChatSettingsChangers ChangeAdmins { get; set; }

        [JsonProperty("change_style")]
        public ChatSettingsChangers ChangeStyle { get; set; }
    }

    public class ChatSettings {
        [JsonProperty("members_count")]
        public int MembersCount { get; set; }

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("acl")]
        public ChatACL ACL { get; set; }

        [JsonProperty("is_group_channel")]
        public bool IsGroupChannel { get; set; }

        [JsonProperty("pinned_message")]
        public Message PinnedMessage { get; set; }

        [JsonProperty("state")]
        public UserStateInChat State { get; set; }

        [JsonProperty("photo")]
        public ChatPhoto Photo { get; set; }

        [JsonProperty("active_ids")]
        public List<long> ActiveIDs { get; set; }

        [JsonProperty("admin_ids")]
        public List<long> AdminIDs { get; set; }

        [JsonProperty("is_disappearing")]
        public bool IsDisappearing { get; set; }

        [JsonProperty("is_service")]
        public bool IsService { get; set; }

        [JsonProperty("is_donut")]
        public bool IsDonut { get; set; }

        [JsonProperty("donut_owner_id")]
        public long DonutOwnerId { get; set; }

        [JsonProperty("permissions")]
        public ChatPermissions Permissions { get; set; }

        [JsonProperty("theme")]
        public string Theme { get; set; }

        [JsonProperty("writing_disabled")]
        public WritingDisabledInfo WritingDisabled { get; set; }
    }

    public class SortId {
        [JsonProperty("major_id")]
        public int MajorId { get; set; }

        [JsonProperty("minor_id")]
        public int MinorId { get; set; }

        [JsonIgnore]
        public ulong Id {
            get { return Get(); }
        }

        private ulong Get() {
            if (MajorId == 0 && MinorId > 0) return (ulong)MinorId;
            if (MinorId == 0 && MajorId > 0) return (ulong)MajorId;
            return (ulong)MinorId * (ulong)MajorId;
        }
    }

    public class Conversation {

        [JsonProperty("peer")]
        public Peer Peer { get; set; }

        [JsonProperty("in_read_cmid")]
        public int InRead { get; set; }

        [JsonProperty("out_read_cmid")]
        public int OutRead { get; set; }

        [JsonProperty("unread_count")]
        public int UnreadCount { get; set; }

        [JsonProperty("important")]
        public bool Important { get; set; }

        [JsonProperty("unanswered")]
        public bool Unanswered { get; set; }

        [JsonProperty("is_marked_unread")]
        public bool IsMarkedUnread { get; set; }

        [JsonProperty("is_archived")]
        public bool IsArchived { get; set; }

        [JsonProperty("sort_id")]
        public SortId SortId { get; set; }

        [JsonProperty("push_settings")]
        public PushSettings PushSettings { get; set; }

        [JsonProperty("can_write")]
        public CanWrite CanWrite { get; set; }

        [JsonProperty("mention_cmids")]
        public List<int> Mentions { get; set; }

        [JsonProperty("current_keyboard")]
        public BotKeyboard CurrentKeyboard { get; set; }

        [JsonProperty("chat_settings")]
        public ChatSettings ChatSettings { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }

        [JsonProperty("folder_ids")]
        public List<int> FolderIds { get; set; }

        [JsonProperty("unread_reactions")]
        public List<int> UnreadReactions { get; set; }
    }

    // Chat

    public class ChatMember {
        [JsonProperty("member_id")]
        public long MemberId { get; set; }

        [JsonProperty("join_date")]
        public int JoinDateUnix { get; set; }

        [JsonIgnore]
        public DateTime JoinDate { get { return DateTimeOffset.FromUnixTimeSeconds(JoinDateUnix).DateTime.ToLocalTime(); } }

        [JsonProperty("invited_by")]
        public long InvitedBy { get; set; }

        [JsonProperty("can_kick")]
        public bool CanKick { get; set; }

        [JsonProperty("is_admin")]
        public bool IsAdmin { get; set; }

        [JsonProperty("is_restricted_to_write")]
        public bool IsRestrictedToWrite { get; set; }

        [JsonIgnore]
        public Uri Photo { get; set; }

        [JsonIgnore]
        public string Name { get; set; }

        [JsonIgnore]
        public string NameGen { get; set; }

        [JsonIgnore]
        public string Subtitle { get; set; }
    }

    public class Chat {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("admin_id")]
        public long AdminId { get; set; }

        [JsonProperty("members_count")]
        public int MembersCount { get; set; }

        [JsonProperty("push_settings")]
        public PushSettings PushSettings { get; set; }

        [JsonProperty("photo_200")]
        public string Photo { get; set; }
    }

    public class ChatLink {
        [JsonProperty("link")]
        public string Link { get; set; }
    }

    public class MemberRestrictionResponse {
        [JsonProperty("failed_member_ids")]
        public List<long> FailedMemberIds { get; set; }
    }
}