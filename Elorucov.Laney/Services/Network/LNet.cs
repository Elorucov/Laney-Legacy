﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web;

namespace Elorucov.Laney.Services.Network {
    public class VKProxyConfig {
        public List<string> Servers { get; private set; }
        public List<string> Domains { get; private set; }
        public List<VKProxyCertificate> Certificates { get; private set; }

        internal VKProxyConfig(List<string> servers, List<string> domains, List<VKProxyCertificate> certs) {
            Servers = servers;
            Domains = domains;
            Certificates = certs;
        }
    }

    public class VKProxyCertificate {
        public int Id { get; private set; }
        public X509Certificate2 Certificate { get; private set; }

        internal VKProxyCertificate(int id, string cert) {
            Id = id;
            Certificate = new X509Certificate2(Encoding.UTF8.GetBytes(cert), string.Empty);
        }
    }

    public class LNet {
        static HttpClient client;
        static HttpClient nocookieclient;
        static HttpClient tclient;

        static string pingUrl = "https://vk.com/ping.txt";
        const string pingHash = "BCC08EA1CFB021F5595C3190D1FADC49";

        static string gfburl = "https://firebaseremoteconfig.googleapis.com/v1/projects/841415684880/namespaces/firebase:fetch";
        static string gfbbody = "ew0KICAicGxhdGZvcm1WZXJzaW9uIjogIjI2IiwNCiAgImFwcEluc3RhbmNlSWQiOiAiYWJjZGVmZ2hpamtsbW5vcHFyc3R1diIsDQogICJwYWNrYWdlTmFtZSI6ICJjb20udmtvbnRha3RlLmFuZHJvaWQiLA0KICAiYXBwVmVyc2lvbiI6ICI2LjExIiwNCiAgImNvdW50cnlDb2RlIjogIlVTIiwNCiAgInNka1ZlcnNpb24iOiAiMTkuMS40IiwNCiAgImFuYWx5dGljc1VzZXJQcm9wZXJ0aWVzIjoge30sDQogICJhcHBJZCI6ICIxOjg0MTQxNTY4NDg4MDphbmRyb2lkOjYzMmY0MjkzODExNDExMjEiLA0KICAibGFuZ3VhZ2VDb2RlIjogImVuLVVTIiwNCiAgImFwcEluc3RhbmNlSWRUb2tlbiI6ICJhYmNkZWZnaGlqa2xtbm9wcXJzdHV2IiwNCiAgInRpbWVab25lIjogIkdNVCINCn0=";
        static Dictionary<string, string> gfbheaders = new Dictionary<string, string> {
            {"X-Goog-Api-Key", "AIzaSyAvrvAACdzmgDYFM9hvJS88KdSlQsafID0"},
            {"X-Android-Package", "com.vkontakte.android"},
            {"X-Android-Cert", "48761EEF50EE53AFC4CC9C5F10E6BDE7F8F5B82F"},
            {"X-Google-GFE-Can-Retry", "yes"}
        };

        private static VKProxyConfig Config { get; set; }
        public static string CurrentProxy { get; private set; } = string.Empty;
        public static VKProxyCertificate CurrentCertificate { get; private set; }
        public static CookieContainer Cookies { get; private set; }

        public static event EventHandler<string> DebugLog;
        private static void Log(string text) {
            Debug.WriteLine($"LNet: {text}");
            DebugLog?.Invoke(null, text);
        }

        public static async Task<bool> InitConnectionAsync(CancellationTokenSource cts, bool forceProxy = false) {
            Log("Checking VK access...");

            try {
                if (forceProxy) return await TryConnectToProxyServerAsync(cts);
                bool isMatch = await PingAsync(cts);
                Log(isMatch ? $"VK ping successed. No proxy needed" : "Ping hash mismatch!");
                if (isMatch) {
                    return false;
                } else {
                    return await TryConnectToProxyServerAsync(cts);
                }
            } catch (HttpRequestException httpex) {
                WebErrorStatus err = WebError.GetStatus(httpex.HResult);
                Log($"HttpRequestException: WebErrorStatus = {err}!");
                return await TryConnectToProxyServerAsync(cts);
            } catch (TimeoutException) {
                Log($"TimeoutException!");
                return await TryConnectToProxyServerAsync(cts);
            } catch (Exception ex) {
                throw ex;
            }
        }

        private static async Task<bool> TryConnectToProxyServerAsync(CancellationTokenSource cts) {
            Log("Getting VK proxies...");
            VKProxyConfig config = await GetProxiesAsync(cts);
            Log($"Proxy servers: {config.Servers.Count}; Domains count: {config.Domains.Count}; Certs count: {config.Certificates.Count}.");

            bool isMatch = false;
            bool skipIp = false;

            foreach (string proxy in config.Servers) {
                foreach (var cert in config.Certificates) {
                    if (skipIp) {
                        skipIp = false;
                        break;
                    }
                    try {
                        Log($"\nTrying to connect to proxy {proxy} using cert {cert.Id}...");
                        isMatch = await PingAsync(cts, config, proxy, cert);
                        if (isMatch) {
                            Config = config;
                            CurrentProxy = proxy;
                            CurrentCertificate = cert;
                            break;
                        }
                        Log($"Invalid response!\n");
                        skipIp = true;
                    } catch (OperationCanceledException oex) {
                        throw oex;
                    } catch (Exception ex) {
                        if ((ex.HResult == -2147012851 || ex.HResult == -2146697202) &&
                            (ex.InnerException.Message.Contains("The certificate authority is invalid or incorrect") ||
                            ex.InnerException.Message.Contains("A security problem occurred"))) {
                            Log($"Invalid certificate!");
                        } else {
                            Log($"Failed to connect — 0x{ex.HResult.ToString("x8")}: {ex.Message}\n");
                        }
                    }
                }
                if (isMatch) break;
            }

            return !string.IsNullOrEmpty(CurrentProxy) && CurrentCertificate != null;
        }

        private static async Task<VKProxyConfig> GetProxiesAsync(CancellationTokenSource cts) {
            StringContent hsc = new StringContent(Encoding.UTF8.GetString(Convert.FromBase64String(gfbbody)));
            hsc.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, new Uri(gfburl));
            foreach (var h in gfbheaders) {
                hrm.Headers.Add(h.Key, h.Value);
            }
            hrm.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            hrm.Headers.TryAddWithoutValidation("User-Agent", "Dalvik/2.1.0 (Linux; U; Android 8.0.0; Redmi Note 3)");
            hrm.Content = hsc;

            using (HttpClient hc = new HttpClient()) {
                Log("Sending request to firebase...");
                var result = cts == null ? await hc.SendAsync(hrm) : await hc.SendAsync(hrm, cts.Token);
                Log($"Response received. ({result.StatusCode})");
                string response = await result.Content.ReadAsStringAsync();
                return CheckProxiesFromResponse(response);
            }
        }

        private static VKProxyConfig CheckProxiesFromResponse(string response) {
            JObject root = JObject.Parse(response);
            JObject configNetProxy = JObject.Parse(root["entries"]["config_network_proxy"].Value<string>());
            JObject configNetProxyCerts = JObject.Parse(root["entries"]["config_network_proxy_certs"].Value<string>());

            var data = configNetProxy["data"];
            JArray ips = (JArray)data["ip"];
            JArray domains = (JArray)data["domains"];
            JArray certs = (JArray)configNetProxyCerts["certs"];
            Log($"IP-addresses count: {ips.Count}. Certs count: {certs.Count}");

            // TODO: weight

            List<string> ProxyServers = ips.Select(t => t.Value<string>()).ToList();
            List<string> Domains = domains.Select(t => t.Value<string>()).ToList();
            List<VKProxyCertificate> Certificates = new List<VKProxyCertificate>();

            foreach (JObject c in certs) {
                int id = c.Value<int>("id");
                string hpkp = c.Value<string>("hpkp");
                string cert = c.Value<string>("cert");
                Certificates.Add(new VKProxyCertificate(id, cert));
            }

            return new VKProxyConfig(ProxyServers, Domains, Certificates);
        }

        public static async Task<HttpResponseMessage> GetAsync(Uri uri,
            Dictionary<string, string> parameters = null,
            Dictionary<string, string> headers = null,
            CancellationTokenSource cts = null,
            bool dontSendCookies = false, bool throwExIfNonSuccessResponse = true) {
            return await InternalSendRequestAsync(uri, parameters, cts, Config, CurrentProxy, CurrentCertificate, headers, HttpMethod.Get, dontSendCookies, throwExIfNonSuccessResponse);
        }

        public static async Task<HttpResponseMessage> PostAsync(Uri uri,
            Dictionary<string, string> parameters = null,
            Dictionary<string, string> headers = null,
            CancellationTokenSource cts = null,
            bool dontSendCookies = false, bool throwExIfNonSuccessResponse = true) {
            return await InternalSendRequestAsync(uri, parameters, cts, Config, CurrentProxy, CurrentCertificate, headers, HttpMethod.Post, dontSendCookies, throwExIfNonSuccessResponse);
        }

        private static async Task<HttpResponseMessage> InternalSendRequestAsync(Uri uri, Dictionary<string, string> parameters, CancellationTokenSource cts, VKProxyConfig config, string proxyServer, VKProxyCertificate cert, Dictionary<string, string> headers = null, HttpMethod httpMethod = null, bool dontSendCookies = false, bool throwExIfNonSuccessResponse = false) {
            if (httpMethod == null) httpMethod = HttpMethod.Get;

            Uri fixedUri = uri;
            string host = string.Empty;
            var handler = new HttpClientHandler() {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                MaxConnectionsPerServer = 99
            };

            if (!dontSendCookies) {
                if (Cookies == null) Cookies = new CookieContainer();
                string[] domains = new string[] { "https://vk.com", "https://m.vk.com", "https://oauth.vk.com", "https://login.vk.com" };

                Windows.Web.Http.Filters.HttpBaseProtocolFilter f = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
                foreach (string domain in domains) {
                    Uri domainUri = new Uri(domain);
                    Windows.Web.Http.HttpCookieCollection c = f.CookieManager.GetCookies(domainUri);
                    foreach (Windows.Web.Http.HttpCookie hc in c) {
                        // if (hc.Name != "remixsid" && hc.Name != "remixstlid" && hc.Name != "remixlang") continue;
                        Cookie cookie = new Cookie() {
                            Name = hc.Name,
                            Domain = hc.Domain,
                            Path = hc.Path,
                            Secure = hc.Secure,
                            HttpOnly = hc.HttpOnly,
                            Value = hc.Value
                        };
                        cookie.Expires = hc.Expires.Value.DateTime;
                        Cookies.Add(domainUri, cookie);
                    }
                }
                handler.CookieContainer = Cookies;
            }

            HttpRequestMessage hrm = new HttpRequestMessage(httpMethod, fixedUri);
            if (!string.IsNullOrEmpty(host)) hrm.Headers.Host = host;
            if (headers != null) foreach (var header in headers) {
                    hrm.Headers.Add(header.Key, header.Value);
                }
            if (parameters != null) hrm.Content = new FormUrlEncodedContent(parameters);

            TimeoutHandler thandler = new TimeoutHandler {
                DefaultTimeout = TimeSpan.FromSeconds(2),
                InnerHandler = handler
            };

            bool needTimeoutCheckForPing = fixedUri.AbsoluteUri == pingUrl;
            if (client == null) client = new HttpClient(handler, false) { Timeout = TimeSpan.FromSeconds(60) };
            if (needTimeoutCheckForPing && tclient == null) tclient = new HttpClient(thandler, false) { Timeout = TimeSpan.FromSeconds(60) };
            if (dontSendCookies && nocookieclient == null) nocookieclient = new HttpClient(handler, false) { Timeout = TimeSpan.FromSeconds(60) };

            HttpClient cc = needTimeoutCheckForPing ? tclient : client;
            if (!needTimeoutCheckForPing) cc = dontSendCookies ? nocookieclient : client;
            cc.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

            //if (cc.DefaultRequestHeaders.Contains("User-Agent")) cc.DefaultRequestHeaders.Remove("User-Agent");
            //cc.DefaultRequestHeaders.Add("User-Agent", headers != null && headers.ContainsKey("User-Agent") ? headers["User-Agent"] : ApplicationInfo.UserAgent);

            Log($"Sending request to {fixedUri.AbsoluteUri}. User-Agent: \"{cc.DefaultRequestHeaders.UserAgent}\"");
            var stopwatch = Stopwatch.StartNew();
            var result = cts == null ? await cc.SendAsync(hrm, HttpCompletionOption.ResponseHeadersRead) : await cc.SendAsync(hrm, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (throwExIfNonSuccessResponse) result.EnsureSuccessStatusCode();
            stopwatch.Stop();
            Log($"Response received. Code: {result.StatusCode}, request took {stopwatch.ElapsedMilliseconds} ms.");
            return result;
        }

        #region Utils

        private static async Task<bool> PingAsync(CancellationTokenSource cts, VKProxyConfig config = null, string proxy = null, VKProxyCertificate cert = null) {
            HttpResponseMessage result = await InternalSendRequestAsync(new Uri(pingUrl), null, cts, config, proxy, cert);
            string response = await result.Content.ReadAsStringAsync();
            string hash = CreateMD5(response);
            bool isMatch = hash == pingHash;
            return isMatch;
        }

        private static bool ContainsDomain(List<string> domains, string host) {
            string[] sub = host.Split('.');
            List<string> top = new List<string> { sub[sub.Length - 2], sub[sub.Length - 1] };
            string domain = String.Join(".", top);
            return domains.Contains(domain);
        }

        private static string CreateMD5(string input) {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static Tuple<Uri, string> GetFixedUri(Uri uri) {
            Uri fixedUri = uri;
            string host = uri.Host;
            if (Config != null && Config.Servers != null && ContainsDomain(Config.Domains, uri.Host) && Config.Servers.Contains(CurrentProxy)) {
                var ub = new UriBuilder(uri.ToString());
                ub.Host = CurrentProxy;
                fixedUri = ub.Uri;
            }
            return new Tuple<Uri, string>(fixedUri, host);
        }

        #endregion
    }
}