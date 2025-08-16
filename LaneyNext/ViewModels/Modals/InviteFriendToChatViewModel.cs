using ELOR.VKAPILib.Methods;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Helpers.Groupings;
using Elorucov.Laney.VKAPIExecute.Objects;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.Globalization.Collation;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class InviteFriendToChatViewModel : CommonViewModel
    {

        private ThreadSafeObservableCollection<Grouping<string, User>> _friends = new ThreadSafeObservableCollection<Grouping<string, User>>();
        private ThreadSafeObservableCollection<User> _selectedFriends = new ThreadSafeObservableCollection<User>();
        private ushort _visibleMessagesCount = 250;
        private string _bottomText;
        private bool _isEnabled = true;

        private RelayCommand _createCommand;

        public ThreadSafeObservableCollection<Grouping<string, User>> Friends { get { return _friends; } private set { _friends = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<User> SelectedFriends { get { return _selectedFriends; } private set { _selectedFriends = value; OnPropertyChanged(); } }
        public ushort VisibleMessagesCount { get { return _visibleMessagesCount; } set { _visibleMessagesCount = value; OnPropertyChanged(); } }
        public string BottomText { get { return _bottomText; } private set { _bottomText = value; OnPropertyChanged(); } }
        public bool IsEnabled { get { return _isEnabled; } private set { _isEnabled = value; OnPropertyChanged(); } }

        public RelayCommand CreateCommand { get { return _createCommand; } set { _createCommand = value; OnPropertyChanged(); } }

        int ChatId;
        List<int> IgnoredIds;
        int FriendsCount = 0;
        int MaximumSelect = 20;
        Modal OwnerModal;

        public InviteFriendToChatViewModel(Modal modal, int chatId, List<int> ignoredIds)
        {
            OwnerModal = modal;
            ChatId = chatId;
            IgnoredIds = ignoredIds;
            LoadFriends();

            SelectedFriends.CollectionChanged += (a, b) =>
            {
                UpdateBottomText();
                OnPropertyChanged(nameof(SelectedFriends));
            };

            CreateCommand = new RelayCommand(o => Invite());
        }

        private async void LoadFriends()
        {
            if (IsLoading) return;
            Friends.Clear();
            Placeholder = null;
            IsLoading = true;
            try
            {
                VKList<User> friends = await VKSession.Current.API.Friends.GetAsync(VKSession.Current.Id, FriendsOrder.Name, 0, 10000, 0, APIHelper.Fields);
                List<User> withoutIgnored = (from f in friends.Items
                                             where !IgnoredIds.Contains(f.Id) && f.Deactivated == DeactivationState.No
                                             select f)
                                             .ToList();
                FriendsCount = friends.Count;
                GroupFriends(withoutIgnored);
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => LoadFriends());
            }
            IsLoading = false;
        }

        private void GroupFriends(List<User> items)
        {
            ThreadSafeObservableCollection<Grouping<string, User>> list = new ThreadSafeObservableCollection<Grouping<string, User>>();
            CharacterGroupings slg = new CharacterGroupings();

            if (items != null && items.Count > 0)
            {
                foreach (CharacterGrouping key in slg)
                {
                    if (!string.IsNullOrWhiteSpace(key.Label))
                    {
                        string k = key.First.ToUpper();
                        var a = from b in items where b.FirstName[0].ToString().ToUpper() == k select b;
                        if (a.Count() > 0) list.Add(new Grouping<string, User>(k, a, k));
                    }
                }
            }

            Friends = list;
            UpdateBottomText();
        }

        private void UpdateBottomText()
        {
            int c = SelectedFriends.Count;
            if (c == 0)
            {
                BottomText = String.Format(Locale.GetDeclensionForFormat(FriendsCount, "friends_count"), FriendsCount);
            }
            else
            {
                BottomText = String.Format(Locale.GetForFormat("selected_in"), c, MaximumSelect);
            }
        }

        private async void Invite()
        {
            IsEnabled = false;
            try
            {
                List<int> ids = SelectedFriends.Select(i => i.Id).ToList();
                AddChatUserResponse response = await VKSession.Current.Execute.AddChatUserAsync(ChatId, ids, VisibleMessagesCount);
                Log.General.Info("Response received", new ValueSet { { "success", String.Join(",", response.Success) }, { "failed", String.Join(",", response.Failed) } });
                OwnerModal.Hide(true);
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) Invite();
                IsEnabled = true;
            }
        }
    }
}