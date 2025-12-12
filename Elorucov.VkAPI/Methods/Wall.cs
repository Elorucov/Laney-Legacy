using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public enum WallAttachmentsPrimaryMode {
        Grid, Carousel
    }

    public class Wall {
        public static async Task<object> Get(long ownerId, int offset, int count, string filter, string accessToken = null) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "offset", offset.ToString() },
                { "count", count.ToString() },
                { "filter", filter },
                { "extended", "1" },
                { "fields", API.Fields }
            };
            if (!String.IsNullOrEmpty(accessToken)) p.Add("access_token", accessToken);

            var res = await API.SendRequestAsync("wall.get", p);
            return VKResponseHelper.ParseResponse<VKList<WallPost>>(res);
        }

        public static async Task<object> GetById(long ownerId, long id, string accessKey = null, string accessToken = null) {
            if (!String.IsNullOrEmpty(accessKey)) accessKey = $"_{accessKey}";
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "posts", $"{ownerId}_{id}{accessKey}" },
                { "extended", "1" },
                { "fields", API.Fields },
                { "copy_history_depth", "1" }
            };
            if (!String.IsNullOrEmpty(accessToken)) p.Add("access_token", accessToken);

            var res = await API.SendRequestAsync("wall.getById", p);
            return VKResponseHelper.ParseResponse<VKList<WallPost>>(res);
        }

        public static async Task<object> Post(long ownerId, string guid, bool friendsOnly, bool bestFriendsOnly, bool fromGroup, string message, string attachments, bool signed, bool checkSign, bool closeComments, bool muteNotifications, long publishDate, double glong, double glat, string copyright, int? donutPaidDuration, WallAttachmentsPrimaryMode attachmentsPrimaryMode = WallAttachmentsPrimaryMode.Grid) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", $"{ownerId}" },
            };
            if (!string.IsNullOrEmpty(guid)) p.Add("guid", guid);
            if (friendsOnly) p.Add("friends_only", "1");
            if (bestFriendsOnly) p.Add("best_friends_only", "1");
            if (fromGroup) p.Add("from_group", "1");
            if (!string.IsNullOrEmpty(message)) p.Add("message", message);
            if (!string.IsNullOrEmpty(attachments)) p.Add("attachments", attachments);
            if (signed) p.Add("signed", "1");
            if (checkSign) p.Add("check_sign", "1");
            if (closeComments) p.Add("close_comments", "1");
            if (muteNotifications) p.Add("mute_notifications", "1");
            if (publishDate > 0) p.Add("publish_date", publishDate.ToString());
            if (glong != 0) p.Add("long", glong.ToString());
            if (glat != 0) p.Add("lat", glat.ToString());
            if (!string.IsNullOrEmpty(copyright)) p.Add("copyright", copyright);
            if (!string.IsNullOrEmpty(attachments)) p.Add("primary_attachments_mode", attachmentsPrimaryMode == WallAttachmentsPrimaryMode.Carousel ? "carousel" : "grid");
            if (donutPaidDuration.HasValue) p.Add("donut_paid_duration", donutPaidDuration.Value.ToString());

            var res = await API.SendRequestAsync("wall.post", p);
            return VKResponseHelper.ParseResponse<WallPostResponse>(res);
        }

        public static async Task<object> GetPostPreview(long ownerId, bool friendsOnly, bool bestFriendsOnly, bool fromGroup, string message, string attachments, bool signed, bool checkSign, long publishDate, double glong, double glat, int? donutPaidDuration) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", $"{ownerId}" },
                { "extended", "1" },
                { "fields", API.Fields },
            };
            if (friendsOnly) p.Add("friends_only", "1");
            if (bestFriendsOnly) p.Add("best_friends_only", "1");
            if (fromGroup) p.Add("from_group", "1");
            if (!string.IsNullOrEmpty(message)) p.Add("message", message);
            if (!string.IsNullOrEmpty(attachments)) p.Add("attachments", attachments);
            if (signed) p.Add("signed", "1");
            if (checkSign) p.Add("check_sign", "1");
            if (publishDate > 0) p.Add("publish_date", publishDate.ToString());
            if (glong != 0) p.Add("long", glong.ToString());
            if (glat != 0) p.Add("lat", glat.ToString());
            if (donutPaidDuration.HasValue) p.Add("donut_paid_duration", donutPaidDuration.Value.ToString());

            var res = await API.SendRequestAsync("wall.getPostPreview", p);
            return VKResponseHelper.ParseResponse<WallPostPreviewResponse>(res);
        }

        public static async Task<object> Search(long ownerId, string query, bool ownersOnly, int offset = 0, int count = 20, string accessToken = null) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "query", query },
                { "extended", "1" },
                { "owners_only", "1" },
                { "fields", API.Fields },
            };
            if (!String.IsNullOrEmpty(accessToken)) p.Add("access_token", accessToken);

            var res = await API.SendRequestAsync("wall.search", p);
            return VKResponseHelper.ParseResponse<VKList<WallPost>>(res);
        }
    }
}