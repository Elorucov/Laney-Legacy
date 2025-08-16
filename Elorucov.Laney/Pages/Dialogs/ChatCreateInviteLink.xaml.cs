using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class ChatCreateInviteLink : ContentDialog {
        long PeerId = 0;
        bool IsChannel = false;
        bool CanChange = false;
        DispatcherTimer typingtimer = new DispatcherTimer();

        public ChatCreateInviteLink(long peerId, bool isChannel, bool canChange) {
            this.InitializeComponent();
            PeerId = peerId;
            IsChannel = isChannel;
            CanChange = canChange;
            Link.IsReadOnly = !CanChange;
            Loaded += async (a, b) => {
                Title = IsChannel ? Locale.Get("chaninvdlg_header") : Locale.Get("chatinvdlg_header");
                Link.Header = IsChannel ? Locale.Get("chaninvdlg_desc") : Locale.Get("chatinvdlg_desc");
                await GetLink(false);

                typingtimer.Interval = TimeSpan.FromMilliseconds(1000);
                typingtimer.Tick += async (c, d) => await GetLink(false);
            };
        }

        private void GetLinkWithHistory(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await GetLink(false); })();
        }

        private async Task GetLink(bool reset) {
            if (!CanChange) return;
            if (typingtimer.IsEnabled) typingtimer.Stop();
            int vmc = 0;
            if (cb.IsChecked == true) {
                bool isInt = int.TryParse(VisibleMessagesCount.Text, out vmc);
                if (!isInt) {
                    Tips.Show(Locale.Get("addchatuser_vmc_err"));
                    return;
                }

                if (vmc < 0 || vmc > 1000) {
                    Tips.Show(Locale.Get("addchatuser_vmc_err"));
                    return;
                }
            }

            ShowHideContent(false);

            object resp = await Messages.GetInviteLink(PeerId, reset, vmc);
            if (resp is ChatLink) {
                ChatLink link = resp as ChatLink;
                Link.Text = link.Link;
                ShowHideContent(true);
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        private void ShowHideContent(bool show) {
            Progress.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            Link.IsEnabled = show;
            cb.IsEnabled = show;
            IsPrimaryButtonEnabled = show;
            IsSecondaryButtonEnabled = show;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            args.Cancel = true;
            new System.Action(async () => { await GetLink(true); })();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            var dataPackage = new DataPackage();
            dataPackage.SetText(Link.Text);
            Clipboard.SetContent(dataPackage);
        }

        private void VisibleMessagesCount_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args) {
            if (typingtimer.IsEnabled) typingtimer.Stop();
            if (!string.IsNullOrEmpty(sender.Text)) typingtimer.Start();
        }
    }
}