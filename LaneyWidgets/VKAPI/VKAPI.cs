using System.Net.Http.Headers;
using System.Net;
using Newtonsoft.Json;

namespace LaneyWidgets.VKAPI {
    internal static class VKAPI {

        private static HttpClient httpClient;

        internal static async Task<string> SendRequestAsync(string method, Dictionary<string, string> parameters) {
            string requestUri = $@"https://api.vk.com/method/{method}";
            if (httpClient == null) {
                HttpClientHandler handler = new HttpClientHandler() {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Laney Widgets Provider");
            }

            using (HttpRequestMessage hmsg = new HttpRequestMessage(HttpMethod.Post, new Uri(requestUri)) { Version = new Version(2, 0) }) {
                hmsg.Content = new FormUrlEncodedContent(parameters);
                using (var resp = await httpClient.SendAsync(hmsg)) {
                    return await resp.Content.ReadAsStringAsync();
                }
            }
        }

        internal static async Task<T> CallMethodAsync<T>(string method, Dictionary<string, string> parameters) {
            if (parameters == null) parameters = new Dictionary<string, string>();

            string response = await SendRequestAsync(method, parameters);
            APIResponse<T> apiresp = JsonConvert.DeserializeObject<APIResponse<T>>(response);
            if (apiresp.Error != null) {
                throw apiresp.Error;
            } else if (apiresp.Response != null) {
                return apiresp.Response;
            } else {
                throw new InvalidDataException("Invalid response from VK API backend!");
            }
        }
    }

    internal class APIResponse<T> {

        [JsonProperty("response")]
        public T Response { get; set; }

        [JsonProperty("error")]
        public APIException Error { get; set; }
    }

    internal class APIException : Exception {

        [JsonProperty("error_code")]
        public int Code { get; set; }

        [JsonProperty("error_msg")]
        public string Message { get; set; }

        [JsonProperty("captcha_sid")]
        public string CaptchaSID { get; set; }

        [JsonProperty("captcha_img")]
        public string CaptchaImage { get; set; }

        [JsonProperty("redirect_url")]
        public string RedirectUrl { get; set; }

        [JsonProperty("confirmation_text")]
        public string ConfirmationText { get; set; }
    }
}