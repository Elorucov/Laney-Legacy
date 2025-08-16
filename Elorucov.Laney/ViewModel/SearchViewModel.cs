using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Elorucov.Laney.ViewModel {
    public class SearchViewModel : BaseViewModel {
        private static ObservableCollection<LConversation> ImportantConversations = new ObservableCollection<LConversation>();

        private string _query;
        private string _listHeaderLabel;
        private ObservableCollection<LConversation> _foundConversations = new ObservableCollection<LConversation>();
        private ObservableCollection<FoundMessageItem> _foundMessages = new ObservableCollection<FoundMessageItem>();
        private bool _isFoundConvsListLoading;
        private bool _isFoundMessagesListLoading;
        private PlaceholderViewModel _convsPlaceholder;
        private PlaceholderViewModel _messagesPlaceholder;
        private int _currentTab = 0;

        public string Query { get { return _query; } set { _query = value; OnPropertyChanged(); } }
        public string ListHeaderLabel { get { return _listHeaderLabel; } set { _listHeaderLabel = value; OnPropertyChanged(); } }
        public ObservableCollection<LConversation> FoundConversations { get { return _foundConversations; } private set { _foundConversations = value; OnPropertyChanged(); } }
        public ObservableCollection<FoundMessageItem> FoundMessages { get { return _foundMessages; } private set { _foundMessages = value; OnPropertyChanged(); } }
        public bool IsFoundConvsListLoading { get { return _isFoundConvsListLoading; } private set { _isFoundConvsListLoading = value; OnPropertyChanged(); } }
        public bool IsFoundMessagesListLoading { get { return _isFoundMessagesListLoading; } private set { _isFoundMessagesListLoading = value; OnPropertyChanged(); } }
        public PlaceholderViewModel ConvsPlaceholder { get { return _convsPlaceholder; } private set { _convsPlaceholder = value; OnPropertyChanged(); } }
        public PlaceholderViewModel MessagesPlaceholder { get { return _messagesPlaceholder; } private set { _messagesPlaceholder = value; OnPropertyChanged(); } }
        public int CurrentTab { get { return _currentTab; } set { _currentTab = value; OnPropertyChanged(); } }

        public SearchViewModel() {
            PropertyChanged += async (a, b) => {
                switch (b.PropertyName) {
                    case nameof(CurrentTab):
                        await SearchAsync();
                        break;
                }
            };

            new System.Action(async () => { await ShowImportantConversationsAsync(); })();
        }

        public async Task ShowImportantConversationsAsync() {
            if (ImportantConversations.Count == 0) {
                object resp = await Messages.SearchConversations(string.Empty, 5);
                if (resp is VKList<Conversation> scr) {
                    AppSession.AddUsersToCache(scr.Profiles);
                    AppSession.AddGroupsToCache(scr.Groups);
                    AppSession.AddContactsToCache(scr.Contacts);

                    foreach (var c in scr.Items) {
                        ImportantConversations.Add(new LConversation(c));
                    }
                } else {
                    Functions.ShowHandledErrorTip(resp);
                }
            }
            FoundConversations.Clear();
            foreach (var i in ImportantConversations) {
                FoundConversations.Add(i);
            }
            ListHeaderLabel = FoundConversations.Count > 0 ? Locale.Get("important").ToUpper() : null;
        }

        public static void ClearImportantConversations() {
            ImportantConversations.Clear();
        }

        public async Task SearchAsync() {
            if (string.IsNullOrEmpty(Query)) {
                if (_currentTab == 0) await ShowImportantConversationsAsync();
                return;
            }
            switch (_currentTab) {
                case 0:
                    FoundConversations.Clear();
                    await SearchConversationsAsync();
                    break;
                case 1:
                    FoundMessages.Clear();
                    await SearchMessagesAsync();
                    break;
            }
        }

        private async Task SearchConversationsAsync() {
            if (IsFoundConvsListLoading) return;
            ConvsPlaceholder = null;
            IsFoundConvsListLoading = true;
            ListHeaderLabel = null;

            object resp = await Messages.SearchConversations(Query, 100);
            if (resp is VKList<Conversation> scr) {
                AppSession.AddUsersToCache(scr.Profiles);
                AppSession.AddGroupsToCache(scr.Groups);
                AppSession.AddContactsToCache(scr.Contacts);

                foreach (var c in scr.Items) {
                    FoundConversations.Add(new LConversation(c));
                }

                if (scr.Items.Count == 0) {
                    ConvsPlaceholder = new PlaceholderViewModel('', content: Locale.Get("not_found"));
                }
            } else {
                ConvsPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await SearchConversationsAsync());
            }

            IsFoundConvsListLoading = false;
        }

        public async Task SearchMessagesAsync() {
            if (IsFoundMessagesListLoading) return;
            MessagesPlaceholder = null;
            IsFoundMessagesListLoading = true;

            var resp = await Messages.Search(Query, 0, 0, FoundMessages.Count, 50);
            if (resp is MessagesHistoryResponse mhr) {
                AppSession.AddUsersToCache(mhr.Profiles);
                AppSession.AddGroupsToCache(mhr.Groups);

                foreach (var msg in mhr.Items) {
                    Conversation conv = mhr.Conversations.FirstOrDefault(c => msg.PeerId == c.Peer.Id);
                    FoundMessageItem fmi = new FoundMessageItem(msg, conv);
                    FoundMessages.Add(fmi);
                }

                if (mhr.Items.Count == 0 && FoundMessages.Count == 0) {
                    MessagesPlaceholder = new PlaceholderViewModel('', content: Locale.Get("not_found"));
                }
            } else {
                if (FoundMessages.Count > 0) {
                    Functions.ShowHandledErrorDialog(resp);
                } else {
                    MessagesPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await SearchMessagesAsync());
                }
            }

            IsFoundMessagesListLoading = false;
        }
    }
}