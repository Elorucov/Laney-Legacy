using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Upload;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class PeerProfile : Modal {
        WindowType WinType;

        PeerProfileViewModel ViewModel => DataContext as PeerProfileViewModel;

        public PeerProfile(long peerId) {
            this.InitializeComponent();
            WinType = ViewManagement.GetWindowType();

            if (peerId.IsChat()) {
                MainFlipView.Items.RemoveAt(1);
            } else {
                MainFlipView.Items.RemoveAt(0);
            }

            DataContext = new PeerProfileViewModel(peerId, ActionButtons, ChatEditFlyout);

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.CloseWindowRequested += ViewModel_CloseWindowRequested;

            PhotosSV.ViewChanged += PhotosSV_ViewChanged;
            VideosSV.ViewChanged += VideosSV_ViewChanged;
            AudiosSV.ViewChanged += AudiosSV_ViewChanged;
            FilesSV.ViewChanged += FilesSV_ViewChanged;
            LinksSV.ViewChanged += LinksSV_ViewChanged;
        }

        private void ViewModel_CloseWindowRequested(object sender, EventArgs e) {
            Hide();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            // Remove members tab for channels or unavailable chats.
            if (ViewModel.Id.IsChat() && e.PropertyName == "IsLoading" && ViewModel.IsLoading == false) {
                new System.Action(async () => {
                    await Task.Delay(30); // required

                    if (ViewModel.ChatMembers == null) {
                        MainFlipView.Items.RemoveAt(0);
                        SetupTabs();
                        SegmentedTabs.SelectedIndex = 0;
                        MainFlipView.SelectedIndex = 0;
                    }
                })();
            }
            if (e.PropertyName == "IsLoading") SegmentedTabs.SelectedIndex = MainFlipView.SelectedIndex;
        }

        #region Chat

        private void OpenMemberProfile(object sender, RoutedEventArgs e) {
            FrameworkElement element = sender as FrameworkElement;
            ChatMember entity = element?.Tag as ChatMember;
            if (entity == null) return;

            VKLinks.ShowPeerInfoModal(entity.MemberId, () => {
                if (AppSession.CurrentConversationVM?.ConversationId == entity.MemberId) Hide();
            });
        }

        // Temporary
        private void ChatEditFlyout_Opening(object sender, object e) {
            managementBtn.IsEnabled = ViewModel.ChatPermissions != null;
            forwardChk.IsEnabled = ViewModel.ChatACL != null && ViewModel.ChatACL.CanDisableForwardMessages;
            forwardChk.IsChecked = ViewModel.ChatACL != null && ViewModel.ChatACL.CanForwardMessages;
            serviceMessagesChk.IsEnabled = ViewModel.ChatACL != null && ViewModel.ChatACL.CanDisableServiceMessages;
            serviceMessagesChk.IsChecked = !ViewModel.ServiceMessagesDisabled;
            EditableChatName.Text = ViewModel.Header;
        }

        #region Chat photo

        private void ChatPhotoBrowse(object sender, RoutedEventArgs e) {
            ChatEditFlyout.Hide();

            new System.Action(async () => {
                FileOpenPicker fop = new FileOpenPicker();
                fop.FileTypeFilter.Add(".jpg");
                fop.FileTypeFilter.Add(".jpeg");
                fop.FileTypeFilter.Add(".png");
                fop.FileTypeFilter.Add(".bmp");
                fop.FileTypeFilter.Add(".gif");
                fop.FileTypeFilter.Add(".heic");
                fop.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                fop.ViewMode = PickerViewMode.Thumbnail;
                var file = await fop.PickSingleFileAsync();

                if (file != null) {
                    await StartUploadChatPhoto(file);
                }
            })();
        }

        private void ChatPhotoCreate(object sender, RoutedEventArgs e) {
            ChatEditFlyout.Hide();
            AvatarCreator acm = new AvatarCreator(true);
            acm.Closed += (a, b) => {
                if (b != null && b is StorageFile file) new System.Action(async () => { await StartUploadChatPhoto(file); })();
            };
            acm.Show();
        }

        private async Task StartUploadChatPhoto(StorageFile file) {
            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Photos.GetChatUploadServer(ViewModel.Id - 2000000000));
            if (resp is VkUploadServer server) {
                await UploadChatPhoto(server.Uri, file);
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        private async Task UploadChatPhoto(Uri uri, StorageFile file) {
            IFileUploader vkfu = APIHelper.GetUploadMethod("file", uri, file);
            vkfu.UploadFailed += UploadFailed;
            VK.VKUI.Popups.ScreenSpinner<string> ssp1 = new VK.VKUI.Popups.ScreenSpinner<string>();
            string resp = await ssp1.ShowAsync(vkfu.UploadAsync());
            if (resp != null) {
                string result = VKResponseHelper.GetJSONInResponseObject(resp);

                VK.VKUI.Popups.ScreenSpinner<object> ssp2 = new VK.VKUI.Popups.ScreenSpinner<object>();
                object resp2 = await ssp2.ShowAsync(Messages.SetChatPhoto(result));
                if (resp2 is SetChatPhotoResult scpresult) {
                    Hide();
                    VKLinks.ShowPeerInfoModal(ViewModel.Id);
                } else {
                    Functions.ShowHandledErrorDialog(resp2);
                }
            }
        }

        private void UploadFailed(Exception e) {
            Log.Error($"{GetType().Name} > Upload failed! 0x{e.HResult.ToString("x8")}.");
            Functions.ShowHandledErrorDialog(e);
        }

        private void ChatPhotoDelete(object sender, RoutedEventArgs e) {
            ChatEditFlyout.Hide();
            new System.Action(async () => {
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object resp = await ssp.ShowAsync(Messages.DeleteChatPhoto(ViewModel.Id - 2000000000));
                if (resp is SetChatPhotoResult) {
                    Hide();
                    VKLinks.ShowPeerInfoModal(ViewModel.Id);
                } else {
                    Functions.ShowHandledErrorDialog(resp);
                }
            })();
        }

        #endregion

        // Temporary (leftover from old PeerProfile)
        private void RenameChat(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            ChatEditFlyout.Hide();
            new System.Action(async () => { await RenameChatAPI(EditableChatName.Text); })();
        }

        private async Task RenameChatAPI(string text) {
            if (String.IsNullOrEmpty(text)) return;

            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            object response = await ssp.ShowAsync(Messages.EditChat(ViewModel.Id - 2000000000, text));
            if (response is bool) {
                Hide();
                VKLinks.ShowPeerInfoModal(ViewModel.Id);
            }
            Functions.ShowHandledErrorDialog(response);
        }

        // Temporary (leftover from old PeerProfile)
        private void OpenChatDescriptionEditor(object sender, RoutedEventArgs e) {
            ChatEditFlyout.Hide();
            ChatDescEditFlyout.ShowAt(ActionButtons.Children.FirstOrDefault() as FrameworkElement);
        }

        private void SaveDescription(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                string desc = EditableChatDescription.Text;
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object response = await ssp.ShowAsync(Messages.EditChat(ViewModel.Id - 2000000000, description: desc));
                if (response is bool) {
                    Hide();
                    VKLinks.ShowPeerInfoModal(ViewModel.Id);
                }
                Functions.ShowHandledErrorDialog(response);
            })();
        }

        private void OpenChatPermissionsEditor(object sender, RoutedEventArgs e) {
            ChatEditFlyout.Hide();
            ChatSettingsModal csm = new ChatSettingsModal((int)(ViewModel.Id - 2000000000), ViewModel.ChatPermissions);
            csm.Closed += (a, b) => {
                Hide();
                VKLinks.ShowPeerInfoModal(ViewModel.Id);
            };
            csm.Show();
        }

        private void ToggleMessageForwardingAbility(object sender, RoutedEventArgs e) {
            ChatEditFlyout.Hide();
            new System.Action(async () => {
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object r = await ssp.ShowAsync(Messages.EditChat(ViewModel.Id - 2000000000, ViewModel.ChatACL.CanForwardMessages, null));
                if (r is bool b) {
                    Hide();
                    VKLinks.ShowPeerInfoModal(ViewModel.Id);
                } else {
                    Functions.ShowHandledErrorTip(r);
                }
            })();
        }

        private void ToggleServiceMessagesAvailability(object sender, RoutedEventArgs e) {
            ChatEditFlyout.Hide();
            new System.Action(async () => {
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object r = await ssp.ShowAsync(Messages.EditChat(ViewModel.Id - 2000000000, null, !ViewModel.ServiceMessagesDisabled));
                if (r is bool b) {
                    Hide();
                    VKLinks.ShowPeerInfoModal(ViewModel.Id);
                } else {
                    Functions.ShowHandledErrorTip(r);
                }
            })();
        }

        private void FindChatMembers(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            ViewModel.ChatMembers.SearchMember();
        }

        #endregion

        #region Attachments

        private void PhotosSV_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 72) {
                    if (ViewModel.Photos.Items.Count != 0) new System.Action(async () => { await ViewModel.LoadPhotosAsync(); })();
                }
            }
        }

        private void VideosSV_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 128) {
                    if (ViewModel.Videos.Items.Count != 0) new System.Action(async () => { await ViewModel.LoadVideosAsync(); })();
                }
            }
        }

        private void AudiosSV_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 128) {
                    if (ViewModel.Audios.Items.Count != 0) new System.Action(async () => { await ViewModel.LoadAudiosAsync(); })();
                }
            }
        }

        private void FilesSV_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 128) {
                    if (ViewModel.Documents.Items.Count != 0) new System.Action(async () => { await ViewModel.LoadDocsAsync(); })();
                }
            }
        }

        private void LinksSV_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 128) {
                    if (ViewModel.Share.Items.Count != 0) new System.Action(async () => { await ViewModel.LoadLinksAsync(); })();
                }
            }
        }

        private void LoadFlipViewContents(object sender, SelectionChangedEventArgs e) {
            ChangeScrollViewerForAnimation();

            int sIndex = MainFlipView.SelectedIndex;
            SegmentedTabs.SelectedIndex = sIndex;

            FlipViewItem fvi = MainFlipView.SelectedItem as FlipViewItem;
            if (fvi == null) return;
            string tag = (fvi.Content as FrameworkElement)?.Tag?.ToString();
            if (tag == null) return;

            int index = int.Parse(tag);

            new System.Action(async () => {
                switch (index) {
                    case 1: await ViewModel.LoadPhotosAsync(); break;
                    case 2: await ViewModel.LoadVideosAsync(); break;
                    case 3: await ViewModel.LoadAudiosAsync(); break;
                    case 4: await ViewModel.LoadDocsAsync(); break;
                    case 5: await ViewModel.LoadLinksAsync(); break;
                }
            })();
        }

        private void SegmentedTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            MainFlipView.SelectedIndex = SegmentedTabs.SelectedIndex;
        }

        private void OpenAttachment(object sender, RoutedEventArgs e) {
            FrameworkElement el = sender as FrameworkElement;
            if (el.Tag != null && el.Tag is ConversationAttachment atch) {
                Attachment a = atch.Attachment;
                switch (a.Type) {
                    case AttachmentType.Photo:
                        GalleryItem gp = new GalleryItem(a.Photo);
                        PhotoViewer.Show(new Tuple<List<GalleryItem>, GalleryItem>(new List<GalleryItem> { gp }, gp));
                        break;
                    case AttachmentType.Video:
                        new System.Action(async () => { await VideoPlayerView.Show(atch.MessageId, a.Video); })();
                        break;
                    case AttachmentType.Audio:
                        AudioPlayerViewModel.PlaySong(ViewModel.Audios.Items.Select(ca => ca.Attachment.Audio).ToList(), a.Audio, PeerName.Text);
                        break;
                    case AttachmentType.Document:
                        Document doc = a.Document;
                        if (doc.Preview != null) {
                            GalleryItem dgp = new GalleryItem(doc);
                            PhotoViewer.Show(new Tuple<List<GalleryItem>, GalleryItem>(new List<GalleryItem> { dgp }, dgp));
                        } else {
                            new System.Action(async () => { await Windows.System.Launcher.LaunchUriAsync(doc.Uri); })();
                        }
                        break;
                    case AttachmentType.Link:
                        new System.Action(async () => { await Windows.System.Launcher.LaunchUriAsync(a.Link.Uri); })();
                        break;
                }
            }
        }

        private void OpenAttachmentContextMenu(UIElement sender, ContextRequestedEventArgs args) {
            FrameworkElement el = sender as FrameworkElement;
            if (el.Tag != null && el.Tag is ConversationAttachment atch) {
                args.Handled = true;

                MenuFlyout mf = new MenuFlyout();
                MenuFlyoutItem gotom = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("go_to_message") };
                MenuFlyoutItem share = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("share") };
                MenuFlyoutItem copylink = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("copy_link") };

                gotom.Click += (a, b) => {
                    Hide();
                    Main.GetCurrent().ShowConversationPage(ViewModel.Id, atch.MessageId);
                };
                share.Click += (b, c) => {
                    Hide();

                    AttachmentBase attachment = null;
                    Attachment a = atch.Attachment;
                    switch (a.Type) {
                        case AttachmentType.Photo:
                            attachment = a.Photo;
                            break;
                        case AttachmentType.Video:
                            attachment = a.Video;
                            break;
                        case AttachmentType.Audio:
                            attachment = a.Audio;
                            break;
                        case AttachmentType.Document:
                            attachment = a.Document;
                            break;
                    }

                    if (attachment != null) Main.GetCurrent().StartForwardingAttachments(new List<AttachmentBase> { attachment });
                };
                copylink.Click += (a, b) => {
                    if (!String.IsNullOrEmpty(atch.Attachment.Link?.Url)) {
                        DataPackage dp = new DataPackage();
                        dp.RequestedOperation = DataPackageOperation.Copy;
                        dp.SetText(atch.Attachment.Link.Url);
                        Clipboard.SetContent(dp);
                    }
                };

                mf.Items.Add(gotom);
                if (atch.Attachment.Type == AttachmentType.Link) {
                    mf.Items.Add(copylink);
                } else {
                    mf.Items.Add(share);
                }

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
        }

        #endregion

        #region UI

        ScrollViewer currentScrollViewer;
        double AvatarHeight { get { return Avatar.ActualHeight + Avatar.Margin.Top + Avatar.Margin.Bottom; } }
        const double UserNamePanelScale = 4;

        private void OnLoaded(object sender, RoutedEventArgs e) {
            // Disable focus on flip view (IsTabStop is not working for unknown purposes ¯\_(ツ)_/¯)
            //MainFlipView.GotFocus += (a, b) => {
            //    ((Control)FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next))?.Focus(FocusState.Programmatic);
            //};

            SetupMargins();
            Header.SizeChanged += Header_SizeChanged;

            SetupTabs();
            MainFlipView.SelectionChanged += LoadFlipViewContents;

            ChangeScrollViewerForAnimation();

            SizeChanged += OnSizeChanged;
            MainFlipView.SizeChanged += MainFlipView_SizeChanged;
            PointerEntered += OnPointerEntered; // need to change IndicatorMode for scroll bar.
            scrollBar.Scroll += ScrollBar_Scroll;

            foreach (FlipViewItem fvi in MainFlipView.Items) {
                ScrollViewer sv = (fvi.Content as ItemsRepeaterScrollHost).ScrollViewer;
                sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                sv.ViewChanging += SVViewChanging;
            }
        }

        private void SetupTabs() {
            List<string> tabs = new List<string>();

            var items = MainFlipView.Items;
            foreach (FlipViewItem item in items) {
                tabs.Add(item.Tag.ToString());
            }
            SegmentedTabs.ItemsSource = tabs;
        }

        private void Avatar_Click(object sender, RoutedEventArgs e) {
            if (Avatar.Tag != null && Avatar.Tag is Photo photo) {
                GalleryItem gp = new GalleryItem(photo);
                PhotoViewer.Show(new Tuple<List<GalleryItem>, GalleryItem>(new List<GalleryItem> { gp }, gp));
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            ChangeActionButtonsWidth();
            SetupMargins();
        }

        private void Header_SizeChanged(object sender, SizeChangedEventArgs e) {
            SetupMargins();
        }

        private void MainFlipView_SizeChanged(object sender, SizeChangedEventArgs e) {
            SetupScrollBar();
        }

        private void ActionButtons_LayoutUpdated(object sender, object e) {
            ChangeActionButtonsWidth();
        }

        private void ChangeActionButtonsWidth() {
            StackPanel sp = ActionButtons;

            double marginBetweenButtons = 8;
            double rootWidth = sp.ActualWidth;
            double smallButtonWidth = 32;

            if (rootWidth == 0) return;

            var buttons = sp.Children.Where(c => c.Visibility == Visibility.Visible);
            var smallButtons = sp.Children.Where(c => c.Visibility == Visibility.Visible && (c as FrameworkElement).Tag?.ToString() == "1");

            double buttonWidth = rootWidth - (smallButtonWidth * smallButtons.Count()) - (marginBetweenButtons * buttons.Count());
            buttonWidth = buttonWidth / (buttons.Count() - smallButtons.Count());

            foreach (FrameworkElement item in sp.Children) {
                if (item.Visibility == Visibility.Collapsed) continue;
                if (item.Tag is string t && t == "1") {
                    item.Width = smallButtonWidth;
                } else {
                    item.Width = buttonWidth;
                }
            }
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e) {
            var pointer = e.GetCurrentPoint(sender as UIElement);
            if (pointer.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse) {
                scrollBar.IndicatorMode = ScrollingIndicatorMode.MouseIndicator;
            } else {
                scrollBar.IndicatorMode = ScrollingIndicatorMode.TouchIndicator;
            }
        }

        private void ScrollBar_Scroll(object sender, ScrollEventArgs e) {
            currentScrollViewer.ChangeView(null, e.NewValue, null);
        }

        private void ChangeScrollViewerForAnimation() {
            FlipViewItem fvi = MainFlipView.SelectedItem as FlipViewItem;
            if (fvi == null) return;
            currentScrollViewer = (fvi.Content as ItemsRepeaterScrollHost).ScrollViewer;

            new System.Action(async () => {
                await Task.Delay(500);
                ChangeActionButtonsAvailability(currentScrollViewer.VerticalOffset);
                SetupScrollBar();
                UpdateScrollBarPosition();
                SetupExpressionAnimations(currentScrollViewer);
            })();
        }

        private void SetupMargins() {
            foreach (FlipViewItem fvi in MainFlipView.Items) {
                ScrollViewer sv = (fvi.Content as ItemsRepeaterScrollHost).ScrollViewer;
                FrameworkElement el = sv.Content as FrameworkElement;
                double mt = 0;
                if (el.Tag != null && el.Tag is double margin) {
                    mt = margin;
                } else {
                    mt = el.Margin.Top;
                    el.Tag = mt;
                }
                el.Margin = new Thickness(el.Margin.Left, mt + Header.ActualHeight, el.Margin.Right, el.Margin.Bottom);

                double header = Header.ActualHeight;
                double compactHeader = header - AvatarHeight - ActionButtons.ActualHeight - (UserName.ActualHeight / UserNamePanelScale);
                double hh = header - compactHeader;
                double irm = mt + el.Margin.Bottom;
                el.MinHeight = MainFlipView.ActualHeight + hh - header - irm;
            }

            Visual hv = ElementCompositionPreview.GetElementVisual(UserName);
            hv.CenterPoint = new Vector3((int)UserName.ActualWidth / 2, 0, 0);
        }

        private void SetupScrollBar() {
            new System.Action(async () => {
                await Task.Delay(250);
                scrollBar.ViewportSize = currentScrollViewer.ViewportHeight;
                scrollBar.Maximum = currentScrollViewer.ScrollableHeight;
            })();
        }

        private void UpdateScrollBarPosition() {
            scrollBar.Value = currentScrollViewer.VerticalOffset;

            string debug = $"SVEH: {currentScrollViewer.ExtentHeight}\n";
            debug += $"SVSH: {currentScrollViewer.ScrollableHeight}\n";
            debug += $"SVVO: {currentScrollViewer.VerticalOffset}\n";
            debug += $"SVVH: {currentScrollViewer.ViewportHeight}\n";
            debug += $"SBM:  {scrollBar.Maximum}\n";
            debug += $"SBV:  {scrollBar.Value}\n";
            debug += $"SBVP: {scrollBar.ViewportSize}";
            // dbg.Text = debug;
        }

        private void SVViewChanging(object sender, ScrollViewerViewChangingEventArgs e) {
            ScrollViewer sv = sender as ScrollViewer;

            // Autoscroll to fix header in full or compact state.
            double voff = e.FinalView.VerticalOffset;
            double header = Header.ActualHeight;
            double compactHeader = header - AvatarHeight - ActionButtons.ActualHeight - (UserName.ActualHeight / UserNamePanelScale);
            double hh = header - compactHeader;

            if (e.IsInertial && e.NextView.VerticalOffset == voff && voff < hh) {
                sv.CancelDirectManipulations();
                double off = voff < hh / 2 ? 0 : hh;
                sv.ChangeView(null, off, null, false);
            }

            // Disable hit testing for action buttons if offset != 0.
            ChangeActionButtonsAvailability(e.NextView.VerticalOffset);

            // Change scroll bar position
            UpdateScrollBarPosition();
        }

        private void ChangeActionButtonsAvailability(double scrollVerticalOffset) {
            bool isEnabled = scrollVerticalOffset < 2;
            ActionButtons.IsHitTestVisible = isEnabled;
            foreach (Control button in ActionButtons.Children) {
                button.IsTabStop = isEnabled;
            }
        }

        private void SetupExpressionAnimations(ScrollViewer scrollViewer) {
            // Expression animations
            ElementCompositionPreview.SetIsTranslationEnabled(Header, true);
            ElementCompositionPreview.SetIsTranslationEnabled(HeaderBackground, true);
            ElementCompositionPreview.SetIsTranslationEnabled(Avatar, true);
            ElementCompositionPreview.SetIsTranslationEnabled(UserName, true);
            ElementCompositionPreview.SetIsTranslationEnabled(ActionButtons, true);
            ElementCompositionPreview.SetIsTranslationEnabled(SegmentedTabs, true);
            Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

            float avaHeight = (float)(Math.Round(AvatarHeight));
            float usernameHeight = (float)Math.Round(UserName.ActualHeight); // user name & online info stackpanel's height
            float actionButtonsHeight = (float)Math.Round(ActionButtons.ActualHeight);

            // float a = 1.0f / avaHeight * (avaHeight + actionButtonsHeight); // for header animation
            float b = (float)UserNamePanelScale;
            float c = usernameHeight / b;

            // Avatar offset & opacity
            CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, Avatar, $"Clamp(Scroll.Translation.Y / (1 / {avaHeight} * ({avaHeight + actionButtonsHeight})), -{avaHeight}, 0)", "Translation.Y");
            CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, Avatar, $"(Clamp(1 / -{avaHeight + actionButtonsHeight} * Scroll.Translation.Y, 0, 1) - 1) * -1", "Opacity");

            // Username translation & scale
            CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, UserName, $"Clamp(Scroll.Translation.Y / (1 / {avaHeight} * ({avaHeight + actionButtonsHeight})), -{avaHeight}, 0)", "Translation.Y");
            CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, UserName, $"Clamp(Scroll.Translation.Y / (-{avaHeight + actionButtonsHeight + c} * -{b}), 1 / -{b}, 0) + 1", "Scale.X");
            CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, UserName, $"Clamp(Scroll.Translation.Y / (-{avaHeight + actionButtonsHeight + c} * -{b}), 1 / -{b}, 0) + 1", "Scale.Y");

            // Header background offset
            CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, HeaderBackground, $"Clamp(Scroll.Translation.Y, -{avaHeight + actionButtonsHeight + c}, 0)", "Translation.Y");

            // Action buttons scale & opacity
            foreach (FrameworkElement button in ActionButtons.Children) {
                ElementCompositionPreview.SetIsTranslationEnabled(button, true);
                Visual abv = ElementCompositionPreview.GetElementVisual(button);
                abv.CenterPoint = new Vector3((int)button.ActualWidth / 2, 0, 0);

                CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, button, $"(Clamp(Scroll.Translation.Y / -{avaHeight + actionButtonsHeight}, 0, 1) - 1) * -1", "Scale.X");
                CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, button, $"(Clamp(Scroll.Translation.Y / -{avaHeight + actionButtonsHeight}, 0, 1) - 1) * -1", "Scale.Y");
                CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, button, $"(Clamp(Scroll.Translation.Y / -{avaHeight + actionButtonsHeight}, 0, 1) - 1) * -1", "Opacity");
            }

            // Action buttons panel translation
            CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, ActionButtons, $"Clamp(Scroll.Translation.Y / (1 / {avaHeight} * ({avaHeight + actionButtonsHeight})), -{avaHeight + usernameHeight + c}, 0)", "Translation.Y");

            // Pivot tabs translation
            CompositionHelper.SetupExpressionAnimation(compositor, scrollViewer, SegmentedTabs, $"Clamp(Scroll.Translation.Y, -{avaHeight + actionButtonsHeight + c}, 0)", "Translation.Y");
        }

        #endregion
    }
}