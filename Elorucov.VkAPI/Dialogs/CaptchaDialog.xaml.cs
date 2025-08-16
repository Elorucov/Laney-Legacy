using Elorucov.VkAPI.Objects;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.VkAPI.Dialogs {
    internal sealed partial class CaptchaDialog : ContentDialog {
        internal string CaptchaText = "";

        internal CaptchaDialog(VKError err) {
            this.InitializeComponent();
            CaptchaText = "";
            Loaded += (a, b) => {
                img.Source = new BitmapImage(new Uri(err.captcha_img));
            };
        }

        private void Change(TextBox sender, TextBoxTextChangingEventArgs args) {
            if (!String.IsNullOrEmpty(sender.Text)) CaptchaText = sender.Text;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            cptch.Text = "";
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            cptch.Text = "";
        }
    }
}
