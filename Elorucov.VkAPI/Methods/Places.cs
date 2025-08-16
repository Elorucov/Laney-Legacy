using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Places {
        public static async Task<object> Search(double glat, double glong) {
            var req = new Dictionary<string, string> {
                { "latitude", glat.ToString().Replace(",", ".") },
                { "longitude", glong.ToString().Replace(",", ".") }
            };

            var res = await API.SendRequestAsync("places.search", req);
            return VKResponseHelper.ParseResponse<VKList<PlaceSearchResponse>>(res);
        }
    }
}