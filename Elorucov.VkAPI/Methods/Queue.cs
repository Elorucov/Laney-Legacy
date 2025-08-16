using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Queue {
        public static async Task<object> Subscribe(string queueIds) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "queue_ids", queueIds },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("queue.subscribe", req);
            return VKResponseHelper.ParseResponse<QueueSubscribeResponse>(res);
        }
    }
}