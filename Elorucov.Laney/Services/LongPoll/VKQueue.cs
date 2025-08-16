using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elorucov.Laney.Services.LongPoll {
    public class VKQueue {
        #region Variables

        static bool IsCycleEnabled = false;
        const int WaitTime = 25;

        static string Server;
        static string Key;
        static string TimeStamp;

        public static bool IsInitialized { get; private set; }
        static CancellationTokenSource cts;
        static SynchronizationContext context;

        #endregion

        #region Events

        public static event EventHandler<OnlineQueueEvent> Online;

        #endregion

        public static async Task InitAsync(QueueSubscribeResponse info, SynchronizationContext sc) {
            if (info == null) return;
            Log.Info("[QUEUE] Initializing...");

            Server = info.BaseUrl;
            Key = info.Queues[0].Key;
            TimeStamp = info.Queues[0].Timestamp.ToString();
            Log.Info($"[QUEUE] Server: {Server}, TS: {TimeStamp}");
            cts = new CancellationTokenSource();
            context = sc;

            IsCycleEnabled = true;
            await RunAsync();
        }

        public static void Stop() {
            Log.Info("[QUEUE] Stopping...");
            IsCycleEnabled = false;
            cts?.Cancel();
        }

        private static async Task RunAsync() {
            while (IsCycleEnabled) {
                try {
                    Dictionary<string, string> parameters = new Dictionary<string, string> {
                        { "act", "a_check" },
                        { "key", Key },
                        { "ts", TimeStamp },
                        { "id", AppParameters.UserID.ToString() },
                        { "wait", WaitTime.ToString() }
                    };

                    HttpResponseMessage httpResponse = await LNet.PostAsync(new Uri(Server), parameters, cts: cts).ConfigureAwait(false);
                    string respstr = await httpResponse.Content.ReadAsStringAsync();
                    QueueResponse response = JsonConvert.DeserializeObject<QueueResponse>(respstr);

                    TimeStamp = response.Timestamp;
                    for (int i = 0; i < response.Events.Count; i++) {
                        ParseEvent(response.Events[i]);
                    }
                } catch (Exception ex) {
                    Log.Error(ex, $"Error in VKQueue > Run()!");
                    await Task.Delay(5000).ConfigureAwait(false);
                }
            }
        }

        private static void ParseEvent(QueueEvent queueEvent) {
            switch (queueEvent.EntityType) {
                case "online":
                    ParseOnlineEvent(queueEvent.Data.ToObject<OnlineQueueEvent>());
                    break;
            }
        }

        private static void ParseOnlineEvent(OnlineQueueEvent oqe) {
            Log.Verbose($"[QUEUE] (online): User={oqe.UserId}, Online={oqe.Online}, App={oqe.AppId}, Platform={oqe.Platform}, Last seen={oqe.LastSeenUnix}");
            context.Post(o => Online?.Invoke(null, oqe), null);
        }
    }
}