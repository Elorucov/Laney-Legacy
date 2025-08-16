using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI.Popups;

namespace Elorucov.Laney.ViewModel {
    public class SharingModalViewModel : BaseViewModel {
        private long fwdMsgFromPeerId = 0;
        List<int> ForwardedMessagesIds = new List<int>();
        List<AttachmentBase> attachments = new List<AttachmentBase>();
        int randomId = 0;

        private string _searchQuery;
        private string _sharingItemGlyph;
        private string _sharingItemTitle;
        private string _sharingItemDescription;
        private Uri _sharingItemPreviewImage;
        private string _messageText;
        private bool _multiSelectAllowed;
        private bool _multiSelectMode;
        private bool _isLoading;
        private ObservableCollection<LConversation> _conversations = new ObservableCollection<LConversation>();
        private ObservableCollection<Entity> _selectedConvs = new ObservableCollection<Entity>();
        private RelayCommand _sendMessageCommand;

        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }
        public string SharingItemGlyph { get { return _sharingItemGlyph; } private set { _sharingItemGlyph = value; OnPropertyChanged(); } }
        public string SharingItemTitle { get { return _sharingItemTitle; } private set { _sharingItemTitle = value; OnPropertyChanged(); } }
        public string SharingItemDescription { get { return _sharingItemDescription; } private set { _sharingItemDescription = value; OnPropertyChanged(); } }
        public Uri SharingItemPreviewImage { get { return _sharingItemPreviewImage; } private set { _sharingItemPreviewImage = value; OnPropertyChanged(); } }
        public string MessageText { get { return _messageText; } set { _messageText = value; OnPropertyChanged(); } }
        public bool MultiSelectAllowed { get { return _multiSelectAllowed; } set { _multiSelectAllowed = value; OnPropertyChanged(); } }
        public bool MultiSelectMode { get { return _multiSelectMode; } set { _multiSelectMode = value; OnPropertyChanged(); } }
        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }
        public ObservableCollection<LConversation> Conversations { get { return _conversations; } private set { _conversations = value; OnPropertyChanged(); } }
        public ObservableCollection<Entity> SelectedConvs { get { return _selectedConvs; } private set { _selectedConvs = value; OnPropertyChanged(); } }
        public RelayCommand SendMessageCommand { get { return _sendMessageCommand; } private set { _sendMessageCommand = value; OnPropertyChanged(); } }

        public System.Action OnConvsLoaded { get; set; }

        public SharingModalViewModel(long fromPeerId, List<LMessage> messages) {
            randomId = new Random().Next();
            fwdMsgFromPeerId = fromPeerId;
            ForwardedMessagesIds = messages.Select(m => m.ConversationMessageId).ToList();

            SharingItemGlyph = "";
            SharingItemTitle = $"{messages.Count} {Locale.GetDeclension(messages.Count, "forwarded_msgs_link").ToLower()}";
            // SharingItemDescription = messages.Count > 1 ? $"from chat with {fwdMsgFromPeerId}" : $"{messages[0].SenderName}: {messages[0]}";
            SharingItemDescription = messages.Count > 1 ? string.Empty : $"{messages[0].SenderName}: {messages[0]}";

            MultiSelectAllowed = true;
            SendMessageCommand = new RelayCommand(SendMessageMulti);
            new System.Action(async () => { await GetConversationsAsync(); })();
        }

        public SharingModalViewModel(List<AttachmentBase> attachments) {
            randomId = new Random().Next();
            this.attachments = attachments;
            SharingItemGlyph = "";
            if (attachments.Count == 1) {
                var attachment = attachments[0];
                var info = AppSession.GetNameAndAvatar(attachment.OwnerId);
                SharingItemTitle = Locale.GetDeclension(1, $"atch_{attachment.ObjectType}").Capitalize();

                if (attachment is IPreview p && p.PreviewImageUri != null) SharingItemPreviewImage = p.PreviewImageUri;
                if (info != null) SharingItemDescription = String.Join(" ", new List<string> { info.Item1, info.Item2 });

                MultiSelectAllowed = attachment is Photo || attachment is WallPost;

                if (attachment is Audio audio) {
                    SharingItemDescription = $"{audio.Artist} — {audio.Title}";
                    if (!string.IsNullOrEmpty(audio.Subtitle)) SharingItemDescription += $" ({audio.Subtitle})";
                } else if (attachment is Video video) {
                    SharingItemDescription = video.Title;
                }
            } else {
                SharingItemTitle = $"{attachments.Count} {Locale.GetDeclension(attachments.Count, "attachment")}";
            }

            SendMessageCommand = new RelayCommand(SendMessageMulti);
            new System.Action(async () => { await GetConversationsAsync(); })();
        }

        private async Task GetConversationsAsync() {
            if (IsLoading) return;
            IsLoading = true;

            object response = await Messages.GetConversations(100, 0, null, 0, API.WebToken);
            if (response is ConversationsResponse cr) {
                AppSession.AddUsersToCache(cr.Profiles);
                AppSession.AddGroupsToCache(cr.Groups);
                var ids = SelectedConvs.Select(c => c.Id);

                foreach (var conv in cr.Items) {
                    var con = new LConversation(conv.Conversation);
                    Conversations.Add(con);
                }
                OnConvsLoaded?.Invoke();
            } else {
                // TODO: Placeholder
                Functions.ShowHandledErrorDialog(response, async () => await GetConversationsAsync());
            }

            IsLoading = false;
        }

        public async Task SearchConversationsAsync() {
            if (IsLoading) return;

            var backup = SelectedConvs.ToList();
            Conversations.Clear();
            SelectedConvs = new ObservableCollection<Entity>(backup);
            if (string.IsNullOrEmpty(SearchQuery)) {
                await GetConversationsAsync();
                return;
            }

            IsLoading = true;

            object response = await Messages.SearchConversations(SearchQuery, 100);
            if (response is VKList<Conversation> cr) {
                AppSession.AddUsersToCache(cr.Profiles);
                AppSession.AddGroupsToCache(cr.Groups);
                var ids = SelectedConvs.Select(c => c.Id);

                foreach (var conv in cr.Items) {
                    var con = new LConversation(conv);
                    Conversations.Add(con);
                }
                OnConvsLoaded?.Invoke();
            } else {
                // TODO: Placeholder
                Functions.ShowHandledErrorDialog(response, async () => await SearchConversationsAsync());
            }

            IsLoading = false;
        }

        private void SendMessageMulti(object obj) {
            new System.Action(async () => { await SendMessageAsync(); })();
        }

        public async Task SendMessageAsync() {
            string forward = "";
            string attachments = String.Join(",", this.attachments);

            if (ForwardedMessagesIds.Count > 0) {
                forward = $"{{\"peer_id\":{fwdMsgFromPeerId},\"conversation_message_ids\":[{String.Join(',', ForwardedMessagesIds)}]}}";
            }

            List<long> ids = SelectedConvs.Select(c => c.Id).ToList();

            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            var response = await ssp.ShowAsync(Messages.SendMulti(ids, randomId, MessageText, attachments, forward));
            if (response is List<MessageSendMultiResponse> resp) {
                ModalsManager.CloseLastOpenedModal();
            } else {
                Functions.ShowHandledErrorDialog(response, async () => await SendMessageAsync());
            }
        }
    }
}
