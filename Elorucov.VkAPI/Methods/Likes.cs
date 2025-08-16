using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Likes {
        public static async Task<object> Add(string type, long ownerId, long itemId, string accessKey = null) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "type", type },
                { "owner_id", ownerId.ToString() },
                { "item_id", itemId.ToString() }
            };
            if (!String.IsNullOrEmpty(accessKey)) p.Add("access_key", accessKey);

            var res = await API.SendRequestAsync("likes.add", p);
            try {
                if (res is string) {
                    string restr = res.ToString();
                    if (restr.Contains("{\"response\":")) {
                        restr = VKResponseHelper.GetJSONInResponseObject(restr);
                        return JObject.Parse(restr)["likes"].Value<int>();

                    } else if (restr.Contains("{\"error\":")) {
                        VKErrorResponse er = JsonConvert.DeserializeObject<VKErrorResponse>(restr);
                        return er.error;
                    } else {
                        throw new Exception($"A non-standart response was received:\n{restr}");
                    }
                } else {
                    return res;
                }
            } catch (Exception ex) {
                return ex;
            }
        }

        public static async Task<object> Delete(string type, long ownerId, long itemId, string accessKey = null) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "type", type },
                { "owner_id", ownerId.ToString() },
                { "item_id", itemId.ToString() }
            };
            if (!String.IsNullOrEmpty(accessKey)) p.Add("access_key", accessKey);

            var res = await API.SendRequestAsync("likes.delete", p);
            try {
                if (res is string) {
                    string restr = res.ToString();
                    if (restr.Contains("{\"response\":")) {
                        restr = VKResponseHelper.GetJSONInResponseObject(restr);
                        return JObject.Parse(restr)["likes"].Value<int>();

                    } else if (restr.Contains("{\"error\":")) {
                        VKErrorResponse er = JsonConvert.DeserializeObject<VKErrorResponse>(restr);
                        return er.error;
                    } else {
                        throw new Exception($"A non-standart response was received:\n{restr}");
                    }
                } else {
                    return res;
                }
            } catch (Exception ex) {
                return ex;
            }
        }
    }
}