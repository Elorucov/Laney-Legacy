using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Users {
        public static async Task<object> Get(long id = 0, string accessToken = null) {
            var reqs = new Dictionary<string, string>();
            if (id > 0) reqs.Add("user_ids", id.ToString());
            reqs.Add("fields", "can_post,verified,sex,bdate,city,country,photo_100,has_photo,photo_200,online_info,last_seen,domain,has_mobile,site,education,status,followers_count,nickname,can_write_private_message,can_send_friend_request,timezone,screen_name,maiden_name,is_friend,friend_status,career,occupation,status,blacklisted,blacklisted_by_me,first_name_gen,first_name_dat,first_name_acc,first_name_ins,first_name_abl,last_name_gen,last_name_dat,last_name_acc,last_name_ins,last_name_abl");
            if (!string.IsNullOrEmpty(accessToken)) reqs.Add("access_token", accessToken);

            var res = await API.SendRequestAsync("users.get", reqs);
            return VKResponseHelper.ParseResponse<List<User>>(res);
        }
    }
}