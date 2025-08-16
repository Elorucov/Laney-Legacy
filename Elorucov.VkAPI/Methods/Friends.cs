using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Friends {
        public static async Task<object> Get(long id, bool orderByName = false) {
            var req = new Dictionary<string, string> {
                { "user_id", id.ToString() },
                { "order", orderByName ? "name" : "hints" },
                { "fields", "verified,sex,bdate,city,country,photo_100,has_photo,photo_200,online_info,last_seen,domain,has_mobile,site,education,status,followers_count,nickname,can_write_private_message,can_send_friend_request,timezone,screen_name,maiden_name,is_friend,friend_status,career,occupation,status,blacklisted,blacklisted_by_me,first_name_gen,first_name_dat,first_name_acc,first_name_ins,first_name_abl,last_name_gen,last_name_dat,last_name_acc,last_name_ins,last_name_abl" }
            };

            var res = await API.SendRequestAsync("friends.get", req);
            return VKResponseHelper.ParseResponse<VKList<User>>(res);
        }

        public static async Task<object> Add(long userId) {
            var req = new Dictionary<string, string>();
            req.Add("user_id", userId.ToString());

            var res = await API.SendRequestAsync("friends.add", req);
            return VKResponseHelper.ParseResponse<string>(res);
        }

        public static async Task<object> Delete(long userId) {
            var req = new Dictionary<string, string>();
            req.Add("user_id", userId.ToString());

            var res = await API.SendRequestAsync("friends.delete", req);
            try {
                if (res is string) {
                    string restr = res.ToString();
                    if (restr.Contains("{\"response\":")) {
                        restr = VKResponseHelper.GetJSONInResponseObject(restr);
                        return true; // Тут должен быть объект.

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