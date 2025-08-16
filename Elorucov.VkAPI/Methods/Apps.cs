using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Apps {
        public static async Task<object> Get(string accessToken = null) { // needs for checking app token
            var reqs = new Dictionary<string, string>{
                { "access_token", accessToken }
            };

            var res = await API.SendRequestAsync("apps.get", reqs);
            return VKResponseHelper.ParseResponse<VKList<App>>(res);
        }
    }
}