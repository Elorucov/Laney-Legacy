using Elorucov.VkAPI.Objects.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elorucov.VkAPI {
    public class DirectAuth {
        public static async Task<AnonymToken> GetAnonymTokenAsync(int clientId, string clientSecret) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "client_id", clientId.ToString() },
                { "client_secret", clientSecret }
            };

            var resp = await API.InternalRequestAsync("https://oauth.vk.com/get_anonym_token", p);

            byte[] rarr = await resp.Content.ReadAsByteArrayAsync();
            string response = Encoding.UTF8.GetString(rarr);
            resp.Dispose();

            AnonymToken at = JsonConvert.DeserializeObject<AnonymToken>(response);
            return at;
        }

        public static async Task<DirectAuthResponse> AuthAsync(string lang, int clientId, string clientSecret, int scope, string username, string password, string code = null, string captchaSid = null, string captchaKey = null) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "lang", lang },
                { "grant_type", "password" },
                { "client_id", clientId.ToString() },
                { "client_secret", clientSecret },
                { "scope", scope.ToString() },
                { "username", username },
                { "password", password },
                { "2fa_supported", "1" },
                { "v", "5.1" }, // в новых версиях офклиентам возвращается silent_token.
            };
            if (!String.IsNullOrEmpty(code)) p.Add("code", code);
            if (!String.IsNullOrEmpty(captchaSid)) p.Add("captcha_sid", captchaSid);
            if (!String.IsNullOrEmpty(captchaSid)) p.Add("captcha_key", captchaKey);

            var resp = await API.InternalRequestAsync("https://oauth.vk.com/token", p);

            byte[] rarr = await resp.Content.ReadAsByteArrayAsync();
            string response = Encoding.UTF8.GetString(rarr);
            resp.Dispose();

            DirectAuthResponse das = JsonConvert.DeserializeObject<DirectAuthResponse>(response);
            return das;
        }

        public static async Task<DirectAuthResponse> AuthByPhoneConfirmationSIDAsync(string lang, int clientId, string clientSecret, int scope, string username, string password, string sid, string captchaSid = null, string captchaKey = null) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "lang", lang },
                { "grant_type", "phone_confirmation_sid" },
                { "sid", sid },
                { "client_id", clientId.ToString() },
                { "client_secret", clientSecret },
                { "scope", scope.ToString() },
                { "username", username },
                { "password", password },
                { "2fa_supported", "1" }
            };
            if (!String.IsNullOrEmpty(captchaSid)) p.Add("captcha_sid", captchaSid);
            if (!String.IsNullOrEmpty(captchaSid)) p.Add("captcha_key", captchaKey);

            var resp = await API.InternalRequestAsync("https://oauth.vk.com/token", p);

            byte[] rarr = await resp.Content.ReadAsByteArrayAsync();
            string response = Encoding.UTF8.GetString(rarr);
            resp.Dispose();

            DirectAuthResponse das = JsonConvert.DeserializeObject<DirectAuthResponse>(response);
            return das;
        }
    }
}