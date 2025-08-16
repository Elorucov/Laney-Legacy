using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class LongPollServer {
        public static async Task<Tuple<object, string>> GetState(Uri longPollUri, CancellationToken ct) {
            string response = String.Empty;
            if (API.RequestCallback != null) {
                Dictionary<string, string> headers = new Dictionary<string, string> {
                    { "Accept-Encoding", "gzip,deflate" }
                };

                string query = longPollUri.Query.Substring(1);
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                foreach (var a in query.Split('&')) {
                    if (a.Length > 2) {
                        string[] b = a.Split('=');
                        parameters.Add(b[0], b[1]);
                    }
                }

                var resp = await API.RequestCallback.Invoke(longPollUri, headers, parameters);
                response = await resp.Content.ReadAsStringAsync();
            } else {
                var httpClient = new HttpClient(new HttpClientHandler() {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                });
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                var hmsg = new HttpRequestMessage(HttpMethod.Post, longPollUri);
                var resp = await httpClient.SendAsync(hmsg);
                response = await resp.Content.ReadAsStringAsync();
            }

            using (var httpClient = new HttpClient()) {
                if (response.Contains("{\"ts\":")) {
                    LongPollResponse resp = JsonConvert.DeserializeObject<LongPollResponse>(response);
                    return new Tuple<object, string>(resp, response);
                } else if (response.Contains("{\"failed\":")) {
                    var err = JsonConvert.DeserializeObject<LongPollFail>(response);
                    return new Tuple<object, string>(err, response);
                } else {
                    throw new Exception($"A non-standart response was received:\n{response}");
                }
            }
        }
    }
}