using Elorucov.Laney.Services.Common;
using System;
using Windows.UI.Xaml.Controls;


namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class ProxySettings : Page {
        public ProxySettings() {
            this.InitializeComponent();

            Loaded += (a, b) => {
                if (!String.IsNullOrEmpty(AppParameters.VkApiDomain)) srv.Text = "https://" + AppParameters.VkApiDomain;
            };

            srv.TextChanged += Srv_TextChanged;
        }

        private void Srv_TextChanged(object sender, TextChangedEventArgs e) {
            try {
                string str = srv.Text;
                if (!String.IsNullOrEmpty(str)) {
                    Uri uri = new Uri(str, UriKind.Absolute);
                    AppParameters.VkApiDomain = uri.Host;
                } else {
                    AppParameters.VkApiDomain = null;
                }
                errortext.Text = String.Empty;
            } catch (Exception ex) {
                errortext.Text = ex.Message;
            }
        }
    }
}