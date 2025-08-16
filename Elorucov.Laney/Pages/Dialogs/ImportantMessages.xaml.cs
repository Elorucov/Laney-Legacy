using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class ImportantMessages : Modal {
        ObservableCollection<LMessage> ImportantMessagesList = new ObservableCollection<LMessage>();
        ScrollViewer ListScrollViewer;
        int count = 0;
        int loadOffset = 0;

        public ImportantMessages() {
            this.InitializeComponent();
        }

        private void StartLoadMessages(object sender, RoutedEventArgs e) {
            if (ListScrollViewer == null) {
                ListScrollViewer = listView.GetScrollViewerFromListView();
                ListScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
            }
            listView.ContainerContentChanging += ListView_ContainerContentChanging;
            listView.ItemsSource = ImportantMessagesList;
            new System.Action(async () => {
                await LoadMessages();
            })();
        }

        private async Task LoadMessages() {
            if (progress.IsIndeterminate) return;
            int offset = loadOffset + ImportantMessagesList.Count;
            progress.IsIndeterminate = true;
            object resp = await Messages.GetImportantMessages(40, offset);
            if (resp is ImportantMessagesResponse imr) {
                AppSession.AddUsersToCache(imr.Profiles);
                AppSession.AddGroupsToCache(imr.Groups);
                AppSession.AddContactsToCache(imr.Contacts);

                if (ImportantMessagesList.Count == 0) Title = $"{Locale.Get("important_msgs_modal_m")} ({imr.Messages.Count})";
                count = imr.Messages.Count;
                foreach (var m in imr.Messages.Items) {
                    ImportantMessagesList.Add(new LMessage(m));
                }
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
            progress.IsIndeterminate = false;
        }

        private void ListScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 4) {
                    if (!progress.IsIndeterminate && ImportantMessagesList.Count != 0) new System.Action(async () => { await LoadMessages(); })();
                }
            }
        }

        private void ShowMsgContextMenu(UIElement sender, ContextRequestedEventArgs args) {
            FrameworkElement el = sender as FrameworkElement;
            LMessage msg = el.DataContext as LMessage;
            if (msg == null || msg.UISentMessageState == SentMessageState.Deleted || msg.IsExpired) return;
            args.Handled = true;

            MenuFlyout mf = new MenuFlyout();
            string ctext = string.Empty;

            if (AppParameters.AdvancedMessageInfo) {
                MenuFlyoutItem dbg = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = $"MID: {msg.Id} CMID: {msg.ConversationMessageId}" };
                dbg.Click += (a, b) => {
                    Dev.MessageJSONView mjv = new Dev.MessageJSONView(msg);
                    mjv.Show();
                };
                mf.Items.Add(dbg);
                mf.Items.Add(new MenuFlyoutSeparator());
            }

            MenuFlyoutItem gotom = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("go_to_message") };
            MenuFlyoutItem copyText = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_copy_text") };
            MenuFlyoutItem unmark = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_unmark") };

            gotom.Click += (a, b) => {
                Hide(msg);
            };
            copyText.Click += (a, b) => {
                DataPackage dp = new DataPackage();
                dp.RequestedOperation = DataPackageOperation.Copy;
                dp.SetText(ctext);
                Clipboard.SetContent(dp);
            };
            unmark.Click += async (a, b) => {
                object resp = await Messages.MarkAsImportant(msg.PeerId, msg.ConversationMessageId, false);
                if (resp is MarkAsImportantResponse) {
                    ImportantMessagesList.Remove(msg);
                    count--;
                    Title = $"{Locale.Get("important_msgs_modal_m")} ({count})";
                } else {
                    Functions.ShowHandledErrorTip(resp);
                }
            };

            mf.Items.Add(gotom);
            if (msg.TryGetMessageText(out ctext)) mf.Items.Add(copyText);
            mf.Items.Add(unmark);

            Point pos;
            bool posReached = args != null && args.TryGetPosition(el, out pos);

            if (mf.Items.Count > 0) {
                if (posReached) {
                    mf.ShowAt(el, pos);
                } else {
                    mf.ShowAt(el);
                }
            }
        }

        private void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args) {
            if (args.ItemContainer != null && args.Item != null) {
                LMessage msg = args.Item as LMessage;

                if (args.ItemContainer.ContentTemplateRoot is Border b) {
                    args.ItemContainer.BorderThickness = new Thickness(0);
                    b.Child = MessageUIHelper.Build(msg, null, null, true, true);
                }
            }
        }

        private void LoadWithOffset(object sender, RoutedEventArgs e) {
            bool success = int.TryParse(offset.Text, out loadOffset);
            if (!success) loadOffset = 0;
            ImportantMessagesList.Clear();
            new System.Action(async () => { await LoadMessages(); })();
        }

        private void offsetKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter) {
                e.Handled = true;
                LoadWithOffset(sender, e);
            }
        }
    }
}
