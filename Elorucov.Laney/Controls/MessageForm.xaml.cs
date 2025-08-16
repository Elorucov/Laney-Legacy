using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Objects;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class MessageForm : UserControl {
        private static MessageForm GetForCurrentView() {
            var props = CoreApplication.GetCurrentView().Properties;
            if (!props.ContainsKey("mform")) return null;
            return CoreApplication.GetCurrentView().Properties["mform"] as MessageForm;
        }

        RichEditBox MyMessage;

        public MessageForm() {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7) && !AppParameters.UseLegacyMREBImplForModernWindows) {
                MessageFormContainer.ContentTemplate = MessageRichTextBoxModern;
            } else {
                MessageFormContainer.ContentTemplate = MessageRichTextBoxLegacy;
            }

            var props = CoreApplication.GetCurrentView().Properties;
            if (props.ContainsKey("mform")) {
                props["mform"] = this;
            } else {
                props.Add("mform", this);
            }

            if (AppParameters.ShowDebugItemsInMenu) {
                var mfi = new MenuFlyoutItem { Text = "Copy FormatData to clipboard" };
                mfi.Click += Mfi_Click;
                SendMethodsFlyout.Items.Add(new MenuFlyoutSeparator());
                SendMethodsFlyout.Items.Add(mfi);
            }


            DataContextChanged += MessageForm_DataContextChanged;
            MentionsHelper.MentionsPicker.MentionPicked += MentionsPicker_MentionPicked;

            // Hotkeys
            CoreApplication.GetCurrentView().CoreWindow.KeyDown += CoreWindow_KeyDown;
            Unloaded += (a, b) => CoreApplication.GetCurrentView().CoreWindow.KeyDown -= CoreWindow_KeyDown;
        }

        private void MyMessage_Loading(FrameworkElement sender, object args) {
            MyMessage = sender as RichEditBox;

            // TextBox keydown event handler
            KeyEventHandler keh = new KeyEventHandler(ButtonEvents);
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5)) {
                MyMessage.AddHandler(TextBox.PreviewKeyDownEvent, keh, true);
            } else {
                MyMessage.AddHandler(TextBox.KeyDownEvent, keh, true);
            }

            if (MyMessage is MessageRichEditBox1809) {
                MyMessage.IsSpellCheckEnabled = AppParameters.EnableSpellCheckForMessageForm;
                MyMessage.IsTextPredictionEnabled = AppParameters.EnableSpellCheckForMessageForm;
            }

            MyMessage.TextChanged += CheckStickersSuggestions;
        }

        private void Mfi_Click(object sender, RoutedEventArgs e) {
            DataPackage dp = new DataPackage();
            dp.RequestedOperation = DataPackageOperation.Copy;
            dp.SetText(JsonConvert.SerializeObject(ViewModel.FormatData, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            Clipboard.SetContent(dp);
            Tips.Show("Copied to clipboard.");
        }

        MessageFormViewModel ViewModel => DataContext as MessageFormViewModel;

        //

        private void MessageForm_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            if (ViewModel == null) return;
            ViewModel.ReplyMessageRequested += () => FocusToForm();
            ViewModel.EditLastMessageRequested += () => FocusToForm();
            ViewModel.FocusToTextBoxRequested += () => FocusToForm();

            FocusToForm();
        }

        private void CheckStickersSuggestions(object sender, RoutedEventArgs e) {
            if (!AppParameters.StickersKeywordsEnabled) return;
            bool forceHide = ViewModel == null || (ViewModel.Attachments.Count > 0 || !ViewModel.IsEnabled);
            Pages.ConversationView.Current.CheckAndShowStickersSuggestions(ViewModel?.MessageText, forceHide);
        }

        private void FocusToForm() {
            new System.Action(async () => {
                await Task.Delay(100);
                IMessageRichEditBox mform = MyMessage as IMessageRichEditBox;
                var l = mform.Text.Length;
                MyMessage.Document.Selection.StartPosition = l;
                MyMessage.Document.Selection.EndPosition = l;
                MyMessage.Focus(FocusState.Keyboard);
            })();
        }

        private void Send(bool silent = false) {
            new System.Action(async () => {
                if (!ViewModel.IsMessageNotEmpty) return;
                await ViewModel.SendMessage(silent);
                MyMessage.Focus(FocusState.Keyboard);

                // Hint about send settings flyout
                if (!AppParameters.HintMessageSendFlyout) {
                    AppParameters.HintMessageSendFlyout = true;

                    TeachingTip tt = new TeachingTip {
                        Title = Locale.Get(UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Touch ? "hint_msg_flyout_touch_subtitle" : "hint_msg_flyout_mouse_subtitle"),
                        PreferredPlacement = TeachingTipPlacementMode.TopLeft,
                        Target = sendBtn,
                    };
                    Tips.AddToAppRoot(tt);
                    tt.IsOpen = true;
                    Tips.FixUI(tt);
                }
            })();
        }

        private void ShowSendSettings(object sender, RightTappedRoutedEventArgs e) {
            e.Handled = true;
            ShowSendSettings((FrameworkElement)sender);
        }

        private void ShowSendSettings(FrameworkElement target) {
            if (ViewModel == null) return;
            if (ViewModel.IsMessageNotEmpty) {
                SendMethodsFlyout.ShowAt(target);
            } else {
                SendMethodsFlyoutForEmpty.ShowAt(target);
            }
        }

        private void ButtonEvents(object sender, KeyRoutedEventArgs e) {
            if (ViewModel == null && Pages.ConversationView.Current == null) return;
            new System.Action(async () => {
                bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                bool shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                bool isStickersSuggestionsShowing = Pages.ConversationView.Current != null && Pages.ConversationView.Current.IsStickersSuggestionsContainerShowing;

                if (e.Key == VirtualKey.Enter) {
                    if (!AppParameters.MessageSendEnterButtonMode) {
                        if (ctrl) {
                            e.Handled = true;
                            Send(shift);
                        }
                    } else {
                        if (!shift) {
                            e.Handled = true;
                            Send(ctrl);
                        }
                    }
                } else if (e.Key == VirtualKey.Tab) {
                    ViewModel?.ShowStickersAndGraffitiFlyoutCommand.Execute(stckBtn);
                } else if (e.Key == VirtualKey.Up && string.IsNullOrEmpty(ViewModel.MessageText)) {
                    ViewModel?.InvokeEditLastMessageRequested();
                } else if (e.Key == VirtualKey.Up && isStickersSuggestionsShowing) {
                    Pages.ConversationView.Current?.FocusToStickersSuggestions();
                } else if (e.Key == VirtualKey.Down && string.IsNullOrEmpty(ViewModel.MessageText)) {
                    ViewModel?.InvokeReplyLastMessageRequested();
                } else {
                    await ViewModel?.SendTypingActivity();
                }

                // Mentions
                if (ViewModel != null && MentionsHelper.MentionsPicker != null && ViewModel.PeerId.IsChat() &&
                    MentionsHelper.MentionsPicker.Visibility == Visibility.Visible &&
                    (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)) {
                    MentionsHelper.MentionsPicker.FocusToList();
                }

                // Paste file from clipboard
                if (ViewModel == null) return;
                if ((ctrl && e.Key == VirtualKey.V) || (shift && e.Key == VirtualKey.Insert)) {
                    if (ViewModel.Attachments.Where(a => a.Type == OutboundAttachmentType.Attachment).Count() >= 10) return;

                    try {
                        var dataPackageView = Clipboard.GetContent();
                        if (dataPackageView.Contains(StandardDataFormats.Bitmap)) {
                            e.Handled = true;
                            IRandomAccessStreamReference imageReceived = await dataPackageView.GetBitmapAsync();
                            if (imageReceived != null) {
                                var ft = new List<string>(dataPackageView.AvailableFormats);
                                var fileTypes = new List<string>(dataPackageView.Properties.FileTypes);
                                StorageFile file = null;

                                if (ft.Contains("SystemInputDataTransferContent")) { // гифка из системной панели emoji
                                    file = await DataPackageParser.SaveBitmapAsDocFromClipboardAsync(imageReceived, "gif");
                                    await ViewModel.UploadDocMulti(new List<StorageFile> { file });
                                } else {
                                    file = await DataPackageParser.SaveBitmapFromClipboardAsync(imageReceived);
                                    await ViewModel.UploadPhoto(file);
                                }
                            }
                        } else if (dataPackageView.Contains(StandardDataFormats.StorageItems)) {
                            e.Handled = true;
                            var files = await dataPackageView.GetStorageItemsAsync();
                            await DataPackageParser.UploadFilesFromClipboardAsync(ViewModel, files);
                        }
                    } catch (Exception ex) {
                        Log.Error($"Error while getting and sending content from clipboard! 0x{ex.HResult.ToString("x8")}");
                        Functions.ShowHandledErrorDialog(ex);
                    }
                }
            })();
        }

        // Buttons
        private void RemoveOutboundAttachment(object sender, RoutedEventArgs e) {
            if (ViewModel.IsProgressIndeterminate) return;
            OutboundAttachmentViewModel oavm = (sender as FrameworkElement).DataContext as OutboundAttachmentViewModel;
            if (oavm.UploadState == OutboundAttachmentUploadState.InProgress) oavm.CancelUpload();
            ViewModel.Attachments.Remove(oavm);
        }

        private void ShowAudioRecorder(object sender, RoutedEventArgs e) {
            audioRec.Show();
        }

        private void AttachAudioMessage(object sender, Windows.Storage.StorageFile e) {
            new System.Action(async () => { await ViewModel.AttachAudioMessage(e); })();
        }

        private void BotButtonClicked(object sender, BotButtonAction e) {
            new System.Action(async () => {
                Group g = Services.AppSession.GetCachedGroup(-botKeyboard.Keyboard.AuthorId);
                string n = g != null ? $"[club{-botKeyboard.Keyboard.AuthorId}|{g.ScreenName}] {e.Label}" : $"[club{-botKeyboard.Keyboard.AuthorId}|{e.Label}]";

                string text = ViewModel.PeerId.IsChat() ? n : e.Label;
                await ViewModel.SendMessageToBot(e, text, 0, botKeyboard.Keyboard.AuthorId, uiButton: (Button)sender);
            })();
        }

        private void AudioRec_SendRecordingActivityRequested(object sender, EventArgs e) {
            ViewModel?.SendRecordingActivity();
        }

        private void SendSelfDestructMessage(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                int sec = int.Parse((sender as FrameworkElement).Tag.ToString());
                await ViewModel.SendMessage(ttl: sec);
            })();
        }

        private void SendSilentMessage(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                await ViewModel.SendMessage(silent: true);
            })();
        }

        private void InsertThatKaomoji(object sender, RoutedEventArgs e) {
            MyMessage.Document.Selection.Text = "¯\\_(ツ)_/¯";
        }

        // Mentions

        private void CheckMentions(RichEditBox sender, RichEditBoxTextChangingEventArgs args) {
            if (AppSession.CurrentConversationVM == null) return;

            new System.Action(async () => {
                await Task.Delay(32);
                try {
                    var memberIds = AppSession.CurrentConversationVM.Members.Select(m => m.MemberId).ToList();
                    bool k = MentionsHelper.CheckMentions(sender, memberIds, AppSession.CurrentConversationVM.MemberUsers, AppSession.CurrentConversationVM.MemberGroups);
                    if (!k) MentionsHelper.MentionsPicker.Visibility = Visibility.Collapsed;
                } catch (Exception ex) {
                    Log.Error(ex, $"Unable to check and show mentions! Conv ID: {AppSession.CurrentConversationVM.ConversationId}");
                }
            })();
        }

        private void MentionsPicker_MentionPicked(object sender, MentionItem e) {
            IMessageRichEditBox mform = MyMessage as IMessageRichEditBox;
            string text = mform.Text;
            string mentiontemp = (sender as MentionsPicker).SearchDomain;
            int startIndex = text.LastIndexOf(mentiontemp);
            if (startIndex < 0) return;
            string mention = $"@{e.Domain} ";

            var range = MyMessage.Document.GetRange(startIndex, startIndex + Math.Max(mentiontemp.Length, 1));
            range.Text = mention;

            MyMessage.Focus(FocusState.Keyboard);
            MyMessage.Document.Selection.StartPosition = range.EndPosition;
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args) {
            bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

            if (ctrl && args.VirtualKey == VirtualKey.Tab) {
                ViewModel?.ShowStickersAndGraffitiFlyoutCommand.Execute(stckBtn);
            } else if (ctrl && args.VirtualKey == VirtualKey.S) {
                ShowSendSettings(sendBtn);
            }
        }

        // Public

        public static void TryFocusToTextBox() {
            if (UIViewSettings.GetForCurrentView().UserInteractionMode != UserInteractionMode.Mouse) return;
            MessageForm form = GetForCurrentView();
            bool result = form != null ? form.MyMessage.Focus(FocusState.Keyboard) : false;
            if (!result) Log.Warn($"Failed to focus to message form's text box! (isnull: {form == null})");
        }

        public static void TryShowStickersFlyout(long productId = 0) {
            MessageForm form = GetForCurrentView();
            MessageFormViewModel mfvm = form?.DataContext as MessageFormViewModel;
            if (Pages.ConversationView.Current?.ViewModel?.CanWrite.Allowed == true) mfvm?.ShowStickersAndGraffitiFlyout(form.stckBtn, productId, false);
        }

        public static void TryShowStickersFlyoutUGC(long ownerId, long packId) {
            new System.Action(async () => {
                MessageForm form = GetForCurrentView();
                MessageFormViewModel mfvm = form?.DataContext as MessageFormViewModel;
                var cvm = Pages.ConversationView.Current?.ViewModel;
                if (cvm == null) return;

                if (cvm.ConversationId == ownerId) {
                    if (cvm.CanWrite.Allowed == true) mfvm?.ShowStickersAndGraffitiFlyout(form.stckBtn, packId, true);
                } else {
                    ContentDialog dlg = new ContentDialog {
                        Title = Locale.Get("ugc_sticker_chat_restriction_modal_title"),
                        Content = Locale.Get("ugc_sticker_chat_restriction_modal_text"),
                        PrimaryButtonText = Locale.Get("modal_ok"),
                        DefaultButton = ContentDialogButton.Primary
                    };
                    await dlg.ShowAsync();
                }
            })();
        }
    }
}