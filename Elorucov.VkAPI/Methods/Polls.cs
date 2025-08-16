using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Polls {
        public static async Task<object> Create(string question, List<string> answers, int backgroundId, bool isAnonymous, bool isMultiple, bool disableUnvote, long endDate) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "question", question },
                { "add_answers", JsonConvert.SerializeObject(answers) }
            };
            if (backgroundId > 0) req.Add("background_id", backgroundId.ToString());
            req.Add("is_anonymous", isAnonymous ? "1" : "0");
            req.Add("is_multiple", isMultiple ? "1" : "0");
            req.Add("disable_unvote", disableUnvote ? "1" : "0");
            if (endDate != 0) req.Add("end_date", endDate.ToString());

            var res = await API.SendRequestAsync("polls.create", req);
            return VKResponseHelper.ParseResponse<Poll>(res);
        }

        public static async Task<object> GetBackgrounds() {
            var res = await API.SendRequestAsync("polls.getBackgrounds", new Dictionary<string, string>());
            return VKResponseHelper.ParseResponse<List<PollBackground>>(res);
        }

        public static async Task<object> GetById(long ownerId, long Id) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "owner_id", ownerId.ToString() },
                { "poll_id", Id.ToString() },
                { "extended", "1" },
                { "fields", API.Fields }
            };

            var res = await API.SendRequestAsync("polls.getById", req);
            return VKResponseHelper.ParseResponse<Poll>(res);
        }
    }
}