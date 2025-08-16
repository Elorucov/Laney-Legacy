using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Upload;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Docs {
        public static async Task<object> Add(long ownerId, long id, string accessKey) {
            var reqs = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "doc_id", id.ToString() }
            };
            if (!String.IsNullOrEmpty(accessKey)) reqs.Add("access_key", accessKey);

            var res = await API.SendRequestAsync("docs.add", reqs);
            return VKResponseHelper.ParseResponse<string>(res);
        }
        public static async Task<object> Get(int count, int offset, int type, long ownerId) {
            var reqs = new Dictionary<string, string> {
                { "count", count.ToString() },
                { "offset", offset.ToString() },
                { "type", type.ToString() },
                { "owner_id", ownerId.ToString() }
            };

            var res = await API.SendRequestAsync("docs.get", reqs);
            return VKResponseHelper.ParseResponse<VKList<Document>>(res);
        }

        public static async Task<object> GetMessagesUploadServer(string type, string peerId = "0") {
            var reqs = new Dictionary<string, string> {
                { "type", type }
            };
            if (!String.IsNullOrEmpty(peerId)) reqs.Add("peer_id", peerId);

            var res = await API.SendRequestAsync("docs.getMessagesUploadServer", reqs);
            return VKResponseHelper.ParseResponse<VkUploadServer>(res);
        }

        public static async Task<object> GetWallUploadServer(long groupId = 0) {
            var reqs = new Dictionary<string, string>();
            if (groupId > 0) reqs.Add("group_id", groupId.ToString());

            var res = await API.SendRequestAsync("docs.getWallUploadServer", reqs);
            return VKResponseHelper.ParseResponse<VkUploadServer>(res);
        }

        public static async Task<object> Save(string file, string title = null) {
            var reqs = new Dictionary<string, string>();
            reqs.Add("file", file);
            if (!String.IsNullOrEmpty(title)) reqs.Add("title", title);

            var res = await API.SendRequestAsync("docs.save", reqs);
            return VKResponseHelper.ParseResponse<Attachment>(res);
        }
    }
}