using Elorucov.Laney.Services.Common;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class LinkEditorDialog : ContentDialog {
        public LinkEditorDialog() {
            this.InitializeComponent();
        }

        public LinkEditorDialog(string text, string link) {
            this.InitializeComponent();
            _text = text;
            _link = link;
        }

        string _text = null;
        string _link = null;

        public string Link { get; private set; }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e) {
            Title = Locale.Get(string.IsNullOrEmpty(_link) ? "TextCommandLabelLink" : "TextCommandLabelEditLink");
            PrimaryButtonText = Locale.Get("save");
            SecondaryButtonText = Locale.Get("delete");
            LinkTB.Header = _text.Substring(0, Math.Min(50, _text.Length));
            LinkTB.Text = _link;
        }

        private void Link_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args) {
            IsPrimaryButtonEnabled = Uri.IsWellFormedUriString(LinkTB.Text, UriKind.Absolute);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            Link = Uri.IsWellFormedUriString(LinkTB.Text, UriKind.Absolute) ? LinkTB.Text : null;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            Link = null;
        }
    }
}
