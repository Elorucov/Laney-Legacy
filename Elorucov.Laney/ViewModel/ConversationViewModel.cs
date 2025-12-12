using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.LongPoll;
using Elorucov.Laney.Services.MyPeople;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VK.VKUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.ViewModel {
    public enum MessageSendRestriction {
        None, Banned, Channel
    }

    public class ConversationViewModel : BaseViewModel {
        private long _id = 0;
        private bool _isReady;

        private PeerType _type = PeerType.Chat;
        private string _title;
        private Uri _photo;
        private bool _hasUnreadMessageFromCurrentUser;
        private bool _isLastMessageOutgoing;
        private int _unreadMessagesCount;
        private SortId _sortId;
        private bool _isverified;
        private bool _isDisappearing;
        private bool _isDonut;
        private bool _isMuted;
        private bool _isArchived;
        private LMessage _pinnedMessage;
        private int _membersCount;
        private int _online;
        private int _inread;
        private int _outread;
        private List<int> _folderIds = new List<int>();
        private ChatSettings _csettings;
        private CanWrite _canwrite;
        private BotKeyboard _currentKeyboard;
        private ObservableCollection<int> _mentions;
        private ObservableCollection<int> _unreadReactions;
        private bool _hasMention;
        private DataTemplate _mentionIcon;
        private bool _isMarkedUnread;
        private WritingDisabledInfo _isWritingDisabledForAll;

        private bool _isLoading = false;
        private string _subtitle;
        private string _usersActivityInfo;
        private MessagesCollection _messages = new MessagesCollection();
        private MessageFormViewModel _messageFormViewModel;
        private MessageSendRestriction _messageSendRestriction;
        public string _style;
        private string _restrictionReason;
        private bool _isBottomButtonShow;
        private string _bottomButtonContent;
        private bool _isEmptyDialog;
        private bool _isEmptyDialogStubVisible;
        private string _emptyDialogStubText;
        private bool _canAddToFriend;
        private string _errorTitle;
        private string _errorInfo;
        private LMessage _firstVisibleMessage;
        private LMessage _lastVisibleMessage;
        private ObservableCollection<LMessage> _receivedMessages = new ObservableCollection<LMessage>();
        private RelayCommand _titlebarCommand;
        private RelayCommand _contextMenuCommand;
        private RelayCommand _pinnedMessageCommand;
        private RelayCommand _buttonDownCommand;
        private RelayCommand _goToMentionCommand;
        private RelayCommand _bottomButtonCommand;
        private RelayCommand _addFriendCommand;
        private RelayCommand _viewProfileCommand;
        private RelayCommand _retryCommand;
        private RelayCommand _getToUnreadReactedMessageCommand;

        public long ConversationId { get { return _id; } }
        public bool IsReady { get { return _isReady; } private set { _isReady = value; OnPropertyChanged(); } }

        public PeerType Type { get { return _type; } }
        public string Title { get { return _title; } set { _title = value; OnPropertyChanged(); } }
        public Uri Photo { get { return _photo; } set { _photo = value; OnPropertyChanged(); } }
        public bool HasUnreadMessageFromCurrentUser { get { return _hasUnreadMessageFromCurrentUser; } set { _hasUnreadMessageFromCurrentUser = value; OnPropertyChanged(); } }
        public bool IsLastMessageOutgoing { get { return _isLastMessageOutgoing; } set { _isLastMessageOutgoing = value; OnPropertyChanged(); } }
        public bool IsPinned { get { return SortId.MajorId > 0 && SortId.MajorId % 16 == 0; } }
        public bool UICanDisplayReadIndicator { get { return IsLastMessageOutgoing && !HasUnreadMessageFromCurrentUser; } }
        public int UnreadMessagesCount { get { return _unreadMessagesCount; } private set { _unreadMessagesCount = value; OnPropertyChanged(); } }
        public SortId SortId { get { return _sortId; } private set { _sortId = value; OnPropertyChanged(); } }
        public bool IsVerified { get { return _isverified; } private set { _isverified = value; OnPropertyChanged(); } }
        public bool IsDisappearing { get { return _isDisappearing; } private set { _isDisappearing = value; OnPropertyChanged(); } }
        public bool IsDonut { get { return _isDonut; } private set { _isDonut = value; OnPropertyChanged(); } }
        public bool IsMuted { get { return _isMuted; } private set { _isMuted = value; OnPropertyChanged(); } }
        public bool IsArchived { get { return _isArchived; } private set { _isArchived = value; OnPropertyChanged(); } }
        public LMessage PinnedMessage { get { return _pinnedMessage; } private set { _pinnedMessage = value; OnPropertyChanged(); } }
        public int MembersCount { get { return _membersCount; } private set { _membersCount = value; OnPropertyChanged(); } }
        public int Online { get { return _online; } set { _online = value; OnPropertyChanged(); } }
        public int InRead { get { return _inread; } private set { _inread = value; OnPropertyChanged(); } }
        public int OutRead { get { return _outread; } private set { _outread = value; OnPropertyChanged(); } }
        public List<int> FolderIds { get { return _folderIds; } private set { _folderIds = value; OnPropertyChanged(); } }
        public ChatSettings ChatSettings { get { return _csettings; } set { _csettings = value; OnPropertyChanged(); } }
        public CanWrite CanWrite { get { return _canwrite; } set { _canwrite = value; OnPropertyChanged(); } }
        public BotKeyboard CurrentKeyboard { get { return _currentKeyboard; } set { _currentKeyboard = value; OnPropertyChanged(); } }
        public ObservableCollection<int> Mentions { get { return _mentions; } set { _mentions = value; OnPropertyChanged(); } }
        public ObservableCollection<int> UnreadReactions { get { return _unreadReactions; } set { _unreadReactions = value; OnPropertyChanged(); } }
        public int UnreadReactionsCount { get { return _unreadReactions == null ? 0 : _unreadReactions.Count; } }
        public DataTemplate MentionIcon { get { return _mentionIcon; } private set { _mentionIcon = value; OnPropertyChanged(); } }
        public bool HasMention { get { return _hasMention; } private set { _hasMention = value; OnPropertyChanged(); } }
        public bool IsMarkedUnread { get { return _isMarkedUnread; } set { _isMarkedUnread = value; OnPropertyChanged(); } }
        public WritingDisabledInfo IsWritingDisabledForAll { get { return _isWritingDisabledForAll; } set { _isWritingDisabledForAll = value; OnPropertyChanged(); } }

        public bool IsLoading { get { return _isLoading; } set { _isLoading = value; OnPropertyChanged(); } }
        public string Subtitle { get { return _subtitle; } set { _subtitle = value; OnPropertyChanged(); } }
        public string UsersActivityInfo { get { return _usersActivityInfo; } set { _usersActivityInfo = value; OnPropertyChanged(); } }
        public MessagesCollection Messages { get { return _messages; } set { _messages = value; OnPropertyChanged(); } }
        public MessageFormViewModel MessageFormViewModel { get { return _messageFormViewModel; } set { _messageFormViewModel = value; OnPropertyChanged(); } }
        public MessageSendRestriction MessageSendRestriction { get { return _messageSendRestriction; } private set { _messageSendRestriction = value; OnPropertyChanged(); } }
        public string Style { get { return _style; } set { _style = value; OnPropertyChanged(); } }
        public string RestrictionReason { get { return _restrictionReason; } private set { _restrictionReason = value; OnPropertyChanged(); } }
        public bool IsBottomButtonShow { get { return _isBottomButtonShow; } set { _isBottomButtonShow = value; OnPropertyChanged(); } }
        public string BottomButtonContent { get { return _bottomButtonContent; } private set { _bottomButtonContent = value; OnPropertyChanged(); } }
        public bool IsEmptyDialog { get { return _isEmptyDialog; } set { _isEmptyDialog = value; OnPropertyChanged(); } }
        public bool IsEmptyDialogStubVisible { get { return _isEmptyDialogStubVisible; } set { _isEmptyDialogStubVisible = value; OnPropertyChanged(); } }
        public string EmptyDialogStubText { get { return _emptyDialogStubText; } set { _emptyDialogStubText = value; OnPropertyChanged(); } }
        public bool CanAddToFriend { get { return _canAddToFriend; } set { _canAddToFriend = value; OnPropertyChanged(); } }
        public string ErrorTitle { get { return _errorTitle; } set { _errorTitle = value; OnPropertyChanged(); } }
        public string ErrorInfo { get { return _errorInfo; } set { _errorInfo = value; OnPropertyChanged(); } }
        public LMessage FirstVisibleMessage { get { return _firstVisibleMessage; } set { _firstVisibleMessage = value; OnPropertyChanged(); } }
        public LMessage LastVisibleMessage { get { return _lastVisibleMessage; } set { _lastVisibleMessage = value; OnPropertyChanged(); } }
        public ObservableCollection<LMessage> ReceivedMessages { get { return _receivedMessages; } private set { _receivedMessages = value; OnPropertyChanged(); } }
        public LMessage LastMessage { get { return ReceivedMessages.LastOrDefault(); } }
        public RelayCommand TitlebarCommand { get { return _titlebarCommand; } }
        public RelayCommand ContextMenuCommand { get { return _contextMenuCommand; } }
        public RelayCommand PinnedMessageCommand { get { return _pinnedMessageCommand; } }
        public RelayCommand ButtonDownCommand { get { return _buttonDownCommand; } }
        public RelayCommand GoToMentionCommand { get { return _goToMentionCommand; } }
        public RelayCommand BottomButtonCommand { get { return _bottomButtonCommand; } }
        public RelayCommand AddFriendCommand { get { return _addFriendCommand; } private set { _addFriendCommand = value; OnPropertyChanged(); } }
        public RelayCommand ViewProfileCommand { get { return _viewProfileCommand; } private set { _viewProfileCommand = value; OnPropertyChanged(); } }
        public RelayCommand RetryCommand { get { return _retryCommand; } private set { _retryCommand = value; OnPropertyChanged(); } }
        public RelayCommand GetToUnreadReactedMessageCommand { get { return _getToUnreadReactedMessageCommand; } private set { _getToUnreadReactedMessageCommand = value; OnPropertyChanged(); } }

        public List<User> MemberUsers = new List<User>();
        public List<Group> MemberGroups = new List<Group>();
        public List<ChatMember> Members { get; private set; } = new List<ChatMember>();
        private bool IsMessagesFromGroupAllowed = false;

        // Message id, animate, isNewMessage
        public Action<int, bool, bool> ScrollToMessageCallback;
        public Action<Uri, string> ShowSnackbarRequested;

        private int MessagesLoadCount { get { return AppParameters.MessagesLoadCount < 0 ? 40 : AppParameters.MessagesLoadCount; } }

        bool isEventsCommandsReady = false;
        bool isFirstTimeOpened = false;

        public ConversationViewModel() { }

        public ConversationViewModel(Conversation con, Message lastMessage, int messageId = 0) {
            _id = con.Peer.Id;
            PropertyChanged += ConversationViewModel_PropertyChanged;
            MessageFormViewModel = new MessageFormViewModel(con.Peer.Id);
            LMessage lmsg = new LMessage(lastMessage);

            ReceivedMessages.Add(lmsg);
            OnPropertyChanged(nameof(LastMessage));

            SetUpViewModel(con);
            FixState(lmsg);
        }

        public ConversationViewModel(long peerid, int messageId = 0) {
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}, peer id: {peerid}.");
            _id = peerid;
            PropertyChanged += ConversationViewModel_PropertyChanged;
            MessageFormViewModel = new MessageFormViewModel(peerid);

            // Commands
            if (!isEventsCommandsReady) {
                SetUpCommands();
                RegisterLongpollEvents();
            }
        }

        public void OnOpened(int messageId = 0) {
            Log.Info($"{GetType().GetTypeInfo().BaseType.Name} {GetType()} > {ConversationId} opened. First time opened: {isFirstTimeOpened}, msgs count: {Messages.Count}");
            if (isFirstTimeOpened && Messages.Count > 0) return;
            isFirstTimeOpened = true;
            GoToMessage(messageId);
        }

        #region Load messages

        public void GoToMessage(LMessage msg) {
            if (msg.Id == 0 || msg.ConversationMessageId == 0 || msg.IsUnavailable) {
                VKMessageDialog md = new VKMessageDialog(msg);
                md.Title = PinnedMessage != null && PinnedMessage.ConversationMessageId == msg.ConversationMessageId ?
                    Locale.Get("msgmodaltitle_pinned") : Locale.Get("msgmodaltitle_default");
                md.Show();
            } else {
                GoToMessage(msg.ConversationMessageId);
            }
        }

        public void GoToMessage(int messageId) {
            isFirstTimeOpened = true;
            var m = from q in Messages where q.ConversationMessageId == messageId select q;
            if (m.Count() == 1) {
                ScrollToMessageCallback?.Invoke(m.First().ConversationMessageId, true, false);
            } else {
                new System.Action(async () => { await LoadMessagesAsync(messageId); })();
            }
        }

        bool incrementalLoadingTimeout = false;

        public async Task LoadMessagesAsync(int startMessageId = -1, int offset = 0, int count = 0) {
            ErrorInfo = null;
            if (IsLoading || incrementalLoadingTimeout || ConversationId == 0) return;
            if (Messages != null) {
                Messages.Clear();
            } else {
                Messages = new MessagesCollection();
            }
            await Task.Yield();

            if (count == 0) count = MessagesLoadCount;
            if (offset == 0) offset = -count / 2;
            IsLoading = true;
            incrementalLoadingTimeout = true;

            Log.Info($"Starting load messages for {ConversationId}. Count: {count}, offset: {offset}, start: {startMessageId}");

            object resp = await Execute.GetHistory(ConversationId, startMessageId, offset, count);
            if (resp is MessagesHistoryResponseEx mhr) {
                if (mhr.Count == 0 && Messages.Count == 0 && ReceivedMessages.Count > 0) {
                    Log.Error($"Something went wrong with loadng messages in {ConversationId}, start: {startMessageId}!");
                }

                SetUpMembers(mhr);
                SetUpViewModel(mhr.Conversation, mhr.OnlineInfo);
                IsMessagesFromGroupAllowed = mhr.IsMessagesAllowed;
                IsEmptyDialog = mhr.Count == 0;

                mhr.Messages?.Reverse();
                MessagesCollection mc = new MessagesCollection(mhr.Messages);
                foreach (LMessage msg in mc) {
                    FixState(msg);
                }
                TryAddLastMessageToReceived(new LMessage(mhr.LastMessage));
                Messages = mc;
                if (startMessageId > 0) ScrollToMessageCallback?.Invoke(startMessageId, true, false);
                if (startMessageId == -1) {
                    ScrollToMessageCallback?.Invoke(InRead, false, false);
                }
                if (startMessageId == 0) {
                    var fm = Messages.FirstOrDefault();
                    if (fm != null) ScrollToMessageCallback?.Invoke(fm.ConversationMessageId, false, false);
                }
            } else {
                var err = Functions.GetNormalErrorInfo(resp);
                ErrorTitle = err.Item1;
                ErrorInfo = err.Item2;
                RetryCommand = new RelayCommand(async (o) => await LoadMessagesAsync(startMessageId, offset, count));
            }
            IsLoading = false;
            await Task.Delay(500);
            incrementalLoadingTimeout = false;
        }

        private void TryAddLastMessageToReceived(LMessage message) {
            var msgInReceived = _receivedMessages.Where(m => m.ConversationMessageId == message.ConversationMessageId).FirstOrDefault();
            if (msgInReceived != null) {
                if (msgInReceived.UISentMessageState != SentMessageState.Loading) message.UISentMessageState = msgInReceived.UISentMessageState;
                _receivedMessages.Remove(msgInReceived);
            }
            _receivedMessages.Add(message);
            OnPropertyChanged(nameof(LastMessage));
        }

        int firstConvMessageId = 0;

        public async Task LoadPreviousMessagesAsync() {
            if (Messages.Count == 0 || IsLoading || incrementalLoadingTimeout) return;
            if (Messages.Count > 0 && Messages.FirstOrDefault().ConversationMessageId == firstConvMessageId) return;
            int count = MessagesLoadCount;
            IsLoading = true;
            incrementalLoadingTimeout = true;
            object resp = await Execute.GetHistory(ConversationId, Messages.First().ConversationMessageId, 1, count, true);
            if (resp is MessagesHistoryResponseEx mhr) {
                SetUpMembers(mhr);
                SetUpViewModel(mhr.Conversation, mhr.OnlineInfo);
                IsMessagesFromGroupAllowed = mhr.IsMessagesAllowed;
                mhr.Messages?.Reverse();

                if (mhr.Messages != null) {
                    if (mhr.Messages.Count == 0 && Messages.Count > 0)
                        firstConvMessageId = Messages.FirstOrDefault().ConversationMessageId;

                    foreach (Message rmsg in mhr.Messages) {
                        LMessage msg = new LMessage(rmsg);
                        FixState(msg);
                        Messages.Insert(msg);
                    }
                }
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
            IsLoading = false;
            await Task.Delay(500);
            incrementalLoadingTimeout = false;
        }

        public async Task LoadNextMessagesAsync() {
            if (Messages.Count == 0 || IsLoading || incrementalLoadingTimeout) return;
            int count = MessagesLoadCount;
            IsLoading = true;
            incrementalLoadingTimeout = true;
            object resp = await Execute.GetHistory(ConversationId, Messages.Last().ConversationMessageId, -count, count, true);
            if (resp is MessagesHistoryResponseEx mhr) {
                SetUpMembers(mhr);
                SetUpViewModel(mhr.Conversation, mhr.OnlineInfo);
                IsMessagesFromGroupAllowed = mhr.IsMessagesAllowed;
                mhr.Messages?.Reverse();
                if (mhr.Messages != null) foreach (Message rmsg in mhr.Messages) {
                        LMessage msg = new LMessage(rmsg);
                        FixState(msg);
                        Messages.Insert(msg);
                    }
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
            IsLoading = false;
            await Task.Delay(500);
            incrementalLoadingTimeout = false;
        }

        private void SetUpMembers(MessagesHistoryResponseEx mhr) {
            if (mhr.Members != null) Members = mhr.Members;
            AppSession.AddUsersToCache(mhr.Profiles);
            AppSession.AddGroupsToCache(mhr.Groups);
            AppSession.AddContactsToCache(mhr.Contacts);
            if (ConversationId.IsChat()) {
                if (mhr.Profiles != null) MemberUsers = mhr.Profiles;
                if (mhr.Groups != null) MemberGroups = mhr.Groups;
            }
        }

        private void FixState(LMessage msg) {
            long senderId = AppParameters.UserID;
            bool isOutgoing = msg.SenderId == senderId;
            if (isOutgoing) {
                msg.UISentMessageState = msg.ConversationMessageId > OutRead ? SentMessageState.Unread : SentMessageState.Read;
            } else {
                msg.UISentMessageState = msg.ConversationMessageId > InRead ? SentMessageState.Unread : SentMessageState.Read;
            }
        }

        #endregion

        private void SetUpViewModel(Conversation c, UserOnlineInfoEx onlineInfo = null) {
            _type = c.Peer.Type;
            try {
                if (c.Peer.Type == PeerType.Chat) {
                    IsWritingDisabledForAll = c.ChatSettings != null ? c.ChatSettings.WritingDisabled : null;
                    IsDisappearing = c.ChatSettings != null ? c.ChatSettings.IsDisappearing : false;
                    if (MessageFormViewModel != null) MessageFormViewModel.isCasperChat = IsDisappearing;
                    IsDonut = c.ChatSettings != null ? c.ChatSettings.IsDonut : false;
                    MembersCount = c.ChatSettings != null ? c.ChatSettings.MembersCount : 0;
                    Title = c.ChatSettings != null ? c.ChatSettings.Title : "Untitled chat";
                    Photo = new Uri(c.ChatSettings?.Photo?.Medium != null ? c.ChatSettings.Photo.Medium : "https://vk.ru/images/icons/im_multichat_200.png");
                    if (c.ChatSettings?.PinnedMessage != null) PinnedMessage = new LMessage(c.ChatSettings.PinnedMessage);
                } else if (c.Peer.Type == PeerType.User) {
                    if (c.Peer.Id == AppParameters.UserID) {
                        Title = Locale.Get("favorites_myself");
                        Photo = new Uri("https://vk.ru/images/icons/im_favorites_200.png");
                    } else {
                        var u = AppSession.GetCachedUser(c.Peer.Id);
                        Title = u != null ? u.FullName : "Untitled user";
                        Photo = u != null && u.HasPhoto ? u.Photo : null;
                        IsVerified = (u != null && u.Verified == 1);
                        if (u != null && u.OnlineInfo != null) {
                            int platform = u.OnlineInfo.IsMobile ? 1 : VKClientsHelper.GetLPAppIdByAppId(u.OnlineInfo.AppId);
                            Online = u.OnlineInfo.isOnline ? platform : 0;
                            // Online = u.OnlineInfo.isOnline ? VKClientsHelper.GetLPAppIdByAppId(u.OnlineInfo.AppId) : 0;
                        }
                    }
                } else if (c.Peer.Type == PeerType.Group) {
                    var g = AppSession.GetCachedGroup(-c.Peer.Id);
                    Title = g != null ? g.Name : "Untitled group";
                    Photo = g != null && g.HasPhoto ? g.Photo : null;
                    IsVerified = g != null && g.Verified == 1;
                } else if (c.Peer.Type == PeerType.Contact) {
                    var t = AppSession.GetCachedContact(c.Peer.LocalId);
                    Title = t != null ? t.Name : "Untitled contact";
                    Photo = t != null && t.Photo != null ? t.Photo : new Uri("https://vk.ru/images/camera_200.png");
                }

                IsMuted = c.PushSettings != null && c.PushSettings.DisabledForever ? true : false;
                SortId = c.SortId;
                ChatSettings = c.ChatSettings;
                CanWrite = c.CanWrite;
                InRead = c.InRead;
                OutRead = c.OutRead;
                if (c.FolderIds != null) FolderIds = c.FolderIds;
                if (c.CurrentKeyboard != null && c.CurrentKeyboard.Buttons.Count > 0) CurrentKeyboard = c.CurrentKeyboard;
                if (c.Mentions != null) Mentions = new ObservableCollection<int>(c.Mentions);
                if (c.UnreadReactions != null) UnreadReactions = new ObservableCollection<int>(c.UnreadReactions);
                UnreadMessagesCount = c.UnreadCount;
                IsMarkedUnread = c.IsMarkedUnread;
                IsArchived = c.IsArchived;

                // Message form
                MessageFormViewModel.ReplyLastMessageRequested = ReplyLastMessage;
                MessageFormViewModel.EditLastMessageRequested = EditLastUserMessage;

                // Style (условие необходимо для того, чтобы фон не мерцал после загрузки)
                if (Style != c.Style) Style = c.Style;

                // Subtitle
                switch (Type) {
                    case PeerType.Chat:
                        Subtitle = MembersCount > 0 ? $"{MembersCount} {Locale.GetDeclension(MembersCount, "members")}" : "";
                        break;
                    case PeerType.User:
                        if (_id == AppParameters.UserID) {
                            Subtitle = Locale.Get("saved_messages");
                        } else {
                            User u = AppSession.GetCachedUser(_id);
                            Subtitle = VKClientsHelper.GetOnlineInfoString(u.OnlineInfo, u.Sex, onlineInfo?.AppName);
                            IsEmptyDialogStubVisible = u.CanWritePrivateMessage == 1;
                            string suffix = u.Sex == Sex.Female ? "f" : "m";
                            EmptyDialogStubText = Locale.Get($"empty_dialog_stub_text_{suffix}");
                            CanAddToFriend = u.CanSendFriendRequest == 1 && (u.FriendStatus == FriendStatus.None || u.FriendStatus == FriendStatus.InboundRequest);
                            AddFriendCommand = new RelayCommand(async o => await AddFriendAsync(u.Id));
                            ViewProfileCommand = new RelayCommand(o => VKLinks.ShowPeerInfoModal(u.Id));
                        }
                        break;
                    case PeerType.Group: Subtitle = Locale.Get("community"); break;
                    case PeerType.Contact: Subtitle = Locale.Get("contact"); break;
                }

                UpdateMessageSendRestrictionState();
                MessageFormViewModel.Keyboard = CurrentKeyboard;

                // Commands
                if (!isEventsCommandsReady) {
                    SetUpCommands();
                    RegisterLongpollEvents();
                }

                IsReady = true;
            } catch (Exception ex) {
                Log.Error($"Error in SetupViewModel, 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
        }

        private async Task GetLastMessageFromAPIAsync() {
            object resp = await Execute.GetHistory(ConversationId, -1, 0, 1, true);
            if (resp is MessagesHistoryResponseEx mhr) {
                if (mhr.Messages != null && mhr.Messages.Count > 0) {
                    LMessage msg = new LMessage(mhr.Messages[0]);
                    FixState(msg);
                    ReceivedMessages.Add(msg);
                    OnPropertyChanged(nameof(LastMessage));
                }
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        private void ConversationViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(Mentions):
                    if (Mentions != null) {
                        HasMention = Mentions.Count > 0;
                        MentionIcon = VKUILibrary.GetIconTemplate(Mentions.Count > 0 ? VK.VKUI.Controls.VKIconName.Icon28MentionOutline : VK.VKUI.Controls.VKIconName.Icon28BombOutline);
                        Mentions.CollectionChanged += (c, d) => {
                            OnPropertyChanged(nameof(Mentions));
                        };
                    } else {
                        HasMention = false;
                        MentionIcon = null;
                    }
                    break;
                case nameof(IsLastMessageOutgoing):
                case nameof(UnreadMessagesCount):
                case nameof(HasUnreadMessageFromCurrentUser):
                    OnPropertyChanged(nameof(UICanDisplayReadIndicator));
                    break;
                case nameof(SortId):
                    OnPropertyChanged(nameof(IsPinned));
                    OnPropertyChanged(nameof(UICanDisplayReadIndicator));
                    OnPropertyChanged(nameof(HasUnreadMessageFromCurrentUser));
                    break;
                case nameof(LastMessage):
                    IsLastMessageOutgoing = LastMessage?.SenderId == AppParameters.UserID;
                    break;
                case nameof(UnreadReactions):
                    OnPropertyChanged(nameof(UnreadReactionsCount));
                    break;
            }
        }

        private async Task AddFriendAsync(long userId) {
            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            object r = await ssp.ShowAsync(VkAPI.Methods.Friends.Add(userId));
            if (r is string response) {
                CanAddToFriend = false;
            } else {
                Functions.ShowHandledErrorDialog(r);
            }
        }

        private void SetUpCommands() {
            _titlebarCommand = new RelayCommand(ShowConvInfo);
            _contextMenuCommand = new RelayCommand(ShowContextMenu);
            _pinnedMessageCommand = new RelayCommand(GetToPinnedMessage);
            _buttonDownCommand = new RelayCommand(GetToLast);
            _goToMentionCommand = new RelayCommand(GetToMentionedMessage);
            _bottomButtonCommand = new RelayCommand(ChangeSilenceMode);
            _getToUnreadReactedMessageCommand = new RelayCommand(GetToUnreadReactedMessage);
            isEventsCommandsReady = true;
        }

        private void RegisterLongpollEvents() {
            VKQueue.Online += VKQueue_Online;
            LongPoll.MessageReceived += AddMessage;
            LongPoll.DefaultKeyboardReceived += SetKeyboard;
            LongPoll.MessageEdited += ReplaceMessage;
            LongPoll.FlagsEvent += LongPoll_FlagsEvent;
            LongPoll.UserActivityStateChanged += UserActivityStateChanged;
            LongPoll.MessageMarkedAsRead += LongPoll_MessageMarkedAsRead;
            LongPoll.BotCallbackReceived += LongPoll_BotCallbackReceived;
            LongPoll.Event10Received += LongPoll_Event10Received;
            LongPoll.Event12Received += LongPoll_Event12Received;
            LongPoll.ConversationMentionReceived += LongPoll_ConversationMentionReceived;
            LongPoll.ConversationsMajorIdChanged += LongPoll_ConversationsMajorIdChanged;
            LongPoll.ConversationsMinorIdChanged += LongPoll_ConversationsMinorIdChanged;
            LongPoll.FolderConversationsAdded += LongPoll_FolderConversationsAdded;
            LongPoll.FolderConversationsRemoved += LongPoll_FolderConversationsRemoved;
            LongPoll.ReactionsChanged += LongPoll_ReactionsChanged;
            LongPoll.UnreadReactionsChanged += LongPoll_UnreadReactionsChanged;
            LongPoll.ConvMemberRestrictionChanged += LongPoll_ConvMemberRestrictionChanged;
            LongPoll.ConversationAccessRightsChanged += LongPoll_ConversationAccessRightsChanged;
            ActivityStatusUsers.Elapsed += (a, b) => UpdateActivityStatus();
        }

        private void UpdateMessageSendRestrictionState() {
            if (ChatSettings != null && ChatSettings.IsGroupChannel) {
                MessageSendRestriction = MessageSendRestriction.Channel;
            } else {
                if (CanWrite != null && CanWrite.Allowed) {
                    MessageSendRestriction = MessageSendRestriction.None;
                } else {
                    MessageSendRestriction = MessageSendRestriction.Banned;
                }
            }

            if (ChatSettings != null && ChatSettings.State == UserStateInChat.Kicked) {
                RestrictionReason = Locale.Get("readonlyconv_kicked");
            } else if (CanWrite != null && !CanWrite.Allowed) {
                if (CanWrite.Reason == 983) {
                    var date = DateTimeOffset.FromUnixTimeSeconds(CanWrite.Until).DateTime;
                    RestrictionReason = CanWrite.Until > 0 ? String.Format(Locale.GetForFormat("writing_disabled_for_you_until"), APIHelper.GetNormalizedTime(date, true)) : Locale.Get("writing_disabled_for_you");
                    //} else if (CanWrite.Reason == 1012) {
                    //    var date = DateTimeOffset.FromUnixTimeSeconds(CanWrite.Until).DateTime;
                    //    RestrictionReason = CanWrite.Until > 0 ? String.Format(Locale.GetForFormat("writing_disabled_for_all_until"), APIHelper.GetNormalizedTime(date, true)) : Locale.Get("writing_disabled_for_all");
                } else if (CanWrite.Reason == 1012 && IsWritingDisabledForAll != null) {
                    var date = DateTimeOffset.FromUnixTimeSeconds(IsWritingDisabledForAll.UntilTS).DateTime;
                    RestrictionReason = IsWritingDisabledForAll.UntilTS > 0 ? String.Format(Locale.GetForFormat("writing_disabled_for_all_until"), APIHelper.GetNormalizedTime(date, true)) : Locale.Get("writing_disabled_for_all");
                } else {
                    RestrictionReason = Locale.Get("readonlyconv_overall");
                }
            }

            if (MessageSendRestriction == MessageSendRestriction.Channel) {
                BottomButtonContent = !IsMuted ? Locale.Get("conv_disable_notifications").ToUpper() : Locale.Get("conv_enable_notifications").ToUpper();
            }

            IsBottomButtonShow = MessageSendRestriction == MessageSendRestriction.Channel;
        }

        #region Commands

        private void ShowConvInfo(object obj) {
            if (ViewManagement.GetWindowType() == WindowType.Hosted) return;
            if (Type != PeerType.Email) VKLinks.ShowPeerInfoModal(ConversationId);
        }

        private void ShowContextMenu(object obj) {
            FrameworkElement a = obj as FrameworkElement;

            MenuFlyout mf = new MenuFlyout();
            mf.Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom;

            if (Type == PeerType.User && ContactsPanel.IsContactPanelSupported) {
                new System.Action(async () => {
                    bool isPinned = await ContactsPanel.IsPinned(_id);

                    MenuFlyoutItem contactPanelItem = new MenuFlyoutItem {
                        Icon = new FixedFontIcon { Glyph = isPinned ? "" : "" },
                        Text = isPinned ? Locale.Get("contactpanel_unpin") : Locale.Get("contactpanel_pin")
                    };
                    contactPanelItem.Click += contactPanelItem_Click;
                    mf.Items.Add(contactPanelItem);
                })();
            }

            if (mf.Items.Count > 0) mf.Items.Add(new MenuFlyoutSeparator());

            MenuFlyoutItem refresh = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("refresh") };
            MenuFlyoutItem search = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("msgsearch") };
            MenuFlyoutItem gotofirst = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("conv_goto_first") };
            MenuFlyoutItem stats = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("message_stats") };
            MenuFlyoutItem rightsDebug = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = "Test checkChatRights response" };

            refresh.Click += async (b, c) => await LoadMessagesAsync();
            search.Click += (b, c) => OpenMessagesSearchModal();
            gotofirst.Click += async (b, c) => await LoadMessagesAsync(0, -AppParameters.MessagesLoadCount, AppParameters.MessagesLoadCount);
            stats.Click += async (b, c) => await Windows.System.Launcher.LaunchUriAsync(new Uri($"lny://stats?peerId={ConversationId}"));
            rightsDebug.Click += async (b, c) => {
                var resp = await Execute.CheckChatRights(ConversationId);
                if (resp is CheckChatRightsResponse wdix) {
                    string acl = wdix.ACL != null ? $"ACL: Ok.\n" : $"ACL: NULL!!!\n";
                    string perms = wdix.Permissions != null ? $"Permissions: Ok.\n" : $"Permissions: NULL!\n";
                    string canWrite = wdix.CanWrite != null ? $"Can write: {wdix.CanWrite.Allowed}, reason: {wdix.CanWrite.Reason}\n" : $"Can write: NULL!!\n";
                    string writing = wdix.WritingDisabled != null && wdix.WritingDisabled.Value ? $"Writing disabled until {wdix.WritingDisabled.UntilTS}." : $"Writing enabled.";
                    await new ContentDialog() { Title = "Response", Content = $"{acl}{perms}{canWrite}{writing}", PrimaryButtonText = "OK" }.ShowAsync();
                } else {
                    Functions.ShowHandledErrorTip(resp);
                }
            };

            bool isCasper = ChatSettings != null && ChatSettings.IsDisappearing;
            bool isNotEmpty = Messages?.Count > 0; // пока что чекаем не кол-во сообщений в чате, а кол-во отображаемых.

            mf.Items.Add(refresh);
            // mf.Items.Add(search);
            mf.Items.Add(gotofirst);
            if (AppParameters.MessagesStatsFeature && !isCasper && isNotEmpty) mf.Items.Add(stats);
            if (AppParameters.ShowDebugItemsInMenu) mf.Items.Add(rightsDebug);

            mf.ShowAt(a);
        }

        private void GetToPinnedMessage(object obj) {
            GoToMessage(PinnedMessage);
        }

        private void GetToLast(object obj) {
            var msg = ReceivedMessages.LastOrDefault();
            if (msg != null) GoToMessage(msg.ConversationMessageId);
        }

        private void GetToMentionedMessage(object obj) {
            if (Mentions?.Count > 0) {
                var ids = Mentions.ToList();
                ids.Sort();
                GoToMessage(ids.First());
            }
        }
        private void ChangeSilenceMode(object obj) {
            new System.Action(async () => { await ChangeSilenceModeAsync(); })();
        }

        private void GetToUnreadReactedMessage(object obj) {
            if (UnreadReactions?.Count > 0) GoToMessage(UnreadReactions.LastOrDefault());
        }

        private void ReplyLastMessage() {
            if (Messages.Count > 0) {
                MessageFormViewModel.AddReplyMessage(Messages.Last());
            }
        }

        private void EditLastUserMessage() {
            if (Messages.Count > 0) {
                LMessage yourlast = null;
                foreach (LMessage msg in Messages) {
                    if (msg.CanEditMessage(PinnedMessage)) yourlast = msg;
                }
                if (yourlast != null) MessageFormViewModel.StartEditing(yourlast);
            }
        }

        #endregion

        #region Longpoll and queue events

        private void VKQueue_Online(object sender, OnlineQueueEvent e) {
            if (Type == PeerType.User && e.UserId == ConversationId) {
                User u = AppSession.GetCachedUser(_id);
                if (u == null) return;
                Subtitle = VKClientsHelper.GetOnlineInfoString(e, u.Sex);
                Online = e.Online ? e.Platform : 0;
            }
        }

        private void AddMessage(Message msg) {
            new System.Action(async () => {
                try {
                    if (msg != null && msg.PeerId == ConversationId) {
                        bool insert = false;

                        LMessage message = new LMessage(msg);

                        if (Messages.Count == 0) {
                            insert = true;
                        } else if (Messages.LastOrDefault()?.ConversationMessageId == LastMessage.ConversationMessageId) {
                            var z = from u in Messages where u.ConversationMessageId == message.ConversationMessageId select u;
                            if (z.Count() == 0) {
                                insert = true;
                            }
                        }

                        TryAddLastMessageToReceived(message);

                        if (insert) {
                            IsEmptyDialog = false;
                            if (message.SenderId != AppParameters.UserID) UnreadMessagesCount++;
                            if (!msg.IsPartial) message.UISentMessageState = SentMessageState.Unread;
                            if (isFirstTimeOpened) Messages.Insert(message);
                            ScrollToMessageCallback?.Invoke(message.ConversationMessageId, true, true);
                        }

                        try {
                            var act = ActivityStatusUsers.RegisteredObjects.Where(a => a.MemberId == message.SenderId).FirstOrDefault();
                            if (act != null) ActivityStatusUsers.Remove(act);
                            UpdateActivityStatus();
                        } catch (Exception ex) {
                            UsersActivityInfo = string.Empty;
                            string count = ActivityStatusUsers.RegisteredObjects == null ? "null" : ActivityStatusUsers.RegisteredObjects.Count.ToString();
                            Log.Error($"Exception while trying to remove sender from ActivityStatusUsers (0x{ex.HResult.ToString("x8")}), current au count: {count}");
                        }

                        if (message.Action != null) {
                            switch (message.Action.Type) {
                                case "chat_kick_user":
                                    if (message.Action.MemberId == AppParameters.UserID) {
                                        ChatSettings.State = message.FromId == AppParameters.UserID ?
                                            UserStateInChat.Left : UserStateInChat.Kicked;
                                    }
                                    break;
                                case "chat_invite_user":
                                    if (message.FromId == message.Action.MemberId && message.FromId == AppParameters.UserID)
                                        ChatSettings.State = UserStateInChat.In;
                                    break;
                                case "chat_title_update":
                                    Title = message.Action.Text;
                                    break;
                                case "chat_photo_update":
                                    Photo = message.Attachments[0].Photo.PreviewImageUri;
                                    break;
                                case "chat_photo_remove":
                                    Photo = new Uri("https://vk.ru/images/icons/im_multichat_200.png");
                                    break;
                                case "chat_pin_message":
                                    await UpdatePinnedMessageAsync(message.Action.ConversationMessageId);
                                    break;
                                case "conversation_style_update":
                                    Style = message.Action.Style;
                                    break;
                                case "custom":
                                    if (!string.IsNullOrEmpty(Style)) Style = message.Action.Style;
                                    break;
                            }
                            OnPropertyChanged(nameof(Conversation));
                        }
                    }
                } catch (Exception ex) {
                    string m = msg != null ? msg.Id.ToString() : "null";
                    Log.Error($"Error while adding a new message! HR: 0x{ex.HResult.ToString("x8")}; M: {m}; C: {_id}");
                }
            })();

        }

        private void SetKeyboard(BotKeyboard keyboard, long peerId) {
            if (peerId != ConversationId) return;
            if (keyboard == null || keyboard.Inline) return;
            if (keyboard.Buttons == null) keyboard = null;
            CurrentKeyboard = keyboard;
            MessageFormViewModel.Keyboard = CurrentKeyboard;
        }

        private void ReplaceMessage(Message message) {
            if (message.PeerId != _id) return;
            ReplaceMessage(message, Messages);
            ReplaceMessage(message, ReceivedMessages);
            if (ReceivedMessages.Count > 0 && LastMessage?.ConversationMessageId == message.ConversationMessageId) {
                LastMessage.Edit(message);
                OnPropertyChanged(nameof(LastMessage));
            }

            if (message.Action != null && message.Action.Type == "chat_photo_update") {
                Photo = message.Attachments[0].Photo.PreviewImageUri;
            }

            if (PinnedMessage != null && PinnedMessage.ConversationMessageId == message.ConversationMessageId) PinnedMessage = new LMessage(message);
        }

        private void ReplaceMessage(Message message, ObservableCollection<LMessage> messages) {
            if (messages.Count > 0) {
                var fmsg = (from b in messages where b.ConversationMessageId == message.ConversationMessageId select b).FirstOrDefault();
                if (fmsg != null) {
                    fmsg.Edit(message);
                    if (LastMessage.UISentMessageState != SentMessageState.Deleted) FixState(fmsg);
                }
            }
        }

        private void LongPoll_FlagsEvent(LPMessageFlagState state, int msgid, long peerId, int flags) {
            if (ConversationId != peerId) return;
            new System.Action(async () => {
                if (state == LPMessageFlagState.Set) { // Установка
                    if (Functions.CheckFlag(flags, 128)) { // Сообщение удалено
                        var y = (from z in Messages where z.ConversationMessageId == msgid select z).FirstOrDefault();
                        var r = (from z in ReceivedMessages where z.ConversationMessageId == msgid select z).FirstOrDefault();

                        if (r != null) {
                            ReceivedMessages.Remove(r);
                            if (ReceivedMessages.Count == 0) {
                                Log.Warn("Received messages is empty! Getting last message from API...");
                                await GetLastMessageFromAPIAsync();
                            } else {
                                OnPropertyChanged(nameof(LastMessage));
                            }
                        }

                        if (y != null) {
                            if (AppParameters.KeepDeletedMessagesInUI) {
                                y.UISentMessageState = SentMessageState.Deleted;
                            } else {
                                Messages.Remove(y);
                            }
                        }
                    }
                }
                if (state == LPMessageFlagState.Reset) { // Сброс
                    if (Functions.CheckFlag(flags, 128) || // Восстановление сообщения
                        Functions.CheckFlag(flags, 64) || // Отмена пометки сообщения как спам
                        Functions.CheckFlag(flags, 32768)) { // Отмена пометки сообщения как спам
                        object resp = await VkAPI.Methods.Messages.GetByConversationMessageId(peerId, new List<int> { msgid });
                        if (resp is MessagesHistoryResponse r && r.Items.Count > 0) {
                            AppSession.AddUsersToCache(r.Profiles);
                            AppSession.AddGroupsToCache(r.Groups);
                            AppSession.AddContactsToCache(r.Contacts);

                            LMessage msg = new LMessage(r.Items[0]);
                            FixState(msg);
                            Messages.Insert(msg);
                            OnPropertyChanged(nameof(LastMessage));
                        } else {
                            Functions.ShowHandledErrorTip(resp);
                        }
                    }
                }
            })();
        }

        Elapser<LongPollActivityInfo> ActivityStatusUsers = new Elapser<LongPollActivityInfo>();

        private void UserActivityStateChanged(Tuple<long, List<LongPollActivityInfo>> e) {
            if (ConversationId != e.Item1) return;
            double timeout = 7000;
            try {
                foreach (LongPollActivityInfo info in e.Item2) {
                    if (info.MemberId == AppParameters.UserID) continue;
                    var exist = ActivityStatusUsers.RegisteredObjects.Where(u => u.MemberId == info.MemberId).FirstOrDefault();
                    if (exist != null) ActivityStatusUsers.Remove(exist);
                    ActivityStatusUsers.Add(info, timeout);
                }
                UpdateActivityStatus();
            } catch (Exception ex) {
                ActivityStatusUsers.Clear();
                UsersActivityInfo = string.Empty;
                Log.Error($"Error while parsing user activity status, 0x{ex.HResult.ToString("x8")}");
            }
        }

        private void UpdateActivityStatus() {
            try {
                var acts = ActivityStatusUsers.RegisteredObjects;
                int count = acts.Count();

                Debug.WriteLine($"UpdateActivityStatus: {String.Join(";", acts)}");

                if (count == 0) {
                    UsersActivityInfo = string.Empty;
                    return;
                }

                if (Type != PeerType.Chat) {
                    if (count == 1) {
                        UsersActivityInfo = GetLocalizedActivityStatus(acts.FirstOrDefault().Status, 1);
                    }
                } else {
                    var typing = acts.Where(a => a?.Status == LongPollActivityStatus.Typing).ToList();
                    var voice = acts.Where(a => a?.Status == LongPollActivityStatus.RecordingVoiceMessage).ToList();
                    var photo = acts.Where(a => a?.Status == LongPollActivityStatus.SendingPhoto).ToList();
                    var video = acts.Where(a => a?.Status == LongPollActivityStatus.SendingVideo).ToList();
                    var file = acts.Where(a => a?.Status == LongPollActivityStatus.SendingFile).ToList();
                    List<List<LongPollActivityInfo>> groupedActivities = new List<List<LongPollActivityInfo>> {
                        typing, voice, photo, video, file
                    };

                    bool has3AndMoreDifferentTypes = groupedActivities.Where(a => a.Count > 0).Count() >= 3;

                    string status = string.Empty;
                    foreach (var act in groupedActivities) {
                        if (act.Count == 0) continue;
                        var type = act[0].Status;
                        string actstr = GetLocalizedActivityStatus(type, act.Count);

                        if (has3AndMoreDifferentTypes) {
                            if (status.Length != 0) status += ", ";
                            status += $"{act.Count} {actstr}";
                        } else {
                            if (status.Length != 0) status += ", ";
                            List<long> ids = act.Select(s => s.MemberId).ToList();
                            status += $"{GetNamesForActivityStatus(ids, act.Count, act.Count == 1)} {actstr}";
                        }
                    }

                    status = status.Trim();
                    UsersActivityInfo = !string.IsNullOrWhiteSpace(status) ? status + "..." : null;
                }
            } catch (Exception ex) {
                UsersActivityInfo = null;
                string count = ActivityStatusUsers.RegisteredObjects == null ? "null" : ActivityStatusUsers.RegisteredObjects.Count.ToString();
                Log.Error($"Exception in UpdateActivityStatus (0x{ex.HResult.ToString("x8")}), current au count: {count}");
            }
        }

        private string GetLocalizedActivityStatus(LongPollActivityStatus status, int count) {
            string suffix = count == 1 ? "_single" : "_multi";
            switch (status) {
                case LongPollActivityStatus.Typing: return Locale.Get($"lpact_typing{suffix}");
                case LongPollActivityStatus.RecordingVoiceMessage: return Locale.Get($"lpact_voice{suffix}");
                case LongPollActivityStatus.SendingPhoto: return Locale.Get($"lpact_photo{suffix}");
                case LongPollActivityStatus.SendingVideo: return Locale.Get($"lpact_video{suffix}");
                case LongPollActivityStatus.SendingFile: return Locale.Get($"lpact_file{suffix}");
            }
            return string.Empty;
        }

        private string GetNamesForActivityStatus(IReadOnlyList<long> ids, int count, bool showFullLastName) {
            string r = string.Empty;
            foreach (long id in ids) {
                if (id.IsUser()) {
                    User u = AppSession.GetCachedUser(id);
                    if (u != null) {
                        string lastName = showFullLastName ? u.LastName : u.LastName[0] + ".";
                        r = $"{u.FirstName} {lastName}";
                    }
                } else if (id.IsGroup()) {
                    var g = AppSession.GetCachedGroup(id);
                    if (g != null) {
                        r = $"\"{g.Name}\"";
                    }
                }
            }
            if (!string.IsNullOrEmpty(r)) {
                if (count > 1) {
                    r += $" {String.Format(Locale.GetForFormat("im_status_more"), count - 1)}";
                }
            }
            return r;
        }

        private void LongPoll_MessageMarkedAsRead(long peerId, int messageId, int unreadCount, bool isOutgoing) {
            if (ConversationId != peerId) return;

            if (isOutgoing) {
                OutRead = messageId;
            } else {
                InRead = messageId;
            }

            if (unreadCount == -1) {
                if (UnreadMessagesCount > 0) UnreadMessagesCount--;
            } else {
                UnreadMessagesCount = unreadCount >= 0 ? unreadCount : 0;
            }
            foreach (LMessage msg in Messages) {
                if (msg.ConversationMessageId <= messageId && msg.UISentMessageState != SentMessageState.Deleted) {
                    msg.UISentMessageState = SentMessageState.Read;
                }
            }
            foreach (LMessage msg in ReceivedMessages) {
                if (msg.ConversationMessageId <= messageId && msg.UISentMessageState != SentMessageState.Deleted) {
                    msg.UISentMessageState = SentMessageState.Read;
                }
            }

            if (Mentions != null) {
                if (Mentions.Count > 0) {
                    var mentions = Mentions.ToList();
                    foreach (int id in mentions) {
                        if (id <= messageId) Mentions.Remove(id);
                    }
                    if (Mentions.Count == 0) Mentions = null;
                }
            }
        }

        private void LongPoll_BotCallbackReceived(LPBotCallback callback) {
            if (callback.PeerId == _id && callback.Action != null) {
                VKButtonHelper.FireCallbackActionReceived(callback.EventId);
                new System.Action(async () => {
                    switch (callback.Action.Type) {
                        case LPBotCallbackActionType.ShowSnackbar:
                            Group g = AppSession.GetCachedGroup(callback.OwnerId);
                            string name = g != null ? g.Name : string.Empty;
                            ShowSnackbarRequested?.Invoke(g.Photo, callback.Action.Text);
                            break;
                        case LPBotCallbackActionType.OpenLink:
                            await Windows.System.Launcher.LaunchUriAsync(new Uri(callback.Action.Link));
                            break;
                        case LPBotCallbackActionType.OpenApp:
                            string oid = callback.Action.OwnerId.IsUser() ? $"_{callback.Action.OwnerId}" : string.Empty;
                            string hash = string.IsNullOrEmpty(callback.Action.Hash) ? $"#{callback.Action.Hash}" : string.Empty;
                            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://m.vk.ru/app{callback.Action.AppId}{oid}{hash}"));
                            break;
                    }
                })();
            }
        }

        private async Task UpdatePinnedMessageAsync(int cmId) {
            var y = (from z in Messages where z.ConversationMessageId == cmId select z).ToList();
            if (y.Count > 1) {
                PinnedMessage = y[0];
            } else {
                object resp = await VkAPI.Methods.Messages.GetByConversationMessageId(ConversationId, new List<int> { cmId });
                if (resp is MessagesHistoryResponse mhr) {
                    PinnedMessage = new LMessage(mhr.Items[0]);
                } else {
                    Functions.ShowHandledErrorTip(resp);
                }
            }
        }


        private void LongPoll_Event10Received(long peerId, int flags) {
            if (peerId != ConversationId) return;
            if (Functions.CheckFlag(flags, 16)) IsMuted = false;
            if (Functions.CheckFlag(flags, 16384)) {
                Mentions = null;
            }
            if (Functions.CheckFlag(flags, 1048576)) IsMarkedUnread = false;
            if (Functions.CheckFlag(flags, 8388608)) IsArchived = false;
        }

        private void LongPoll_Event12Received(long peerId, int flags) {
            if (peerId != ConversationId) return;
            if (Functions.CheckFlag(flags, 16)) IsMuted = true;
            if (Functions.CheckFlag(flags, 1048576)) IsMarkedUnread = true;
            if (Functions.CheckFlag(flags, 8388608)) IsArchived = true;
        }

        private void LongPoll_ConversationMentionReceived(long peerId, int messageId, bool isBomb) {
            if (peerId != ConversationId) return;
            ObservableCollection<int> mentions = new ObservableCollection<int>();
            if (!isBomb) mentions.Add(messageId);
            Mentions = mentions;
        }

        private void LongPoll_ConversationsMajorIdChanged(Dictionary<long, int> list) {
            foreach (var i in list) {
                if (ConversationId != i.Key) continue;
                SortId.MajorId = i.Value;
                OnPropertyChanged(nameof(SortId));
            }
        }

        private void LongPoll_ConversationsMinorIdChanged(Dictionary<long, int> list) {
            foreach (var i in list) {
                if (ConversationId != i.Key) continue;
                SortId.MinorId = i.Value;
                OnPropertyChanged(nameof(SortId));
            }
        }

        private void LongPoll_FolderConversationsAdded(int id, List<long> conversationsIds) {
            if (conversationsIds.Contains(ConversationId)) {
                if (!FolderIds.Contains(id)) FolderIds.Add(id);
            }
        }

        private void LongPoll_FolderConversationsRemoved(int id, List<long> conversationsIds) {
            if (conversationsIds.Contains(ConversationId)) {
                if (FolderIds.Contains(id)) FolderIds.Remove(id);
            }
        }

        private void LongPoll_ReactionsChanged(long peerId, int cmId, ReactionEventType type, int myReactionId, List<Reaction> reactions) {
            if (peerId != ConversationId) return;

            LMessage msg = Messages.Where(m => m.ConversationMessageId == cmId).FirstOrDefault();
            if (msg != null) SetReactions(msg, type, myReactionId, reactions);

            LMessage rmsg = ReceivedMessages.Where(m => m.ConversationMessageId == cmId).FirstOrDefault();
            if (rmsg != null) SetReactions(rmsg, type, myReactionId, reactions);
        }

        private void SetReactions(LMessage msg, ReactionEventType type, int myReactionId, List<Reaction> reactions) {
            bool reRenderRequired = false;

            if (reactions.Count > 0) {
                reRenderRequired = msg.Reactions.Count == 0;

                if (msg.Reactions.Count > 0) {
                    List<Reaction> toRemove = new List<Reaction>();
                    foreach (var mr in msg.Reactions) {
                        Reaction reaction = reactions.Where(r => r.Id == mr.Id).FirstOrDefault();
                        if (reaction == null) toRemove.Add(mr);
                    }
                    foreach (var mr in toRemove) {
                        msg.Reactions.Remove(mr);
                    }
                }

                foreach (var reaction in reactions) {
                    Reaction mr = msg.Reactions.Where(r => r.Id == reaction.Id).FirstOrDefault();
                    if (mr != null) {
                        mr.Count = reaction.Count;
                        mr.Members = reaction.Members;
                    } else {
                        msg.Reactions.Add(reaction);
                    }
                }

                msg.Reactions.Sort(r => {
                    var rr = reactions.Where(rrr => rrr.Id == r.Id).FirstOrDefault();
                    return reactions.IndexOf(rr);
                });
            } else {
                msg.Reactions.Clear();
                reRenderRequired = true;
            }

            if (type == ReactionEventType.IAdded || type == ReactionEventType.IRemoved) {
                msg.SelectedReactionId = myReactionId;
            }

            if (reRenderRequired) msg.NotifyReRender();
        }

        private void LongPoll_UnreadReactionsChanged(long peerId, List<int> cmIds) {
            if (peerId != ConversationId) return;
            UnreadReactions = new ObservableCollection<int>(cmIds);
            if (UnreadReactionsCount == 0) UnreadReactions = null;
        }

        private void LongPoll_ConvMemberRestrictionChanged(long peerId, long memberId, bool deny, int duration) {
            if (peerId != ConversationId || memberId != AppParameters.UserID) return;
            CanWrite.Allowed = !deny;
            if (deny) {
                CanWrite.Reason = 983;
                if (duration > 0) CanWrite.Until = DateTimeOffset.Now.ToUnixTimeSeconds() + duration;
            } else {
                CanWrite.Reason = 0;
                CanWrite.Until = 0;
            }
            UpdateMessageSendRestrictionState();
        }

        bool isProcessingEvent52 = false;
        private void LongPoll_ConversationAccessRightsChanged(long peerId, long mask) {
            // Смена прав в беседе.
            // Это также признак того, что поменялся возможность всем писать в чат

            if (peerId != ConversationId || isProcessingEvent52) return;
            isProcessingEvent52 = true;
            Debug.WriteLine($"Updating chat rights for peer {ConversationId}...");

            new System.Action(async () => {
                await Task.Delay(500); // нужно, ибо выяснилось, что объекты acl и permissions в chat_settings отсутствуют сразу после смены прав.
                var resp = await Execute.CheckChatRights(ConversationId);
                if (resp is CheckChatRightsResponse wdix) {
                    if (wdix.ACL == null) {
                        Log.Error($"execute.checkChatRights returns an strange response!");
                        return;
                    }
                    try {
                        IsWritingDisabledForAll = wdix.WritingDisabled;
                        CanWrite = wdix.CanWrite;
                        ChatSettings.ACL = wdix.ACL;
                        if (wdix.Permissions != null) ChatSettings.Permissions = wdix.Permissions;
                        Debug.WriteLine($"Chat rights for peer {ConversationId} successfully updated!");
                    } catch (Exception ex) {
                        Log.Error($"An error occured while checking chat rights! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                        return;
                    }
                } else {
                    Functions.ShowHandledErrorTip(resp);
                }
            })();

            UpdateMessageSendRestrictionState();
            isProcessingEvent52 = false;
        }

        #endregion

        #region Conversation context menu

        private void contactPanelItem_Click(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                bool isPinned = await ContactsPanel.IsPinned(_id);
                if (isPinned) {
                    await ContactsPanel.UnpinUserAsync(_id);
                } else {
                    await ContactsPanel.PinUserAsync(AppSession.GetCachedUser(_id));
                }
            })();
        }

        public void OpenMessagesSearchModal() {
            if (ConversationId == 0) return;
            MessageSearch msd = new MessageSearch(new Tuple<long, string>(ConversationId, Title));
            msd.Closed += (m, ms) => { if (ms != null && ms is LMessage) GoToMessage((ms as LMessage).ConversationMessageId); };
            msd.Show();
        }

        private async Task ChangeSilenceModeAsync() {
            int sec = IsMuted ? 0 : -1;
            object resp = await VkAPI.Methods.Account.SetSilenceMode(sec, ConversationId);
            if (resp is bool r && r) {
                IsMuted = !IsMuted;
                UpdateMessageSendRestrictionState();
            }
            Functions.ShowHandledErrorTip(resp);
        }

        #endregion

        #region Sample conversation with messages

        public static ConversationViewModel GetSampleConversation() {
            return new ConversationViewModel {
                _id = int.MaxValue,
                CanWrite = new CanWrite { Allowed = false },
                Messages = new MessagesCollection { 
                    // LMessage.GetSampleMessage(1, true, Locale.Get("msg_sample_01")),
                    LMessage.GetSampleMessage(2, false, Locale.Get("msg_sample_02"), true, true),
                    LMessage.GetSampleMessage(63, true, Locale.Get("msg_sample_04")),
                },
                Style = "settings_demo",
                IsLoading = true, // to prevent LoadMessages execution
            };
        }

        #endregion
    }
}