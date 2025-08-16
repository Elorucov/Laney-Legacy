using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Objects;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Notifications;

namespace Elorucov.Laney.Services.PushNotifications {

    class VKRawNotificationContext {
        [JsonProperty("chat_id")]
        public long ChatId { get; set; }

        [JsonProperty("sender_id")]
        public long SenderId { get; set; }

        [JsonProperty("msg_id")]
        public int MessageId { get; set; }

        [JsonProperty("conversation_message_id")]
        public int ConversationMessageId { get; set; }
    }

    class VKRawNotification {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")] // msg, chat, erase_messages
        public string Type { get; set; }

        [JsonProperty("items")] // For type=erase_messages
        public Dictionary<string, int> Items { get; set; }

        [JsonProperty("from_id")]
        public long FromId { get; set; }

        [JsonProperty("context")]
        public VKRawNotificationContext Context { get; set; }

        [JsonProperty("sender")] // message sender (if type=chat)
        public string Sender { get; set; }

        [JsonProperty("image")] // Image to be displayed as "appLogoOverride".
        public List<PhotoSizes> Image { get; set; }

        [JsonProperty("image_type")] // "user", "group"
        public string ImageType { get; set; }

        [JsonProperty("big_image")] // Image to be displayed under body. 
        public List<PhotoSizes> BigImage { get; set; }

        [JsonProperty("big_image_type")] // "photo", "video", "doc", "sticker",
        public string BigImageType { get; set; }

        [JsonProperty("badge")] // Unread count
        public uint Badge { get; set; }

        [JsonProperty("sound")]  // if enabled — "1" (string), otherwise false.
        public object Sound { get; set; }

        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("title")] // Sender name (if type=msg) or chat name (if type=chat) 
        public string Title { get; set; }

        [JsonProperty("body")] // message text
        public string Body { get; set; }
    }

    public class RawPushNotificationHandler {
        private static void LogPush(string content) {
            if (!AppParameters.LogPushNotifications) return;
            string filename = Path.Combine(ApplicationData.Current.LocalFolder.Path, "push.log");
            File.AppendAllText(filename, content + "\n\n=====================\n\n");
        }

        public static void ParsePushNotification(string content, IBackgroundTaskInstance taskInstance) {
            BackgroundTaskDeferral deferral = null;
            if (taskInstance != null) deferral = taskInstance.GetDeferral();
            ParsePushNotificationInternal(content);
            if (deferral != null) deferral.Complete();
        }

        private static void ParsePushNotificationInternal(string content) {
            LogPush(content);
            string ptype = "N/A";
            string pid = "N/A";
            Debug.WriteLine($"ParsePushNotification content: {content}");
            try {
                VKRawNotification n = JsonConvert.DeserializeObject<VKRawNotification>(content);
                ptype = n.Type;
                switch (n.Type) {
                    case "msg":
                    case "chat":
                        pid = n.Id;
                        ShowMessageToast(n);
                        break;
                    case "erase_messages":
                        RemoveMessagesFromNC(n.Items);
                        break;
                }

                VKNotificationHelper.SetBadge(n.Badge);
            } catch (Exception ex) {
                string dbg = $"Cannot parse push with type \"{ptype}\" and id \"{pid}\"!\nHResult: 0x{ex.HResult.ToString("x8")}\n{ex.Message}";
                Debug.WriteLine(dbg);
                LogPush(dbg);
            }
        }

        public static void TestPush(string content) {
            ParsePushNotificationInternal(content);
        }

        private static void ShowMessageToast(VKRawNotification msg) {
            bool isChat = msg.Type == "chat";
            long peer = isChat ? msg.Context.ChatId : msg.Context.SenderId;
            string chatName = isChat ? msg.Title : string.Empty;
            string senderName = isChat ? msg.Sender : msg.Title;
            string attribution = string.Empty;

            // Пустой senderAvatar — признак "no_text"
            string senderAvatar = msg.Image != null && msg.Image.Count > 0
                ? msg.Image[Math.Min(msg.Image.Count - 1, 1)].Url
                : string.Empty;
            string attachmentImage = msg.BigImage != null && msg.BigImage.Count > 0
                ? msg.BigImage[Math.Min(msg.BigImage.Count - 1, 1)].Url
                : string.Empty;
            bool hasSound = (msg.Sound is string s && s == "1") || (msg.Sound is bool b && b) || (msg.Sound is int i && i == 1);

            ToastContent content = new ToastContent {
                Launch = $"action=openConversation&peerId={peer}&messageId={msg.Context.MessageId}",
                Visual = new ToastVisual { BindingGeneric = new ToastBindingGeneric() },
                Audio = new ToastAudio {
                    Loop = false,
                    Silent = !hasSound,
                    Src = new Uri("ms-appx:///Assets/bb2.mp3")
                },
                DisplayTimestamp = DateTimeOffset.FromUnixTimeSeconds(msg.Time),
            };

            if (isChat) {
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                    content.Header = new ToastHeader(peer.ToString(), chatName, $"action=openConversation&peerId={peer}&cmid={msg.Context.ConversationMessageId}");
                } else {
                    attribution += $"{Locale.Get("in")} \"{chatName}\"";
                }
            }

            if (!string.IsNullOrEmpty(attribution))
                content.Visual.BindingGeneric.Attribution = new ToastGenericAttributionText { Text = attribution };

            if (!string.IsNullOrEmpty(senderAvatar)) {
                content.Visual.BindingGeneric.AppLogoOverride = new ToastGenericAppLogo {
                    HintCrop = ToastGenericAppLogoCrop.Circle,
                    Source = senderAvatar
                };
            }

            content.Visual.BindingGeneric.Children.Add(new AdaptiveText { Text = senderName, HintMaxLines = 2, HintStyle = AdaptiveTextStyle.Base });
            content.Visual.BindingGeneric.Children.Add(new AdaptiveText { Text = msg.Body, HintMaxLines = 5 });

            if (!string.IsNullOrEmpty(attachmentImage) && !string.IsNullOrEmpty(senderAvatar)) {
                content.Visual.BindingGeneric.Children.Add(new AdaptiveImage { HintRemoveMargin = true, Source = attachmentImage });
            }

            if (!string.IsNullOrEmpty(senderAvatar)) {
                content.Actions = new ToastActionsCustom {
                    Inputs = {
                        new ToastTextBox("tb") { PlaceholderContent = Locale.Get("msg_reply") },
                    },
                    Buttons = {
                        new ToastButton("Send", $"action=send&peerId={peer}&cmid={msg.Context.ConversationMessageId}") {
                            ActivationType = ToastActivationType.Background,
                            TextBoxId = "tb",
                            ImageUri = "Assets/send.png"
                        }
                    }
                };
            }

            var toast = new ToastNotification(content.GetXml());
            toast.Tag = msg.Context.MessageId.ToString();
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private static void RemoveMessagesFromNC(Dictionary<string, int> items) {
            var manager = ToastNotificationManager.GetDefault();
            var history = manager.History;
            var toasts = history.GetHistory().ToList();
            if (toasts.Count == 0) return;
            foreach (var item in items) {
                var toast = toasts.Where(t => t.Tag == item.Value.ToString()).FirstOrDefault();
                if (toast != null) {
                    history.Remove(toast.Tag);
                    break;
                }
            }
        }
    }
}