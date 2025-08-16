using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Pages.Popups;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.MyPeople;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Elorucov.Laney.ViewModel {
    public sealed class ConversationAttachmentsTabViewModel : ItemsViewModel<ConversationAttachment> {
        public bool End { get; set; } = false;
    }

    public sealed class ChatMembersTabViewModel : ItemsViewModel<ChatMemberEntity> {
        private List<ChatMemberEntity> _allMembers;
        private Func<string, Task<List<ChatMemberEntity>>> _searchAction;

        private string _searchQuery;
        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }
        public bool SearchAvailable { get { return Items.Count > 0; } }

        public ChatMembersTabViewModel(ObservableCollection<ChatMemberEntity> displayedItems, Func<List<ChatMemberEntity>> getAllMembersCallback, Func<string, Task<List<ChatMemberEntity>>> searchAction) : base(displayedItems) {
            _allMembers = getAllMembersCallback();
            _searchAction = searchAction;
            Items.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            OnPropertyChanged(nameof(SearchAvailable));
        }

        public void SearchMember() {
            if (IsLoading) return;
            new System.Action(async () => {
                Items.CollectionChanged -= Items_CollectionChanged; // Required, because searchbox is temporary disappear and focus losing from that searchbox.
                Items.Clear();
                if (!string.IsNullOrWhiteSpace(SearchQuery) && _searchAction != null) {
                    var items = await _searchAction(SearchQuery);
                    if (items != null) {
                        foreach (var member in items) Items.Add(member);
                    }
                } else {
                    foreach (var member in _allMembers) Items.Add(member);
                }
                Items.CollectionChanged += Items_CollectionChanged;
            })();
        }

        ~ChatMembersTabViewModel() {
            Items.CollectionChanged -= Items_CollectionChanged;
        }
    }

    public class PeerProfileViewModel : BaseViewModel {
        private long _id;
        private string _header;
        private string _subhead;
        private string _description;
        private Uri _avatar;
        private Photo _avatarPhoto;
        private ObservableCollection<TwoStringTuple> _information = new ObservableCollection<TwoStringTuple>();
        private ChatMembersTabViewModel _chatMembers;

        private ConversationAttachmentsTabViewModel _photos = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _videos = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _audios = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _documents = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _share = new ConversationAttachmentsTabViewModel();

        private bool _isLoading;
        private PlaceholderViewModel _placeholder;

        private UICommand _openAvatarCommand;

        private UICommand _firstCommand;
        private UICommand _secondCommand;
        private UICommand _thirdCommand;
        private UICommand _moreCommand;


        public long Id { get { return _id; } private set { _id = value; OnPropertyChanged(); } }
        public string Header { get { return _header; } private set { _header = value; OnPropertyChanged(); } }
        public string Subhead { get { return _subhead; } private set { _subhead = value; OnPropertyChanged(); } }
        public string Description { get { return _description; } private set { _description = value; OnPropertyChanged(); } }
        public Uri Avatar { get { return _avatar; } private set { _avatar = value; OnPropertyChanged(); } }
        public Photo AvatarPhoto { get { return _avatarPhoto; } private set { _avatarPhoto = value; OnPropertyChanged(); } }
        public ObservableCollection<TwoStringTuple> Information { get { return _information; } private set { _information = value; OnPropertyChanged(); } }
        public ChatMembersTabViewModel ChatMembers { get { return _chatMembers; } private set { _chatMembers = value; OnPropertyChanged(); } }

        public ConversationAttachmentsTabViewModel Photos { get { return _photos; } set { _photos = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Videos { get { return _videos; } set { _videos = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Audios { get { return _audios; } set { _audios = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Documents { get { return _documents; } set { _documents = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Share { get { return _share; } set { _share = value; OnPropertyChanged(); } }

        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } private set { _placeholder = value; OnPropertyChanged(); } }

        public UICommand OpenAvatarCommand { get { return _openAvatarCommand; } private set { _openAvatarCommand = value; OnPropertyChanged(); } }

        public UICommand FirstCommand { get { return _firstCommand; } private set { _firstCommand = value; OnPropertyChanged(); } }
        public UICommand SecondCommand { get { return _secondCommand; } private set { _secondCommand = value; OnPropertyChanged(); } }
        public UICommand ThirdCommand { get { return _thirdCommand; } private set { _thirdCommand = value; OnPropertyChanged(); } }
        public UICommand MoreCommand { get { return _moreCommand; } private set { _moreCommand = value; OnPropertyChanged(); } }

        private int _lastCmid = 0;
        private List<ChatMemberEntity> _allMembers = new List<ChatMemberEntity>();
        private ObservableCollection<ChatMemberEntity> _displayedMembers = new ObservableCollection<ChatMemberEntity>();
        private UICommand _notificationToggleCommand = null;

        private FrameworkElement _flyoutTarget;
        private Flyout _chatEditFlyout;

        public ChatPermissions ChatPermissions { get; private set; }

        public event EventHandler CloseWindowRequested;

        public PeerProfileViewModel(long peerId, FrameworkElement flyoutTarget, Flyout chatEditFlyout) {
            Id = peerId;
            _flyoutTarget = flyoutTarget;
            _chatEditFlyout = chatEditFlyout;
            new System.Action(async () => await SetupAsync())();
        }

        private async Task SetupAsync() {
            Header = null;
            if (Id.IsChat()) {
                ChatMembers = null;
                await GetChatAsync(Id);
            } else if (Id.IsUser()) {
                await GetUserAsync(Id);
            } else if (Id.IsGroup()) {
                await GetGroupAsync(Id * -1);
            }
        }

        #region User-specific

        private async Task GetUserAsync(long userId) {
            if (IsLoading) return;
            IsLoading = true;
            Placeholder = null;

            object response = await Execute.GetUserCard(userId);
            if (response is UserEx user) {
                _lastCmid = user.LastCMID;
                Header = user.FullName;
                if (user.Photo != null) Avatar = user.Photo;
                AvatarPhoto = user.OriginalPhoto;

                switch (user.Deactivated) {
                    case DeactivationState.Banned: Subhead = Locale.Get("userbanned"); break;
                    case DeactivationState.Deleted: Subhead = Locale.Get("userdeleted"); break;
                    default:
                        Subhead = VKClientsHelper.GetOnlineStatus(user);
                        if (user.OnlineAppId > 0 && AppParameters.ShowOnlineApplicationId)
                            Subhead += $" ({user.OnlineAppId})";
                        break;
                }

                SetupInfo(user);
                new System.Action(async () => await SetupCommandsAsync(user))();
            } else {
                Log.Error($"Wrong response for PeerProfileViewModel.GetUser!");
                Header = null;
                Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await GetUserAsync(userId));
            }

            IsLoading = false;
        }

        private void SetupInfo(UserEx user) {
            Information.Clear();

            // Banned/deleted/blocked...
            if (user.Blacklisted == 1) {
                Information.Add(new TwoStringTuple("", $"{String.Format(Locale.GetForFormat(user.Sex == Sex.Female ? "blacklisted_female" : "blacklisted_male"), user.FirstName)}."));
            }
            if (user.BlacklistedByMe == 1) {
                Information.Add(new TwoStringTuple("", $"{String.Format(Locale.GetForFormat("blacklisted_byme"), user.FirstNameAcc)}."));
            }

            // Domain
            Information.Add(new TwoStringTuple("", user.Domain));

            // Private profile
            if (user.IsClosed && !user.CanAccessClosed)
                Information.Add(new TwoStringTuple("", Locale.Get("userprivate")));

            // Status
            if (!string.IsNullOrEmpty(user.Status))
                Information.Add(new TwoStringTuple("", user.Status.Trim()));

            // Birthday
            if (!string.IsNullOrEmpty(user.BirthDate))
                Information.Add(new TwoStringTuple("", APIHelper.GetNormalizedBirthDate(user.BirthDate)));

            // Live in
            if (!string.IsNullOrWhiteSpace(user.LiveIn))
                Information.Add(new TwoStringTuple("", user.LiveIn.Trim()));

            // Work
            if (user.CurrentCareer != null) {
                var c = user.CurrentCareer;
                string h = c.Company.Trim();
                Information.Add(new TwoStringTuple("", string.IsNullOrWhiteSpace(c.Position) ? h : $"{h} — {c.Position.Trim()}"));
            }

            // Education
            if (!string.IsNullOrWhiteSpace(user.CurrentEducation)) Information.Add(new TwoStringTuple("", user.CurrentEducation.Trim()));

            // Site
            if (!string.IsNullOrWhiteSpace(user.Site)) Information.Add(new TwoStringTuple("", user.Site.Trim()));

            // Friends & Followers
            if (user.FriendsCount > 0) Information.Add(new TwoStringTuple("", $"{Locale.Get("friends")}: {user.FriendsCount}"));

            if (user.Followers > 0) Information.Add(new TwoStringTuple("", $"{Locale.Get("followers")}: {user.Followers}"));

            Information.Add(new TwoStringTuple("", user.Id.ToString()));
        }

        private async Task SetupCommandsAsync(UserEx user) {
            FirstCommand = null;
            SecondCommand = null;
            ThirdCommand = null;
            MoreCommand = null;
            List<UICommand> commands = new List<UICommand>();
            List<UICommand> moreCommands = new List<UICommand>();

            // Если нет истории сообщений с этим юзером,
            // и ему нельзя писать сообщение,
            // или если открыт чат с этим юзером,
            // то не будем добавлять эту кнопку
            if ((user.CanWritePrivateMessage == 1 || user.MessagesCount > 0) && (AppSession.CurrentConversationVM?.ConversationId != user.Id)) {
                UICommand messageCmd = new UICommand('', Locale.Get("go_to_conv"), false, (a) => {
                    CloseWindowRequested?.Invoke(this, null);
                    Main.GetCurrent().ShowConversationPage(user.Id);
                });
                commands.Add(messageCmd);
            }

            // Friend
            if (AppParameters.UserID != user.Id && user.Blacklisted == 0 && user.BlacklistedByMe == 0
                && user.Deactivated == DeactivationState.No && user.CanSendFriendRequest == 1) {
                char ficon = '';
                string flabel = "";

                switch (user.FriendStatus) {
                    case FriendStatus.None: flabel = Locale.Get("friend_add"); ficon = ''; break;
                    case FriendStatus.IsFriend: flabel = Locale.Get("friend_your"); ficon = ''; break;
                    case FriendStatus.InboundRequest: flabel = Locale.Get("friend_accept"); ficon = ''; break;
                    case FriendStatus.RequestSent: flabel = Locale.Get("friend_request"); ficon = ''; break;
                }

                UICommand friendCmd = new UICommand(ficon, flabel, false, async (a) => {
                    switch (user.FriendStatus) {
                        case FriendStatus.None: await AddFriend(); break;
                        case FriendStatus.IsFriend: await FriendDeleteConfirmation(Locale.Get("friend_delete")); break;
                        case FriendStatus.InboundRequest: await AddFriend(); break;
                        case FriendStatus.RequestSent: await FriendDeleteConfirmation(Locale.Get("friend_cancel")); break;
                    }
                });
                commands.Add(friendCmd);
            }

            // Notifications
            if (AppParameters.UserID != user.Id) {
                char notifIcon = user.NotificationsDisabled ? '' : '';
                Action<object> notifAct = async (a) => {
                    bool result = await ToggleNotificationsAsync(!user.NotificationsDisabled, user.Id);
                    if (result) user.NotificationsDisabled = !user.NotificationsDisabled;
                };
                _notificationToggleCommand = new UICommand(notifIcon, Locale.Get(user.NotificationsDisabled ? "disabled" : "enabled"), false, notifAct);
                commands.Add(_notificationToggleCommand);
            }

            // Open in browser
            UICommand openExternalCmd = new UICommand('', Locale.Get("open_browser_btn/Content"), false, async (a) => await Launcher.LaunchUriAsync(new Uri($"https://vk.com/id{user.Id}")));
            commands.Add(openExternalCmd);

            // Shared chats
            if (user.Id != AppParameters.UserID) {
                UICommand sharedChats = new UICommand('', Locale.Get("shared_chats"), false, (a) => {
                    var transform = _flyoutTarget.TransformToVisual(Window.Current.Content);
                    var position = transform.TransformPoint(new Point(-2, -2));
                    SharedConversations sc = new SharedConversations(position, user.Id);
                    sc.Closed += (c, d) => {
                        if (d != null && d is long peerId && peerId != 0) {
                            Main.GetCurrent().ShowConversationPage(peerId);
                            CloseWindowRequested?.Invoke(this, null);
                        }
                    };
                    sc.Show();
                });
                moreCommands.Add(sharedChats);
            }

            if (AppParameters.UserID != user.Id && user.CanWritePrivateMessage == 1 && ContactsPanel.IsContactPanelSupported) {
                bool isPinned = await ContactsPanel.IsPinned(user.Id);

                UICommand contactPanel = new UICommand(isPinned ? '' : '', Locale.Get(isPinned ? "contactpanel_unpin" : "contactpanel_pin"), false, async (a) => {
                    if (isPinned) {
                        await ContactsPanel.UnpinUserAsync(user.Id);
                    } else {
                        await ContactsPanel.PinUserAsync(AppSession.GetCachedUser(user.Id));
                    }
                });
                moreCommands.Add(contactPanel);
            }

            // Ban/unban
            if (AppParameters.UserID != user.Id && user.Blacklisted == 0) {
                char banIcon = user.BlacklistedByMe == 1 ? '' : '';
                string banLabel = Locale.Get(user.BlacklistedByMe == 1 ? "unblock" : "block");
                UICommand banCmd = new UICommand(banIcon, banLabel, user.BlacklistedByMe == 0, async (a) => await ToggleBanAsync(user, user.BlacklistedByMe == 1));
                moreCommands.Add(banCmd);
            }

            // Clear history
            if (user.MessagesCount > 0) {
                UICommand clearCmd = new UICommand('', Locale.Get("convctx_delete"), true, async (a) => {
                    await APIHelper.ClearChatHistoryAsync(Id, () => CloseWindowRequested?.Invoke(this, null));
                });
                moreCommands.Add(clearCmd);
            }

            UICommand moreCommand = new UICommand('', Locale.Get("more"), false, (a) => OpenContextMenu(a, commands, moreCommands));

            FirstCommand = commands[0];

            if (commands.Count < 2) {
                SecondCommand = moreCommand;
            } else if (commands.Count < 3) {
                SecondCommand = commands[1];
                ThirdCommand = moreCommand;
            } else {
                SecondCommand = commands[1];
                ThirdCommand = commands[2];
                MoreCommand = moreCommand;
            }
        }

        private async Task AddFriend() {
            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            object response = await ssp.ShowAsync(Friends.Add(Id));
            if (response is string str) {
                await SetupAsync();
            } else {
                Functions.ShowHandledErrorDialog(response);
            }
        }

        private async Task FriendDeleteConfirmation(string label) {
            var dialogResult = await new ContentDialog {
                Content = label,
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            }.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary) {
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object response = await ssp.ShowAsync(Friends.Delete(Id));
                if (response is bool) {
                    await SetupAsync();
                } else {
                    Functions.ShowHandledErrorDialog(response);
                }
            }
        }

        private async Task ToggleBanAsync(UserEx user, bool unban) {
            ContentDialogResult dlgresult = ContentDialogResult.Primary;

            if (!unban) {
                ContentDialog dlg = new ContentDialog {
                    Title = Locale.Get("block"),
                    Content = String.Format(Locale.GetForFormat("block_confirmation"), $"{user.FirstNameAcc} {user.LastNameAcc}"),
                    PrimaryButtonText = Locale.Get("yes"),
                    SecondaryButtonText = Locale.Get("no")
                };

                dlgresult = await dlg.ShowAsync();
            }

            IsLoading = true;

            var result = unban ?
                await Account.Unban(user.Id) :
                await Account.Ban(user.Id);

            if (result is bool) {
                IsLoading = false;
                await SetupAsync();
            } else {
                IsLoading = false;
                Functions.ShowHandledErrorDialog(result);
            }
        }

        #endregion

        #region Group-specific

        private async Task GetGroupAsync(long groupId) {
            if (IsLoading) return;
            IsLoading = true;
            Placeholder = null;

            object response = await Execute.GetGroupCard(groupId);
            if (response is GroupEx group) {
                Header = group.Name;
                if (group.Photo != null) Avatar = group.Photo;
                Subhead = group.Activity;
                _lastCmid = group.LastCMID;
                SetupInfo(group);
                SetupCommands(group);
            } else {
                Log.Error($"Wrong response for PeerProfileViewModel.GetGroup!");
                Header = null; // чтобы содержимое окна было скрыто
                Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await GetGroupAsync(groupId));
            }

            IsLoading = false;
        }

        private void SetupInfo(GroupEx group) {
            Information.Clear();

            Information.Add(new TwoStringTuple("", group.Id.ToString()));

            // Domain
            Information.Add(new TwoStringTuple("", !string.IsNullOrEmpty(group.ScreenName) ? group.ScreenName : $"club{group.Id}"));

            // Status
            if (!string.IsNullOrEmpty(group.Status))
                Information.Add(new TwoStringTuple("", group.Status.Trim()));

            // City
            string cc = null;
            if (group.City != null) cc = group.City.Title.Trim();
            if (group.Country != null) cc += !string.IsNullOrEmpty(cc) ? $", {group.Country.Title.Trim()}" : group.Country.Title.Trim();
            if (!string.IsNullOrEmpty(cc))
                Information.Add(new TwoStringTuple("", cc));

            // Site
            if (!string.IsNullOrWhiteSpace(group.Site))
                Information.Add(new TwoStringTuple("", group.Site.Trim()));

            // Members
            if (group.Members > 0)
                Information.Add(new TwoStringTuple("", $"{Locale.Get("followers")}: {group.Members}"));
        }

        private void SetupCommands(GroupEx group) {
            FirstCommand = null;
            SecondCommand = null;
            ThirdCommand = null;
            MoreCommand = null;
            List<UICommand> commands = new List<UICommand>();
            List<UICommand> moreCommands = new List<UICommand>();

            if ((group.CanMessage || group.MessagesCount > 0) && AppSession.CurrentConversationVM?.ConversationId != -group.Id) {
                UICommand messageCmd = new UICommand('', Locale.Get("go_to_conv"), false, (a) => {
                    CloseWindowRequested?.Invoke(this, null);
                    Main.GetCurrent().ShowConversationPage(-group.Id);
                });
                commands.Add(messageCmd);
            }

            // Notifications
            char notifIcon = group.NotificationsDisabled ? '' : '';
            Action<object> notifAct = async (a) => {
                bool result = await ToggleNotificationsAsync(!group.NotificationsDisabled, group.Id);
                if (result) group.NotificationsDisabled = !group.NotificationsDisabled;
            };
            _notificationToggleCommand = new UICommand(notifIcon, Locale.Get(group.NotificationsDisabled ? "disabled" : "enabled"), false, notifAct);
            commands.Add(_notificationToggleCommand);

            // Open in browser
            UICommand openExternalCmd = new UICommand('', Locale.Get("open_browser_btn/Content"), false, async (a) => await Launcher.LaunchUriAsync(new Uri($"https://vk.com/club{group.Id}")));
            commands.Add(openExternalCmd);

            // Allow/deny messages from group
            char banIcon = group.MessagesAllowed ? '' : '';
            string banLabel = Locale.Get(group.MessagesAllowed ? "conv_block_messages" : "conv_allow_messages");
            UICommand banCmd = new UICommand(banIcon, banLabel, group.MessagesAllowed, async (a) => await ToggleMessagesFromGroupAsync(group.Id, group.MessagesAllowed));
            moreCommands.Add(banCmd);

            // Clear history
            if (group.MessagesCount > 0) {
                UICommand clearCmd = new UICommand('', Locale.Get("convctx_delete"), true, async (a) => {
                    await APIHelper.ClearChatHistoryAsync(Id, () => CloseWindowRequested?.Invoke(this, null));
                });
                moreCommands.Add(clearCmd);
            }

            UICommand moreCommand = new UICommand('', Locale.Get("more"), false, (a) => OpenContextMenu(a, commands, moreCommands));

            FirstCommand = commands[0];

            if (commands.Count < 2) {
                SecondCommand = moreCommand;
            } else if (commands.Count < 3) {
                SecondCommand = commands[1];
                ThirdCommand = moreCommand;
            } else {
                SecondCommand = commands[1];
                ThirdCommand = commands[2];
                MoreCommand = moreCommand;
            }
        }

        private async Task ToggleMessagesFromGroupAsync(long groupId, bool allowed) {
            IsLoading = true;
            object result = allowed ?
                    await Messages.DenyMessagesFromGroup(groupId) :
                    await Messages.AllowMessagesFromGroup(groupId);
            IsLoading = false;

            if (result is bool) {
                await SetupAsync();
            } else {
                Log.Error($"Error in PeerProfileViewModel.ToggleMessageFromGroup!");
                Functions.ShowHandledErrorDialog(result);
            }
        }

        #endregion

        #region Chat-specific

        private async Task GetChatAsync(long peerId) {
            if (IsLoading) return;
            IsLoading = true;
            Placeholder = null;

            var response = await Execute.GetChat(peerId - 2000000000);
            if (response is ChatInfoEx chat) {
                _lastCmid = chat.LastCMID;
                Header = chat.Name;
                Description = chat.Description;
                ChatPermissions = chat.Permissions;
                if (chat.PhotoUri != null) Avatar = chat.PhotoUri;

                UpdateChatSubhead(chat);

                SetupCommands(chat);
                if (!chat.IsChannel && chat.State == UserStateInChat.In) {
                    if (ChatMembers == null) {
                        ChatMembers = new ChatMembersTabViewModel(_displayedMembers, () => _allMembers, async (query) => {
                            return await SearchChatMembersAsync(chat, query);
                        });
                    }
                    IsLoading = false; // required
                    await LoadChatMembersAsync(chat);
                }
            } else {
                Log.Error($"Error in PeerProfileViewModel.GetChatAsync!");
                Header = null; // чтобы содержимое окна было скрыто
                Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await GetChatAsync(peerId));
            }
            IsLoading = false;
        }

        private void UpdateChatSubhead(ChatInfoEx chat) {
            if (chat.State == UserStateInChat.In) {
                Subhead = string.Empty;
                if (chat.IsCasperChat) Subhead = $"{Locale.Get("chatinfo_casper").ToLowerInvariant()}, ";
                Subhead += $"{chat.MembersCount} {Locale.GetDeclension(chat.MembersCount, "members")}";
            } else {
                Subhead = Locale.Get(chat.State == UserStateInChat.Left ? "chat_left" : "chat_kicked");
            }
        }

        private async Task LoadChatMembersAsync(ChatInfoEx chat) {
            if (ChatMembers.IsLoading) return;

            ChatMembers.IsLoading = true;
            _allMembers.Clear();
            _displayedMembers.Clear();

            var response = await Messages.GetConversationMembers(Id);
            if (response is VKList<ChatMember> members) {
                AppSession.AddUsersToCache(members.Profiles);
                AppSession.AddGroupsToCache(members.Groups);
                if (members == null || members.Count == 0) {
                    ChatMembers.IsLoading = false;
                    return;
                }

                foreach (var member in members.Items) {
                    SetupMember(chat, member);
                }

                foreach (var member in _allMembers) {
                    _displayedMembers.Add(member);
                }
            } else {
                Log.Error($"Error in PeerProfileViewModel.LoadChatMembersAsync!");
                _displayedMembers.Clear(); // вдруг краш произойдёт при парсинге участников, а часть из них уже были добавлены в список/UI, их надо удалить.
                ChatMembers.Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await LoadChatMembersAsync(chat));
            }

            ChatMembers.IsLoading = false;
        }

        private async Task<List<ChatMemberEntity>> SearchChatMembersAsync(ChatInfoEx chat, string query) {
            if (ChatMembers.IsLoading) return null;

            ChatMembers.IsLoading = true;
            _displayedMembers.Clear();

            var response = await Messages.SearchConversationMembers(Id, query, 0, 200);
            if (response is VKList<ChatMember> members) {
                AppSession.AddUsersToCache(members.Profiles);
                AppSession.AddGroupsToCache(members.Groups);
                if (members == null || members.Count == 0) {
                    ChatMembers.IsLoading = false;
                    return null;
                }

                var items = new List<ChatMemberEntity>();
                foreach (var member in members.Items) {
                    SetupMember(chat, member, items);
                }
                ChatMembers.IsLoading = false;
                return items;
            } else {
                Log.Error($"Error in PeerProfileViewModel.SearchChatMembersAsync!");
                _displayedMembers.Clear(); // вдруг краш произойдёт при парсинге участников, а часть из них уже были добавлены в список/UI, их надо удалить.
                ChatMembers.Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await SearchChatMembersAsync(chat, query));
            }

            ChatMembers.IsLoading = false;
            return null;
        }

        private void SetupMember(ChatInfoEx chat, ChatMember member, List<ChatMemberEntity> list = null) {
            string name = member.MemberId.ToString();
            string desc = string.Empty;
            long mid = member.MemberId;
            long iid = member.InvitedBy;
            Uri avatar = null;

            string joinDate = APIHelper.GetNormalizedDate(member.JoinDate);

            member.NameGen = Locale.Get("chatinfo_kick_confirm_unknown");
            if (mid != iid) {
                string invitedBy = string.Empty;
                if (iid.IsUser()) {
                    var user = AppSession.GetCachedUser(iid);
                    if (user != null) {
                        invitedBy = $"{Locale.Get($"ChatInvitedBy{user.Sex}")} {user.FullName}";
                    }
                } else if (iid.IsGroup()) {
                    var group = AppSession.GetCachedGroup(iid);
                    if (group != null) {
                        invitedBy = $"{Locale.Get($"ChatInvitedByMale")} {group.Name}";
                    }
                }
                if (member.IsAdmin) desc = $"{Locale.Get("chatinfo_member_admin")}, ";
                desc += $"{invitedBy} {joinDate}";
            } else if (mid == chat.OwnerId) {
                desc = Locale.Get("chatinfo_member_creator");
            }

            if (mid.IsUser()) {
                var user = AppSession.GetCachedUser(member.MemberId);
                if (user != null) {
                    name = user.FullName;
                    avatar = user.Photo;
                }
                member.NameGen = user.FirstNameAcc;
            } else if (mid.IsGroup()) {
                var group = AppSession.GetCachedGroup(member.MemberId);
                if (group != null) {
                    name = group.Name;
                    avatar = group.Photo;
                }
            }

            var entity = new ChatMemberEntity(member, mid, name, desc, avatar);
            entity.ExtraButtonIcon = new FixedFontIcon() { Glyph = "" };
            entity.ExtraButtonCommand = SetUpMemberCommand(chat, member);

            if (list != null) {
                list.Add(entity);
            } else {
                _allMembers.Add(entity);
            }
        }

        private RelayCommand SetUpMemberCommand(ChatInfoEx chat, ChatMember member) {
            MenuFlyout mf = new MenuFlyout {
                Placement = FlyoutPlacementMode.Bottom
            };

            // TODO: админы (не создатель), которые тоже имеют права менять админов.
            bool canChangeAdmin = chat.OwnerId == AppParameters.UserID;

            if (canChangeAdmin && !member.IsAdmin && !member.IsRestrictedToWrite) {
                CheckAndAddMemRestrictionCommand(mf, chat, member);
            }
            if (canChangeAdmin && member.IsRestrictedToWrite) {
                MenuFlyoutItem dwmfi = new MenuFlyoutItem { Text = Locale.Get("chatinfo_memctx_enable_writing") };
                dwmfi.Click += async (c, d) => {
                    object r = await Messages.ChangeConversationMemberRestrictions(chat.PeerId, member.MemberId, false);
                    if (r is MemberRestrictionResponse resp) {
                        if (resp.FailedMemberIds.Contains(member.MemberId)) {
                            Tips.Show(Locale.Get("global_error"));
                            return;
                        }
                        member.IsRestrictedToWrite = false;
                        UpdateMemberInUI(chat, member);
                    } else {
                        Functions.ShowHandledErrorTip(r);
                    }
                };
                mf.Items.Add(dwmfi);
            }

            if (member.MemberId != AppParameters.UserID && canChangeAdmin) {
                var makeAdmin = new MenuFlyoutItem {
                    Text = Locale.Get(!member.IsAdmin ? "chatinfo_memctx_addadmin" : "chatinfo_memctx_remadmin")
                };
                makeAdmin.Click += async (c, d) => {
                    object r = await Messages.SetMemberRole(chat.PeerId, member.MemberId, member.IsAdmin ? "member" : "admin");
                    if (r is bool b) {
                        member.IsAdmin = !member.IsAdmin;
                        UpdateMemberInUI(chat, member);
                    } else {
                        Functions.ShowHandledErrorTip(r);
                    }
                };
                mf.Items.Add(makeAdmin);
            }

            if (member.MemberId != AppParameters.UserID && member.CanKick) {
                MenuFlyoutItem kick = new MenuFlyoutItem { Text = Locale.Get("chatinfo_memctx_kick") };
                kick.Click += async (c, d) => await RemoveMemberConfirmationAsync(chat, member);
                mf.Items.Add(kick);
            }

            if (mf.Items.Count > 0) {
                return new RelayCommand(o => mf.ShowAt(o as Control));
            }

            return null;
        }

        private void SetupCommands(ChatInfoEx chat) {
            FirstCommand = null;
            SecondCommand = null;
            ThirdCommand = null;
            MoreCommand = null;
            List<UICommand> commands = new List<UICommand>();
            List<UICommand> moreCommands = new List<UICommand>();

            // Edit
            if (chat.ACL.CanChangeInfo) {
                UICommand editCmd = new UICommand('', Locale.Get("chat_edit"), false, (a) => {
                    var target = a as FrameworkElement;
                    target.AllowFocusOnInteraction = true; // vk.cc/ceaSiX
                    _chatEditFlyout.ShowAt(target);
                });
                commands.Add(editCmd);
            }

            // Add member
            if (chat.ACL.CanInvite) {
                UICommand addCmd = new UICommand('', Locale.Get("add_member"), false, (a) => {
                    AddChatUserModal acum = new AddChatUserModal(chat.ChatId, _allMembers.Select(k => k.Id).ToList());
                    acum.Closed += async (sender, result) => {
                        if (result is bool) {
                            await SetupAsync();
                        }
                    };
                    acum.Show();
                });
                commands.Add(addCmd);
            }

            // Notifications
            bool notifsDisabled = chat.PushSettings != null && chat.PushSettings.DisabledForever;
            char notifIcon = notifsDisabled ? '' : '';

            Action<object> notifAct = async (a) => {
                bool result = await ToggleNotificationsAsync(!notifsDisabled, chat.PeerId);
                if (result && chat.PushSettings != null) {
                    chat.PushSettings.DisabledForever = !chat.PushSettings.DisabledForever;
                    notifsDisabled = !notifsDisabled;
                }
            };
            _notificationToggleCommand = new UICommand(notifIcon, Locale.Get(notifsDisabled ? "disabled" : "enabled"), false, notifAct);
            commands.Add(_notificationToggleCommand);

            // Link
            if (chat.ACL.CanSeeInviteLink) {
                UICommand chatLinkCmd = new UICommand('', Locale.Get("copy_link_short"), false, async (a) => {
                    ChatCreateInviteLink dlg = new ChatCreateInviteLink(Id, chat.IsChannel, chat.ACL.CanChangeInviteLink);
                    await dlg.ShowAsync();
                });
                commands.Add(chatLinkCmd);
            }

            // Unpin message
            if (chat.ACL.CanChangePin && chat.PinnedMessage != null) {
                UICommand unpinCmd = null;

                Action<object> task = new Action<object>(async (a) => {
                    VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                    object response = await ssp.ShowAsync(Messages.Unpin(chat.PeerId));
                    if (response is bool && (bool)response) {
                        chat.PinnedMessage = null;
                        moreCommands.Remove(unpinCmd);
                    } else {
                        Functions.ShowHandledErrorDialog(response);
                    }
                });

                unpinCmd = new UICommand('', Locale.Get("unpin_message"), false, task);
                moreCommands.Add(unpinCmd);
            }

            // Change mention parameters
            MenuFlyout mf1 = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
            var mentParams = new List<string> { "none", "allOnline", "all" };
            foreach (string p in mentParams) {
                bool mentionsOff = chat.PushSettings.DisabledMentions;
                bool massMentionsOff = chat.PushSettings.DisabledMassMentions;
                bool isCurrent = false;

                switch (p) {
                    case "none":
                        isCurrent = !mentionsOff && !massMentionsOff;
                        break;
                    case "allOnline":
                        isCurrent = !mentionsOff && massMentionsOff;
                        break;
                    case "all":
                        isCurrent = mentionsOff && massMentionsOff;
                        break;
                }

                ToggleMenuFlyoutItem mfi = new ToggleMenuFlyoutItem {
                    Text = Locale.Get($"mention_param_{p}"),
                    IsChecked = isCurrent
                };
                mfi.Click += async (c, d) => {
                    object r = await Messages.MuteChatMentions(Id, p);
                    if (r is bool b && b) {
                        await SetupAsync();
                    } else {
                        Functions.ShowHandledErrorTip(r);
                    }
                };
                mf1.Items.Add(mfi);
            }
            UICommand mentionParameter = new UICommand('', Locale.Get("mention_param"), false, (a) => {
                mf1.ShowAt(_flyoutTarget);
            });
            moreCommands.Add(mentionParameter);

            // Enable/disable writing
            if (chat.OwnerId == AppParameters.UserID) {
                if (chat.WritingDisabled != null && chat.WritingDisabled.Value) {
                    UICommand enableWriting = new UICommand('', Locale.Get("convctx_writing_enable"), false, async (a) => {
                        object r = await Messages.EnableChatWriting(chat.ChatId);
                        if (r is bool b && b) {
                            await SetupAsync();
                        } else {
                            Functions.ShowHandledErrorTip(r);
                        }
                    });
                    moreCommands.Add(enableWriting);
                } else {
                    MenuFlyout mf2 = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
                    var seconds = new List<int> { 3600, 28800, 86400, 0 };
                    foreach (int second in seconds) {
                        int hour = second / 3600;
                        MenuFlyoutItem mfi = new MenuFlyoutItem {
                            Text = second == 0 ?
                                Locale.Get("forever") :
                                Locale.GetDeclensionForFormatSimple(hour, "for_hours")
                        };
                        mfi.Click += async (c, d) => {
                            object r = await Messages.DisableChatWriting(chat.ChatId, second);
                            if (r is bool b && b) {
                                await SetupAsync();
                            } else {
                                Functions.ShowHandledErrorTip(r);
                            }
                        };
                        mf2.Items.Add(mfi);
                    }
                    UICommand disableWriting = new UICommand('', Locale.Get("convctx_writing_disable"), false, (a) => {
                        mf2.ShowAt(_flyoutTarget);
                    });
                    moreCommands.Add(disableWriting);
                }
            }

            // Exit or return to chat/channel
            if (chat.State != UserStateInChat.Kicked) {
                string type = chat.IsChannel ? "channel" : "chat";
                string exitLabel = Locale.Get($"chatinfo_btn_leave{type}");
                string returnLabel = Locale.Get(chat.IsChannel ? "channel_subscribe" : "conv_return_chat");
                char icon = chat.State == UserStateInChat.In ? '' : '';
                UICommand exitRetCmd = new UICommand(icon, chat.State == UserStateInChat.In ? exitLabel : returnLabel, chat.State == UserStateInChat.In, async (a) => {
                    if (chat.State == UserStateInChat.In) {
                        await LeaveChatAsync(chat);
                    } else {
                        await ReturnToChatAsync(chat);
                    }
                });
                moreCommands.Add(exitRetCmd);
            }

            // Clear history
            UICommand clearCmd = new UICommand('', Locale.Get("convctx_delete"), true, async (a) => {
                await APIHelper.ClearChatHistoryAsync(Id, () => CloseWindowRequested?.Invoke(this, null));
            });
            moreCommands.Add(clearCmd);

            // Delete chat
            UICommand deleteChatCmd = new UICommand('', Locale.Get("convctx_delete_for_all"), true, async (a) => {
                await APIHelper.DeleteChatForAllAsync(chat.ChatId, chat.Name, () => CloseWindowRequested?.Invoke(this, null));
            });
            if (chat.Permissions != null) moreCommands.Add(deleteChatCmd);

            UICommand moreCommand = new UICommand('', Locale.Get("more"), false, (a) => OpenContextMenu(a, commands, moreCommands));

            FirstCommand = commands[0];

            if (commands.Count < 2) {
                SecondCommand = moreCommand;
            } else if (commands.Count < 3) {
                SecondCommand = commands[1];
                ThirdCommand = moreCommand;
            } else {
                SecondCommand = commands[1];
                ThirdCommand = commands[2];
                MoreCommand = moreCommand;
            }
        }

        private void CheckAndAddMemRestrictionCommand(MenuFlyout mf, ChatInfoEx chat, ChatMember member) {
            MenuFlyoutSubItem dwmfi = new MenuFlyoutSubItem { Text = Locale.Get("chatinfo_memctx_disable_writing") };
            var seconds = new List<int> { 3600, 28800, 86400, 0 };
            foreach (int second in seconds) {
                int hour = second / 3600;
                MenuFlyoutItem mfi = new MenuFlyoutItem {
                    Text = second == 0 ?
                        Locale.Get("forever") :
                        Locale.GetDeclensionForFormatSimple(hour, "for_hours")
                };
                mfi.Click += async (c, d) => {
                    object r = await Messages.ChangeConversationMemberRestrictions(chat.PeerId, member.MemberId, true, second);
                    if (r is MemberRestrictionResponse resp) {
                        if (resp.FailedMemberIds.Contains(member.MemberId)) {
                            Tips.Show(Locale.Get("global_error"));
                            return;
                        }
                        member.IsRestrictedToWrite = true;
                        UpdateMemberInUI(chat, member);
                    } else {
                        Functions.ShowHandledErrorTip(r);
                    }
                };
                dwmfi.Items.Add(mfi);
            }

            mf.Items.Add(dwmfi);
        }


        private async Task RemoveMemberConfirmationAsync(ChatInfoEx chat, ChatMember member) {
            ContentDialog dlg = new ContentDialog {
                Content = string.Format(Locale.GetForFormat("chatinfo_kick_confirm"), member.NameGen),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object resp = await ssp.ShowAsync(Messages.RemoveChatUser(chat.ChatId, member.MemberId));

                if (resp is bool) {
                    RemoveMemberFromList(member);

                    chat.MembersCount -= 1;
                    UpdateChatSubhead(chat);
                } else {
                    Functions.ShowHandledErrorDialog(resp);
                }
            }
        }

        private void UpdateMemberInUI(ChatInfoEx chat, ChatMember member) {
            SetupMember(chat, member);

            var entity = _allMembers.Where(e => e.Id == member.MemberId).FirstOrDefault();
            if (entity != null) {
                int i = _allMembers.IndexOf(entity);
                _allMembers.Remove(entity);
                _allMembers.Insert(i, entity);
            }

            entity = _displayedMembers.Where(e => e.Id == member.MemberId).FirstOrDefault();
            if (entity != null) {
                int i = _displayedMembers.IndexOf(entity);
                _displayedMembers.Remove(entity);
                _displayedMembers.Insert(i, entity);
            }
        }

        private void RemoveMemberFromList(ChatMember member) {
            var entity = _allMembers.Where(m => m.Id == member.MemberId).FirstOrDefault();
            if (entity != null) _allMembers.Remove(entity);

            entity = _displayedMembers.Where(m => m.Id == member.MemberId).FirstOrDefault();
            if (entity != null) _displayedMembers.Remove(entity);
        }

        private async Task LeaveChatAsync(ChatInfoEx chat) {
            string type = chat.IsChannel ? "channel" : "chat";
            ContentDialog dlg = new ContentDialog {
                Title = Locale.Get($"chatinfo_btn_leave{type}"),
                Content = Locale.Get($"{type}_leave_confirmation"),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no"),
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object response = await ssp.ShowAsync(Messages.RemoveChatUser(chat.ChatId, AppParameters.UserID));
                if (response is bool r && r) {
                    CloseWindowRequested?.Invoke(this, null);
                } else {
                    Functions.ShowHandledErrorDialog(response);
                }
            }
        }

        private async Task ReturnToChatAsync(ChatInfoEx chat) {
            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            object response = await ssp.ShowAsync(Messages.AddChatUser(chat.ChatId, AppParameters.UserID));
            if (response is bool r && r) {
                CloseWindowRequested?.Invoke(this, null);
            } else {
                Functions.ShowHandledErrorDialog(response);
            }
        }

        #endregion

        #region General UICommands

        private void OpenContextMenu(object target, List<UICommand> commands, List<UICommand> moreCommands) {
            MenuFlyout mf = new MenuFlyout {
                Placement = FlyoutPlacementMode.Bottom
            };

            if (commands.Count > 3) {
                commands = commands.GetRange(3, commands.Count - 3);
                foreach (var item in commands) {
                    MenuFlyoutItem mfi = new MenuFlyoutItem {
                        Icon = new FixedFontIcon { Glyph = item.Icon.ToString() },
                        Text = item.Label
                    };
                    mfi.Click += (a, b) => item.Action.Execute(mfi);
                    if (item.IsDestructive) mfi.Style = (Style)App.Current.Resources["DestructiveMenuFlyoutItem"];
                    mf.Items.Add(mfi);
                }
            }

            if (mf.Items.Count > 0) mf.Items.Add(new MenuFlyoutSeparator());

            foreach (var item in moreCommands) {
                MenuFlyoutItem mfi = new MenuFlyoutItem {
                    Icon = new FixedFontIcon { Glyph = item.Icon.ToString() },
                    Text = item.Label
                };
                mfi.Click += (a, b) => item.Action.Execute(mfi);
                if (item.IsDestructive) mfi.Style = (Style)App.Current.Resources["DestructiveMenuFlyoutItem"];
                mf.Items.Add(mfi);
            }

            mf.ShowAt(target as Control);

        }

        private async Task<bool> ToggleNotificationsAsync(bool enabled, long id) {
            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Account.SetSilenceMode(!enabled ? 0 : -1, id));
            if (resp is bool r && r) {
                _notificationToggleCommand.Icon = enabled ? '' : '';
                _notificationToggleCommand.Label = Locale.Get(!enabled ? "enabled" : "disabled");

                // Signal UI to refresh bindings
                OnPropertyChanged(nameof(FirstCommand));
                OnPropertyChanged(nameof(SecondCommand));
                OnPropertyChanged(nameof(ThirdCommand));
                return true;
            }
            Functions.ShowHandledErrorTip(resp);
            return false;
        }

        #endregion

        #region Conversation attachments

        public async Task LoadPhotosAsync() {
            await LoadConvAttachmentsAsync(Photos, "photo");
        }

        public async Task LoadVideosAsync() {
            await LoadConvAttachmentsAsync(Videos, "video");
        }

        public async Task LoadAudiosAsync() {
            await LoadConvAttachmentsAsync(Audios, "audio");
        }

        public async Task LoadDocsAsync() {
            await LoadConvAttachmentsAsync(Documents, "doc");
        }

        public async Task LoadLinksAsync() {
            await LoadConvAttachmentsAsync(Share, "share");
        }

        //public void LoadGraffities() {
        //    LoadVM(Graffities, HistoryAttachmentMediaType.Graffiti);
        //}

        //public void LoadAudioMessages() {
        //    LoadVM(AudioMessages, HistoryAttachmentMediaType.AudioMessage);
        //}

        private async Task LoadConvAttachmentsAsync(ConversationAttachmentsTabViewModel ivm, string type) {
            if (ivm.IsLoading || ivm.End) return;
            ivm.Placeholder = null;
            ivm.IsLoading = true;

            object response = await Messages.GetHistoryAttachments(Id, type, _lastCmid, ivm.Items.Count, 60);
            if (response is ConversationAttachmentsResponse resp) {
                AppSession.AddUsersToCache(resp.Profiles);
                AppSession.AddGroupsToCache(resp.Groups);
                foreach (var item in resp.Items) {
                    ivm.Items.Add(item);
                }
                if (resp.Items.Count < 60) ivm.End = true;

                if (ivm.Items.Count == 0) {
                    ivm.Placeholder = new PlaceholderViewModel(null, null, Locale.Get($"pp_attachments_{type}"));
                    ivm.End = true;
                }
            } else {
                Log.Error("Error in PeerProfileViewModel.LoadConvAttachmentsAsync!");
                if (ivm.Items.Count == 0) {
                    ivm.Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await LoadConvAttachmentsAsync(ivm, type));
                } else {
                    Functions.ShowHandledErrorDialog(response, async () => await LoadConvAttachmentsAsync(ivm, type));
                }
            }

            ivm.IsLoading = false;
        }

        #endregion
    }
}
