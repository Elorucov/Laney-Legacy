using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.LongPoll;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.ViewManagement.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.ViewModel.Controls {
    public delegate void MessageSentDelegate(LMessage message);
    public class MessageFormViewModel : BaseViewModel {
        private int RandomId = 0;
        private int ViewId = 0;
        string vkRef = null;
        string vkRefSource = null;
        public bool isCasperChat = false;

        private bool _isEnabled = true;
        private bool _isProgressIndeterminate;
        private bool _isMessageNotEmpty;
        private long _peerId;
        private int _editingMessageId;
        private string _messageText;
        private MessageFormatData _formatData;
        private ObservableCollection<OutboundAttachmentViewModel> _attachments = new ObservableCollection<OutboundAttachmentViewModel>();
        private Visibility _hasAttachments = Visibility.Collapsed;
        private Visibility _editingMessageInfoVisibility = Visibility.Collapsed;
        private LMessage _replyMessage;
        private Sticker _sticker;
        private BotKeyboard _keyboard;
        private RelayCommand _messageSendCommand;
        private RelayCommand _showAttachmentPickerCommand;
        private RelayCommand _ShowStickersAndGraffitiFlyoutCommand;
        private RelayCommand _removeReplyMessageCommand;
        private RelayCommand _cancelEditingMessageCommand;

        public bool IsEnabled { get { return _isEnabled; } set { _isEnabled = value; OnPropertyChanged(); } }
        public bool IsProgressIndeterminate { get { return _isProgressIndeterminate; } set { _isProgressIndeterminate = value; OnPropertyChanged(); } }
        public bool IsMessageNotEmpty { get { return _isMessageNotEmpty; } private set { _isMessageNotEmpty = value; OnPropertyChanged(); } }
        public long PeerId { get { return _peerId; } set { _peerId = value; OnPropertyChanged(); } }
        public int EditingMessageId { get { return _editingMessageId; } set { _editingMessageId = value; OnPropertyChanged(); } }
        public string MessageText { get { return _messageText; } set { _messageText = value; OnPropertyChanged(); } }
        public MessageFormatData FormatData { get { return _formatData; } set { _formatData = value; OnPropertyChanged(); } }
        public ObservableCollection<OutboundAttachmentViewModel> Attachments { get { return _attachments; } private set { _attachments = value; OnPropertyChanged(); } }
        public Visibility HasAttachments { get { return _hasAttachments; } set { _hasAttachments = value; OnPropertyChanged(); } }
        public Visibility EditingMessageInfoVisibility { get { return _editingMessageInfoVisibility; } set { _editingMessageInfoVisibility = value; OnPropertyChanged(); } }
        public LMessage ReplyMessage { get { return _replyMessage; } private set { _replyMessage = value; OnPropertyChanged(); } }
        public Sticker Sticker { get { return _sticker; } set { _sticker = value; OnPropertyChanged(); } }
        public BotKeyboard Keyboard { get { return _keyboard; } set { _keyboard = value; OnPropertyChanged(); } }
        public RelayCommand MessageSendCommand { get { return _messageSendCommand; } }
        public RelayCommand ShowAttachmentPickerCommand { get { return _showAttachmentPickerCommand; } }
        public RelayCommand ShowStickersAndGraffitiFlyoutCommand { get { return _ShowStickersAndGraffitiFlyoutCommand; } }
        public RelayCommand RemoveReplyMessageCommand { get { return _removeReplyMessageCommand; } }
        public RelayCommand CancelEditingMessageCommand { get { return _cancelEditingMessageCommand; } }

        private void CheckProperty(string prop) {
            if (prop == nameof(MessageText)) CheckIsMessageNotEmpty();
            if (prop == nameof(EditingMessageId)) ChangeEditingMessageInfoVisibility();
        }

        public System.Action ReplyMessageRequested;
        public System.Action ReplyLastMessageRequested;
        public System.Action EditLastMessageRequested;
        public System.Action FocusToTextBoxRequested;

        public MessageFormViewModel(long id) {
            PropertyChanged += (a, b) => CheckProperty(b.PropertyName);
            PeerId = id;
            RandomId = GetRandom();
            ViewId = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Id;
            _messageSendCommand = new RelayCommand(SendMessage);
            _showAttachmentPickerCommand = new RelayCommand(ShowAttachmentPicker);
            _ShowStickersAndGraffitiFlyoutCommand = new RelayCommand(ShowStickersAndGraffitiFlyoutInternal);
            _removeReplyMessageCommand = new RelayCommand((e) => ReplyMessage = null);
            _cancelEditingMessageCommand = new RelayCommand(CancelEditingMessage);

            if (_attachments != null) _attachments.CollectionChanged += (a, b) => {
                HasAttachments = _attachments.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                CheckIsMessageNotEmpty();
            };
        }

        private int GetRandom() {
            DateTime a = new DateTime(2006, 10, 1, 0, 0, 0);
            TimeSpan t = DateTime.UtcNow - a;
            return (int)t.TotalSeconds;
        }

        private int GetAttachmentsCount() {
            return Attachments.Where(a => a.Type == OutboundAttachmentType.Attachment).Count();
        }

        private void CheckIsMessageNotEmpty() {
            bool a = !string.IsNullOrEmpty(MessageText);
            bool b = Attachments.Count > 0;
            IsMessageNotEmpty = a || b;
        }

        private void ChangeEditingMessageInfoVisibility() {
            EditingMessageInfoVisibility = EditingMessageId > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Commands

        private void SendMessage(object obj) {
            if (!IsEnabled) return;
            SendMessage();
        }

        private void ShowAttachmentPicker(object obj) {
            if (!IsEnabled) return;
            int count = GetAttachmentsCount();
            if (count < 10) {
                bool pollDisabled = !PeerId.IsChat() || Attachments.Any(a => a.Type == OutboundAttachmentType.Attachment && a.Attachment is Poll);

                FrameworkElement fe = obj as FrameworkElement;
                MenuFlyout mf = new MenuFlyout();

                MenuFlyoutItem photo = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("atchpicker_photo_mf") };
                MenuFlyoutItem video = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("atchpicker_video_mf") };
                MenuFlyoutItem file = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("atchpicker_file_mf") };
                MenuFlyoutItem audio = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("atchpicker_audio_mf") };
                MenuFlyoutItem poll = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("poll") };
                MenuFlyoutItem location = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("botbtn_position") };

                photo.Click += (a, b) => OpenAttachmentPicker(0);
                video.Click += (a, b) => OpenAttachmentPicker(1);
                file.Click += (a, b) => OpenAttachmentPicker(2);
                audio.Click += (a, b) => OpenAttachmentPicker(3);

                poll.Click += (a, b) => {
                    PollEditor pe = new PollEditor();
                    pe.Title = Locale.Get("polleditor_create");
                    pe.Closed += (c, d) => { if (d != null) AddPoll((Poll)d); };
                    pe.Show();
                };

                location.Click += (a, b) => {
                    PlacePicker pp = new PlacePicker();
                    pp.Title = Locale.Get("botbtn_position");
                    pp.Closed += (c, d) => SendGeolocation((Geopoint)d);
                    pp.Show();
                };

                mf.Items.Add(photo);
                mf.Items.Add(video);
                mf.Items.Add(file);
                mf.Items.Add(audio);
                if (!pollDisabled) mf.Items.Add(poll);
                mf.Items.Add(location);

                mf.Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Top;
                mf.ShowAt(fe);
            }
        }

        private void OpenAttachmentPicker(int tab) {
            if (Attachments.Count == 10) return;
            int limit = 10 - Attachments.Count;
            AttachmentPicker picker = new AttachmentPicker(limit, tab);
            picker.Closed += async (a, b) => {
                if (b is AttachmentPickerResult result) {
                    switch (result.Type) {
                        case AttachmentPickerResultType.Attachments:
                            foreach (var attachment in result.Attachments) {
                                OutboundAttachmentViewModel oavm = new OutboundAttachmentViewModel(attachment);
                                Attachments.Add(oavm);
                            }
                            break;
                        case AttachmentPickerResultType.PhotoFiles:
                            await AttachFilesAndUpload(result.Files, OutboundAttachmentUploadFileType.PhotoForWall);
                            break;
                        case AttachmentPickerResultType.VideoFiles:
                            await AttachFilesAndUpload(result.Files, OutboundAttachmentUploadFileType.VideoForWall);
                            break;
                        case AttachmentPickerResultType.Files:
                            await AttachFilesAndUpload(result.Files, OutboundAttachmentUploadFileType.DocumentForWall);
                            break;
                    }
                }
            };
            picker.Show();
        }

        private void ShowStickersAndGraffitiFlyoutInternal(object obj) {
            ShowStickersAndGraffitiFlyout(obj, 0, false);
        }

        public void ShowStickersAndGraffitiFlyout(object obj, long productId, bool isChatSticker) {
            if (!IsEnabled) return;
            if (ModalsManager.HaveOpenedModals) return;

            FrameworkElement fe = obj as FrameworkElement;
            Flyout f = new Flyout();

            var p = new StickersFlyout(f, PeerId, productId, isChatSticker);
            p.StickerSelected += async (st) => {
                if (st != null) {
                    if (st is Sticker sticker) {
                        Sticker = sticker;
                        await SendMessage();
                    } else if (st is UGCSticker ugc) {
                        Attachments.Add(new OutboundAttachmentViewModel(ugc));
                        await SendMessage();
                    }
                } else { // показываем панель смайликов
                    InvokeFocusToTextBoxRequested();
                    try {
                        CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
                    } catch {
                        // await (new MessageDialog($"{Locale.Get("emoji_panel_error")}\n\n0x{ex.HResult.ToString("x8")}: {ex.Message}", Locale.Get("global_error"))).ShowAsync();
                        InvokeFocusToTextBoxRequested();
                    }
                }
            };
            p.GraffitiSelected += (gr) => {
                if (Attachments.Count < 10) AddDocument(gr);
            };
            f.Content = p;
            f.Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Top;
            f.ShowAt(fe);
        }

        private void CancelEditingMessage(object obj) {
            ClearForm();
            EditingMessageId = 0;
        }

        #endregion

        #region Attachments

        private void SendGeolocation(Geopoint location) {
            if (location == null) return;
            Attachments.Add(new OutboundAttachmentViewModel(location.Position.Latitude, location.Position.Longitude));
        }

        private void AddPhoto(Photo p) {
            Attachments.Add(new OutboundAttachmentViewModel(p));
        }

        private void AddVideo(Video v) {
            Attachments.Add(new OutboundAttachmentViewModel(v));
        }

        private void AddDocument(Document d) {
            Attachments.Add(new OutboundAttachmentViewModel(d));
        }

        private void AddAudioMessage(AudioMessage d) {
            Attachments.Add(new OutboundAttachmentViewModel(d));
        }

        private void AddPoll(Poll poll) {
            Attachments.Add(new OutboundAttachmentViewModel(poll));
        }

        public async Task AttachFilesAndUpload(IEnumerable<StorageFile> files, OutboundAttachmentUploadFileType type) {
            int start = Attachments.Count;
            foreach (var file in files) {
                OutboundAttachmentViewModel oavm = OutboundAttachmentViewModel.CreateFromFile(PeerId, file, type, true);
                Attachments.Add(oavm);
            }
            for (int pos = start; pos < Attachments.Count; pos++) {
                await Attachments[pos].DoUpload();
                await Task.Delay(500);
            }
        }

        public async Task UploadPhoto(StorageFile file = null) {
            if (file != null) {
                await UploadPhotoMulti(new List<StorageFile> { file });
            } else {
                await UploadPhotoMulti();
            }
        }

        public async Task UploadPhotoMulti(IEnumerable<StorageFile> files = null) {
            if (files == null) {
                FileOpenPicker fop = new FileOpenPicker();
                foreach (string format in DataPackageParser.ImageFormats) {
                    fop.FileTypeFilter.Add(format);
                }
                fop.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                fop.ViewMode = PickerViewMode.Thumbnail;
                files = await fop.PickMultipleFilesAsync();
            }

            if (files != null && files.Count() > 0) {
                await AttachFilesAndUpload(files, OutboundAttachmentUploadFileType.PhotoForMessage);
            }
        }

        public async Task UploadVideo(StorageFile file = null) {
            if (file != null) {
                await UploadVideoMulti(new List<StorageFile> { file });
            } else {
                await UploadVideoMulti();
            }
        }

        public async Task UploadVideoMulti(IEnumerable<StorageFile> files = null) {
            if (files == null) {
                FileOpenPicker fop = new FileOpenPicker();
                foreach (string format in DataPackageParser.VideoFormats) {
                    fop.FileTypeFilter.Add(format);
                }
                fop.SuggestedStartLocation = PickerLocationId.VideosLibrary;
                fop.ViewMode = PickerViewMode.Thumbnail;
                files = await fop.PickMultipleFilesAsync();
            }

            if (files != null) {
                await AttachFilesAndUpload(files, OutboundAttachmentUploadFileType.VideoForMessage);
            }
        }

        public async Task AttachAudioMessage(StorageFile file) {
            if (file != null) {
                await AttachFilesAndUpload(new List<StorageFile> { file }, OutboundAttachmentUploadFileType.AudioMessage);
                //try {
                //    if (AppSession.IsFeatureAvailable(104)) {
                //        IsProgressIndeterminate = true;
                //        object r = await Messages.GetAudioMessageUploadServer();
                //        if (r is VkUploadServer dus) {
                //            await UploadAudioMsg(dus.Uri, file);
                //        } else {
                //            Functions.ShowHandledErrorDialog(r);
                //            IsProgressIndeterminate = false;
                //            UploadProgress = 0;
                //        }
                //    } else {
                //        IsProgressIndeterminate = true;
                //        object r = await Docs.GetMessagesUploadServer("audio_message", PeerId.ToString());
                //        if (r is VkUploadServer dus) {
                //            await UploadDoc(dus.Uri, file, true);
                //        } else {
                //            Functions.ShowHandledErrorDialog(r);
                //            IsProgressIndeterminate = false;
                //            UploadProgress = 0;
                //        }
                //    }
                //} catch (Exception ex) {
                //    IsProgressIndeterminate = false;
                //    UploadProgress = 0;
                //    Functions.ShowHandledErrorDialog(ex);
                //}
            }
        }

        public async Task UploadDocMulti(IEnumerable<StorageFile> files = null) {
            if (files == null) {
                FileOpenPicker fop = new FileOpenPicker();
                fop.FileTypeFilter.Add("*");
                fop.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                fop.ViewMode = PickerViewMode.List;
                files = await fop.PickMultipleFilesAsync();
            }

            if (files != null) {
                try {
                    await AttachFilesAndUpload(files, OutboundAttachmentUploadFileType.DocumentForMessage);
                } catch (Exception ex) {
                    Functions.ShowHandledErrorDialog(ex);
                }
            }
        }

        #endregion

        #region Forwarded messages

        public void AddForwardedMessages(long fromPeerId, List<LMessage> messages) {
            if (messages.Count == 0 || fromPeerId == 0) return;
            ReplyMessage = null;

            OutboundAttachmentViewModel oavm = Attachments.FirstOrDefault(a => a.Type == OutboundAttachmentType.ForwardedMessages);

            if (oavm != null && oavm.ForwardedMessages.Count == 0) Attachments.Remove(oavm);
            if (oavm == null) {
                oavm = new OutboundAttachmentViewModel(messages, fromPeerId);
                Attachments.Insert(0, oavm);
            }
        }

        public void AddReplyMessage(LMessage message) {
            if (EditingMessageId > 0) CancelEditingMessage(null);

            OutboundAttachmentViewModel oavm = Attachments.FirstOrDefault(a => a.Type == OutboundAttachmentType.ForwardedMessages);
            if (oavm != null) Attachments.Remove(oavm);

            ReplyMessage = message;
            InvokeReplyMessageRequested();
        }

        #endregion

        #region User typing and recording

        DateTime lastTimeUserIsTypingWasCalled = DateTime.Now;
        public async Task SendTypingActivity() {
            if (AppParameters.DontSendActivity) return;
            if ((DateTime.Now - lastTimeUserIsTypingWasCalled).TotalSeconds <= 10) return;
            await Messages.SetActivity(PeerId, "typing");
            lastTimeUserIsTypingWasCalled = DateTime.Now;
        }

        public async Task SendRecordingActivity() {
            if (!AppParameters.DontSendActivity) await Messages.SetActivity(PeerId, "audiomessage");
        }

        #endregion

        public void SetRef(string vkRef, string vkRefSource) {
            this.vkRef = vkRef;
            this.vkRefSource = vkRefSource;
        }

        public async Task SendMessage(bool silent = false, int ttl = 0) {
            if (IsEnabled && !IsProgressIndeterminate) {

                // Check if files are still uploading...
                bool isStillUploading = Attachments.Any(a => a.UploadState == OutboundAttachmentUploadState.InProgress);
                if (isStillUploading) return;

                List<string> unsuccessfulUploadFileNames = new List<string>();

                foreach (var attachment in Attachments) {
                    if (attachment.UploadState == OutboundAttachmentUploadState.Failed) {
                        unsuccessfulUploadFileNames.Add(attachment.DisplayName);
                    }
                }
                int unsuccessfulUploads = unsuccessfulUploadFileNames.Count;
                if (unsuccessfulUploads > 0) {
                    string desc = unsuccessfulUploads == 1 ?
                        String.Format(Locale.GetForFormat("sharetarget_err_upload_desc_single"), unsuccessfulUploadFileNames[0]) :
                        String.Format(Locale.GetForFormat("sharetarget_err_upload_desc_multi"), String.Join("\n", unsuccessfulUploadFileNames));
                    ContentDialog dlg = new ContentDialog {
                        Title = Locale.Get("sharetarget_err_upload_title"),
                        Content = desc,
                        PrimaryButtonText = Locale.Get("yes"),
                        SecondaryButtonText = Locale.Get("no")
                    };
                    var result = await dlg.ShowAsync();
                    if (result != ContentDialogResult.Primary) return;
                }

                IsEnabled = false;
                IsProgressIndeterminate = true;


                double? glat = null;
                double? glong = null;

                var place = Attachments.Where(a => a.Type == OutboundAttachmentType.Place).FirstOrDefault();
                if (place != null) {
                    glat = place.Place.Item1;
                    glong = place.Place.Item2;
                }

                List<string> attachmentsList = new List<string>();
                long FwdMessageFromPeerId = 0;
                List<int> ForwardedMessagesIds = new List<int>();
                string forward = "";

                foreach (OutboundAttachmentViewModel oavm in Attachments) {
                    if (oavm.Type == OutboundAttachmentType.ForwardedMessages && oavm.ForwardedMessages?.Count > 0) {
                        FwdMessageFromPeerId = oavm.FromPeerId;
                        ForwardedMessagesIds = oavm.ForwardedMessages.Select(m => m.ConversationMessageId).ToList();
                    } else if (oavm.Type == OutboundAttachmentType.Attachment) {
                        if (oavm.Attachment != null) attachmentsList.Add(oavm.Attachment.ToString());
                    }
                }

                if (ForwardedMessagesIds.Count > 0) {
                    forward = $"{{\"peer_id\":{FwdMessageFromPeerId},\"conversation_message_ids\":[{String.Join(',', ForwardedMessagesIds)}]}}";
                } else if (ReplyMessage != null) {
                    forward = $"{{\"peer_id\":{PeerId},\"conversation_message_ids\":[{ReplyMessage.ConversationMessageId}],\"is_reply\":true}}";
                }

                string attachments = null;
                if (attachmentsList.Count > 0) attachments = string.Join(',', attachmentsList);
                string txt = !string.IsNullOrEmpty(MessageText) ? MessageText.Replace("\r\n", "\r").Replace("\r", "\r\n") : null;

                object resp = EditingMessageId <= 0 ? await Messages.Send(PeerId, RandomId, txt, glat, glong, attachments,
                    forward, Sticker?.StickerId, null,
                    AppParameters.MessageSendDontParseLinks, AppParameters.MessageSendDisableMentions, silent, ttl,
                    vkRef, vkRefSource, FormatData, isCasperChat || ttl > 0 ? API.WebToken : null) :
                    await Messages.Edit(PeerId, EditingMessageId, txt, glat, glong, attachments,
                    AppParameters.MessageSendDontParseLinks, ForwardedMessagesIds.Count > 0, FormatData, isCasperChat || ttl > 0 ? API.WebToken : null);
                if (resp is int || resp is MessageSendResponse) {
                    ClearForm();
                    vkRef = null;
                    vkRefSource = null;
                    EditingMessageId = 0;
                    RandomId = GetRandom();
                    IsEnabled = true;
                    IsProgressIndeterminate = false;
                } else {
                    Functions.ShowHandledErrorDialog(resp);
                    IsEnabled = true;
                    IsProgressIndeterminate = false;
                }
            }
        }

        public async Task SendMessageToBot(BotButtonAction bba, string customText = null, int ownerMessageId = 0, long authorId = 0, Button uiButton = null) {
            IsEnabled = false;
            object resp = null;
            string t = string.IsNullOrEmpty(customText) ? bba.Label : customText;
            switch (bba.Type) {
                case BotButtonType.Text:
                    IsProgressIndeterminate = true;
                    resp = await Messages.Send(PeerId, RandomId, t, null, null, null, null, null, bba.Payload,
                    AppParameters.MessageSendDontParseLinks, AppParameters.MessageSendDisableMentions, false, 0,
                    vkRef, vkRefSource, null, isCasperChat ? API.WebToken : null);
                    break;
                case BotButtonType.OpenApp:
                    string link = string.IsNullOrEmpty(bba.Hash) ? $"https://m.vk.com/app{bba.AppId}" : $"https://m.vk.com/app{bba.AppId}#{bba.Hash}";
                    await Launcher.LaunchUriAsync(new Uri(link));
                    break;
                case BotButtonType.VKPay:
                    await Launcher.LaunchUriAsync(new Uri($"https://m.vk.com/app6217559#{bba.Hash}"));
                    break;
                case BotButtonType.OpenLink:
                    await Launcher.LaunchUriAsync(bba.LinkUri);
                    break;
                case BotButtonType.Location:
                    IsProgressIndeterminate = true;
                    BasicGeoposition? bg = await GetGeopositionAsync();
                    if (bg != null) {
                        double? glat = bg?.Latitude;
                        double? glong = bg?.Longitude;
                        Tips.Show($"Lat:  {glat}\nLong: {glong}");
                        resp = await Messages.Send(PeerId, RandomId, Locale.Get("botbtn_position"), glat, glong, null, null, null, bba.Payload,
                            AppParameters.MessageSendDontParseLinks, AppParameters.MessageSendDisableMentions, false, 0,
                            vkRef, vkRefSource, null, isCasperChat ? API.WebToken : null);
                    } else {
                        Tips.Show(Locale.Get("geo_error"));
                    }
                    break;
                case BotButtonType.Callback:
                    await Task.Delay(1);
                    object eresp = await Messages.SendMessageEvent(PeerId, bba.Payload, ownerMessageId, authorId);
                    if (eresp is string eventId) {
                        eventId = eventId.Replace("\"", "");
                        LongPoll.AddBotCallbackEventId(eventId);
                        Log.Info($"sendMessageEvent returned event_id \"{eventId}\"");
                        if (uiButton != null) uiButton.Tag = eventId;
                    } else {
                        Functions.ShowHandledErrorTip(eresp);
                    }
                    break;
            }
            if (resp is MessageSendResponse) {
                ClearForm();
                vkRef = null;
                vkRefSource = null;
                EditingMessageId = 0;
                RandomId = GetRandom();
                IsEnabled = true;
                IsProgressIndeterminate = false;
            } else {
                Functions.ShowHandledErrorDialog(resp);
                IsEnabled = true;
                IsProgressIndeterminate = false;
            }
        }

        public void StartEditing(LMessage msg) {
            ClearForm();
            EditingMessageId = msg.ConversationMessageId;
            FormatData = msg.FormatData;
            MessageText = msg.Text;
            if (msg.ForwardedMessages.Count > 0) {
                long fromPeerId = msg.ForwardedMessages.FirstOrDefault().PeerId;
                AddForwardedMessages(fromPeerId, msg.ForwardedMessages);
            }

            foreach (var attach in msg.Attachments) {
                switch (attach.Type) {
                    case AttachmentType.Photo:
                        AddPhoto(attach.Photo);
                        break;
                    case AttachmentType.Video:
                        AddVideo(attach.Video);
                        break;
                    case AttachmentType.Document:
                        AddDocument(attach.Document);
                        break;
                    case AttachmentType.Audio:
                        Attachments.Add(new OutboundAttachmentViewModel(attach.Audio));
                        break;
                    case AttachmentType.AudioMessage:
                        AddAudioMessage(attach.AudioMessage);
                        break;
                    case AttachmentType.Poll:
                        AddPoll(attach.Poll);
                        break;
                    case AttachmentType.Story:
                        Attachments.Add(new OutboundAttachmentViewModel(attach.Story));
                        break;
                    case AttachmentType.Wall:
                        Attachments.Add(new OutboundAttachmentViewModel(attach.Wall));
                        break;
                    case AttachmentType.WallReply:
                        Attachments.Add(new OutboundAttachmentViewModel(attach.WallReply));
                        break;
                    case AttachmentType.Podcast:
                        Attachments.Add(new OutboundAttachmentViewModel(attach.Podcast));
                        break;
                    case AttachmentType.Narrative:
                        Attachments.Add(new OutboundAttachmentViewModel(attach.Narrative));
                        break;
                    case AttachmentType.AudioPlaylist:
                        Attachments.Add(new OutboundAttachmentViewModel(attach.AudioPlaylist));
                        break;
                        // TODO: по-другому добавлять вложения.
                }
            }

            if (msg.Geo != null) {
                GeoCoordinates c = msg.Geo.Coordinates;
                Attachments.Add(new OutboundAttachmentViewModel(c.Latitude, c.Longitude));
            }
        }

        private async Task<BasicGeoposition?> GetGeopositionAsync() {
            var accessStatus = await Geolocator.RequestAccessAsync();
            switch (accessStatus) {
                case GeolocationAccessStatus.Allowed:
                    Geolocator geolocator = new Geolocator();
                    Geoposition pos = await geolocator.GetGeopositionAsync();
                    if (pos != null) {
                        return pos.Coordinate.Point.Position;
                    }
                    return null;
                default:
                    return null;
            }
        }

        private void ClearForm() {
            MessageText = "";
            Attachments.Clear();
            ReplyMessage = null;
            Sticker = null;
        }

        public void InvokeReplyMessageRequested() {
            ReplyMessageRequested?.Invoke();
        }

        public void InvokeReplyLastMessageRequested() {
            ReplyLastMessageRequested?.Invoke();
        }

        public void InvokeEditLastMessageRequested() {
            EditLastMessageRequested?.Invoke();
        }

        public void InvokeFocusToTextBoxRequested() {
            FocusToTextBoxRequested?.Invoke();
        }
    }
}