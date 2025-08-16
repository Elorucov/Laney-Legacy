using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Upload;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Photos {
        public static async Task<object> Get(long ownerId, long albumId, bool photoSizes, bool rev = true, int offset = 0, int count = 100) {
            var reqs = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "album_id", albumId.ToString() },
                { "rev", rev ? "1" : "0" },
                { "photo_sizes", photoSizes ? "1" : "0" },
                { "offset", offset.ToString() },
                { "count", count.ToString() }
            };

            var res = await API.SendRequestAsync("photos.get", reqs);
            return VKResponseHelper.ParseResponse<VKList<Photo>>(res);
        }

        public static async Task<object> GetAll(long ownerId, bool photoSizes, int offset = 0, int count = 100) {
            var reqs = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "offset", offset.ToString() },
                { "count", count.ToString() },
                { "photo_sizes", photoSizes ? "1" : "0" }
            };

            var res = await API.SendRequestAsync("photos.getAll", reqs);
            return VKResponseHelper.ParseResponse<VKList<Photo>>(res);
        }

        public static async Task<object> GetUserPhotos(long ownerId, bool photoSizes, int offset = 0, int count = 100) {
            var reqs = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "offset", offset.ToString() },
                { "count", count.ToString() },
                { "sort", "0" },
                { "photo_sizes", photoSizes ? "1" : "0" }
            };

            var res = await API.SendRequestAsync("photos.getUserPhotos", reqs);
            return VKResponseHelper.ParseResponse<VKList<Photo>>(res);
        }

        public static async Task<object> Copy(long ownerId, long photoId, string accessKey) {
            var reqs = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "photo_id", photoId.ToString() },
                { "access_key", accessKey }
            };

            var res = await API.SendRequestAsync("photos.copy", reqs);
            return VKResponseHelper.ParseResponse<int>(res);
        }

        public static async Task<object> GetMessagesUploadServer() {
            var res = await API.SendRequestAsync("photos.getMessagesUploadServer", new Dictionary<string, string>());
            return VKResponseHelper.ParseResponse<PhotoUploadServer>(res);
        }

        public static async Task<object> GetChatUploadServer(long chatId) {
            var reqs = new Dictionary<string, string> {
                { "chat_id", chatId.ToString() }
            };

            var res = await API.SendRequestAsync("photos.getChatUploadServer", reqs);
            return VKResponseHelper.ParseResponse<VkUploadServer>(res);
        }

        public static async Task<object> GetWallUploadServer(long groupId = 0) {
            var reqs = new Dictionary<string, string>();
            if (groupId > 0) reqs.Add("group_id", groupId.ToString());

            var res = await API.SendRequestAsync("photos.getWallUploadServer", reqs);
            return VKResponseHelper.ParseResponse<PhotoUploadServer>(res);
        }

        public static async Task<object> SaveMessagesPhoto(string photo, string server, string hash) {
            var reqs = new Dictionary<string, string> {
                { "photo", photo },
                { "server", server },
                { "hash", hash }
            };

            var res = await API.SendRequestAsync("photos.saveMessagesPhoto", reqs);
            return VKResponseHelper.ParseResponse<List<PhotoSaveResult>>(res);
        }

        public static async Task<object> SaveWallPhoto(string photo, string server, string hash, long wallOwnerId = 0) {
            var reqs = new Dictionary<string, string> {
                { "photo", photo },
                { "server", server },
                { "hash", hash }
            };
            if (wallOwnerId > 0) reqs.Add("user_id", wallOwnerId.ToString());
            if (wallOwnerId < 0) reqs.Add("group_id", (-wallOwnerId).ToString());

            var res = await API.SendRequestAsync("photos.saveWallPhoto", reqs);
            return VKResponseHelper.ParseResponse<List<PhotoSaveResult>>(res);
        }
    }
}