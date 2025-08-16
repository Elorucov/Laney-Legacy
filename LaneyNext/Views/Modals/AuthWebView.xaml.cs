using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class AuthWebView : Modal
    {
        private Uri _requestUri;
        private Uri _callBackUri;

        public AuthWebView(Uri requestUri, Uri callbackUri)
        {
            this.InitializeComponent();
            _requestUri = requestUri;
            _callBackUri = callbackUri;
            Web.Loaded += Web_Loaded;
            Unloaded += WebSignUp_Unloaded;
        }

        private void Web_Loaded(object sender, RoutedEventArgs e)
        {
            Web.DOMContentLoaded += Web_DOMContentLoaded;
            Web.NavigationFailed += Web_NavigationFailed;
            Web.NavigationStarting += Web_NavigationStarting;

            Start();
        }

        private void WebSignUp_Unloaded(object sender, RoutedEventArgs e)
        {
            Web.DOMContentLoaded -= Web_DOMContentLoaded;
            Web.NavigationFailed -= Web_NavigationFailed;
            Web.NavigationStarting -= Web_NavigationStarting;

            Web.Loaded -= Web_Loaded;
            Unloaded -= WebSignUp_Unloaded;
        }

        private void Start()
        {
            Web.Navigate(_requestUri);
        }

        private void Web_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Web.Visibility = Visibility.Collapsed;
            Loading.Visibility = Visibility.Visible;
        }

        private void Web_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            if (args.Uri.Host == _callBackUri.Host && args.Uri.AbsolutePath == _callBackUri.AbsolutePath)
            {
                Hide(args.Uri);
            }
            else
            {
                Web.Visibility = Visibility.Visible;
                Loading.Visibility = Visibility.Collapsed;
            }
        }

        private async void Web_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            await (new MessageDialog($"Status: {e.WebErrorStatus};\nURL: {e.Uri}", $"Navigation failed!")).ShowAsync();
            Hide(null);
        }

        #region Static methods

        public static async Task<Uri> AuthenticateAsync(Uri requestUri, Uri callbackUri)
        {
            Uri uri = null;
            bool busy = true;

            var awm = new AuthWebView(requestUri, callbackUri);
            awm.Closed += (a, b) =>
            {
                if (b is Uri cbUri) uri = cbUri;
                busy = false;
            };
            awm.Show();

            return await Task.Run(() =>
            {
                while (busy)
                {
                    Task.Delay(100).ConfigureAwait(false);
                }
                return uri;
            });
        }

        #endregion
    }
}