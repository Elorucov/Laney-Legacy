using Elorucov.Toolkit.UWP.Controls;
using System;
using Windows.UI.Xaml;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ModalWebView : Modal {
        Uri _uri;

        public ModalWebView(Uri uri, string title = null) {
            this.InitializeComponent();
            _uri = uri;
            if (!string.IsNullOrEmpty(title)) Title = title;
            Closed += (a, b) => web.Destroy();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            web.Navigate(_uri);
        }

        private void web_NavigationStarting(object sender, Uri uri) {
            ShowLoader();
        }

        private void web_ContentLoaded(object sender, Uri e) {
            HideLoader();
        }

        private void web_NavigationFailed(object sender, string e) {
            HideLoader(e);
        }

        private void ShowLoader() {
            web.Visibility = Visibility.Collapsed;
            infocontainer.Visibility = Visibility.Visible;
            progress.IsActive = true;
            errorinfo.Text = string.Empty;
        }

        private void HideLoader(string error = null) {
            web.Visibility = !string.IsNullOrEmpty(error) ? Visibility.Collapsed : Visibility.Visible;
            infocontainer.Visibility = !string.IsNullOrEmpty(error) ? Visibility.Visible : Visibility.Collapsed;
            progress.IsActive = false;
            if (!string.IsNullOrEmpty(error)) errorinfo.Text = error;
        }
    }
}
