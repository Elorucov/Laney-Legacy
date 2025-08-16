using LaneyWidgets.VKAPI;
using Microsoft.Windows.Widgets.Providers;
using Newtonsoft.Json;
using System.Diagnostics;
using WidgetHelpers;

namespace LaneyWidgets {
    internal class FriendsOnlineWidget : WidgetBase {

        // This function wil be invoked when the Increment button was clicked by the user.
        public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs) {
            var verb = actionInvokedArgs.Verb;
            
        }

        public override void Activate() {
            base.Activate();
            Debug.WriteLine($"FriendsOnlineWidget > Activate");
            Start().Wait();
        }

        public override void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs) {
            base.OnWidgetContextChanged(contextChangedArgs);
            //WidgetUpdateRequestOptions updateOptions = new WidgetUpdateRequestOptions(Id);
            //updateOptions.Data = "{ \"errorText\": \"Size: " + contextChangedArgs.WidgetContext.Size + "\", \"friends\": [] }";
            //updateOptions.CustomState = State;

            //WidgetManager.GetDefault().UpdateWidget(updateOptions);
        }

        public override string GetTemplateForWidget() {
            var widgetTemplate = GetTemplateFromFile("ms-appx:///LaneyWidgets/Templates/FriendsOnlineTemplate.json");
            return widgetTemplate;
        }

        public override string GetDataForWidget() {
            return "{ \"errorText\": \"" + AppLang.Get("loading") + "\", \"friends\": [] }";
        }

        private async Task Start() {
            string data = "{ \"errorText\": \"" + AppLang.Get("loading") + "\", \"friends\": [] }";

            try {
                var id = AppParameters.UserID;
                var token = AppParameters.AccessToken;
                if (id <= 0 || string.IsNullOrEmpty(token)) {
                    data = "{ \"errorText\": \"" + AppLang.Get("auth_required") + "\", \"friends\": [] }";
                } else {
                    var resp = await GetFriendsOnlineFromAPI(token, AppLang.GetCurrentLang());
                    string error = resp.Item2;
                    string json = resp.Item1;

                    data = "{ \"errorText\": \"" + error + "\", \"friends\": [" + json + "] }";
                }
            } catch (Exception ex) {
                data = "{ \"errorText\": \"Error 0x" + ex.HResult.ToString("x8") + ": " + ex.Message + "\", \"friends\": [] }";
            }

            WidgetUpdateRequestOptions updateOptions = new WidgetUpdateRequestOptions(Id);
            updateOptions.Data = data;
            updateOptions.CustomState = State;

            WidgetManager.GetDefault().UpdateWidget(updateOptions);
        }

        private async Task<Tuple<string, string>> GetFriendsOnlineFromAPI(string token, string lang) {
            try {
                FriendsOnlineResponse response = await VKAPI.VKAPI.CallMethodAsync<FriendsOnlineResponse>("execute.wbGetFriendsOnline", new Dictionary<string, string> {
                    { "access_token", token },
                    { "lang", lang },
                    { "v", "5.238" }
                });

                List<string> jsonObjects = new List<string>();

                foreach (var convo in response.Conversations) {
                    string avatar = "https://vk.com/images/camera_200.png";
                    string name = String.Empty;
                    string info = String.Empty;
                    int unread = convo.UnreadCount;
                    long pid = convo.Peer.Id;

                    var user = response.Profiles.Where(u => u.Id == convo.Peer.LocalId).FirstOrDefault();
                    if (user != null) {
                        avatar = user.Photo.ToString();
                        name = user.FullName;

                        if (user.OnlineInfo.AppId > 0) {
                            info = $"{AppLang.Get("online_via")} {user.OnlineInfo.AppName}";
                        } else {
                            if (user.OnlineInfo.IsMobile) {
                                info = AppLang.Get("online_mobile");
                            } else {
                                info = AppLang.Get("online");
                            }
                        }
                    } else {
                        name = $"id{convo.Peer.LocalId}";
                    }

                    if (convo.UnreadCount > 0) {
                        var message = response.LastMessages.Where(m => m.PeerId == convo.Peer.Id).FirstOrDefault();
                        if (message != null) { 
                            if (!String.IsNullOrEmpty(message.Text)) {
                                info = $"📩 {message.Text}";
                            } else if (message.Attachments != null && message.Attachments.Count > 0) {
                                info = $"📩 Attachments: {message.Attachments.Count}";
                            }
                        }
                    }

                    jsonObjects.Add($"{{\"link\": \"vk://vk.com/write{pid}\", \"avatar\": \"{avatar}\", \"name\": \"{name}\", \"info\": \"{info}\", \"unread\": {unread}}}");
                }

                return new Tuple<string, string>(String.Join(",", jsonObjects), String.Empty);

            } catch (APIException apiex) {
                return new Tuple<string, string>(String.Empty, $"VK API error ({apiex.Code}): {apiex.Message}");
            } catch (Exception ex) {
                return new Tuple<string, string>(String.Empty, $"An error occured! 0x{ex.HResult.ToString("x8")}");
            }
        }
    }

    internal class FriendsOnlineResponse {

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("conversations")]
        public List<Conversation> Conversations { get; set; }

        [JsonProperty("last_messages")]
        public List<Message> LastMessages { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }
    }
}