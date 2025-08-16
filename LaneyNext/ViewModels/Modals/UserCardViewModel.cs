using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.VKAPIExecute.Objects;
using Elorucov.Toolkit.UWP.Controls;
using System;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using Windows.UI.Xaml;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class UserCardViewModel : CommonViewModel
    {
        private UserEx _user;
        private string _profileStatus;
        private string _birthdate;
        private string _career;
        private string _followers;
        private VKIconName _friendButtonIcon = VKIconName.Icon28UsersOutline;
        private string _friendButtonString;
        private bool _isActionButtonsVisible;
        private RelayCommand _sendMessageCommand;
        private RelayCommand _friendCommand;

        public UserEx User { get { return _user; } set { _user = value; OnPropertyChanged(); } }
        public string ProfileStatus { get { return _profileStatus; } set { _profileStatus = value; OnPropertyChanged(); } }
        public string Birthdate { get { return _birthdate; } set { _birthdate = value; OnPropertyChanged(); } }
        public string Career { get { return _career; } set { _career = value; OnPropertyChanged(); } }
        public string Followers { get { return _followers; } set { _followers = value; OnPropertyChanged(); } }
        public VKIconName FriendButtonIcon { get { return _friendButtonIcon; } set { _friendButtonIcon = value; OnPropertyChanged(); } }
        public string FriendButtonString { get { return _friendButtonString; } set { _friendButtonString = value; OnPropertyChanged(); } }
        public bool IsActionButtonsVisible { get { return _isActionButtonsVisible; } set { _isActionButtonsVisible = value; OnPropertyChanged(); } }
        public RelayCommand SendMessageCommand { get { return _sendMessageCommand; } set { _sendMessageCommand = value; OnPropertyChanged(); } }
        public RelayCommand FriendCommand { get { return _friendCommand; } set { _friendCommand = value; OnPropertyChanged(); } }

        public UserCardViewModel(int userId)
        {
            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(User): SetInfos(); break;
                }
            };

            SendMessageCommand = new RelayCommand(e =>
            {
                (e as Modal).Hide();
                VKSession.Current.SessionBase.SwitchToConversation(User.Id);
            });
            FriendCommand = new RelayCommand(e =>
            {
                FriendCommandRequested((FrameworkElement)e);
            });

            GetUserCard(userId);
        }

        private async void GetUserCard(int userId)
        {
            try
            {
                Placeholder = null;
                IsLoading = true;
                UserEx user = await VKSession.Current.Execute.GetUserCardAsync(userId);
                CacheManager.Add(user);
                User = user;
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => GetUserCard(userId));
            }
            IsLoading = false;
        }

        private void SetInfos()
        {
            if (User == null) return;

            // Disable friends button if user is You.
            if (VKSession.Current.Id == User.Id) User.CanSendFriendRequest = false;

            // Unavailable
            switch (User.UnavailableReason)
            {
                default: ProfileStatus = APIHelper.GetOnlineInfoString(User.OnlineInfo, User.Sex); break;
                case 1: ProfileStatus = Locale.Get("user_blocked"); break;
                case 2: ProfileStatus = Locale.Get("user_deleted"); break;
                case 3: ProfileStatus = Locale.Get("user_private"); break;
                case 4: ProfileStatus = Locale.Get("user_blacklisted", User.Sex); break;
                case 5: ProfileStatus = Locale.Get("user_blacklisted_by_me", User.Sex); break;
            }

            // Friend status
            switch (User.FriendStatus)
            {
                case FriendStatus.None:
                    FriendButtonString = Locale.Get("usercard_friend_add");
                    FriendButtonIcon = VKIconName.Icon28UserAddOutline;
                    break;
                case FriendStatus.RequestSent:
                    FriendButtonString = Locale.Get("usercard_friend_req_sent");
                    FriendButtonIcon = VKIconName.Icon28UserOutgoingOutline;
                    break;
                case FriendStatus.InboundRequest:
                    FriendButtonString = Locale.Get("usercard_friend_req_accept");
                    FriendButtonIcon = VKIconName.Icon28UserIncomingOutline;
                    break;
                case FriendStatus.IsFriend:
                    FriendButtonString = Locale.Get("usercard_friend");
                    FriendButtonIcon = VKIconName.Icon28UsersOutline;
                    break;
            }

            // Action buttons
            IsActionButtonsVisible = User.CanSendFriendRequest || User.CanWritePrivateMessage;

            // Birthdate
            Birthdate = APIHelper.GetNormalizedBirthDate(User.BirthDate);
            if (User.CurrentCareer != null)
            {
                var c = User.CurrentCareer;
                string h = c.Company;
                Career = String.IsNullOrEmpty(c.Position) ? h : $"{h} — {c.Position}";
            }
            else
            {
                Career = String.Empty;
            }

            // Followers
            Followers = User.Followers > 0 ? $"{User.Followers} {Locale.GetDeclension(User.Followers, "followers")}" : String.Empty;
        }

        private void FriendCommandRequested(FrameworkElement e)
        {
            CellButton cb = null;
            switch (User.FriendStatus)
            {
                case FriendStatus.None: break;
                case FriendStatus.InboundRequest: break;
                case FriendStatus.RequestSent:
                    cb = new CellButton { Icon = VKIconName.Icon28CancelOutline, Text = Locale.Get("usercard_friend_req_cancel") };
                    break;
                case FriendStatus.IsFriend:
                    cb = new CellButton { Icon = VKIconName.Icon28CancelOutline, Text = Locale.Get("usercard_friend_remove") };
                    break;
            }
            if (cb != null)
            {
                MenuFlyout mf = new MenuFlyout();
                mf.Items.Add(cb);
                mf.ShowAt(e);
            }
            else
            {
                // TODO: accept or send friend request
            }
        }
    }
}
