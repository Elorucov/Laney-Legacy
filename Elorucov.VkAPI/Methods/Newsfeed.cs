using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Newsfeed {
        public static async Task<object> Get(string filters, string sourceIds, string startFrom = null, int count = 10, string accessToken = null) {
            Dictionary<string, string> p = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(filters)) p.Add("filters", filters);
            if (!string.IsNullOrEmpty(sourceIds)) p.Add("source_ids", sourceIds);
            if (!string.IsNullOrEmpty(startFrom)) p.Add("start_from", startFrom);
            if (count > 0) p.Add("count", count.ToString());
            p.Add("fields", API.Fields);
            if (!string.IsNullOrEmpty(accessToken)) p.Add("access_token", accessToken);

            var res = await API.SendRequestAsync("newsfeed.get", p);
            return VKResponseHelper.ParseResponse<NewsfeedResponse>(res);
        }
    }
}