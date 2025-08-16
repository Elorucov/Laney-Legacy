using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.Profile;

namespace Elorucov.Laney.Services.Execute {
    public class Execute {
        private static async Task<string> GetCodeAsync(string fileName) {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/Execute/{fileName}.js"));
            string text = await FileIO.ReadTextAsync(file);
            return text;
        }

        public static async Task<object> Startup() {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "app_build", ApplicationInfo.Build.ToString() },
                { "os_build", Functions.GetOSBuild().ToString() },
                { "user_lang", Locale.Get("lang") },
                { "user_ids", AppParameters.UserID.ToString() },
                { "device_type", AnalyticsInfo.VersionInfo.DeviceFamily }
            };

            int type = AppParameters.Notifications;
            p.Add("push_type", type.ToString());
            if (type != 0) {
                string token = await PushNotifications.VKNotificationHelper.GetChannelUri();
                string id = PushNotifications.VKNotificationHelper.GetDeviceId();
                p.Add("token", token);
                p.Add("device_id", id);
            }

            string code = await GetCodeAsync("startup.17");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<StartupInfo>(res);
        }

        public static async Task<object> RegisterDevice() {
            Dictionary<string, string> p = new Dictionary<string, string>();
            int type = AppParameters.Notifications;
            p.Add("push_type", type.ToString());
            string token = await PushNotifications.VKNotificationHelper.GetChannelUri();
            string id = PushNotifications.VKNotificationHelper.GetDeviceId();
            p.Add("token", token);
            p.Add("device_id", id);

            string code = await GetCodeAsync("registerDevice.2");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<RegisterDeviceResult>(res);
        }

        public static async Task<object> GetRecentStickersAndGraffities() {
            Dictionary<string, string> p = new Dictionary<string, string>();

            string code = await GetCodeAsync("getRecentStickersAndGraffities.2");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<StickersFlyoutRecentItems>(res);
        }

        public static async Task<object> GetConvsFoldersAndCounters(int count = 60, int offset = 0) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "user_id", AppParameters.UserID.ToString() },
                { "count", count.ToString() },
                { "offset", offset.ToString() },
                { "fields", API.Fields }
            };
            string code = await GetCodeAsync("getConvsFoldersAndCounters.1");
            p.Add("code", code);
            p.Add("access_token", API.WebToken);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<ConvsAndCountersResponse>(res);
        }

        public static async Task<object> GetHistory(long peerId, int startMessageId, int offset = 0, int count = 40, bool doNotReturnMembers = false) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "count", count.ToString() },
                { "offset", offset.ToString() },
                { "start_cmid", startMessageId.ToString() },
                { "extended", "1" },
                { "do_not_return_members", doNotReturnMembers ? "1" : "0" },
                { "fields", API.Fields }
            };
            string code = await GetCodeAsync("getHistoryWithMembers.7");
            p.Add("code", code);
            p.Add("access_token", API.WebToken);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<MessagesHistoryResponseEx>(res);
        }

        public static async Task<object> GetChat(long chatId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "chat_id", chatId.ToString() }
            };

            string code = await GetCodeAsync("getChatInfo.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<ChatInfoEx>(res);
        }

        public static async Task<object> GetUserCard(long userId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "user_id", userId.ToString() }
            };

            string code = await GetCodeAsync("getUserCard.3");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<UserEx>(res);
        }

        public static async Task<object> GetGroupCard(long groupId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "group_id", groupId.ToString() }
            };

            string code = await GetCodeAsync("getGroupCard.2");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<GroupEx>(res);
        }

        public static async Task<object> AddChatUser(long chatId, List<long> userIds, int visibleMessagesCount = 0) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "chat_id", chatId.ToString() },
                { "user_ids", string.Join(",", userIds) },
                { "visible_messages_count", visibleMessagesCount.ToString() }
            };

            string code = await GetCodeAsync("addChatUser.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<AddChatUserResponse>(res);
        }

        public static async Task<object> AddVoteAndGetResult(long ownerId, long Id, List<ulong> answerIds) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "poll_id", Id.ToString() },
                { "answer_ids", string.Join(",", answerIds) }
            };

            string code = await GetCodeAsync("addVoteAndGetResult.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<AddVoteResponse>(res);
        }

        public static async Task<object> MultiSend(List<long> peerIds, string message, List<string> attachments) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "peer_ids", string.Join(",", peerIds) },
                { "message", message }
            };
            if (attachments.Count > 0) p.Add("attachments", string.Join(",", attachments));

            string code = await GetCodeAsync("multiSend.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<List<MultiSendResult>>(res);
        }

        public static async Task<object> GetPhotoAlbums(long ownerId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() }
            };

            string code = await GetCodeAsync("getPhotoAlbums.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<List<AlbumLite>>(res);
        }

        public static async Task<object> GetVideoAlbums(long ownerId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() }
            };

            string code = await GetCodeAsync("getVideoAlbums.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<List<AlbumLite>>(res);
        }

        public static async Task<object> GetUGCPacks(long ownerId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() }
            };

            string code = await GetCodeAsync("getUGCPacks.1");
            p.Add("code", code);
            p.Add("access_token", API.WebToken);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<UGCStickerPacksResponse>(res);
        }

        public static async Task<object> StatsPrepare(long ownerId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "peer_id", ownerId.ToString() }
            };

            string code = await GetCodeAsync("caPrepare.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<StatsPrepareResponse>(res);
        }

        public static async Task<object> StatsGetIdsByRange(long ownerId, string startDate, string endDate) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "peer_id", ownerId.ToString() },
                { "start_date", startDate },
                { "end_date", endDate }
            };

            string code = await GetCodeAsync("caGetIdsByRange.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<StatsRangeResponse>(res);
        }

        public static async Task<object> GetReactedPeersMulti(long ownerId, List<int> cmids) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "peer_id", ownerId.ToString() },
                { "cmids", string.Join(',', cmids) }
            };

            string code = await GetCodeAsync("getReactedPeersMulti.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<List<GetReactedPeersResponse>>(res);
        }

        public static async Task<object> CheckChatRights(long peerId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() }
            };

            string code = await GetCodeAsync("checkChatRights.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<CheckChatRightsResponse>(res);
        }

        public static async Task<object> GetFeedSources() {
            Dictionary<string, string> p = new Dictionary<string, string>();

            string code = await GetCodeAsync("getFeedSourcesShort.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<FeedSourcesResponse>(res);
        }

        public static async Task<object> SearchUsersAndGroups(string query) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "query", query },
                { "fields", API.Fields }
            };

            string code = await GetCodeAsync("searchUsersAndGroups.1");
            p.Add("code", code);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<UsersAndGroupsList>(res);
        }

        public static async Task<object> GetAudiosAndPlaylists() {
            Dictionary<string, string> p = new Dictionary<string, string>();

            string code = await GetCodeAsync("getAudiosAndPlaylists.1");
            p.Add("code", code);
            p.Add("access_token", API.WebToken);

            var res = await API.SendRequestAsync("execute", p);
            return VKResponseHelper.ParseResponse<AudiosAndPlaylistsResponse>(res);
        }
    }
}