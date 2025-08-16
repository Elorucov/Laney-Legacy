using ELOR.VKAPILib.Methods;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Helpers.Groupings;
using System;
using System.Collections.Generic;
using System.Linq;
using VK.VKUI.Popups;
using Windows.Globalization.Collation;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.ViewModels
{
    public class ChatCreationViewModel : CommonViewModel
    {
        private ThreadSafeObservableCollection<Grouping<string, User>> _friends = new ThreadSafeObservableCollection<Grouping<string, User>>();
        private bool _isChatCreationMode;
        private ThreadSafeObservableCollection<User> _selectedFriendsForChat = new ThreadSafeObservableCollection<User>();
        private string _chatName;
        private bool _isChatCreateDoneButtonEnabled;
        private ListViewSelectionMode _friendsListSelectionMode;
        private string _searchQuery;
        private ThreadSafeObservableCollection<User> _foundFriends;
        private bool _isFriendSearchMode;
        private RelayCommand _convPermissionsEditorCommand;
        private RelayCommand _conversationCreateCommand;

        public ThreadSafeObservableCollection<Grouping<string, User>> Friends { get { return _friends; } private set { _friends = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<User> SelectedFriendsForChat { get { return _selectedFriendsForChat; } private set { _selectedFriendsForChat = value; OnPropertyChanged(); } }
        public bool IsChatCreationMode { get { return _isChatCreationMode; } set { _isChatCreationMode = value; OnPropertyChanged(); } }
        public string ChatName { get { return _chatName; } set { _chatName = value; OnPropertyChanged(); } }
        public bool IsChatCreateDoneButtonEnabled { get { return _isChatCreateDoneButtonEnabled; } private set { _isChatCreateDoneButtonEnabled = value; OnPropertyChanged(); } }
        public ListViewSelectionMode FriendsListSelectionMode { get { return _friendsListSelectionMode; } private set { _friendsListSelectionMode = value; OnPropertyChanged(); } }
        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<User> FoundFriends { get { return _foundFriends; } private set { _foundFriends = value; OnPropertyChanged(); } }
        public bool IsFriendSearchMode { get { return _isFriendSearchMode; } private set { _isFriendSearchMode = value; OnPropertyChanged(); } }
        public RelayCommand ConvPermissionsEditorCommand { get { return _convPermissionsEditorCommand; } set { _convPermissionsEditorCommand = value; OnPropertyChanged(); } }
        public RelayCommand ConversationCreateCommand { get { return _conversationCreateCommand; } set { _conversationCreateCommand = value; OnPropertyChanged(); } }

        private List<User> FriendsInternal;
        private List<User> ImportantFriends;
        private System.Action<int> ChatCreatedCallback;

        public ChatCreationViewModel(System.Action<int> chatCreatedCallback)
        {
            ChatCreatedCallback = chatCreatedCallback;
            ConvPermissionsEditorCommand = new RelayCommand(o => Utils.ShowUnderConstructionInfo());
            ConversationCreateCommand = new RelayCommand(o => CreateChat());

            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(IsChatCreationMode):
                        FriendsListSelectionMode = IsChatCreationMode ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None;
                        if (!IsChatCreationMode) SelectedFriendsForChat.Clear();
                        break;
                    case nameof(ChatName):
                        CheckCanCreateChat();
                        break;
                }
            };
            SelectedFriendsForChat.CollectionChanged += (a, b) => CheckCanCreateChat();

            LoadFriends();
        }

        private async void LoadFriends()
        {
            if (IsLoading) return;
            Friends.Clear();
            Placeholder = null;
            IsLoading = true;
            try
            {
                VKList<User> friends = await VKSession.Current.API.Friends.GetAsync(VKSession.Current.Id, FriendsOrder.Hints, 0, 10000, 0, APIHelper.Fields);
                ImportantFriends = friends.Items.GetRange(0, 5);
                FriendsInternal = friends.Items.OrderBy(f => f.FirstName).ToList();
                GroupFriends(FriendsInternal, ImportantFriends);
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => LoadFriends());
            }
            IsLoading = false;
        }

        private void GroupFriends(List<User> items, List<User> important)
        {
            bool dontAddImportants =
                FriendsListSelectionMode == ListViewSelectionMode.Multiple ||
                FriendsListSelectionMode == ListViewSelectionMode.Extended;

            ThreadSafeObservableCollection<Grouping<string, User>> list = new ThreadSafeObservableCollection<Grouping<string, User>>();
            CharacterGroupings slg = new CharacterGroupings();

            if (items != null && items.Count > 0)
            {
                if (!dontAddImportants)
                {
                    list.Add(new Grouping<string, User>(Locale.Get("important").ToUpper(), important, "", "Segoe MDL2 Assets"));
                }

                foreach (CharacterGrouping key in slg)
                {
                    if (!string.IsNullOrWhiteSpace(key.Label))
                    {
                        string k = key.First.ToUpper();
                        var a = from b in items where b.FirstName[0].ToString().ToUpper() == k select b;
                        if (a.Count() > 0)
                        {
                            list.Add(new Grouping<string, User>(k, a, k));
                        }
                    }
                }
            }

            Friends = list;
        }

        public async void SearchFriend()
        {
            IsFriendSearchMode = !String.IsNullOrEmpty(SearchQuery);
            if (!IsFriendSearchMode) return;
            IsLoading = true;
            try
            {
                Placeholder = null;
                VKList<User> foundFriends = await VKSession.Current.API.Friends.SearchAsync(VKSession.CurrentUser.Id, SearchQuery, 1000, 0, APIHelper.UserFields);
                IsLoading = false;
                FoundFriends = new ThreadSafeObservableCollection<User>(foundFriends.Items);
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => SearchFriend());
            }
            IsLoading = false;
        }

        private void CheckCanCreateChat()
        {
            IsChatCreateDoneButtonEnabled = SelectedFriendsForChat.Count > 0 || !String.IsNullOrWhiteSpace(ChatName);
        }

        private async void CreateChat()
        {
            try
            {
                var ids = SelectedFriendsForChat.Select(m => m.Id).ToList();

                ScreenSpinner<int> ssp = new ScreenSpinner<int>();
                int id = await ssp.ShowAsync(VKSession.Current.API.Messages.CreateChatAsync(0, ids, ChatName));
                ChatCreatedCallback?.Invoke(2000000000 + id);
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) CreateChat();
            }
        }
    }
}