using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
// using Microsoft.Web.WebView2.Core;
using System;
using Windows.UI.Xaml.Controls;

// Как же сильно я хочу отказаться от поддержки старых версий винды вместе с винмобайлом,
// чтобы нормально, без костылей, юзать новые фичи...

namespace Elorucov.Laney.Controls {
    public sealed class CustomWebView : Control {
        // bool canUseNew = Functions.GetOSBuild() >= 17763 && !AppParameters.AlwaysUseLegacyWebView;
        bool canUseNew = false;

        public CustomWebView() {
            this.DefaultStyleKey = typeof(CustomWebView);
        }

        Border Root;
        Uri startUri = null;

        protected override void OnApplyTemplate() {
            base.OnApplyTemplate();
            Root = (Border)GetTemplateChild(nameof(Root));
            InitWebView();
        }

        private void InitWebView() {
            try {
                if (canUseNew) {
                    //Log.Info($"CustomWebView: using WebView2 (chromium-based msedge)");
                    //var web2 = new WebView2();
                    //Root.Child = web2;

                    //web2.CoreWebView2Initialized += (a, b) => {
                    //    bool ok = web2.CoreWebView2 != null;
                    //    Log.Info($"CustomWebView: WebView2 initialized! ok: {ok}");
                    //    if (ok) {
                    //        web2.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                    //        web2.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                    //        web2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                    //        if (startUri != null) web2.CoreWebView2.Navigate(startUri.ToString());
                    //    }
                    //};

                    //web2.EnsureCoreWebView2Async();
                } else {
                    Log.Info($"CustomWebView: using legacy msedge WebView");
                    var web1 = new WebView();
                    Root.Child = web1;

                    web1.NavigationStarting += Web1_NavigationStarting;
                    web1.DOMContentLoaded += Web1_DOMContentLoaded;
                    web1.NavigationFailed += Web1_NavigationFailed;
                }

                Unloaded += CustomWebView_Unloaded;
            } catch (Exception ex) {
                Log.Error($"CustomWebView: initialization error! is webview2: {canUseNew}, hr: 0x{ex.HResult.ToString("x8")}");
                Functions.ShowHandledErrorDialog(ex);
                if (canUseNew) {
                    // AppParameters.AlwaysUseLegacyWebView = true;
                    InitWebView();
                }
            }
        }

        private void CustomWebView_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            if (canUseNew) {
                //var web2 = Root.Child as WebView2;
                //var cvw = web2.CoreWebView2;
                //if (cvw != null) {
                //    cvw.NavigationStarting -= CoreWebView2_NavigationStarting;
                //    cvw.DOMContentLoaded -= CoreWebView2_DOMContentLoaded;
                //    cvw.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                //    cvw.Stop();
                //}
            } else {
                var web1 = Root.Child as WebView;
                if (web1 != null) {
                    web1.NavigationStarting -= Web1_NavigationStarting;
                    web1.DOMContentLoaded -= Web1_DOMContentLoaded;
                    web1.NavigationFailed -= Web1_NavigationFailed;
                }
            }
            Unloaded -= CustomWebView_Unloaded;
        }

        //private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args) {
        //    NavigationStarting?.Invoke(this, new Uri(args.Uri));
        //}

        //private void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args) {
        //    ContentLoaded?.Invoke(this, new Uri(sender.Source));
        //}

        //private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args) {
        //    if (!args.IsSuccess) {
        //        NavigationFailed?.Invoke(this, args.WebErrorStatus.ToString());
        //    }
        //}

        private void Web1_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {
            NavigationStarting?.Invoke(this, args.Uri);
        }

        private void Web1_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args) {
            ContentLoaded?.Invoke(this, args.Uri);
        }

        private void Web1_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e) {
            NavigationFailed?.Invoke(this, e.WebErrorStatus.ToString());
        }

        #region Events

        public event EventHandler<Uri> NavigationStarting;
        public event EventHandler<Uri> ContentLoaded;
        public event EventHandler<string> NavigationFailed;

        #endregion

        #region Methods

        public void Navigate(Uri destination) {
            if (canUseNew) {
                //var web2 = Root.Child as WebView2;
                //if (web2.CoreWebView2 != null) {
                //    web2.CoreWebView2.Navigate(destination.AbsolutePath);
                //} else {
                //    startUri = destination;
                //}
            } else {
                var web1 = Root.Child as WebView;
                web1?.Navigate(destination);
            }
        }

        public void Destroy() {
            if (Root == null || Root.Child == null) return;
            if (canUseNew) {
                //var web2 = Root.Child as WebView2;
                //if (web2.CoreWebView2 != null) {
                //    web2.CoreWebView2.Stop();
                //}
                //web2.Close();
            } else {
                var web1 = Root.Child as WebView;
                web1?.NavigateToString("");
            }
            Root.Child = null;
        }

        //public void TryClearCookiesForWV2() {
        //    if (canUseNew) {
        //        var web2 = Root.Child as WebView2;
        //        if (web2.CoreWebView2 == null) await web2.EnsureCoreWebView2Async();
        //        web2.CoreWebView2.CookieManager.DeleteAllCookies();
        //    }
        //}

        #endregion
    }
}