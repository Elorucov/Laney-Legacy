using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI;
using Microsoft.QueryStringDotNET;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace Elorucov.Laney.Services.PushNotifications {
    public class ToastBackgroundActivation {
        public static async Task<bool> TryRegisterBackgroundTaskAsync() {
            try {
                const string taskName = "ToastBackgroundTask";

                foreach (var t in BackgroundTaskRegistration.AllTasks) {
                    if (t.Value.Name == taskName) {
                        AppParameters.TestLogBackgroundToast = $"Task registered, unregistering...";
                        t.Value.Unregister(true);
                        break;
                    }
                }

                // Otherwise request access
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                AppParameters.TestLogBackgroundToast = status.ToString();
                if (status.ToString().Contains("Denied") || status == BackgroundAccessStatus.Unspecified) return false;

                // Create the background task
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder() {
                    Name = taskName,
                    IsNetworkRequested = true
                };
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

                // Assign the toast action trigger
                builder.SetTrigger(new ToastNotificationActionTrigger());

                // And register the task
                BackgroundTaskRegistration registration = builder.Register();
                AppParameters.TestLogBackgroundToast += $"; {registration.TaskId}";
                return true;
            } catch { return false; }
        }

        public static async Task ParseAsync(ToastNotificationActionTriggerDetail details) {
            string arguments = details.Argument;
            var userInput = details.UserInput;

            AppParameters.TestLogBackgroundToast = $"Arg: {arguments}\nInput: {userInput}\n";

            try {
                var q = QueryString.Parse(arguments);
                AppParameters.TestLogBackgroundToast = $"Arg: {arguments}\nAct: {q["action"]}\nInput: {userInput["tb"]}\n";

                switch (q["action"]) {
                    case "send":
                        await SendMessageAsync(q["peerId"], userInput["tb"].ToString(), q.Contains("cmid") ? q["cmid"] : null);
                        return;
                }
            } catch (Exception ex) {
                AppParameters.TestLogBackgroundToast += $"Exception (0x{ex.HResult.ToString("x8")}): {ex.Message}";
            }
        }

        private static async Task SendMessageAsync(string peerId, string text, string replyMessageId) {
            bool canReply = AppParameters.SendMessageWithReplyFromToast && !string.IsNullOrEmpty(replyMessageId);
            string forward = "";
            if (canReply) forward = $"&forward={{\"peer_id\":{peerId},\"conversation_message_ids\":[{replyMessageId}],\"is_reply\":true}}";

            string at = AppParameters.AccessToken;
            string domain = AppParameters.VkApiDomain ?? "https://api.vk.me";
            string lang = Locale.Get("lang");

            string url = $"{domain}/method/messages.send?peer_id={peerId}&random_id=0&message={text}{forward}&access_token={at}&v={API.Version}&lang={lang}";
            string resp = await SendRequestAsync(new Uri(url));
            AppParameters.TestLogBackgroundToast += $"Response: {resp}";
        }

        private static async Task<string> SendRequestAsync(Uri uri) {
            var response = await LNet.GetAsync(uri);
            return await response.Content.ReadAsStringAsync();
        }
    }
}