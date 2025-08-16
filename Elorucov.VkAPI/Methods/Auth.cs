using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Auth {
        public static async Task<object> GetOauthToken(string accessToken, int appId, int scope, string hash, string authUserHash) {
            var reqs = new Dictionary<string, string> {
                { "access_token", accessToken },
                { "app_id", appId.ToString() },
                { "client_id", appId.ToString() },
                { "scope", scope.ToString() },
                { "hash", hash },
                { "auth_user_hash", authUserHash },
                { "is_seamless_auth", "0" },
            };

            var res = await API.SendRequestAsync("auth.getOauthToken", reqs);
            return VKResponseHelper.ParseResponse<OauthResponse>(res);
        }

        public static async Task<object> ValidateLogin(string accessToken, string lang, string deviceId, string login) {
            var reqs = new Dictionary<string, string> {
                { "access_token", accessToken },
                { "lang", lang },
                { "login", login },
                { "device_id", deviceId }
            };

            var res = await API.SendRequestAsync("auth.validateLogin", reqs);
            return VKResponseHelper.ParseResponse<ValidateLoginResponse>(res);
        }

        public static async Task<object> ValidatePhone(string accessToken, string lang, string deviceId, string sid, string phone) {
            var reqs = new Dictionary<string, string> {
                { "access_token", accessToken },
                { "lang", lang },
                { "device_id", deviceId },
                { "sid", sid },
                { "phone", phone },
                { "supported_ways", "push,email,call_in" },
                { "allow_callreset", "1" }
            };

            var res = await API.SendRequestAsync("auth.validatePhone", reqs);
            return VKResponseHelper.ParseResponse<ValidatePhoneResponse>(res);
        }

        public static async Task<object> ValidatePhoneConfirm(string accessToken, string lang, string deviceId, string sid, string phone, string code) {
            var reqs = new Dictionary<string, string> {
                { "access_token", accessToken },
                { "lang", lang },
                { "device_id", deviceId },
                { "sid", sid },
                { "phone", phone },
                { "code", code }
            };

            var res = await API.SendRequestAsync("auth.validatePhoneConfirm", reqs);
            return VKResponseHelper.ParseResponse<ValidatePhoneConfirmResponse>(res);
        }

        public static async Task<object> RefreshTokens(int clientId, string clientSecret, string exchangeTokens) {
            var reqs = new Dictionary<string, string> {
                { "api_id", clientId.ToString() },
                { "client_id", clientId.ToString() },
                { "client_secret", clientSecret },
                { "exchange_tokens", exchangeTokens },
                { "scope", "all" },
                { "initiator", "expired_token" },
                { "active_index", "0" }
            };

            var res = await API.SendRequestAsync("auth.refreshTokens", reqs);
            return VKResponseHelper.ParseResponse<RefreshTokensResponse>(res);
        }

        public static async Task<object> GetAuthCode(string anonymousToken, string lang, string deviceName, int clientId) {
            var reqs = new Dictionary<string, string> {
                { "access_token", anonymousToken },
                { "lang", lang },
                { "device_name", deviceName },
                { "client_id", clientId.ToString() },
                { "force_regenerate", "1" },
                { "auth_code_flow", "0" }
            };

            var res = await API.SendRequestAsync("auth.getAuthCode", reqs);
            return VKResponseHelper.ParseResponse<GetAuthCodeResponse>(res);
        }

        public static async Task<object> CheckAuthCode(string anonymousToken, string lang, int clientId, string hash, bool isWebAuth = false) {
            var reqs = new Dictionary<string, string> {
                { "access_token", anonymousToken },
                { "lang", lang },
                { "client_id", clientId.ToString() },
                { "auth_hash", hash },
            };
            if (isWebAuth) reqs.Add("web_auth", "1");

            var res = await API.SendRequestAsync("auth.checkAuthCode", reqs);
            return VKResponseHelper.ParseResponse<CheckAuthCodeResponse>(res);
        }

        public static async Task<object> ProcessAuthCodeInfo(string accessToken, string lang, string authCode) {
            var reqs = new Dictionary<string, string> {
                { "access_token", accessToken },
                { "lang", lang },
                { "auth_code", authCode },
                { "action", "0" }
            };

            var res = await API.SendRequestAsync("auth.processAuthCode", reqs);
            return VKResponseHelper.ParseResponse<ProcessAuthCodeResponse>(res);
        }

        public static async Task<object> ProcessAuthCodeAllow(string accessToken, string lang, string authCode) {
            var reqs = new Dictionary<string, string> {
                { "access_token", accessToken },
                { "lang", lang },
                { "auth_code", authCode },
                { "action", "1" }
            };

            var res = await API.SendRequestAsync("auth.processAuthCode", reqs);
            return VKResponseHelper.ParseResponse<ProcessAuthCodeResponse>(res);
        }
    }
}