using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using System;
using System.Linq;

namespace Elorucov.Laney.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private ItemsViewModel<ConversationViewModel> _foundConversations = new ItemsViewModel<ConversationViewModel>();
        private ItemsViewModel<FoundMessageItem> _foundMessages = new ItemsViewModel<FoundMessageItem>();
        private string _query;
        private int _selectedTabIndex;

        private RelayCommand _searchCommand;

        public ItemsViewModel<ConversationViewModel> FoundConversations { get { return _foundConversations; } private set { _foundConversations = value; OnPropertyChanged(); } }
        public ItemsViewModel<FoundMessageItem> FoundMessages { get { return _foundMessages; } private set { _foundMessages = value; OnPropertyChanged(); } }
        public string Query { get { return _query; } set { _query = value; OnPropertyChanged(); } }
        public int SelectedTabIndex { get { return _selectedTabIndex; } set { _selectedTabIndex = value; OnPropertyChanged(); } }

        public RelayCommand SearchCommand { get { return _searchCommand; } private set { _searchCommand = value; } }

        public SearchViewModel()
        {
            SearchCommand = new RelayCommand(o => DoSearch());
            PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(SelectedTabIndex) && !String.IsNullOrEmpty(Query))
                {
                    switch (SelectedTabIndex)
                    {
                        case 0: if (FoundConversations.Items.Count == 0) SearchConversations(); break;
                        case 1: if (FoundMessages.Items.Count == 0) SearchMessages(); break;
                    }
                }
            };
        }

        public void DoSearch()
        {
            switch (SelectedTabIndex)
            {
                case 0: SearchConversations(); break;
                case 1: FoundMessages.Items.Clear(); SearchMessages(); break;
            }
        }

        private async void SearchConversations()
        {
            if (FoundConversations.IsLoading) return;
            FoundConversations.Items.Clear();
            FoundConversations.Placeholder = null;
            FoundConversations.IsLoading = true;
            try
            {
                VKList<Conversation> response = await VKSession.Current.API.Messages.SearchConversationsAsync(VKSession.Current.GroupId, Query, 50, true, APIHelper.Fields);
                CacheManager.Add(response.Profiles);
                CacheManager.Add(response.Groups);

                foreach (var conv in response.Items)
                {
                    ConversationViewModel cvm = new ConversationViewModel(conv);
                    FoundConversations.Items.Add(cvm);
                }
            }
            catch (Exception ex)
            {
                if (FoundConversations.Items.Count == 0)
                {
                    FoundConversations.Placeholder = PlaceholderViewModel.GetForException(ex, () => SearchConversations());
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex)) SearchConversations();
                }
            }
            FoundConversations.IsLoading = false;
        }

        public async void SearchMessages()
        {
            if (FoundMessages.IsLoading) return;
            FoundMessages.Placeholder = null;
            FoundMessages.IsLoading = true;
            try
            {
                VKList<Message> response = await VKSession.Current.API.Messages.SearchAsync(VKSession.Current.GroupId, Query, 0, null, 20,
                    FoundMessages.Items.Count, 40, true, APIHelper.Fields);
                CacheManager.Add(response.Profiles);
                CacheManager.Add(response.Groups);

                foreach (var msg in response.Items)
                {
                    Conversation conv = null;
                    conv = response.Conversations.FirstOrDefault(c => msg.PeerId == c.Peer.Id);
                    FoundMessageItem fmi = new FoundMessageItem(msg, conv);
                    FoundMessages.Items.Add(fmi);
                }
            }
            catch (Exception ex)
            {
                if (FoundMessages.Items.Count == 0)
                {
                    FoundMessages.Placeholder = PlaceholderViewModel.GetForException(ex, () => SearchMessages());
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex)) SearchMessages();
                }
            }
            FoundMessages.IsLoading = false;
        }
    }
}
