using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.Views.Modals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ConversationView : Page
    {
        private ConversationViewModel ViewModel { get { return DataContext as ConversationViewModel; } }
        private Shell AppShell { get { return Tag as Shell; } }
        UISettings uis = new UISettings();
        DispatcherTimer MarkAsReadTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };

        public ConversationView()
        {
            this.InitializeComponent();
            Loaded += (a, b) =>
            {
                if (AppShell != null)
                {
                    MainHeader.Margin = new Thickness(0, AppShell.TitleBarHeight, 0, 0);
                    MultiselectHeader.Margin = new Thickness(0, AppShell.TitleBarHeight, 0, 0);
                    AppShell.TitleBarHeightChanged += (c, d) =>
                    {
                        MainHeader.Margin = new Thickness(0, d, 0, 0);
                        MultiselectHeader.Margin = new Thickness(0, d, 0, 0);
                    };
                }

                if (AppShell == null)
                { // Standalone conversation view
                    MainHeader.DetectNonSafeArea = true;
                    MultiselectHeader.DetectNonSafeArea = true;
                    WindowName.Visibility = Visibility.Visible;
                    WindowName.Text = VKSession.Current.DisplayName;
                    ApplicationView.GetForCurrentView().Consolidated += async (c, d) =>
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            Window.Current.Close();
                        });
                    };
                }

                // Mentions picker
                MentionsHelper.RegisterMentionsPickerForCurrentView(mentionsPicker);

                ScrollViewer sv = MessagesListView.ScrollingHost;
                if (sv != null)
                {
                    sv.VerticalContentAlignment = VerticalAlignment.Bottom;
                    sv.VerticalAlignment = VerticalAlignment.Bottom;
                }

                // Drag'n'drop
                this.AllowDrop = true;
                this.DragOver += ShowDropArea;
                this.DragLeave += HideDropArea;
                this.Drop += HideDropArea;
                DropDoc.DragOver += DocDragOver;
                DropImg.DragOver += ImgDragOver;
                DropVid.DragOver += VidDragOver;

                // Snackbar
                VKSession.Current.LongPoll.BotCallbackReceived += ShowSnackbar;
            };

            MarkAsReadTimer.Tick += async (a, b) =>
            {
                try
                {
                    if (ViewModel == null || ViewModel.LastVisibleMessage == null) return;
                    MessageViewModel mvm = ViewModel.LastVisibleMessage;
                    if ((mvm.State == MessageVMState.Unread && mvm.SenderId != VKSession.Current.SessionId) || ViewModel.IsMarkedAsUnread)
                    {
                        await VKSession.Current.API.Messages.MarkAsReadAsync(VKSession.Current.GroupId, ViewModel.Id, mvm.Id);
                    }
                }
                catch (Exception ex)
                {
                    Log.General.Error("MarkAsReadTimer error!", ex);
                }
            };
            MarkAsReadTimer.Start();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is SingleConversationViewData scvd)
            {
                VKSession.BindSessionToCurrentView(scvd.Session);
                ViewManagement.AddToStandaloneConversations(scvd.Conversation);
                Window.Current.Closed += (a, b) => ViewManagement.RemoveFromStandaloneConversations(scvd.Conversation);
                DataContext = scvd.Conversation;
                ViewModel.LoadMessages();
            }
        }

        #region UI and events

        private void ConversationView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BackButton.Visibility = AppShell != null && !AppShell.IsWide ? Visibility.Visible : Visibility.Collapsed;
            ConversationInfoWide.Visibility = e.NewSize.Width > 576 ? Visibility.Visible : Visibility.Collapsed;
            ConversationInfoNarrow.Visibility = e.NewSize.Width > 576 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void MessagesListView_Scrolling(object sender, Tuple<double, double> e)
        {
            FrameworkElement el = sender as FrameworkElement;

            DebugMsgIDs.Visibility = Core.Settings.DebugShowMessagesListScrollInfo ? Visibility.Visible : Visibility.Collapsed;
            double t = e.Item1; // Total
            double s = e.Item2; // Current position
            double a = el.ActualHeight;
            DebugScrollInfo.Text = $"{Math.Round(s, 0)} / {Math.Round(t, 0)}";

            // Go to last msg button
            GetToLastMsgButton.Visibility = NeedShowDownButton(t, s, a) ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        private void GoBack(object sender, RoutedEventArgs e)
        {
            if (MessagesListView.SelectionMode == ListViewSelectionMode.Multiple)
            {
                MessagesListView.SelectedItems.Clear();
                return;
            }
            ;
            AppShell.SwitchFrame(false);
        }

        private bool NeedShowDownButton(double t, double s, double a)
        {
            if (ViewModel == null || ViewModel.Messages.Count == 0 || ViewModel.LastMessage == null || ViewModel.IsLoading) return false;
            if (ViewModel.Messages.Last().Id != ViewModel.LastMessage.Id && ViewModel.Messages.Last().Id != Int32.MaxValue) return true;
            if (t != 0 && s < t - a) return true;
            return false;
        }

        private void ShowSnackbar(object sender, LPBotCallback e)
        {
            if (ViewModel.Id == e.PeerId && e.Action.Type == LPBotCallbackActionType.ShowSnackbar)
            {
                if (ViewModel.PeerType == ELOR.VKAPILib.Objects.PeerType.Chat)
                {
                    var group = CacheManager.GetGroup(e.OwnerId);
                    BotCBSnackbar.BeforeAvatar = group.Photo;
                }
                BotCBSnackbar.Content = e.Action.Text;
                BotCBSnackbar.Show(10000);
            }
        }

        #region Context menu

        private void ShowContextMenuHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Touch && e.HoldingState == Windows.UI.Input.HoldingState.Started)
                if (e.OriginalSource is FrameworkElement) ShowContextMenu((FrameworkElement)e.OriginalSource);
        }

        private void ShowContextMenuRight(object sender, RightTappedRoutedEventArgs e)
        {
            if (UIViewSettings.GetForCurrentView().UserInteractionMode != UserInteractionMode.Touch)
                if (e.OriginalSource is FrameworkElement) ShowContextMenu((FrameworkElement)e.OriginalSource);
        }

        private void ShowContextMenu(FrameworkElement el)
        {
            if (MessagesListView.SelectionMode == ListViewSelectionMode.None)
            {
                MessageViewModel msg = null;

                string rms = String.Empty;
                try
                {
                    if (el is ListViewItem lvi)
                    {
                        msg = (lvi.ContentTemplateRoot as Control).DataContext as MessageViewModel;
                        if (lvi.Tag != null && lvi.Tag is MessageView mw) rms = $", R: {mw.RenderMilliseconds} ms";
                    }
                    else
                    {
                        msg = el.DataContext as MessageViewModel;
                    }
                }
                catch { }

                if (msg == null ||
                    msg.State == MessageVMState.Deleted ||
                    msg.State == MessageVMState.Sending ||
                    msg.State == MessageVMState.Failed) return;

                VK.VKUI.Popups.MenuFlyout mf = new VK.VKUI.Popups.MenuFlyout();
                CellButton dbg = new CellButton { Icon = VKIconName.Icon28BugOutline, Text = $"MID: {msg.Id}, CID: {msg.ConversationMessageId}{rms}" };
                CellButton edit = new CellButton { Icon = VKIconName.Icon28EditOutline, Text = Locale.Get("msg_ctx_edit") };
                CellButton reply = new CellButton { Icon = VKIconName.Icon28ReplyOutline, Text = Locale.Get("msg_ctx_reply") };
                CellButton repriv = new CellButton { Icon = VKIconName.Icon28MessageOutline, Text = Locale.Get("msg_ctx_repriv") };
                CellButton forward = new CellButton { Icon = VKIconName.Icon28ShareOutline, Text = Locale.Get("msg_ctx_forward") };
                CellButton pin = new CellButton { Icon = VKIconName.Icon28PinOutline, Text = Locale.Get("msg_ctx_pin") };
                CellButton star = new CellButton { Icon = VKIconName.Icon28FavoriteOutline, Text = Locale.Get("msg_ctx_star") };
                CellButton unstar = new CellButton { Icon = VKIconName.Icon28Favorite, Text = Locale.Get("msg_ctx_unstar") };
                CellButton spam = new CellButton { Icon = VKIconName.Icon28ReportOutline, Text = Locale.Get("msg_ctx_spam") };
                CellButton delete = new CellButton { Icon = VKIconName.Icon28DeleteOutline, Text = Locale.Get("msg_ctx_delete") };

                dbg.Click += (a, b) => Utils.ShowUnderConstructionInfo();
                edit.Click += (a, b) => ViewModel.MessageInput.StartEditing(msg);
                reply.Click += (a, b) => ViewModel.MessageInput.ReplyMessage = msg;
                repriv.Click += (a, b) => ReplyToPrivateMessage(msg);
                forward.Click += (a, b) => ShowMsgForwardingModal(new List<MessageViewModel> { msg });
                forward.RightTapped += (a, b) => { if (ViewModel.MessageInput.ForwardedMessagesCount < 100) ViewModel.MessageInput.AttachForwardedMessage(msg); mf.Hide(); };
                pin.Click += (a, b) => PinMessage(msg);
                star.Click += (a, b) => MarkAsImportant(msg, true);
                unstar.Click += (a, b) => MarkAsImportant(msg, false);
                spam.Click += (a, b) => ShowMessageDeleteConfirmation(new List<MessageViewModel> { msg }, true);
                delete.Click += (a, b) => ShowMessageDeleteConfirmation(new List<MessageViewModel> { msg });

                bool isServiceMessage = msg.Action != null || msg.IsExpired;
                bool canWrite = ViewModel.CanWrite.Allowed;

                if (Core.Settings.DebugShowMessageIdCtx) mf.Items.Add(dbg);
                if (!isServiceMessage && msg.SenderId == VKSession.Current.SessionId && !msg.HasSticker() && msg.SentDateTime.AddDays(1) > DateTime.Now && canWrite) mf.Items.Add(edit);
                if (!isServiceMessage && canWrite) mf.Items.Add(reply);
                if (!isServiceMessage && msg.SenderId > 0 && msg.PeerId > 2000000000)
                { // Check can we write private message to sender
                    var sender = CacheManager.GetUser(msg.SenderId);
                    if (sender != null && sender.CanWritePrivateMessage) mf.Items.Add(repriv);
                }
                if (!isServiceMessage && msg.TTL == 0) mf.Items.Add(forward);
                if (!isServiceMessage && ViewModel.ChatSettings != null && ViewModel.ChatSettings.ACL.CanChangePin && msg.TTL == 0) mf.Items.Add(pin);
                if (!isServiceMessage && msg.TTL == 0) mf.Items.Add(msg.IsImportant ? unstar : star);
                if (!isServiceMessage && msg.SenderId == VKSession.Current.SessionId) mf.Items.Add(spam);
                if (!isServiceMessage) mf.Items.Add(delete);

                // Clicked FrameworkElement
                //CellButton ife = new CellButton { Icon = VKIconName.Icon28BugOutline, Text = $"{el.GetType().Name}" };
                //if (!String.IsNullOrEmpty(el.Name)) ife.Text += $" — {el.Name}";
                //mf.Items.Add(ife);

                mf.ShowAt(el);
            }
        }

        #endregion

        #region Multiselect

        private void MessagesListItemClick(object sender, ItemClickEventArgs e)
        {
            MessageViewModel msg = e.ClickedItem as MessageViewModel;
            if (MessagesListView.SelectionMode != ListViewSelectionMode.Multiple && msg.Action == null)
            {
                MessagesListView.SelectionMode = ListViewSelectionMode.Multiple;
                MessagesListView.SelectionChanged += MessagesSelectionChanged;
                MessagesListView.SelectedItem = e.ClickedItem;
            }
        }

        private void MessagesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int count = MessagesListView.SelectedItems.Count;
            if (count == 0 && MessagesListView.SelectionMode == ListViewSelectionMode.Multiple)
            {
                MessagesListView.SelectionChanged -= MessagesSelectionChanged;
                MessagesListView.SelectionMode = ListViewSelectionMode.None;
                MultiselectHeader.Visibility = Visibility.Collapsed;
                MainHeader.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                if (e.AddedItems.Count > 0)
                {
                    object last = e.AddedItems.Last();
                    MessageViewModel lastmsg = last as MessageViewModel;
                    if (lastmsg.Action != null)
                    {
                        MessagesListView.SelectedItems.Remove(last);
                        return;
                    }
                }

                MainHeader.Visibility = Visibility.Collapsed;
                MultiselectHeader.Visibility = Visibility.Visible;
                MultiselectHeader.Content = count.ToString();

                bool hasMyMessages = MessagesListView.SelectedItems.Where(m => ((MessageViewModel)m).SenderId == VKSession.Current.SessionId).Count() > 0;
                bool hasExpiredMessage = MessagesListView.SelectedItems.Where(m => ((MessageViewModel)m).IsExpired).Count() > 0;
                bool isCasperChat = ViewModel.ChatSettings?.IsDisappearing == true;

                List<MessageViewModel> SelectedMessages = MessagesListView.SelectedItems.Select(m => m as MessageViewModel).ToList();

                MultiselectHeader.RightButtons.Clear();

                PageHeaderButton replybtn = new PageHeaderButton { Icon = VKIconName.Icon28ReplyOutline, Text = Locale.Get("msg_ctx_reply") };
                PageHeaderButton fwdherebtn = new PageHeaderButton { Icon = VKIconName.Icon28ReplyOutline, Text = Locale.Get("msg_ctx_forward_here") };
                PageHeaderButton deletebtn = new PageHeaderButton { Icon = VKIconName.Icon28DeleteOutline, Text = Locale.Get("msg_ctx_delete") };
                PageHeaderButton forwardbtn = new PageHeaderButton { Icon = VKIconName.Icon28ArrowRightOutline, Text = Locale.Get("msg_ctx_forward") };
                PageHeaderButton spambtn = new PageHeaderButton { Icon = VKIconName.Icon28ReportOutline, Text = Locale.Get("msg_ctx_spam") };

                replybtn.Click += (a, b) => ViewModel.MessageInput.ReplyMessage = SelectedMessages[0];
                fwdherebtn.Click += (a, b) => { ViewModel.MessageInput.AttachForwardedMessages(SelectedMessages); MessagesListView.SelectedItems.Clear(); };
                deletebtn.Click += (a, b) => ShowMessageDeleteConfirmation(SelectedMessages);
                forwardbtn.Click += (a, b) => ShowMsgForwardingModal(SelectedMessages);
                spambtn.Click += (a, b) => ShowMessageDeleteConfirmation(SelectedMessages, true);

                if (count == 1 && !hasExpiredMessage) MultiselectHeader.RightButtons.Add(replybtn);
                if (count > 1 && count <= 100 && !isCasperChat && !hasExpiredMessage && ViewModel.MessageInput.ReplyMessage == null) MultiselectHeader.RightButtons.Add(fwdherebtn);
                MultiselectHeader.RightButtons.Add(deletebtn);
                if (count <= 100 && !isCasperChat && !hasExpiredMessage) MultiselectHeader.RightButtons.Add(forwardbtn);
                if (!hasMyMessages && !hasExpiredMessage) MultiselectHeader.RightButtons.Add(spambtn);
            }
        }

        #endregion

        #region Drag'n'Drop

        private async void ShowDropArea(object sender, DragEventArgs e)
        {
            Debug.WriteLine("ShowDropArea");
            if (ViewModel == null) return;
            e.AcceptedOperation = DataPackageOperation.None;
            e.DragUIOverride.IsContentVisible = true;

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                bool isImage = await DataPackageViewHelpers.HasOnlyImageFiles(e.DataView);
                bool isVideo = await DataPackageViewHelpers.HasOnlyVideoFiles(e.DataView);
                DropArea.Visibility = Visibility.Visible;
                DropDocPlaceholder.Content = Locale.Get("drop_doc");
                if (isImage) DropDocPlaceholder.Content = Locale.Get("drop_photo_doc");
                if (isVideo) DropDocPlaceholder.Content = Locale.Get("drop_video_doc");
                DropDoc.Visibility = Visibility.Visible;
                DropImg.Visibility = isImage ? Visibility.Visible : Visibility.Collapsed;
                DropVid.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
                DropArea.RowDefinitions[1].Height = new GridLength(isImage || isVideo ? 1.5 : 0, GridUnitType.Star);
            }
        }

        private void HideDropArea(object sender, DragEventArgs e)
        {
            Debug.WriteLine("HideDropArea");
            DropDoc.Visibility = Visibility.Collapsed;
            DropImg.Visibility = Visibility.Collapsed;
            DropVid.Visibility = Visibility.Collapsed;
            DropArea.Visibility = Visibility.Collapsed;
        }

        private void DocDragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine("DocDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void ImgDragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine("ImgDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void VidDragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine("ImgDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void DropToDoc(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed;
            ViewModel.MessageInput.AttachFromDataPackageView(e.DataView);
        }

        private void DropToImg(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed;
            ViewModel.MessageInput.AttachFromDataPackageViewNonDoc(e.DataView, ViewModels.Controls.OutboundAttachmentUploadFileType.Photo);
        }

        private void DropToVid(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed;
            ViewModel.MessageInput.AttachFromDataPackageViewNonDoc(e.DataView, ViewModels.Controls.OutboundAttachmentUploadFileType.Video);
        }

        #endregion

        #region Context menu and multiselect functions

        private async void PinMessage(MessageViewModel msg)
        {
            try
            {
                var resp = await VKSession.Current.API.Messages.PinAsync(VKSession.Current.GroupId, ViewModel.Id, msg.Id);
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) PinMessage(msg);
            }
        }

        private async void MarkAsImportant(MessageViewModel msg, bool important)
        {
            try
            {
                var resp = await VKSession.Current.API.Messages.MarkAsImportantAsync(new List<int> { msg.Id }, important);
                if (resp.First() == msg.Id) msg.IsImportant = important;
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) MarkAsImportant(msg, important);
            }
        }

        private async void ReplyToPrivateMessage(MessageViewModel msg)
        {
            if (AppShell != null)
            {
                VKSession.Current.SessionBase.SwitchToConversation(msg.SenderId, new List<MessageViewModel> { msg });
            }
            else
            {
                var view = await ViewManagement.GetViewBySession(VKSession.Current);
                ViewManagement.SwitchToView(view, () =>
                {
                    VKSession.Current.SessionBase.SwitchToConversation(msg.SenderId, new List<MessageViewModel> { msg });
                });
            }
        }

        private async void ShowMsgForwardingModal(List<MessageViewModel> msgs)
        {
            if (AppShell != null)
            {
                InternalSharing ish = new InternalSharing(msgs);
                ish.Show();
            }
            else
            {
                var view = await ViewManagement.GetViewBySession(VKSession.Current);
                ViewManagement.SwitchToView(view, () =>
                {
                    InternalSharing ish = new InternalSharing(msgs);
                    ish.Show();
                });
            }
        }

        #endregion

        #region Delete messages

        private async void ShowMessageDeleteConfirmation(List<MessageViewModel> messages, bool spam = false)
        {
            int sid = VKSession.Current.SessionId;
            int count = messages.Count;
            int a = messages.Where(m => m.SenderId == sid && ViewModel.Id != sid && m.SentDateTime.AddDays(1) > DateTime.Now).Count();
            bool ableToDeleteAll = a == messages.Count && !spam;

            CheckBox cb = new CheckBox
            {
                Content = Locale.Get("messagedelete_dialog_for_all"),
                Style = (Style)Application.Current.Resources["VKCheckBox"],
                Margin = new Thickness(0, 8, 0, 0)
            };

            Alert alert = new Alert
            {
                Header = spam ? Locale.Get("messagespam_dialog_title") : Locale.Get(count > 1 ? "messagedelete_dialog_title_multi" : "messagedelete_dialog_title_single"),
                Text = String.Format(Locale.GetDeclensionForFormat(count, spam ? "messagespam_dialog" : "messagedelete_dialog"), count),
                Content = ableToDeleteAll ? cb : null,
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                int dt = spam ? 0 : 1;
                if (cb.IsChecked == true) dt = 2;
                DeleteMessages(messages.Select(m => m.Id).ToList(), dt);
            }
        }

        private async void DeleteMessages(List<int> ids, int method)
        {
            try
            {
                Dictionary<string, int> result = null;
                switch (method)
                {
                    case 0: result = await VKSession.Current.API.Messages.DeleteAsync(VKSession.Current.GroupId, ids, true, false); break;
                    case 1: result = await VKSession.Current.API.Messages.DeleteAsync(VKSession.Current.GroupId, ids, false, false); break;
                    case 2: result = await VKSession.Current.API.Messages.DeleteAsync(VKSession.Current.GroupId, ids, false, true); break;
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync(ex, true);
            }
        }

        #endregion

        #region Stickers suggestions

        private void StickersSuggestionList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Sticker sticker = e.ClickedItem as Sticker;
            if (sticker.StickerId <= 0)
            {
                VKSession.Current.SessionBase.SwitchToConversation(-184940019);
            }
            else
            {
                ViewModel.MessageInput.SendSticker(sticker);
                ViewModel.StickersSuggestions = null;
            }
        }

        #endregion
    }
}