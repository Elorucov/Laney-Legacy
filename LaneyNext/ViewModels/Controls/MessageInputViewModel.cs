using ELOR.VKAPILib.Objects;
using ELOR.VKAPILib.Objects.Messages;
using Elorucov.Laney.Controls;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace Elorucov.Laney.ViewModels.Controls
{
    public class MessageInputViewModel : BaseViewModel
    {
        internal static readonly byte[] Enhancements = new byte[] { 0x4b, 0x61, 0x4d, 0x54 };

        private int _editMessageId = Int32.MaxValue;
        private string _text;
        private MessageViewModel _replyMessage;
        private ThreadSafeObservableCollection<OutboundAttachmentViewModel> _outboundAttachments = new ThreadSafeObservableCollection<OutboundAttachmentViewModel>();
        private bool _hasAttachments;
        private bool _sendButtonVisibility;
        private ThreadSafeObservableCollection<MessageTemplate> _messageTemplates = new ThreadSafeObservableCollection<MessageTemplate>();
        private bool _isTemplatesLoading = false;
        private RelayCommand _sendCommand;
        private RelayCommand _recordVoiceCommand;
        private RelayCommand _stickerPickerCommand;
        private RelayCommand _loadTemplatesCommand;
        private RelayCommand _addTemplateCommand;
        private RelayCommand _deleteTemplateCommand;

        public int EditMessageId { get { return _editMessageId; } set { _editMessageId = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsInEditMode)); } }
        public string Text { get { return _text; } set { _text = value; OnPropertyChanged(); } }
        public MessageViewModel ReplyMessage { get { return _replyMessage; } set { _replyMessage = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<OutboundAttachmentViewModel> OutboundAttachments { get { return _outboundAttachments; } private set { _outboundAttachments = value; OnPropertyChanged(); } }
        public bool HasAttachments { get { return _hasAttachments; } private set { _hasAttachments = value; OnPropertyChanged(); } }
        public bool SendButtonVisibility { get { return _sendButtonVisibility; } set { _sendButtonVisibility = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<MessageTemplate> MessageTemplates { get { return _messageTemplates; } set { _messageTemplates = value; OnPropertyChanged(); } }
        public bool IsTemplatesLoading { get { return _isTemplatesLoading; } set { _isTemplatesLoading = value; OnPropertyChanged(); } }

        public RelayCommand SendCommand { get { return _sendCommand; } set { _sendCommand = value; OnPropertyChanged(); } }
        public RelayCommand RecordVoiceCommand { get { return _recordVoiceCommand; } set { _recordVoiceCommand = value; OnPropertyChanged(); } }
        public RelayCommand StickerPickerCommand { get { return _stickerPickerCommand; } set { _stickerPickerCommand = value; OnPropertyChanged(); } }
        public RelayCommand LoadTemplatesCommand { get { return _loadTemplatesCommand; } set { _loadTemplatesCommand = value; OnPropertyChanged(); } }
        public RelayCommand AddTemplateCommand { get { return _addTemplateCommand; } set { _addTemplateCommand = value; OnPropertyChanged(); } }
        public RelayCommand DeleteTemplateCommand { get { return _deleteTemplateCommand; } set { _deleteTemplateCommand = value; OnPropertyChanged(); } }

        public bool IsInEditMode { get { return EditMessageId > 0 && EditMessageId < Int32.MaxValue; } }
        private DateTime? EditMessageSentTime;

        public int ForwardedMessagesCount { get { return GetForwardedMessagesCount(); } }
        public int AttachmentsCount { get { return GetAttachmentsCount(); } }

        private ConversationViewModel _ownerConversation;

        public MessageInputViewModel(ConversationViewModel owner)
        {
            _ownerConversation = owner;
            OutboundAttachments.CollectionChanged += (a, b) =>
            {
                HasAttachments = OutboundAttachments.Count > 0;
                ChangeSendButtonVisibility();
            };
            SendCommand = new RelayCommand((o) => SendMessage());
            StickerPickerCommand = new RelayCommand(ShowStickerPicker);
            LoadTemplatesCommand = new RelayCommand(LoadMessageTemplates);
            AddTemplateCommand = new RelayCommand((o) => Utils.ShowUnderConstructionInfo());
            DeleteTemplateCommand = new RelayCommand((o) => Utils.ShowUnderConstructionInfo());

            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(Text):
                        ChangeSendButtonVisibility();
                        CheckStickersSuggestions();
                        break;
                }
            };
        }

        #region Public methods

        public void Attach(AttachmentBase attachment)
        {
            OutboundAttachments.Add(new OutboundAttachmentViewModel(attachment));
        }

        public void Attach(OutboundAttachmentViewModel oavm)
        {
            OutboundAttachments.Add(oavm);
        }

        public async void AttachFromDataPackageView(DataPackageView dpview)
        {
            int limit = 10 - OutboundAttachments.Count;
            if (limit == 0) return;
            List<OutboundAttachmentViewModel> attachments = await OutboundAttachmentViewModel.CreateFromDataPackageView(_ownerConversation.Id, dpview, OutboundAttachmentUploadFileType.Doc, limit);
            for (int i = 0; i < Math.Min(limit, attachments.Count); i++)
            {
                OutboundAttachments.Add(attachments[i]);
            }
        }

        public async void AttachFromDataPackageViewNonDoc(DataPackageView dpview, OutboundAttachmentUploadFileType type)
        {
            bool ok = false;
            switch (type)
            {
                case OutboundAttachmentUploadFileType.Photo: ok = await DataPackageViewHelpers.HasOnlyImageFiles(dpview); break;
                case OutboundAttachmentUploadFileType.Video: ok = await DataPackageViewHelpers.HasOnlyVideoFiles(dpview); break;
            }
            if (!ok) return;

            int limit = 10 - OutboundAttachments.Count;
            if (limit == 0) return;
            List<OutboundAttachmentViewModel> attachments = await OutboundAttachmentViewModel.CreateFromDataPackageView(_ownerConversation.Id, dpview, type, limit);
            for (int i = 0; i < Math.Min(limit, attachments.Count); i++)
            {
                OutboundAttachments.Add(attachments[i]);
            }
        }

        public void AttachForwardedMessages(List<MessageViewModel> messages, int groupId = 0)
        {
            if (OutboundAttachments.Count > 0 && OutboundAttachments[0].Type == OutboundAttachmentType.ForwardedMessages)
            {
                OutboundAttachmentViewModel oavm = OutboundAttachments[0];
                oavm.ForwardedMessages.Clear();
                messages.ForEach(m => oavm.ForwardedMessages.Add(m));
            }
            else
            {
                OutboundAttachments.Insert(0, new OutboundAttachmentViewModel(messages, groupId));
            }
        }

        public void AttachForwardedMessage(MessageViewModel message, int groupId = 0)
        {
            AttachForwardedMessages(new List<MessageViewModel> { message }, groupId);
        }

        #endregion

        private int GetForwardedMessagesCount()
        {
            if (OutboundAttachments.Count > 0 && OutboundAttachments[0].Type == OutboundAttachmentType.ForwardedMessages)
            {
                return OutboundAttachments[0].ForwardedMessages.Count;
            }
            return 0;
        }

        private int GetAttachmentsCount()
        {
            if (OutboundAttachments.Count == 0) return 0;
            return OutboundAttachments.Where(a => a.Type == OutboundAttachmentType.Attachment).Count();
        }

        private int GetOutboundAttachmentsCountByUploadState(OutboundAttachmentUploadState state)
        {
            return OutboundAttachments.Where(a => a.Type == OutboundAttachmentType.Attachment && a.UploadState == state).Count();
        }

        private void ChangeSendButtonVisibility()
        {
            SendButtonVisibility = HasAttachments || !String.IsNullOrEmpty(Text);
        }

        private void CheckStickersSuggestions()
        {
            if (!Core.Settings.SuggestStickers) return;
            List<Sticker> stickers = StickersKeywords.GetStickersByWord(Text);

            // Add link to bot
            if (stickers != null && stickers.LastOrDefault()?.StickerId > 0) stickers.Add(new Sticker
            {
                StickerId = 0
            });

            _ownerConversation.StickersSuggestions = stickers == null ? null : new ObservableCollection<Sticker>(stickers);
        }

        private void ShowStickerPicker(object obj)
        {
            VK.VKUI.Popups.Flyout sf = new VK.VKUI.Popups.Flyout() { Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Top };

            StickerPicker g = new StickerPicker(sf) { Margin = new Thickness(-12) }; // TODO: Fix padding in vkui flyout
            g.StickerSelected += (s) => SendSticker(s);

            sf.Content = g;
            sf.ShowAt(obj as FrameworkElement);
        }

        private async void LoadMessageTemplates(object obj)
        {
            MessageTemplates.Clear();
            IsTemplatesLoading = true;
            try
            {
                VKList<MessageTemplate> response = await VKSession.Current.API.Messages.GetTemplatesAsync(VKSession.Current.GroupId);
                MessageTemplates = new ThreadSafeObservableCollection<MessageTemplate>(response.Items);
            }
            catch (Exception ex)
            {

            }
            IsTemplatesLoading = false;
        }

        public async void SendMessage()
        {
            if (!HasAttachments && String.IsNullOrEmpty(Text)) return;
            string error = String.Empty;

            int failedAttachmentsCount = GetOutboundAttachmentsCountByUploadState(OutboundAttachmentUploadState.Failed);
            if (failedAttachmentsCount > 0)
            {
                // TODO: Диалоговое окно с подтверждением об отправке сообщения с удалением "неудачных" вложений.
                await new MessageDialog($"You have {failedAttachmentsCount} failed attachments. Please re-upload or delete these attachments.", "Error").ShowAsync();
                return;
            }

            int inProgressAttachmentsCount = GetOutboundAttachmentsCountByUploadState(OutboundAttachmentUploadState.InProgress);
            if (inProgressAttachmentsCount > 0)
            {
                // TODO: Диалоговое окно с предупреждением о недозагруженных вложений.
                await new MessageDialog($"You have {inProgressAttachmentsCount} in-progress attachments. Please wait.", "Error").ShowAsync();
                return;
            }

            var attachments = OutboundAttachments.Where(a => a.Type == OutboundAttachmentType.Attachment).Select(b => b.Attachment).ToList();
            ThreadSafeObservableCollection<MessageViewModel> fwd = null;
            int fwdMsgFromGroupId = 0;

            if (OutboundAttachments.Count > 0 && OutboundAttachments[0].Type == OutboundAttachmentType.ForwardedMessages)
            {
                fwd = OutboundAttachments[0].ForwardedMessages;
                fwdMsgFromGroupId = OutboundAttachments[0].ForwardedMessagesFromGroupId;
            }

            Tuple<double, double> place = null;
            var placeAttachment = OutboundAttachments.Where(a => a.Type == OutboundAttachmentType.Place);
            if (placeAttachment.Count() == 1) place = placeAttachment.First().Place;

            DateTime? sentTime = IsInEditMode ? EditMessageSentTime : DateTime.Now;

            MessageViewModel mvm = MessageViewModel.Build(EditMessageId, sentTime.Value, ConversationViewModel.CurrentFocused.Id, ReplyMessage, Text, attachments, fwd, place, null, null, fwdMsgFromGroupId);
            ConversationViewModel.CurrentFocused.Messages.Insert(mvm);

            await Task.Delay(1); // Без этого при редактировании сообщения произойдёт внутренняя ошибка xaml. 🤡
            Clear();

            await Task.Delay(50);
            mvm.SendOrEditMessage();
        }

        public async void SendSticker(Sticker sticker)
        {
            DateTime? sentTime = IsInEditMode ? EditMessageSentTime : DateTime.Now;

            MessageViewModel mvm = MessageViewModel.Build(EditMessageId, sentTime.Value, ConversationViewModel.CurrentFocused.Id, ReplyMessage, null,
                new List<AttachmentBase>(), new ThreadSafeObservableCollection<MessageViewModel>(), null, sticker);
            ConversationViewModel.CurrentFocused.Messages.Insert(mvm);
            await Task.Delay(20);
            mvm.SendOrEditMessage();
        }

        public void StartEditing(MessageViewModel msg)
        {
            Clear();
            EditMessageId = msg.Id;
            EditMessageSentTime = msg.SentDateTime;
            Text = msg.Text;
            ReplyMessage = msg.ReplyMessage;

            foreach (var a in msg.Attachments)
            {
                OutboundAttachmentViewModel oavm = null;
                switch (a.Type)
                {
                    case AttachmentType.Photo: oavm = new OutboundAttachmentViewModel(a.Photo); break;
                    case AttachmentType.Video: oavm = new OutboundAttachmentViewModel(a.Video); break;
                    case AttachmentType.Document: oavm = new OutboundAttachmentViewModel(a.Document); break;
                    case AttachmentType.Poll: oavm = new OutboundAttachmentViewModel(a.Poll); break;
                    case AttachmentType.AudioMessage: oavm = new OutboundAttachmentViewModel(a.AudioMessage); break;
                    case AttachmentType.Audio: oavm = new OutboundAttachmentViewModel(a.Audio); break;
                    case AttachmentType.Graffiti: oavm = new OutboundAttachmentViewModel(a.Graffiti); break;
                }
                if (oavm != null) OutboundAttachments.Add(oavm);
            }

            if (msg.ForwardedMessages.Count > 0)
            {
                OutboundAttachments.Insert(0, new OutboundAttachmentViewModel(msg.ForwardedMessages));
            }

            if (msg.Location != null)
            {
                OutboundAttachments.Add(new OutboundAttachmentViewModel(msg.Location.Coordinates.Latitude, msg.Location.Coordinates.Longitude));
            }
        }

        public void Clear()
        {
            Text = null;
            OutboundAttachments.Clear();
            ReplyMessage = null;
            EditMessageId = Int32.MaxValue;
        }
    }
}