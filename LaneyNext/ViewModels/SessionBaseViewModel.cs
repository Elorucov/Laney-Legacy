using ELOR.VKAPILib.Methods;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;

namespace Elorucov.Laney.ViewModels
{
    public class SessionBaseViewModel : BaseViewModel
    {
        private ThreadSafeObservableCollection<ConversationViewModel> _conversations = new ThreadSafeObservableCollection<ConversationViewModel>();
        private ThreadSafeObservableCollection<ConversationViewModel> _pinnedConversations = new ThreadSafeObservableCollection<ConversationViewModel>();
        private ConversationViewModel _selectedConversation;
        private bool _isLoading;
        private PlaceholderViewModel _placeholder;
        private string _displayName;
        private Uri _avatar;
        private SessionType _sessionType;

        public ThreadSafeObservableCollection<ConversationViewModel> Conversations { get { return _conversations; } private set { _conversations = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<ConversationViewModel> PinnedConversations { get { return _pinnedConversations; } private set { _pinnedConversations = value; OnPropertyChanged(); } }
        public ConversationViewModel SelectedConversation { get { return _selectedConversation; } set { _selectedConversation = value; OnPropertyChanged(); } }
        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } private set { _placeholder = value; OnPropertyChanged(); } }
        public string DisplayName { get { return _displayName; } private set { _displayName = value; OnPropertyChanged(); } }
        public Uri Avatar { get { return _avatar; } private set { _avatar = value; OnPropertyChanged(); } }
        public SessionType SessionType { get { return _sessionType; } private set { _sessionType = value; OnPropertyChanged(); } }

        ConversationsFilter currentFilter;
        bool lpEventRegistered = false;

        public AudioPlayerViewModel MainAudioPlayerInstance { get { return AudioPlayerViewModel.MainInstance; } }
        public AudioPlayerViewModel VoiceMessagePlayerInstance { get { return AudioPlayerViewModel.VoiceMessageInstance; } }

        public SessionBaseViewModel()
        {
            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(SelectedConversation): LoadMessagesInConversation(); break;
                }
            };
            VKSession.Current.PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(VKSession.Type): SessionType = VKSession.Current.Type; break;
                    case nameof(VKSession.DisplayName): DisplayName = VKSession.Current.DisplayName; break;
                    case nameof(VKSession.Avatar): Avatar = VKSession.Current.Avatar; break;
                }
            };

            SessionType = VKSession.Current.Type;
            DisplayName = VKSession.Current.DisplayName;
            Avatar = VKSession.Current.Avatar;

            Conversations.CollectionChanged += (a, b) => OnPropertyChanged(nameof(Conversations));
            PinnedConversations.CollectionChanged += (a, b) => OnPropertyChanged(nameof(PinnedConversations));

            // Audio player
            AudioPlayerViewModel.InstancesChanged += (a, b) =>
            {
                OnPropertyChanged(nameof(MainAudioPlayerInstance));
                OnPropertyChanged(nameof(VoiceMessagePlayerInstance));
            };
        }

        private void LoadMessagesInConversation()
        {
            if (SelectedConversation != null)
            {
                SelectedConversation.OnShowing();
                VKSession.Current.AddConversationToCache(SelectedConversation);
            }
        }

        #region Public methods

        public void GetConversationsByFilter(ConversationsFilter filter)
        {
            currentFilter = filter;
            Conversations.Clear();
            PinnedConversations.Clear();
            GetConversations();
        }

        public void SwitchToConversation(int id)
        {
            SwitchToConversation(id, 0, null, null);
        }

        public void SwitchToConversation(int id, int messageId)
        {
            SwitchToConversation(id, messageId, null, null);
        }

        public void SwitchToConversation(int id, AttachmentBase attachment)
        {
            SwitchToConversation(id, 0, attachment, null);
        }

        public void SwitchToConversation(int id, List<MessageViewModel> forwardedMessages)
        {
            SwitchToConversation(id, 0, null, forwardedMessages);
        }

        public async void SwitchToConversation(int id, int messageId, AttachmentBase attachment, List<MessageViewModel> forwardedMessages)
        {
            var conv = VKSession.Current.GetCachedConversation(id);
            if (conv == null) conv = PinnedConversations.FirstOrDefault(c => c.Id == id);
            if (conv == null) conv = Conversations.FirstOrDefault(c => c.Id == id);
            if (conv != null)
            {
                SelectedConversation = conv;
            }
            else
            {
                Log.General.Info("Conversation not found in list and cache", new ValueSet { { "id", id } });
                ConversationViewModel c = new ConversationViewModel(id);
                SelectedConversation = c;
            }
            VKSession.Current.AddConversationToCache(SelectedConversation);
            if (attachment != null) SelectedConversation.MessageInput.Attach(attachment);
            if (forwardedMessages != null) SelectedConversation.MessageInput.AttachForwardedMessages(forwardedMessages);
            if (messageId > 0) SelectedConversation.GetToMessage(messageId);
        }

        #endregion

        #region Private methods

        public async void GetConversations()
        {
            if (IsLoading) return;
            Placeholder = null;
            IsLoading = true;
            try
            {
                var response = await VKSession.Current.API.Messages.GetConversationsAsync(VKSession.Current.GroupId, APIHelper.Fields, currentFilter, true, 60, Conversations.Count + PinnedConversations.Count);
                if (response.Items.Count == 0)
                {
                    switch (currentFilter)
                    {
                        case ConversationsFilter.All: Placeholder = PlaceholderViewModel.ForEmptyConversations; break;
                        case ConversationsFilter.Unread: Placeholder = PlaceholderViewModel.ForEmptyUnreadConversations; break;
                        case ConversationsFilter.Unanswered: Placeholder = PlaceholderViewModel.ForEmptyUnansweredConversations; break;
                        case ConversationsFilter.Important: Placeholder = PlaceholderViewModel.ForEmptyImportantConversations; break;
                    }
                }
                CacheManager.Add(response.Profiles);
                CacheManager.Add(response.Groups);

                // Pinned conversations
                var pinned = response.Items.Where(c => APIHelper.IsPinnedConversation(c.Conversation));
                foreach (var conv in pinned)
                {
                    ConversationViewModel cvm = new ConversationViewModel(conv.Conversation);
                    if (conv.LastMessage != null) cvm.ReceivedMessages.Add(new MessageViewModel(conv.LastMessage));
                    PinnedConversations.Add(cvm);
                }
                if (PinnedConversations.Count > 0) PinnedConversations.SortDescending(s => s.MajorSortId / 16);

                foreach (var conv in response.Items)
                {
                    if (APIHelper.IsPinnedConversation(conv.Conversation)) continue;
                    ConversationViewModel cvm = new ConversationViewModel(conv.Conversation);
                    if (conv.LastMessage != null) cvm.ReceivedMessages.Add(new MessageViewModel(conv.LastMessage));
                    Conversations.Add(cvm);
                }
                if (Conversations.Count > 0) Conversations.SortDescending(s => s.LastMessage.Id);

                if (!lpEventRegistered)
                {
                    lpEventRegistered = true;
                    VKSession.Current.LongPoll.NewMessageReceived += LongPoll_NewMessageReceived;
                    VKSession.Current.LongPoll.MajorIdChanged += LongPoll_MajorIdChanged;
                    VKSession.Current.LongPoll.ConversationRemoved += LongPoll_ConversationRemoved;
                }
            }
            catch (Exception ex)
            {
                if (Conversations.Count == 0)
                {
                    Placeholder = PlaceholderViewModel.GetForException(ex, () => GetConversations());
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                    {
                        GetConversations();
                    }
                }
            }
            IsLoading = false;
        }

        #endregion

        #region Long poll

        private async void LongPoll_NewMessageReceived(object sender, Message m)
        {
            MessageViewModel msg = new MessageViewModel(m);

            // Проверяем закреплённую беседу
            if (PinnedConversations.Count > 0)
            {
                var pinned = PinnedConversations.Where(c => c.Id == msg.PeerId).FirstOrDefault();
                if (pinned != null) return;
            }

            ConversationViewModel cvm = null;
            if (Conversations.Count > 0) cvm = Conversations.Where(c => c.Id == msg.PeerId).FirstOrDefault();

            if (cvm == null)
            { // беседы нет в списке
                Log.General.Info("Message received in conversation that is not found in list", new ValueSet { { "id", msg.PeerId } });
                VKList<Conversation> clist = await VKSession.Current.API.Messages.GetConversationsByIdAsync(VKSession.Current.GroupId, new List<int> { msg.PeerId }, true, APIHelper.Fields);
                CacheManager.Add(clist.Profiles);
                CacheManager.Add(clist.Groups);
                var conv = new ConversationViewModel(clist.Items.First());
                conv.ReceivedMessages.Add(msg);
                Conversations.Insert(0, conv);
            }
            else
            {
                if (Conversations.IndexOf(cvm) == 0) return;
                Conversations.Remove(cvm);
                Conversations.Insert(0, cvm);
            }
        }

        private void LongPoll_MajorIdChanged(object sender, Dictionary<int, int> e)
        {
            foreach (var mid in e)
            {
                bool pinned = mid.Value % 16 == 0 && mid.Value / 16 > 0;
                if (!pinned)
                { // беседу открепили
                    ConversationViewModel cvm = PinnedConversations.Where(c => c.Id == mid.Key).FirstOrDefault();
                    if (cvm != null)
                    {
                        PinnedConversations.Remove(cvm);
                        Conversations.Add(cvm);
                    }
                }
                else
                {
                    ConversationViewModel cvm = Conversations.Where(c => c.Id == mid.Key).FirstOrDefault();
                    if (cvm != null)
                    {
                        Conversations.Remove(cvm);
                        PinnedConversations.Add(cvm);
                    }
                    else
                    {
                        // TODO: получить инфо о закреплённых беседах не в цикле.
                    }
                }
            }
            if (PinnedConversations.Count > 0) PinnedConversations.SortDescending(s => s.MajorSortId / 16);
            if (Conversations.Count > 0) Conversations.SortDescending(s => s.LastMessage.Id);
        }

        private void LongPoll_ConversationRemoved(object sender, Tuple<int, int> e)
        {
            ConversationViewModel rcvm = null;

            rcvm = Conversations.Where(c => c.Id == e.Item1).FirstOrDefault();
            if (rcvm == null) rcvm = PinnedConversations.Where(c => c.Id == e.Item1).FirstOrDefault();
            if (rcvm != null)
            {
                rcvm.Messages.Clear();
                rcvm.ReceivedMessages.Clear();
                if (SelectedConversation == rcvm) SelectedConversation = null;
                Conversations.Remove(rcvm);
                PinnedConversations.Remove(rcvm);
            }
        }

        #endregion
    }
}