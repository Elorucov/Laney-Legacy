using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs.Dev {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MessageJSONView : Modal {
        private long convoId;
        private long cmid;

        public MessageJSONView(LMessage msg) {
            this.InitializeComponent();
            convoId = msg.PeerId;
            cmid = msg.ConversationMessageId;
            APIVersion.Text = VkAPI.API.Version;
            wtcb.Visibility = Visibility.Visible;
        }

        public MessageJSONView(ViewModel.ConversationViewModel convo) {
            this.InitializeComponent();
            convoId = convo.ConversationId;
            APIVersion.Text = VkAPI.API.Version;
            wtcb.Visibility = Visibility.Visible;
        }

        private void GetMessageJSON(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await GetMessageJSON(); })();

        }

        private void Resend(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await GetMessageJSON(); })();
        }

        private void Copy(object sender, RoutedEventArgs e) {
            DataPackage dp = new DataPackage();
            dp.RequestedOperation = DataPackageOperation.Copy;
            dp.SetText(JSONCode.Text);
            Clipboard.SetContent(dp);
            Tips.Show("Copied!");
        }

        private async Task GetMessageJSON() {
            JSONCode.Text = "Loading...";
            string apiVersion = APIVersion.Text;
            object resp;

            if (cmid > 0) {
                Title = "Message JSON";
                Dictionary<string, string> parameters = new Dictionary<string, string> {
                    { "peer_id", convoId.ToString() },
                    { "conversation_message_ids", cmid.ToString() },
                    { "access_token", AppParameters.AccessToken }
                };

                resp = await VkAPI.API.SendRequestAsync("messages.getByConversationMessageId", parameters, apiVersion);
            } else {
                Title = "Conversation JSON";
                Dictionary<string, string> parameters = new Dictionary<string, string> {
                    { "peer_ids", convoId.ToString() },
                    { "access_token", AppParameters.AccessToken }
                };

                resp = await VkAPI.API.SendRequestAsync("messages.getConversationsById", parameters, apiVersion);
            }

            if (resp is string) {
                string restr = resp.ToString();
                if (restr.Contains("{\"response\":")) {
                    var items = JToken.Parse(restr)["response"]["items"];
                    if (items.Count() == 0) {
                        JSONCode.Text = "Empty";
                        return;
                    }
                    JToken parsed = items[0];
                    restr = parsed.ToString(Newtonsoft.Json.Formatting.Indented);
                } else {
                    JToken parsed = JToken.Parse(restr);
                    restr = parsed.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                JSONCode.Text = restr;
            } else if (resp is Exception) {
                Exception ex = resp as Exception;
                JSONCode.Text = $"Exception 0x{ex.HResult.ToString("x8")}:\n{ex.Message}";
            }
        }
    }
}