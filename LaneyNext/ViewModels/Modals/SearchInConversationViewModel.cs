using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class SearchInConversationViewModel : CommonViewModel
    {
        private string _query;
        private MessagesCollection _foundMessages = new MessagesCollection();
        private DateTime? _date;

        private RelayCommand _searchCommand;

        public string Query { get { return _query; } set { _query = value; OnPropertyChanged(); } }
        public MessagesCollection FoundMessages { get { return _foundMessages; } private set { _foundMessages = value; OnPropertyChanged(); } }
        public DateTime? Date { get { return _date; } set { _date = value; OnPropertyChanged(); } }

        public RelayCommand SearchCommand { get { return _searchCommand; } private set { _searchCommand = value; } }

        private int PeerId;

        public SearchInConversationViewModel(int peerId)
        {
            PeerId = peerId;
            SearchCommand = new RelayCommand(o => DoSearch());
            PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(Date) && !String.IsNullOrEmpty(Query))
                {
                    FoundMessages.Clear();
                    DoSearch();
                }
            };
        }

        public async void DoSearch()
        {
            if (IsLoading) return;
            Placeholder = null;
            IsLoading = true;
            try
            {
                VKList<Message> response = await VKSession.Current.API.Messages.SearchAsync(VKSession.Current.GroupId, Query, PeerId, Date, 0,
                    FoundMessages.Count, 40, true, APIHelper.Fields);
                CacheManager.Add(response.Profiles);
                CacheManager.Add(response.Groups);

                if (FoundMessages.Count > 0)
                {
                    foreach (var msg in response.Items)
                    {
                        MessageViewModel mvm = new MessageViewModel(msg);
                        FoundMessages.Insert(mvm);
                    }
                }
                else
                {
                    FoundMessages = new MessagesCollection(response.Items);
                }
            }
            catch (Exception ex)
            {
                if (FoundMessages.Count == 0)
                {
                    Placeholder = PlaceholderViewModel.GetForException(ex, () => DoSearch());
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex)) DoSearch();
                }
            }
            IsLoading = false;
        }
    }
}
