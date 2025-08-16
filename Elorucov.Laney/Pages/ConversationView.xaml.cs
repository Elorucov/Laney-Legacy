using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Pages.Popups;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ConversationView : Page {
        WindowType windowType = ViewManagement.GetWindowType();

        public ConversationView() {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}, window type: {windowType}.");
            ConvInfoContainer.Visibility = windowType == WindowType.ContactPanel ? Visibility.Collapsed : Visibility.Visible;
            ContextMenuBtn.Visibility = windowType == WindowType.Hosted ? Visibility.Collapsed : Visibility.Visible;
            if (Theme.IsMicaAvailable) {
                LayoutRoot.Background = new SolidColorBrush(Colors.Transparent);
                FindName(nameof(MicaSemiTransparent));
            }

            Loaded += (a, b) => {
                DataContextChanged += Page_DataContextChanged;
                ViewModel = AppSession.CurrentConversationVM;

                // Bottom controls(a.k.a.write bar) shadows
                List<UIElement> receivers = new List<UIElement> { LayoutRoot, ChatBackground };
                if (MicaSemiTransparent != null) receivers.Add(MicaSemiTransparent);
                Services.UI.Shadow.TryDrawUsingThemeShadow(BottomControls, BottomControlsShadow, receivers, 6);

                // Drag'n'drop
                this.AllowDrop = true;
                this.DragOver += ShowDropArea;
                this.DragLeave += HideDropArea;
                this.Drop += HideDropArea;
                DropDoc.DragOver += DocDragOver;
                DropImg.DragOver += ImgDragOver;
                DropVid.DragOver += VidDragOver;

                // Background
                ChatThemeService.RegisterBackgroundElement(ChatBackground);
                ChatThemeService.RegisterChatRootElement(this);
                Theme.ThemeChanged += (c, d) => CheckChatTheme(true);

                // Hotkeys
                CoreApplication.GetCurrentView().CoreWindow.KeyDown += CoreWindow_KeyDown;
            };

            MentionsHelper.RegisterMentionsPickerForCurrentView(mentionsPicker);
            if (!AppParameters.DontSendMarkAsRead) InitMarkAsReadTimer();

            var viewprops = CoreApplication.GetCurrentView().Properties;
            if (viewprops.ContainsKey("conversation_view")) {
                Log.Warn($"CoreApplicationView already contains a ConversationView instance!");
                viewprops["conversation_view"] = this;
            } else {
                CoreApplication.GetCurrentView().Properties.Add("conversation_view", this);
            }
            Unloaded += (a, b) => {
                CoreApplication.GetCurrentView().Properties.Remove("conversation_view");
                CoreApplication.GetCurrentView().CoreWindow.KeyDown -= CoreWindow_KeyDown;
                dthTimer.Tick -= DthTimer_Tick;
            };

            if (Main.GetCurrent() != null) {
                CheckBackButton();
                Main.GetCurrent().SizeChanged += (a, b) => CheckBackButton();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            DataContext = e.Parameter as ConversationViewModel;
        }

        DispatcherTimer dthTimer = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds(2),
        };

        static readonly bool IsMsgRenderingEnabled = AppParameters.MessageRenderingPhase;

        public ConversationViewModel ViewModel { get; private set; }
        public static ConversationView Current { get { return GetInstance(); } }

        private static ConversationView GetInstance() {
            var coreapp = CoreApplication.GetCurrentView();
            if (coreapp.Properties.ContainsKey("conversation_view") && coreapp.Properties["conversation_view"] is ConversationView cv) return cv;
            return null;
        }

        List<long> ScrollEventRegisteredConvIds = new List<long>();
        List<long> MsgStateChangedEventRegisteredConvIds = new List<long>();

        private void Page_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            ConversationViewModel cvm = args.NewValue as ConversationViewModel;
            long oldcvmid = ViewModel != null ? ViewModel.ConversationId : 0;
            long newcvmid = cvm != null ? cvm.ConversationId : 0;

            if (oldcvmid == newcvmid) {
                args.Handled = true;
                return;
            }

            if (ViewModel != null) {
                ViewModel.ShowSnackbarRequested = null;
                ViewModel.PropertyChanged -= OnVMPropertyChanged;
            }

            ItemsStackPanel isp = chatListView.ItemsPanelRoot as ItemsStackPanel;
            if (isp != null) {
                isp.ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepLastItemInView;
            }

            ViewModel = cvm;
            PinnedMessageControl.Message = ViewModel?.PinnedMessage;
            PinnedMsgBtn.Visibility = ViewModel?.PinnedMessage == null ? Visibility.Collapsed : Visibility.Visible;
            CheckAndShowStickersSuggestions(null, true);
            ShowWelcomeStickers();
            ViewModel.PropertyChanged += OnVMPropertyChanged;
            if (ViewModel != null) {
                newcvmid = ViewModel.ConversationId;
                var b = (from c in ScrollEventRegisteredConvIds where c == ViewModel.ConversationId select c).FirstOrDefault();
                if (b == 0) {
                    ScrollEventRegisteredConvIds.Add(ViewModel.ConversationId);
                }

                var d = (from c in MsgStateChangedEventRegisteredConvIds where c == ViewModel.ConversationId select c).FirstOrDefault();
                if (d == 0) {
                    MsgStateChangedEventRegisteredConvIds.Add(ViewModel.ConversationId);
                }

                // Callback event snackbar
                ViewModel.ShowSnackbarRequested = (avatar, text) => {
                    VK.VKUI.Controls.Snackbar sb = new VK.VKUI.Controls.Snackbar {
                        Content = text,
                        BeforeAvatar = avatar,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    Grid.SetRow(sb, 1);
                    LayoutRoot.Children.Add(sb);
                    sb.Dismissed += (f, g) => LayoutRoot.Children.Remove(sb);
                    sb.Show(10000);
                };

                CheckChatTheme();
                CheckBackButton();
            }
            Log.Info($"{GetType().Name} > DataContext changed. Old: {oldcvmid}, New: {newcvmid}");
        }

        private void OnVMPropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(ConversationViewModel.Style):
                    CheckChatTheme();
                    break;
                case nameof(ConversationViewModel.PinnedMessage):
                    PinnedMessageControl.Message = ViewModel?.PinnedMessage;
                    PinnedMsgBtn.Visibility = ViewModel?.PinnedMessage == null ? Visibility.Collapsed : Visibility.Visible;
                    break;
                case nameof(ConversationViewModel.IsEmptyDialog):
                    ShowWelcomeStickers();
                    break;
            }
        }

        private void ShowWelcomeStickers() {
            if (ViewModel.MessageSendRestriction == MessageSendRestriction.None &&
                ViewModel.Messages.Count == 0) CheckAndShowStickersSuggestions("Hi", !ViewModel.IsEmptyDialog);
        }

        public void CheckChatTheme(bool dontUpdateTheme = false) {
            var theme = ChatThemeService.GetCurrentChatTheme();
            if (!dontUpdateTheme) ChatThemeService.UpdateTheme();
        }


        bool scrollEventRegistered = false;
        private void RegisterScrollEvents(ListViewBase sender, ContainerContentChangingEventArgs args) {
            sender.ContainerContentChanging -= RegisterScrollEvents;
            var scrollViewer = chatListView.ScrollingHost;
            FixDownButton();
            if (scrollViewer != null && !scrollEventRegistered) {
                dthTimer.Tick += DthTimer_Tick;
                scrollViewer.ViewChanging += ScrollEvent;
                scrollEventRegistered = true;
            }
        }

        private void FixDownButton() {
            var scrollViewer = chatListView.ScrollingHost;
            var isp = chatListView.ItemsPanelRoot as ItemsStackPanel;

            if (ViewModel != null && ViewModel.Messages != null && ViewModel.Messages.Count > 0) {
                if (ViewModel.LastMessage.ConversationMessageId != ViewModel.Messages.Last().ConversationMessageId) DownButtonContainer.Visibility = Visibility.Visible;
            }
        }

        private void ScrollEvent(object sender, ScrollViewerViewChangingEventArgs e) {
            var scrollViewer = sender as ScrollViewer;

            new System.Action(async () => {
                if (ViewModel != null && ViewModel.Messages != null && ViewModel.Messages.Count > 0) {
                    if (ViewModel.LastMessage == null) await Task.Delay(10); // otherwise app hangs if last message is deleted.
                    if (ViewModel.LastMessage?.ConversationMessageId == ViewModel.Messages.Last().ConversationMessageId) {
                        DownButtonContainer.Visibility = e.FinalView.VerticalOffset > (scrollViewer.ScrollableHeight - scrollViewer.ActualHeight) ?
                    Visibility.Collapsed : Visibility.Visible;
                    } else {
                        DownButtonContainer.Visibility = Visibility.Visible;
                    }
                } else {
                    DownButtonContainer.Visibility = Visibility.Collapsed;
                }
            })();

            // Auto hide date control
            if (dthTimer.IsEnabled) {
                dthTimer.Stop();
            }
            dthTimer.Start();

            var state = DateShowAnimation.GetCurrentState();
            if (state != ClockState.Active) DateShowAnimation.Begin();
        }

        private void DthTimer_Tick(object sender, object e) {
            dthTimer.Stop();
            DateHideAnimation.Begin();
        }

        #region Multiselection and commandbar

        //private void EnableSelectionMode(object sender, RoutedEventArgs e) {
        //    chatListView.SelectionMode = ListViewSelectionMode.Multiple; SelMsgCount.Text = $"{0} {Locale.GetDeclension(0, "message").ToLowerInvariant()}";
        //    if (windowType != WindowType.ContactPanel) ConvInfoContainer.Visibility = Visibility.Collapsed;
        //    MultiSelectCommandBar.Visibility = Visibility.Visible;
        //    ChangeHitTestVisibleForMessages(false);
        //    chatListView.SelectionChanged += chatListView_SelectionChanged;
        //}

        private void EnableSelectionMode(LMessage msg) {
            if (msg == null) return;
            if (msg.Type == LMessageType.VKMessage && !msg.IsExpired && chatListView.SelectionMode == ListViewSelectionMode.None) {
                chatListView.SelectionMode = ListViewSelectionMode.Multiple;
                if (windowType != WindowType.ContactPanel) ConvInfoContainer.IsHitTestVisible = false;
                MultiSelectCommandBar.Visibility = Visibility.Visible;
                MSCBShowAnimation.Begin();
                ChangeHitTestVisibleForMessages(false);

                if (chatListView.SelectedItems.Count == 0) {
                    chatListView.SelectionChanged += chatListView_SelectionChanged;

                    chatListView.SelectedItems.Add(msg);
                }
            }
        }

        private void chatListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            new System.Action(async () => {
                if (chatListView.SelectedItems.Count == 0) {
                    MultiSelectCommandButtons.Children.Clear();
                    chatListView.SelectionChanged -= chatListView_SelectionChanged;
                    chatListView.SelectionMode = ListViewSelectionMode.None;
                    if (windowType != WindowType.ContactPanel) ConvInfoContainer.IsHitTestVisible = true;
                    MSCBHideAnimation.Begin();
                    await Task.Delay(200);
                    MultiSelectCommandBar.Visibility = Visibility.Collapsed;
                    ChangeHitTestVisibleForMessages(true);
                } else {
                    var a = e.AddedItems.Where(m => ((LMessage)m).IsExpired || ((LMessage)m).Action != null || ((LMessage)m).HasCall());
                    if (a.Count() > 0) {
                        foreach (var m in a) {
                            chatListView.SelectedItems.Remove(m);
                        }
                    }
                    ShowHideCommandBarButtons(chatListView.SelectedItems);
                }
            })();
        }

        private void ChangeHitTestVisibleForMessages(bool v) {
            foreach (var a in chatListView.Items) {
                ListViewItem lvi = chatListView.ContainerFromItem(a) as ListViewItem;
                if (lvi != null && lvi.ContentTemplateRoot is Grid) {
                    Grid g = lvi.ContentTemplateRoot as Grid;
                    g.IsHitTestVisible = v;
                }
            }
        }

        private void ClearSelectedMessages(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                MultiSelectCommandButtons.Children.Clear();
                chatListView.SelectionChanged -= chatListView_SelectionChanged;
                chatListView.SelectionMode = ListViewSelectionMode.None;
                if (windowType != WindowType.ContactPanel) ConvInfoContainer.IsHitTestVisible = true;
                MSCBHideAnimation.Begin();
                await Task.Delay(200);
                MultiSelectCommandBar.Visibility = Visibility.Collapsed;
                ChangeHitTestVisibleForMessages(true);
            })();
        }

        private void ShowHideCommandBarButtons(IList<object> items) {
            try {
                SelMsgCount.Text = $"{chatListView.SelectedItems.Count} {Locale.GetDeclension(chatListView.SelectedItems.Count, "message").ToLowerInvariant()}";

                List<LMessage> SelectedMessages = new List<LMessage>();
                foreach (var i in items) {
                    if (i is LMessage msg) SelectedMessages.Add(msg);
                }

                MultiSelectCommandButtons.Children.Clear();
                if (items.Count == 0) return;

                HyperlinkButton replybtn = new HyperlinkButton { Width = 48, Height = 48, Padding = new Thickness(0), Style = (Style)Resources["ConvAccentStyle"], Content = new FixedFontIcon { Glyph = "" } };
                HyperlinkButton fwdmsgbtn = new HyperlinkButton { Width = 48, Height = 48, Padding = new Thickness(0), Style = (Style)Resources["ConvAccentStyle"], Content = new FixedFontIcon { Glyph = "" } };
                HyperlinkButton deletebtn = new HyperlinkButton { Width = 48, Height = 48, Padding = new Thickness(0), Style = (Style)Resources["ConvAccentStyle"], Content = new FixedFontIcon { Glyph = "" } };

                ToolTipService.SetToolTip(MultiselectCloseButton, Locale.Get("msg_uncheck_all"));
                ToolTipService.SetToolTip(replybtn, Locale.Get(items.Count == 1 ? "msg_reply" : "msg_forwardhere"));
                ToolTipService.SetToolTip(fwdmsgbtn, Locale.Get("msg_forward"));
                ToolTipService.SetToolTip(deletebtn, Locale.Get("delete"));

                replybtn.Click += (a, b) => {
                    if (!string.IsNullOrEmpty(ViewModel.RestrictionReason)) return;
                    if (items.Count > 1) {
                        ViewModel.MessageFormViewModel.AddForwardedMessages(ViewModel.ConversationId, SelectedMessages);
                    } else if (items.Count == 1) {
                        if (SelectedMessages[0].CanReply()) ViewModel.MessageFormViewModel.AddReplyMessage(SelectedMessages[0]);
                    }
                    chatListView.SelectedItems.Clear();
                };
                fwdmsgbtn.Click += (a, b) => {
                    chatListView.SelectedItems.Clear();
                    Main.GetCurrent().StartForwardingMessage(ViewModel.ConversationId, SelectedMessages);
                };
                deletebtn.Click += async (a, b) => {
                    await DeleteMessages(SelectedMessages.Select(z => z.ConversationMessageId).ToList(), 1);
                };

                if (ViewModel.MessageSendRestriction == MessageSendRestriction.None) MultiSelectCommandButtons.Children.Add(replybtn);
                if (windowType != WindowType.ContactPanel) MultiSelectCommandButtons.Children.Add(fwdmsgbtn);
                MultiSelectCommandButtons.Children.Add(deletebtn);
            } catch (Exception ex) {
                Log.Error($"ShowHideCommandBarButtons 0x{ex.HResult.ToString("x8")}");
            }
        }

        #endregion

        #region Context menu

        private void ShowMsgContextMenu(UIElement sender, ContextRequestedEventArgs args) {
            try {
                bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                FrameworkElement el = sender as FrameworkElement;
                LMessage msg = el.DataContext as LMessage;

                if (ctrl && msg != null) {
                    new Dialogs.Dev.MessageJSONView(msg).Show();
                    return;
                }

                if (msg == null || msg.UISentMessageState == SentMessageState.Deleted || msg.IsExpired) return;
                args.Handled = true;

                Point pos;
                bool posReached = args != null && args.TryGetPosition(el, out pos);

                MenuFlyout mf = new MenuFlyout();
                string ctext = string.Empty;

                MenuFlyoutItem copyText = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_copy_text") };
                MenuFlyoutItem edit = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_edit") };
                MenuFlyoutItem reply = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_reply") };
                MenuFlyoutItem forward = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_forward") };
                MenuFlyoutItem pin = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_pin") };
                MenuFlyoutItem mark = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_mark") };
                if (msg.IsImportant) mark = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_unmark") };
                MenuFlyoutItem translate = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("translate") };
                MenuFlyoutSubItem delete = new MenuFlyoutSubItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("delete"), Style = (Style)App.Current.Resources["DestructiveMenuFlyoutSubItem"] };

                MenuFlyoutItem spam = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_del_ctx_spam") };
                MenuFlyoutItem delme = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_del_ctx_delme") };
                MenuFlyoutItem delall = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msg_del_ctx_delall") };

                MenuFlyoutItem stickerKeywords = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("sticker_keywords_ctx") };
                MenuFlyoutItem wrongWidget = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("widget_report_title") };
                MenuFlyoutItem select = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("multiselect/Content") };

                copyText.Click += (a, b) => {
                    DataPackage dp = new DataPackage();
                    dp.RequestedOperation = DataPackageOperation.Copy;
                    dp.SetText(ctext);
                    Clipboard.SetContent(dp);
                };
                edit.Click += (a, b) => ViewModel.MessageFormViewModel.StartEditing(msg);
                reply.Click += (a, b) => ViewModel.MessageFormViewModel.AddReplyMessage(msg);
                forward.Click += (a, b) => Main.GetCurrent().StartForwardingMessage(ViewModel.ConversationId, new List<LMessage> { msg });
                mark.Click += async (a, b) => {
                    object resp = await Messages.MarkAsImportant(ViewModel.ConversationId, msg.ConversationMessageId, !msg.IsImportant);
                    if (resp is MarkAsImportantResponse) {
                        msg.IsImportant = !msg.IsImportant;
                    } else {
                        Functions.ShowHandledErrorTip(resp);
                    }
                };
                pin.Click += async (a, b) => await PinMessage(msg);
                translate.Click += (a, b) => new Translator(msg).Show();
                spam.Click += async (a, b) => await DeleteMessages(new List<int> { msg.ConversationMessageId }, 0);
                delme.Click += async (a, b) => await DeleteMessages(new List<int> { msg.ConversationMessageId }, 1);
                delall.Click += async (a, b) => await DeleteMessages(new List<int> { msg.ConversationMessageId }, 2);

                if (msg.FromId != AppParameters.UserID) delete.Items.Add(spam);
                delete.Items.Add(delme);
                if (msg.CanDeleteForAll(ViewModel.ConversationId, ViewModel.ChatSettings)) delete.Items.Add(delall);

                stickerKeywords.Click += async (a, b) => {
                    var sticker = msg.Attachments.Where(atch => atch.Type == AttachmentType.Sticker)
                                  .Select(atch => atch.Sticker).FirstOrDefault();
                    if (sticker == null) return;
                    await Functions.ShowStickerKeywordsFlyoutAsync(el, args, sticker.StickerId);
                };
                wrongWidget.Click += (a, b) => {
                    var widgets = msg.Attachments.Where(atch => atch.Type == AttachmentType.Widget).Select(atch => atch.Widget);
                    WidgetReportModal modal = new WidgetReportModal(widgets.ToList());
                    modal.Title = wrongWidget.Text;
                    modal.Show();
                };
                select.Click += (a, b) => {
                    EnableSelectionMode(msg);
                };

                if (AppParameters.AdvancedMessageInfo) {
                    MenuFlyoutItem dbg = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = $"MID: {msg.Id} CMID: {msg.ConversationMessageId}" };
                    dbg.Click += (a, b) => {
                        new Dialogs.Dev.MessageJSONView(msg).Show();
                    };
                    mf.Items.Add(dbg);
                    mf.Items.Add(new MenuFlyoutSeparator());
                }

                if (msg.Type == LMessageType.VKMessage) {
                    if (ViewModel.Type == PeerType.Chat &&
                        msg.FromId == AppParameters.UserID && msg.UISentMessageState == SentMessageState.Read) {
                        MenuFlyoutItem readers = new MenuFlyoutItem {
                            Icon = new FixedFontIcon { Glyph = "" },
                            Text = Locale.Get("loading"),
                            MinWidth = 240,
                            Style = (Style)Application.Current.Resources["MenuFlyoutItemWithContentOnRightStyle"]
                        };

                        new System.Action(async () => await CheckWhoReadMessages(msg, readers))();

                        readers.Click += (a, b) => {
                            var transform = mf.Items.FirstOrDefault().TransformToVisual(Window.Current.Content);
                            var position = transform.TransformPoint(new Point(-2, -2));

                            WhoReadMessagePopup wrmp = new WhoReadMessagePopup(position, msg.PeerId, msg.ConversationMessageId);
                            wrmp.Show();
                        };
                        if (ViewModel.ChatSettings?.State == UserStateInChat.In) mf.Items.Add(readers);
                    }

                    if (msg.Reactions.Count > 0 && ViewModel.Type == PeerType.Chat) {
                        int totalReactions = 0;
                        foreach (var reaction in msg.Reactions) {
                            totalReactions += reaction.Count;
                        }

                        MenuFlyoutItem reactions = new MenuFlyoutItem {
                            Icon = new FixedFontIcon { Glyph = "" },
                            Text = String.Format(Locale.GetDeclensionForFormat(totalReactions, "reaction"), totalReactions)
                        };

                        reactions.Click += (a, b) => {
                            var transform = mf.Items.FirstOrDefault().TransformToVisual(Window.Current.Content);
                            var position = transform.TransformPoint(new Point(-2, -2));

                            ReactedPeersPopup rpp = new ReactedPeersPopup(position, msg.PeerId, msg.ConversationMessageId);
                            rpp.Show();
                        };

                        mf.Items.Add(reactions);
                    }

                    if (mf.Items.Count > 0 && mf.Items.LastOrDefault() is MenuFlyoutItem) mf.Items.Add(new MenuFlyoutSeparator());

                    if (msg.TryGetMessageText(out ctext)) mf.Items.Add(copyText);
                    if (msg.CanEditMessage(ViewModel.PinnedMessage)) mf.Items.Add(edit);
                    if (string.IsNullOrEmpty(ViewModel.RestrictionReason) && msg.CanReply()) mf.Items.Add(reply);
                    if (windowType == WindowType.Main && msg.TTL == 0 && !msg.HasCall()) mf.Items.Add(forward);
                    if (ViewModel.ChatSettings != null && ViewModel.ChatSettings.ACL.CanChangePin && msg.TTL == 0 && !msg.HasCall()) mf.Items.Add(pin);
                    if (!msg.HasCall()) mf.Items.Add(mark);
                    if (AppSession.MessagesTranslationLanguagePairs.Count > 0 && !string.IsNullOrEmpty(msg.Text)) mf.Items.Add(translate);
                    if (!msg.HasCall()) mf.Items.Add(delete);

                    // Disable writing
                    ChatMember icmb = ViewModel.Members?.Where(m => m.MemberId == AppParameters.UserID).FirstOrDefault();
                    bool canMute = icmb != null && icmb.IsAdmin;

                    ChatMember cmb = ViewModel.Members?.Where(m => m.MemberId == msg.SenderId).FirstOrDefault();
                    if (cmb != null && !cmb.IsAdmin && canMute) CheckAndAddMemRestrictionCommand(mf, cmb);

                    mf.Items.Add(new MenuFlyoutSeparator());
                    if (msg.ContainsWidgets()) mf.Items.Add(wrongWidget);
                    if (msg.HasOnlyStandartSticker()) mf.Items.Add(stickerKeywords);
                    if (chatListView.SelectionMode == ListViewSelectionMode.None && !msg.IsExpired && msg.Action == null && !msg.HasCall()) mf.Items.Add(select);

                    if (mf.Items.Last() is MenuFlyoutSeparator sep) mf.Items.Remove(sep);

                    bool canSendReactions = true;
                    if (ViewModel.ChatSettings != null) canSendReactions = ViewModel.ChatSettings.ACL.CanSendReactions;
                    if (!msg.HasGift() && !msg.HasCall() && msg.TTL == 0 && canSendReactions) {
                        ReactionsPicker.RegisterForMenuFlyout(mf, msg.PeerId, msg.ConversationMessageId, msg.SelectedReactionId);
                    }
                }

                if (mf.Items.Count > 0) {
                    if (posReached) {
                        if (pos.Y <= 72) pos = new Point(pos.X, 72);
                        mf.ShowAt(el, pos);
                    } else {
                        mf.ShowAt(el);
                    }
                }
            } catch (Exception ex) {
                Log.Error($"Error while opening a message context menu! 0x{ex.HResult.ToString("x8")}");
            }
        }

        private void CheckAndAddMemRestrictionCommand(MenuFlyout mf, ChatMember cmb) {
            if (cmb.IsRestrictedToWrite) {
                MenuFlyoutItem ewmfi = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("chatinfo_memctx_enable_writing") };
                ewmfi.Click += async (c, d) => {
                    object r = await Messages.ChangeConversationMemberRestrictions(ViewModel.ConversationId, cmb.MemberId, false);
                    if (r is MemberRestrictionResponse resp) {
                        if (resp.FailedMemberIds.Contains(cmb.MemberId)) {
                            Tips.Show(Locale.Get("global_error"));
                            return;
                        }
                        cmb.IsRestrictedToWrite = false;

                        int i = ViewModel.Members.IndexOf(cmb);
                        ViewModel.Members.Remove(cmb);
                        ViewModel.Members.Insert(i, cmb);
                    } else {
                        Functions.ShowHandledErrorTip(r);
                    }
                };
                mf.Items.Add(ewmfi);

                return;
            }
            MenuFlyoutSubItem dwmfi = new MenuFlyoutSubItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("chatinfo_memctx_disable_writing") };
            var seconds = new List<int> { 3600, 28800, 86400, 0 };
            foreach (int second in seconds) {
                int hour = second / 3600;
                MenuFlyoutItem mfi = new MenuFlyoutItem {
                    Text = second == 0 ?
                        Locale.Get("forever") :
                        Locale.GetDeclensionForFormatSimple(hour, "for_hours")
                };
                mfi.Click += async (c, d) => {
                    object r = await Messages.ChangeConversationMemberRestrictions(ViewModel.ConversationId, cmb.MemberId, true, second);
                    if (r is MemberRestrictionResponse resp) {
                        if (resp.FailedMemberIds.Contains(cmb.MemberId)) {
                            Tips.Show(Locale.Get("global_error"));
                            return;
                        }
                        cmb.IsRestrictedToWrite = true;

                        int i = ViewModel.Members.IndexOf(cmb);
                        ViewModel.Members.Remove(cmb);
                        ViewModel.Members.Insert(i, cmb);
                    } else {
                        Functions.ShowHandledErrorTip(r);
                    }
                };
                dwmfi.Items.Add(mfi);
            }

            mf.Items.Add(dwmfi);
        }

        private async Task CheckWhoReadMessages(LMessage msg, MenuFlyoutItem mfi) {
            if (ViewModel.ChatSettings?.State != UserStateInChat.In) return;
            ObservableCollection<UserAvatarItem> avatars = new ObservableCollection<UserAvatarItem>();

            var response = await Messages.WhoReadMessageLite(msg.PeerId, msg.ConversationMessageId);
            if (response is VKList<long> viewers && viewers.TotalCount > 0) {
                mfi.Text = String.Format(Locale.GetDeclensionForFormat(viewers.TotalCount, "views"), viewers.TotalCount);

                // Объекты юзеров и групп лайтовые, так что не будем добавлять их в кэш.
                for (int i = 0; i < Math.Min(viewers.TotalCount, 3); i++) {
                    long viewer = viewers.Items[i];
                    if (viewer > 0) {
                        var user = viewers.Profiles.Where(u => u.Id == viewer).FirstOrDefault();
                        if (user != null) {
                            BitmapImage ava = new BitmapImage();
                            await ava.SetUriSourceAsync(new Uri(user.Photo50));
                            avatars.Add(new UserAvatarItem {
                                Name = user.FullName,
                                Image = ava
                            });
                            if (viewers.TotalCount == 1)
                                mfi.Text = String.Format(Locale.GetForFormat(user.Sex == Sex.Female ? "viewed_by_f" : "viewed_by_m"), user.FirstName);
                        }
                    } else if (viewer < 0) {
                        var group = viewers.Groups.Where(g => g.Id == viewer * -1).FirstOrDefault();
                        if (group != null) {
                            BitmapImage ava = new BitmapImage();
                            await ava.SetUriSourceAsync(new Uri(group.Photo50));
                            avatars.Add(new UserAvatarItem {
                                Name = group.Name,
                                Image = ava
                            });
                            if (viewers.TotalCount == 1)
                                mfi.Text = String.Format(Locale.GetForFormat("viewed_by_g"), group.Name);
                        }
                    }
                }
            } else {
                mfi.Text = Locale.Get("no_views");
                Functions.ShowHandledErrorTip(response);
            }

            UserAvatars avas = new UserAvatars {
                Height = 18,
                Margin = new Thickness(0, 1, 0, 0),
                Avatars = avatars
            };
            mfi.Tag = avas;
        }

        private async Task PinMessage(LMessage msg) {
            object resp = await Messages.Pin(ViewModel.ConversationId, msg.ConversationMessageId);
            Functions.ShowHandledErrorDialog(resp);
        }

        #endregion

        #region Mark message as read

        private void InitMarkAsReadTimer() {
            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(2);
            t.Tick += async (s, e) => {
                try {
                    var isp = chatListView.ItemsPanelRoot as ItemsStackPanel;
                    if (isp == null || ViewModel == null || ViewModel.Messages == null) return;
                    if (isp.LastVisibleIndex < 0) return;
                    if (ViewModel.Messages.Count == 0 || isp.LastVisibleIndex >= ViewModel.Messages.Count) return;

                    await MarkReactionsAsRead(isp.FirstVisibleIndex, isp.LastVisibleIndex);
                    LMessage msg = ViewModel.Messages[isp.LastVisibleIndex];
                    if (msg.UISentMessageState == SentMessageState.Unread && msg.FromId != AppParameters.UserID)
                        await Messages.MarkAsRead(ViewModel.ConversationId, msg.ConversationMessageId);

                } catch { }
            };
            t.Start();
        }

        private async Task MarkReactionsAsRead(int fi, int li) {
            if (ViewModel?.UnreadReactionsCount == 0) return;

            LMessage fvmsg = ViewModel.Messages[fi];
            LMessage lvmsg = ViewModel.Messages[li];

            List<int> cmIds = new List<int>();
            foreach (int cmId in ViewModel.UnreadReactions) {
                if (cmId >= fvmsg.ConversationMessageId && cmId <= lvmsg.ConversationMessageId) cmIds.Add(cmId);
            }

            if (cmIds.Count == 0) return;
            Log.Info($"ConversationView: found messages with unread reactions among visible messages: {String.Join(",", cmIds)}");
            object response = await Messages.MarkReactionsAsRead(ViewModel.ConversationId, cmIds);
            Functions.ShowHandledErrorTip(response);
        }

        #endregion

        #region Delete messages

        private async Task DeleteMessageWithConfirm(List<int> ids, int method) {
            int count = ids.Count;
            string title = string.Empty, content = string.Empty;
            switch (method) {
                case 0:
                    title = Locale.Get("msg_del_ctx_spam");
                    content = String.Format(Locale.GetDeclensionForFormat(count, "messagespam_dialog"), count);
                    break;
                case 1:
                    title = $"{Locale.Get("delete")} {Locale.Get("msg_del_ctx_delme").ToLower()}";
                    content = String.Format(Locale.GetDeclensionForFormat(count, "messagedelete_dialog"), count);
                    break;
                case 2:
                    title = $"{Locale.Get("delete")} {Locale.Get("msg_del_ctx_delall").ToLower()}";
                    content = String.Format(Locale.GetDeclensionForFormat(count, "messagedelete_all_dialog"), count);
                    break;
            }

            ContentDialog dlg = new ContentDialog {
                Title = title,
                Content = content,
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no"),
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                await DeleteMessages(ids, method);
            }
        }

        private async Task DeleteMessages(List<int> ids, int method) {
            object resp = null;
            switch (method) {
                case 0: resp = await Messages.Delete(ViewModel.ConversationId, ids, true, false); break;
                case 1: resp = await Messages.Delete(ViewModel.ConversationId, ids, false, false); break;
                case 2: resp = await Messages.Delete(ViewModel.ConversationId, ids, false, true); break;
            }
            if (resp is Dictionary<string, int>) {
                var d = resp as Dictionary<string, int>;

                foreach (var m in d) {
                    if (m.Value == 1) {
                        var msgs = (from z in ViewModel.Messages where z.ConversationMessageId.ToString() == m.Key select z).ToList();
                        if (msgs.Count > 0) {
                            if (AppParameters.KeepDeletedMessagesInUI) {
                                msgs[0].UISentMessageState = SentMessageState.Deleted;
                            } else {
                                ViewModel.Messages.Remove(msgs[0]);
                            }
                        }
                    }
                }

                if (d.Count > 1) {
                    Tips.Show(method == 0 ? Locale.Get("msg_del_spam_multi") : Locale.Get("msg_del_multi"));
                } else if (d.Count == 1) {
                    if (method == 1) {
                        Tips.Show(Locale.Get("msg_del_single"), null, Locale.Get("restore"), async () => await RestoreDeletedMessage(ids[0]));
                    } else {
                        Tips.Show(method == 0 ? Locale.Get("msg_del_spam_single") : Locale.Get("msg_del_single"));
                    }
                }
                if (chatListView.SelectionMode == ListViewSelectionMode.Multiple && chatListView.SelectedItems.Count > 0) chatListView.SelectedItems.Clear();
            } else {
                Functions.ShowHandledErrorTip(resp);
            }
        }

        private async Task RestoreDeletedMessage(int id) {
            object resp = await Messages.Restore(ViewModel.ConversationId, id);
            Functions.ShowHandledErrorTip(resp);
        }

        #endregion

        #region Stickers suggestions

        public bool IsStickersSuggestionsContainerShowing { get { return StickersSuggestionContainer.Visibility == Visibility.Visible; } }

        public void FocusToStickersSuggestions() {
            StickersSuggestionList.Focus(FocusState.Keyboard);
        }

        public void CheckAndShowStickersSuggestions(string text, bool forceHide = false) {
            if (forceHide) {
                StickersSuggestionContainer.Visibility = Visibility.Collapsed;
                return;
            }
            List<Sticker> stickers = StickersKeywords.GetStickersByWord(text);

            // Add link to bot
            if (stickers != null && stickers.LastOrDefault()?.StickerId > 0) stickers.Add(new Sticker {
                StickerId = 0
            });

            StickersSuggestionContainer.Visibility = stickers == null ? Visibility.Collapsed : Visibility.Visible;
            StickersSuggestionList.ItemsSource = stickers;
        }

        private void StickersSuggestionList_ItemClick(object sender, ItemClickEventArgs e) {
            Sticker sticker = e.ClickedItem as Sticker;
            if (sticker.StickerId <= 0) {
                Main.GetCurrent().ShowConversationPage(-184940019);
            } else {
                new System.Action(async () => {
                    ViewModel.MessageFormViewModel.Sticker = sticker;
                    await ViewModel.MessageFormViewModel.SendMessage();
                })();
            }
            StickersSuggestionContainer.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Drag'n'Drop

        private void ShowDropArea(object sender, DragEventArgs e) {
            new System.Action(async () => {
                try {
                    if (ViewModel == null || ViewManagement.GetWindowType() == WindowType.Hosted || DropArea.Visibility == Visibility.Visible) return;
                    e.AcceptedOperation = DataPackageOperation.None;
                    e.DragUIOverride.IsContentVisible = true;

                    if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                        DropDoc.Visibility = Visibility.Collapsed;
                        DropImg.Visibility = Visibility.Collapsed;
                        DropVid.Visibility = Visibility.Collapsed;
                        // выше три строчки нужны, т. к. парсинг файлов идёт не мгновенно,
                        // и можно случайно бросить файлы не туда, куда надо.

                        var items = await e.DataView.GetStorageItemsAsync();
                        if (items.Count > 10) return;
                        DropArea.Visibility = Visibility.Visible;
                        int imagesCount = 0;
                        int videosCount = 0;
                        foreach (IStorageItem sitem in items) {
                            if (sitem is StorageFile file) {
                                if (DataPackageParser.IsImage(file)) {
                                    imagesCount++;
                                    continue;
                                }
                                if (DataPackageParser.IsVideo(file)) {
                                    videosCount++;
                                    continue;
                                }
                            }
                        }

                        DropDoc.Visibility = Visibility.Visible;
                        if (Math.Min(imagesCount, videosCount) == 0 && Math.Max(imagesCount, videosCount) == items.Count) {
                            bool isImage = imagesCount == items.Count;
                            bool isVideo = videosCount == items.Count;
                            if (isImage) DropDocText.Text = Locale.Get("cv_drop_photo_doc");
                            if (isVideo) DropDocText.Text = Locale.Get("cv_drop_video_doc");
                            DropImg.Visibility = isImage ? Visibility.Visible : Visibility.Collapsed;
                            DropVid.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
                            DropArea.RowDefinitions[1].Height = new GridLength(1.5, GridUnitType.Star);
                        } else {
                            DropDocText.Text = Locale.Get("cv_drop_doc");
                            DropArea.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
                        }
                    }
                } catch (Exception ex) {
                    Functions.ShowHandledErrorTip(ex);
                }
            })();
        }

        private void HideDropArea(object sender, DragEventArgs e) {
            Debug.WriteLine("HideDropArea");
            DropDoc.Visibility = Visibility.Collapsed;
            DropImg.Visibility = Visibility.Collapsed;
            DropVid.Visibility = Visibility.Collapsed;
            DropArea.Visibility = Visibility.Collapsed;
        }

        private void DocDragOver(object sender, DragEventArgs e) {
            Debug.WriteLine("DocDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void ImgDragOver(object sender, DragEventArgs e) {
            Debug.WriteLine("ImgDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void VidDragOver(object sender, DragEventArgs e) {
            Debug.WriteLine("ImgDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void DropToDoc(object sender, DragEventArgs e) {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed;
            new System.Action(async () => {
                var items = await e.DataView.GetStorageItemsAsync();
                var files = items.Cast<StorageFile>();
                await ViewModel.MessageFormViewModel.UploadDocMulti(files);
            })();
        }

        private void DropToImg(object sender, DragEventArgs e) {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed;
            new System.Action(async () => {
                var items = await e.DataView.GetStorageItemsAsync();
                var files = items.Cast<StorageFile>();
                await ViewModel.MessageFormViewModel.UploadPhotoMulti(files);
            })();
        }

        private void DropToVid(object sender, DragEventArgs e) {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed;
            new System.Action(async () => {
                var items = await e.DataView.GetStorageItemsAsync();
                var files = items.Cast<StorageFile>();
                await ViewModel.MessageFormViewModel.UploadVideoMulti(files);
            })();
        }

        #endregion

        private void CheckBackButton() {
            bool isWide = Main.GetCurrent().IsWideMode;
            bool canShow = AppSession.ChatNavigationHistory.Count > 0 || !isWide;
            BackButton.Visibility = !canShow ? Visibility.Collapsed : Visibility.Visible;
            ConvInfoButton.Padding = new Thickness(!canShow ? 8 : 0, 0, 0, 0);
        }

        private void GoBack(object sender, RoutedEventArgs e) {
            //if (AppSession.ChatNavigationHistory.Count > 0) {
            //    long prev = AppSession.ChatNavigationHistory.Pop(); // remove last (previous) peer id from stack
            //    Main.GetCurrent().ShowConversationPage(prev, forceLoad: false);
            //    AppSession.ChatNavigationHistory.Pop(); // remove last (navigated from) peer id from stack again.
            //    CheckBackButton();
            //} else {
            //    Main.GetCurrent().SwitchToLeftFrame();
            //}
            Main.GetCurrent().GoBack();
            CheckBackButton();
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args) {
            bool ctrl = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shift = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            if (ctrl && !shift && args.VirtualKey == VirtualKey.F) {
                var d = DataContext as ConversationViewModel;
                if (d != null) d.OpenMessagesSearchModal();
            }

            if (args.VirtualKey == VirtualKey.Application) {
                var focused = FocusManager.GetFocusedElement();
                if (focused is ListViewItem lvi) ShowMsgContextMenu(lvi, null);
            }
            if (!ctrl && args.VirtualKey == VirtualKey.F5) {
                ViewModel?.LoadMessagesAsync();
            }
            if (ctrl && args.VirtualKey == VirtualKey.R) {
                LMessage msg = null;
                var focused = FocusManager.GetFocusedElement();
                if (focused is ListViewItem lvi) {
                    msg = lvi.Content as LMessage;
                }
                if (msg == null || msg.UISentMessageState == SentMessageState.Deleted || msg.IsExpired) return;
                ViewModel?.MessageFormViewModel.AddReplyMessage(msg);
            }
            if (args.VirtualKey == VirtualKey.Delete) {
                LMessage msg = null;
                var focused = FocusManager.GetFocusedElement();
                if (focused is ListViewItem lvi) {
                    msg = lvi.Content as LMessage;
                }
                if (msg == null) return;

                bool spamAvailable = msg.FromId != AppParameters.UserID;
                bool deleteForAllAvailable = msg.CanDeleteForAll(ViewModel.ConversationId, ViewModel.ChatSettings);
                List<int> ids = new List<int> { msg.ConversationMessageId };

                new System.Action(async () => {
                    if (shift && !ctrl && deleteForAllAvailable) { // Delete for all
                        await DeleteMessageWithConfirm(ids, 2);
                    } else if (ctrl && !shift && spamAvailable) { // Spam
                        await DeleteMessageWithConfirm(ids, 0);
                    } else { // Delete only me
                        await DeleteMessageWithConfirm(ids, 1);
                    }
                })();
            }
        }

        private void MessageDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            if (!IsMsgRenderingEnabled) return;
            var msg = args.NewValue as LMessage;
            if (ViewModel == null) return;
            if (msg == null) return;
            bool isGroupChannel = ViewModel.ChatSettings != null && ViewModel.ChatSettings.IsGroupChannel;

            Microsoft.UI.Xaml.Controls.SwipeControl swc = sender as Microsoft.UI.Xaml.Controls.SwipeControl;
            LMessage prev = null;

            if (ViewModel.ConversationId.IsChat()) {
                MessagesCollection mc = ViewModel.Messages;
                int idx = mc.IndexOf(msg);
                if (idx > 0 && mc != null && mc.Count > 0 && idx <= mc.Count) {
                    prev = mc[idx - 1];
                }
            }

            if (isGroupChannel || msg.IsExpired || msg.Action != null || ViewModel.ConversationId == int.MaxValue) {
                swc.RightItems = null;
            } else {
                var replyCommand = new Microsoft.UI.Xaml.Controls.SwipeItem {
                    BehaviorOnInvoked = Microsoft.UI.Xaml.Controls.SwipeBehaviorOnInvoked.Close,
                    Background = new SolidColorBrush(Colors.Transparent),
                };
                replyCommand.Invoked += (a, b) => {
                    ViewModel.MessageFormViewModel.AddReplyMessage(msg);
                };
                swc.RightItems = new Microsoft.UI.Xaml.Controls.SwipeItems {
                    Mode = Microsoft.UI.Xaml.Controls.SwipeMode.Execute
                };
                swc.RightItems.Add(replyCommand);
            }

            Stopwatch sw = Stopwatch.StartNew();

            try {
                swc.Content = MessageUIHelper.Build(msg, prev, chatListView.ScrollingHost);

                // Disappearing message
                if (msg.TTL > 0) {
                    TimeSpan expiration = DateTime.Now - msg.Date;
                    int remaining = msg.TTL - Convert.ToInt32(expiration.TotalSeconds);
                }

                sw.Stop();
                string prevs = prev != null ? $" Prev: {prev.ConversationMessageId}" : string.Empty;
                Debug.WriteLine($"UI for {msg.PeerId}_{msg.ConversationMessageId} built in {sw.ElapsedMilliseconds} ms.{prevs}");
                if (sw.ElapsedMilliseconds > 100)
                    Log.Warn($"MessageDataContextChanged: UI for msg {msg.PeerId}_{msg.ConversationMessageId} built too long! ({sw.ElapsedMilliseconds} ms.)");

                msg.MessageEditedCallback = () => {
                    swc.Content = MessageUIHelper.Build(msg, prev, chatListView.ScrollingHost);
                    if (msg.TTL > 0) {
                        TimeSpan expiration = DateTime.Now - msg.Date;
                        int remaining = msg.TTL - Convert.ToInt32(expiration.TotalSeconds);
                    }
                };
            } catch (Exception ex) {
                HyperlinkButton hbtn = new HyperlinkButton {
                    Padding = new Thickness(0),
                    FontSize = 13,
                    ContentTemplate = (DataTemplate)Application.Current.Resources["TextLikeHyperlinkBtnTemplate"],
                    Content = "An error occured when rendering this message. Click here for more info."
                };
                hbtn.Click += async (a, b) => {
                    await new MessageDialog($"{ex.Message}\n\nStackTrace:\n{ex.StackTrace}", $"HResult: 0x{ex.HResult.ToString("x8")}").ShowAsync();
                };
                swc.Content = new ContentControl {
                    Template = (ControlTemplate)Application.Current.Resources["ActionMessageTemplate"],
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Content = hbtn
                };
            }
        }

        private void SwipeControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            FrameworkElement el = sender as FrameworkElement;
            if (el == null) {
                Log.Error("SwipeControl_DoubleTapped: cannot get FrameworkElement itself!!!");
                return;
            }

            LMessage msg = el.DataContext as LMessage;
            if (msg == null) {
                Log.Error("SwipeControl_DoubleTapped: cannot get message from doubletapped element!!!");
                return;
            }

            try {
                int frid = AppParameters.FastReactionId;
                bool canSendReactions = true;
                if (ViewModel.ChatSettings != null) canSendReactions = ViewModel.ChatSettings.ACL.CanSendReactions;
                if (!msg.HasGift() && !msg.HasCall() && msg.TTL == 0 && canSendReactions && AppSession.AvailableReactions.Contains(frid)) {
                    new System.Action(async () => {
                        var response = msg.SelectedReactionId == frid ?
                            await Messages.DeleteReaction(ViewModel.ConversationId, msg.ConversationMessageId) :
                            await Messages.SendReaction(ViewModel.ConversationId, msg.ConversationMessageId, frid);
                        Functions.ShowHandledErrorTip(response);
                    })();
                } else {
                    if (msg == null || msg.UISentMessageState == SentMessageState.Deleted || msg.IsExpired || msg.Action != null) return;
                    ViewModel?.MessageFormViewModel.AddReplyMessage(msg);
                }
            } catch (Exception ex) {
                Log.Error(ex, "SwipeControl_DoubleTapped: an error occured!");
            }
        }

        private void Date_Click(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                CalendarView cv = new CalendarView {
                    Margin = new Thickness(-24),
                    BorderThickness = new Thickness(0),
                    Background = new SolidColorBrush(Colors.Transparent),
                    Width = 318,
                    MinDate = new DateTime(2006, 10, 10, 0, 0, 0),
                    MaxDate = DateTime.Now
                };
                cv.SelectedDates.Add(DateTimeOffset.Now);

                ContentDialog dlg = new ContentDialog {
                    Content = cv,
                    PrimaryButtonText = Locale.Get("go"),
                    CloseButtonText = Locale.Get("close"),
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dlg.ShowAsync();
                if (result == ContentDialogResult.Primary && cv.SelectedDates.Count > 0) {
                    var selected = cv.SelectedDates.FirstOrDefault().Date;
                    selected = selected.AddDays(-1); // надо

                    VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                    object resp = await ssp.ShowAsync(Messages.Search(string.Empty, ViewModel.ConversationId, 0, 0, 1, APIHelper.ConvertDateToVKFormat(selected)));
                    if (resp is MessagesHistoryResponse scr && scr.Items.Count > 0) {
                        ViewModel.GoToMessage(scr.Items.FirstOrDefault().ConversationMessageId);
                    } else {
                        Functions.ShowHandledErrorDialog(resp);
                    }
                }
            })();
        }

        private void OnHeaderContextRequested(UIElement sender, ContextRequestedEventArgs args) {
            bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            if (ctrl) {
                new Dialogs.Dev.MessageJSONView(ViewModel).Show();
                return;
            }
        }

        private void SearchInChatBtn_Click(object sender, RoutedEventArgs e) {
            ViewModel.OpenMessagesSearchModal();
        }
    }
}