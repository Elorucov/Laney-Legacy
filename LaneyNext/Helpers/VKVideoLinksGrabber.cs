using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace Elorucov.Laney.Helpers
{
    public static class VKVideoLinksGrabber
    {
        public static bool IsBusy { get; private set; } = false;
        static WebView Web;
        static string script;

        public static void TryToGetLinksAsync(Video video, Action<List<VideoSource>> successCallback, Action<Exception> errorCallback, WebView web = null)
        {
            if (IsBusy) throw new Exception("Method is busy!");
            IsBusy = true;

            try
            {
                Log.General.Info("Starting...");
                if (String.IsNullOrEmpty(script)) LoadScript();

                Web = web != null ? web : new WebView();
                Web.DOMContentLoaded += (a, b) => InjectScript(a);
                Web.ScriptNotify += (a, b) => ParseCallsFromWebView(b.Value, successCallback, errorCallback);

                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://m.vk.com/video{video.OwnerId}_{video.Id}"));
                string ua = "Mozilla/5.0 (Windows Phone 10.0; Android 8.0.1; NOKIA; Lumia 1520) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.125 MobileSafari/537.36 Edge/15.15254";
                msg.Headers.Add("User-Agent", ua);
                Web.NavigateWithHttpRequestMessage(msg);
            }
            catch (Exception ex)
            {
                IsBusy = false;
                errorCallback?.Invoke(ex);
            }

            IsBusy = false;
        }

        private static async void LoadScript()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///WebInject/VKVideoLinks.js"));
            script = File.ReadAllText(file.Path);
        }

        private static async void InjectScript(WebView a)
        {
            Log.General.Info("Injecting script...");
            await Task.Delay(1000);
            await a.InvokeScriptAsync("eval", new string[] { script });
        }

        private static void ParseCallsFromWebView(string value, Action<List<VideoSource>> successCallback, Action<Exception> errorCallback)
        {
            JObject jo = JObject.Parse(value);
            string method = jo["method"].Value<string>();
            switch (method)
            {
                case "DebugInfo":
                    string dbg = jo["param"].Value<string>();
                    Log.General.Info($"JS: {dbg}");
                    break;
                case "JSError":
                    var param = jo.SelectToken("param");
                    string message = param["message"].Value<string>();
                    Log.General.Warn("JS Error", new ValueSet { { "message", message } });
                    errorCallback?.Invoke(new Exception($"Javascript error!\n{message}"));
                    break;
                case "VideoLinksGrabbed":
                    JArray array = jo.SelectToken("param") as JArray;
                    List<VideoSource> sources = new List<VideoSource>();
                    foreach (var s in array)
                    {
                        int r = s["resolution"].Value<int>();
                        string src = s["src"].Value<string>();
                        VideoSource source = new VideoSource(r, new Uri(src));
                        sources.Add(source);
                    }
                    Log.General.Info("Links received!", new ValueSet { { "count", sources.Count } });
                    successCallback?.Invoke(sources);
                    break;
            }
        }
    }
}