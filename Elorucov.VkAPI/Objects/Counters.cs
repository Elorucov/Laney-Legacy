using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class FolderCounters {
        [JsonProperty("folder_id")]
        public int FolderId { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        [JsonProperty("unmuted_count")]
        public int UnmutedCount { get; set; }
    }

    public class Counters {
        [JsonProperty("messages")]
        public int Messages { get; set; }

        [JsonProperty("messages_archive")]
        public int MessagesArchive { get; set; }

        [JsonProperty("messages_archive_unread")]
        public int MessagesArchiveUnread { get; set; }

        [JsonProperty("messages_archive_unread_unmuted")]
        public int MessagesArchiveUnreadUnmuted { get; set; }

        [JsonProperty("messages_archive_mentions_count")]
        public int MessagesArchiveMentionsCount { get; set; }

        [JsonProperty("messages_unread_unmuted")]
        public int MessagesUnreadUnmuted { get; set; }

        [JsonProperty("messages_folders")]
        public List<FolderCounters> MessagesFolders { get; set; }
    }
}