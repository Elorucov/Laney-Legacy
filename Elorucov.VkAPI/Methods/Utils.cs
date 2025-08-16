using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Utils {
        public static async Task<object> CheckLink(string url) {
            var reqs = new Dictionary<string, string> {
                { "url", url }
            };

            var res = await API.SendRequestAsync("utils.checkLink", reqs);
            return VKResponseHelper.ParseResponse<CheckLinkResult>(res);
        }
    }
}