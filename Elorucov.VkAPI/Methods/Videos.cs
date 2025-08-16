using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Upload;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Videos {
        public static async Task<object> Get(long ownerId, int offset, int count, long albumId = 0) {
            var reqs = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "offset", offset.ToString() },
                { "count", count.ToString() }
            };
            if (albumId != 0) reqs.Add("album_id", albumId.ToString());

            var res = await API.SendRequestAsync("video.get", reqs);
            return VKResponseHelper.ParseResponse<VKList<Video>>(res);
        }

        public static async Task<object> Get(long ownerId, string videos, string accessToken = null) {
            var reqs = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "videos", videos }
            };
            if (!String.IsNullOrEmpty(accessToken)) reqs.Add("access_token", accessToken);

            var res = await API.SendRequestAsync("video.get", reqs);
            return VKResponseHelper.ParseResponse<VKList<Video>>(res);
        }

        public static async Task<object> Save(string name, string description, bool isPrivate, string link = null, long groupId = 0) {
            var reqs = new Dictionary<string, string> {
                { "name", name },
                { "description", description },
                { "is_private", isPrivate ? "1" : "0" }
            };
            if (!String.IsNullOrEmpty("link")) reqs.Add("link", link);
            if (groupId > 0) reqs.Add("group_id", groupId.ToString());

            var res = await API.SendRequestAsync("video.save", reqs);
            return VKResponseHelper.ParseResponse<VideoUploadServer>(res);
        }
    }
}