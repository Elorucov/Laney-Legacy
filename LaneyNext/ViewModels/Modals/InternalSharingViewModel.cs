using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class InternalSharingViewModel : CommonViewModel
    {
        private bool _isGroupSession;
        private ObservableCollection<ConversationViewModel> _conversations;
        private ObservableCollection<string> _comboBoxItems;
        private string _searchQuery;

        private string _selectedComboBoxItem;
        private RelayCommand _convSelectedCommand;

        public bool IsGroupSession { get { return _isGroupSession; } private set { _isGroupSession = value; OnPropertyChanged(); } }
        public ObservableCollection<ConversationViewModel> Conversations { get { return _conversations; } private set { _conversations = value; OnPropertyChanged(); } }
        public ObservableCollection<string> ComboBoxItems { get { return _comboBoxItems; } private set { _comboBoxItems = value; OnPropertyChanged(); } }
        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }

        public string SelectedComboBoxItem { get { return _selectedComboBoxItem; } set { _selectedComboBoxItem = value; OnPropertyChanged(); } }
        public RelayCommand ConvSelectedCommand { get { return _convSelectedCommand; } private set { _convSelectedCommand = value; OnPropertyChanged(); } }

        private AttachmentBase Attachment;
        private IEnumerable<MessageViewModel> ForwardedMessages;

        private readonly string toCommunity = Locale.Get("intsharing_to_group");
        private readonly string toPrivate = Locale.Get("intsharing_to_pm");

        public InternalSharingViewModel(AttachmentBase attachment)
        {
            Attachment = attachment;
            IsGroupSession = VKSession.Current.Type == SessionType.VKGroup;

            ConvSelectedCommand = new RelayCommand(ConversationSelected);
            GetConversations();
        }

        public InternalSharingViewModel(IEnumerable<MessageViewModel> forwardedMessages)
        {
            ForwardedMessages = forwardedMessages;
            IsGroupSession = VKSession.Current.Type == SessionType.VKGroup;

            ConvSelectedCommand = new RelayCommand(ConversationSelected);
            GetConversations();
        }

        private async void ConversationSelected(object obj)
        {
            ConversationViewModel conv = obj as ConversationViewModel;
            if (IsGroupSession && SelectedComboBoxItem == toPrivate)
            {
                int gid = VKSession.Current.GroupId;
                var ownerview = await ViewManagement.GetViewBySession(VKSession.CurrentUser);
                ViewManagement.SwitchToView(ownerview, () =>
                {
                    if (Attachment != null) conv.MessageInput.Attach(Attachment);
                    if (ForwardedMessages != null) conv.MessageInput.AttachForwardedMessages(ForwardedMessages.ToList(), gid);
                    VKSession.Current.SessionBase.SelectedConversation = conv;
                });
            }
            else
            {
                if (Attachment != null) conv.MessageInput.Attach(Attachment);
                if (ForwardedMessages != null) conv.MessageInput.AttachForwardedMessages(ForwardedMessages.ToList());
                VKSession.Current.SessionBase.SelectedConversation = conv;
            }
        }

        private void GetConversations()
        {
            var sbase = VKSession.Current.SessionBase;
            if (IsGroupSession)
            {
                ComboBoxItems = new ObservableCollection<string>() { toCommunity, toPrivate };
                PropertyChanged += (a, b) =>
                {
                    if (b.PropertyName == nameof(SelectedComboBoxItem))
                    {
                        if (SelectedComboBoxItem == toPrivate)
                        {
                            var gbase = VKSession.CurrentUser.SessionBase;
                            Conversations = new ObservableCollection<ConversationViewModel>(gbase.PinnedConversations.Concat(gbase.Conversations));
                        }
                        else
                        {
                            Conversations = new ObservableCollection<ConversationViewModel>(sbase.Conversations);
                        }
                    }
                    ;
                };
                SelectedComboBoxItem = ComboBoxItems.First();
            }
            else
            {
                Conversations = new ObservableCollection<ConversationViewModel>(sbase.PinnedConversations.Concat(sbase.Conversations));
            }
        }

        public async void SearchConversations()
        {
            Placeholder = null;
            if (String.IsNullOrEmpty(SearchQuery))
            {
                GetConversations();
                return;
            }

            try
            {
                Conversations.Clear();
                IsLoading = true;
                VKSession session = IsGroupSession && SelectedComboBoxItem == toCommunity ?
                    VKSession.Current : VKSession.CurrentUser;

                VKList<Conversation> response = await session.API.Messages.SearchConversationsAsync(session.GroupId, SearchQuery, 50, true, APIHelper.Fields);
                CacheManager.Add(response.Profiles);
                CacheManager.Add(response.Groups);
                foreach (var conv in response.Items)
                {
                    ConversationViewModel cvm = new ConversationViewModel(conv);
                    Conversations.Add(cvm);
                }
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => SearchConversations());
            }
            IsLoading = false;
        }
    }
}
