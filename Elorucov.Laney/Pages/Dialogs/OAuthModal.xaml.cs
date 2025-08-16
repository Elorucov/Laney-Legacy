using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Logger;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

// From old L2 for UWP.

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class OAuthModal : Modal {
        private Uri _requestUri;
        private Uri _callBackUri;

        public OAuthModal(Uri requestUri, Uri callbackUri) {
            this.InitializeComponent();
            _requestUri = requestUri;
            _callBackUri = callbackUri;
            Web.Loaded += Web_Loaded;
            Unloaded += WebSignUp_Unloaded;
        }
        private void Web_Loaded(object sender, RoutedEventArgs e) {
            Web.DOMContentLoaded += Web_DOMContentLoaded;
            Web.NavigationFailed += Web_NavigationFailed;
            Web.NavigationStarting += Web_NavigationStarting;

            Start();
        }

        private void WebSignUp_Unloaded(object sender, RoutedEventArgs e) {
            Web.DOMContentLoaded -= Web_DOMContentLoaded;
            Web.NavigationFailed -= Web_NavigationFailed;
            Web.NavigationStarting -= Web_NavigationStarting;

            Web.Loaded -= Web_Loaded;
            Unloaded -= WebSignUp_Unloaded;
        }

        private void Start() {
            var req = new HttpRequestMessage(HttpMethod.Get, _requestUri);
            // Этот юзер-агент позволяет принудительно отобразить старую страницу авторизации (точнее, подтверждения)
            // P. S. с апреля и это перестало работать.
            req.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows Phone 8.0; Trident/6.0; IEMobile/10.0; ARM; Touch; NOKIA; Lumia 520)");
            Web.NavigateWithHttpRequestMessage(req);
        }

        private void Web_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {
            Web.Visibility = Visibility.Collapsed;
            Loading.Visibility = Visibility.Visible;
        }

        private async void Web_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args) {
            Debug.WriteLine($"DOMContentLoaded: {args.Uri}");
            try {
                if (args.Uri.Host == _callBackUri.Host && args.Uri.AbsolutePath == _callBackUri.AbsolutePath) {
                    Hide(args.Uri);
                    return;
                } else {
                    Web.Visibility = Visibility.Visible;
                    Loading.Visibility = Visibility.Collapsed;
                }

                if (args.Uri.AbsoluteUri.Contains("https://oauth.vk.com/")) {
                    // string code1 = "document.getElementById(\"allow_btn\").click();";
                    string code1 = "try { window.location = document.getElementById(\"allow_btn\").href; } catch {}";
                    await Web.InvokeScriptAsync("eval", new string[] { code1 });
                }
            } catch (Exception ex) {
                if (ex.HResult.ToString("x8") != "80020101") {
                    await new MessageDialog(ex.Message, $"Error 0x{ex.HResult.ToString("x8")}").ShowAsync();
                    Hide(null);
                }
            }
        }

        private async void Web_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e) {
            Web.Visibility = Visibility.Visible;
            Loading.Visibility = Visibility.Collapsed;
            Logger.Write(LogImportance.Error, $"OAuthModal: Failed to navigate to {e.Uri}");
            await new MessageDialog($"Status: {e.WebErrorStatus};\nURL: {e.Uri}", $"Navigation failed!").ShowAsync();
            Hide(null);
        }

        #region Static methods

        public static async Task<Uri> AuthAsync(Uri requestUri, Uri callbackUri) {
            Uri uri = null;
            bool busy = true;

            var awm = new OAuthModal(requestUri, callbackUri);
            awm.Closed += (a, b) => {
                if (b is Uri cbUri) uri = cbUri;
                busy = false;
            };
            awm.Show();

            // TODO: use ManuallyResetEventSlim instead this strange crap from 2020.
            return await Task.Run(() => {
                while (busy) {
                    Task.Delay(500).ConfigureAwait(false);
                }
                APIHelper.ClearWebViewCookies();
                return uri;
            });
        }

        #endregion
    }
}
