using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.PreviewDebug.Pages {

    public sealed partial class APICall : Page {
        public APICall() {
            this.InitializeComponent();
            wtcb.Visibility = Visibility.Visible;
        }

        private void Kcuf(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                btn.IsEnabled = false;
                resp.Text = "Sending...";

                string token = AppParameters.AccessToken;

                try {
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    foreach (var p in paramz.Text.Split('&')) {
                        if (p.Length > 2) {
                            string[] b = p.Split('=');
                            parameters.Add(b[0], b[1]);
                        }
                    }

                    if (!string.IsNullOrEmpty(execode.Text) && method.Text == "execute") {
                        if (parameters.ContainsKey("code")) parameters.Remove("code");
                        parameters.Add("code", execode.Text);
                    }

                    parameters.Add("access_token", token);

                    object a = await API.SendRequestAsync(method.Text, parameters);
                    if (a is string s) {
                        JToken parsed = JToken.Parse(s);
                        resp.Text = parsed.ToString(Newtonsoft.Json.Formatting.Indented);
                    } else if (a is Exception) {
                        Exception ex = a as Exception;
                        resp.Text = $"Exception (0x{ex.HResult.ToString("X8")})\n{ex.Message}";
                    }
                } catch (Exception ex) {
                    resp.Text = $"Exception (0x{ex.HResult.ToString("X8")})\n{ex.Message}";
                }

                btn.IsEnabled = true;
            })();
        }
    }
}