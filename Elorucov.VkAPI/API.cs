using Elorucov.VkAPI.Dialogs;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Elorucov.VkAPI {
    public class API {
        public const string Version = "5.251";
        private static string _api = "api.vk.me";
        public static string VkApiDomain { get { return _api; } private set { _api = value; } }
        private static string UserAgent;
        private static string _webToken;
        private static string _exchangeToken;

        public static bool Initialized { get { return !String.IsNullOrEmpty(accessToken); } }
        public static int VKMClientId { get; private set; }
        public static string VKMClientSecret { get; private set; }
        public static string WebToken { get { return _webToken; } set { _webToken = value; } }
        public static string ExchangeToken { get { return _exchangeToken; } set { _exchangeToken = value; } }
        public static Func<Uri, Dictionary<string, string>, Dictionary<string, string>, Task<HttpResponseMessage>> RequestCallback { get; set; }
        public static Action<bool, string, int> WebTokenRefreshed;

        static string accessToken;
        internal static string Lang { get; private set; }

        public static readonly string Fields = "can_post,can_suggest,type,donut,wall,verified,online_info,last_seen,photo_50,photo_100,has_photo,screen_name,sex,first_name_gen,first_name_dat,first_name_acc,first_name_ins,first_name_abl,last_name_gen,last_name_dat,last_name_acc,last_name_ins,last_name_abl";

        public static event EventHandler InvalidSessionErrorReceived;

        public static void Initialize(string atx, string lng, string userAgent, int clientId, string clientSecret, string apidomain = null) {
            accessToken = atx;
            Lang = lng;
            UserAgent = userAgent;
            VKMClientId = clientId;
            VKMClientSecret = clientSecret;
            // VkApiDomain = !String.IsNullOrEmpty(apidomain) ? apidomain : "api.vk.com";
        }

        public static void Uninitialize() {
            accessToken = null;
            WebToken = null;
            Lang = null;
        }

        private static Task waitForNextSend = Task.CompletedTask;
        public static async Task<object> SendRequestAsync(string method, Dictionary<string, string> parameters, string apiVersion = null) {
            Task oldWait = waitForNextSend;
            var tcs = new TaskCompletionSource<int>();
            waitForNextSend = tcs.Task;
            await oldWait;
            WaitHelper(tcs);
            return await InternalSendRequestAsync(method, parameters, apiVersion);
        }

        private static async void WaitHelper(TaskCompletionSource<int> tcs) {
            await Task.Delay(TimeSpan.FromSeconds(1.0 / 3));
            tcs.SetResult(0);
        }

        private static async Task<object> InternalSendRequestAsync(string method, Dictionary<string, string> parameters, string apiVersion = null) {
            string acctoken = accessToken;
            string version = String.IsNullOrEmpty(apiVersion) ? Version : apiVersion;
            string response;
            string requestUri = $@"https://{VkApiDomain}/method/{method}";

            Dictionary<string, string> prmkv = new Dictionary<string, string>();

            foreach (var a in parameters) {
                if (method == "account.registerDevice" && a.Key == "token") {
                    var f = new KeyValuePair<string, string>(a.Key, WebUtility.UrlDecode(a.Value));
                    prmkv.Add(a.Key, WebUtility.UrlDecode(a.Value));
                } else {
                    prmkv.Add(a.Key, a.Value);
                }
            }

            if (!prmkv.ContainsKey("lang")) prmkv.Add("lang", Lang);
            if (!prmkv.ContainsKey("v")) prmkv.Add("v", version);

            HttpResponseMessage resp;

            try {
                string auth = !prmkv.ContainsKey("access_token") ? acctoken : prmkv["access_token"];
                if (prmkv.ContainsKey("access_token")) prmkv.Remove("access_token");
                if (method == "auth.refreshTokens") auth = null;

                resp = await InternalRequestAsync(requestUri, prmkv, auth);

                if (resp.IsSuccessStatusCode) {
                    byte[] rarr = await resp.Content.ReadAsByteArrayAsync();
                    response = Encoding.UTF8.GetString(rarr);
                    object respt = await HandleNeedTokenExchangeRequest(method, parameters, response).ConfigureAwait(false);
                    if (respt is string rt) {
                        object respc = await HandleCaptchaRequest(method, parameters, rt).ConfigureAwait(false);
                        if (respc is string rc) {
                            response = rc;
                        } else if (respc is Exception) { throw respc as Exception; }
                    } else if (respt is Exception) { throw respt as Exception; }
                } else {
                    throw new Exception($"API server returns http-error: {resp.StatusCode}. \n{resp.ReasonPhrase}");
                }

                resp.Dispose();
                return response;
            } catch (Exception ex) {
                return ex;
            }
        }

        internal static async Task<HttpResponseMessage> InternalRequestAsync(string requestUri, Dictionary<string, string> prmkv, string auth = null) {
            HttpResponseMessage resp;
            if (RequestCallback != null) {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                if (!String.IsNullOrEmpty(auth)) headers.Add("Authorization", $"Bearer {auth}");
                if (requestUri.Contains("auth.getAuthCode") || requestUri.Contains("auth.checkAuthCode") || requestUri.Contains("auth.processAuthCode")) {
                    headers.Add("Origin", $"https://id.vk.com");
                }
                // if (auth == WebToken || (prmkv.ContainsKey("client_id") && prmkv["client_id"] == "3140623")) headers.Add("User-Agent", "com.vk.vkclient/2800 (unknown, iOS 17.4.1, iPhone 15 Pro Max, Scale/2.0)");
                resp = await RequestCallback.Invoke(new Uri(requestUri), prmkv, headers);
            } else {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                if (!String.IsNullOrEmpty(auth)) httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);
                HttpRequestMessage hmsg = new HttpRequestMessage(HttpMethod.Post, new Uri(requestUri));
                hmsg.Content = new FormUrlEncodedContent(prmkv);

                resp = await httpClient.SendAsync(hmsg);
                hmsg.Dispose();
                httpClient.Dispose();
            }
            return resp;
        }

        internal static bool CheckIsSessionInvalid(VKError error) {
            return error != null && error.error_code == 5;
        }

        // ВОт эту историческую фигню тоже надо бы переписать...
        private static async Task<object> HandleCaptchaRequest(string method, Dictionary<string, string> parameters, string response) {
            object resp = response;
            if (response.Contains("{\"error\":")) {
                VKErrorResponse er = JsonConvert.DeserializeObject<VKErrorResponse>(response);
                if (er.error.error_code == 14 && !String.IsNullOrEmpty(er.error.captcha_img) && !String.IsNullOrEmpty(er.error.captcha_sid)) {
                    CaptchaDialog dlg = new CaptchaDialog(er.error);
                    ContentDialogResult r = await dlg.ShowAsync();
                    if (r == ContentDialogResult.Primary) {
                        parameters.Add("captcha_sid", er.error.captcha_sid);
                        parameters.Add("captcha_key", dlg.CaptchaText);

                        resp = await SendRequestAsync(method, parameters).ConfigureAwait(false);
                    }
                }
            }
            return resp;
        }

        private static async Task<object> HandleNeedTokenExchangeRequest(string method, Dictionary<string, string> parameters, string response) {
            object resp = response;
            if (response.Contains("{\"error\":")) {
                VKErrorResponse er = JsonConvert.DeserializeObject<VKErrorResponse>(response);
                if (er.error.error_code == 1117) {
                    // For those who updated form Laney v1.21.
                    object resp1 = await Methods.Auth.RefreshTokens(3140623, "VeWdmVclDCtn6ihuP1nt", ExchangeToken);
                    if (resp1 is RefreshTokensResponse rtr) {
                        WebToken = rtr.Success[0].AccessToken.Token;
                        if (parameters.ContainsKey("access_token")) parameters["access_token"] = WebToken;
                        WebTokenRefreshed?.Invoke(true, WebToken, rtr.Success[0].AccessToken.ExpiresIn);
                        resp = await SendRequestAsync(method, parameters).ConfigureAwait(false);
                    } else {
                        WebTokenRefreshed?.Invoke(false, null, 0);
                    }
                }
            }
            return resp;
        }

        internal static void FireSessionInvalidEvent() {
            InvalidSessionErrorReceived?.Invoke(null, null);
        }
    }
}