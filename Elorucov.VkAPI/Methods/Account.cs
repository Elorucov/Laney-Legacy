using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Account {
        public static async Task<object> Ban(long id) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", id.ToString() }
            };

            var res = await API.SendRequestAsync("account.ban", p);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> Unban(long id) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "owner_id", id.ToString() }
            };

            var res = await API.SendRequestAsync("account.unban", p);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> SetSilenceMode(int time, long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "time", time.ToString() },
                { "peer_id", peerId.ToString() }
            };

            var res = await API.SendRequestAsync("account.setSilenceMode", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> SetOnline() {
            var res = await API.SendRequestAsync("account.setOnline", new Dictionary<string, string>());
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> SetOffline() {
            var res = await API.SendRequestAsync("account.setOffline", new Dictionary<string, string>());
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> SetPushSettings(string deviceId, string key, string value) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "device_id", deviceId },
                { "key", key },
                { "value", value }
            };

            var res = await API.SendRequestAsync("account.setPushSettings", p);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> UnregisterDevice(string deviceId) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "device_id", deviceId.ToString() }
            };

            var res = await API.SendRequestAsync("account.unregisterDevice", p);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> GetBanned() {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "fields", "photo_100,has_photo" }
            };

            var res = await API.SendRequestAsync("account.getBanned", req);
            return VKResponseHelper.ParseResponse<VKList<long>>(res);
        }

        public static async Task<object> GetPrivacySettings(string privacyKeys = null) {
            Dictionary<string, string> req = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(privacyKeys)) {
                req.Add("privacy_keys", privacyKeys);
                req.Add("need_default", "0");
            }

            var res = await API.SendRequestAsync("account.getPrivacySettings", req);
            return VKResponseHelper.ParseResponse<PrivacyResponse>(res);
        }

        public static async Task<object> SetPrivacy(string key, string value, string category = null) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "key", key },
                { "value", value }
            };
            if (!String.IsNullOrEmpty(category)) req.Add("category", category);

            var res = await API.SendRequestAsync("account.setPrivacy", req);
            return VKResponseHelper.ParseResponse<PrivacySettingValue>(res);
        }
    }
}