using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Groups {
        public static async Task<object> GetById(long id) {
            var req = new Dictionary<string, string> {
                { "group_ids", id.ToString() },
                { "fields", "donut,can_post,city,country,can_message,place,description,members_count,activity,status,verified,site,cover" }
            };

            var res = await API.SendRequestAsync("groups.getById", req);
            return VKResponseHelper.ParseResponse<VKList<object>>(res);
        }
    }
}