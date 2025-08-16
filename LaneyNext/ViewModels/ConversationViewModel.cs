using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels.Controls;
using Elorucov.Laney.Views.Modals;
using Elorucov.Laney.VKAPIExecute.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

namespace Elorucov.Laney.ViewModels
{
    public class ConversationViewModel : BaseViewModel
    {
        private PeerType _peerType;
        private int _id;
        private string _title;
        private Uri _avatar;
        private bool _isVerified;
        private string _subtitle;
        private string _activityStatus;
        private UserOnlineInfo _online;
        private int _unreadMessagesCount;
        private MessagesCollection _messages = new MessagesCollection();
        private MessageViewModel _pinnedMessage;
        private bool _isMuted;
        private int _inread;
        private int _outread;
        private ChatSettings _csettings;
        private CanWrite _canwrite;
        private BotKeyboard _currentKeyboard;
        private bool _isMarkedAsUnread;
        private int _majorSortId;
        private bool _isPinIconShowing;
        private ThreadSafeObservableCollection<int> _mentions;
        private MessageInputViewModel _messageInput;
        private MessageViewModel _firstVisibleMessage;
        private MessageViewModel _lastVisibleMessage;
        private Visibility _spinnerVisibility;
        private string _restrictionReason;
        private DataTemplate _mentionIcon;
        private ObservableCollection<Sticker> _stickersSuggestions = new ObservableCollection<Sticker>();

        private RelayCommand _test1Command;
        private RelayCommand _getToLastMessageCommand;
        private RelayCommand _contextMenuCommand;
        private RelayCommand _getToPinnedMessageCommand;
        private RelayCommand _notificationToggleCommand;

        public PeerType PeerType { get { return _peerType; } private set { _peerType = value; OnPropertyChanged(); } }
        public int Id { get { return _id; } private set { _id = value; OnPropertyChanged(); } }
        public string Title { get { return _title; } private set { _title = value; OnPropertyChanged(); } }
        public Uri Avatar { get { return _avatar; } private set { _avatar = value; OnPropertyChanged(); } }
        public bool IsVerified { get { return _isVerified; } private set { _isVerified = value; OnPropertyChanged(); } }
        public string Subtitle { get { return _subtitle; } set { _subtitle = value; OnPropertyChanged(); } }
        public string ActivityStatus { get { return _activityStatus; } set { _activityStatus = value; OnPropertyChanged(); } }
        public UserOnlineInfo Online { get { return _online; } set { _online = value; OnPropertyChanged(); } }
        public int UnreadMessagesCount { get { return _unreadMessagesCount; } private set { _unreadMessagesCount = value; OnPropertyChanged(); } }
        public MessagesCollection Messages { get { return _messages; } private set { _messages = value; OnPropertyChanged(); } }
        public MessageViewModel PinnedMessage { get { return _pinnedMessage; } private set { _pinnedMessage = value; OnPropertyChanged(); } }
        public bool IsMuted { get { return _isMuted; } private set { _isMuted = value; OnPropertyChanged(); } }
        public int InRead { get { return _inread; } private set { _inread = value; OnPropertyChanged(); } }
        public int OutRead { get { return _outread; } private set { _outread = value; OnPropertyChanged(); } }
        public ChatSettings ChatSettings { get { return _csettings; } private set { _csettings = value; OnPropertyChanged(); } }
        public CanWrite CanWrite { get { return _canwrite; } private set { _canwrite = value; OnPropertyChanged(); } }
        public BotKeyboard CurrentKeyboard { get { return _currentKeyboard; } private set { _currentKeyboard = value; OnPropertyChanged(); } }
        public bool IsMarkedAsUnread { get { return _isMarkedAsUnread; } private set { _isMarkedAsUnread = value; OnPropertyChanged(); } }
        public int MajorSortId { get { return _majorSortId; } private set { _majorSortId = value; OnPropertyChanged(); } }
        public bool IsPinIconShowing { get { return _isPinIconShowing; } private set { _isPinIconShowing = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<int> Mentions { get { return _mentions; } private set { _mentions = value; OnPropertyChanged(); } }
        public MessageInputViewModel MessageInput { get { return _messageInput; } private set { _messageInput = value; OnPropertyChanged(); } }
        public MessageViewModel FirstVisibleMessage { get { return _firstVisibleMessage; } set { _firstVisibleMessage = value; OnPropertyChanged(); } }
        public MessageViewModel LastVisibleMessage { get { return _lastVisibleMessage; } set { _lastVisibleMessage = value; OnPropertyChanged(); } }

        public Visibility SpinnerVisibility { get { return _spinnerVisibility; } private set { _spinnerVisibility = value; OnPropertyChanged(); } }
        public string RestrictionReason { get { return _restrictionReason; } private set { _restrictionReason = value; OnPropertyChanged(); } }
        public DataTemplate MentionIcon { get { return _mentionIcon; } private set { _mentionIcon = value; OnPropertyChanged(); } }
        public ObservableCollection<Sticker> StickersSuggestions { get { return _stickersSuggestions; } set { _stickersSuggestions = value; OnPropertyChanged(); } }

        public RelayCommand Test1Command { get { return _test1Command; } private set { _test1Command = value; OnPropertyChanged(); } }
        public RelayCommand GetToLastMessageCommand { get { return _getToLastMessageCommand; } private set { _getToLastMessageCommand = value; OnPropertyChanged(); } }
        public RelayCommand ContextMenuCommand { get { return _contextMenuCommand; } private set { _contextMenuCommand = value; OnPropertyChanged(); } }
        public RelayCommand GetToPinnedMessageCommand { get { return _getToPinnedMessageCommand; } private set { _getToPinnedMessageCommand = value; OnPropertyChanged(); } }
        public RelayCommand NotificationToggleCommand { get { return _notificationToggleCommand; } private set { _notificationToggleCommand = value; OnPropertyChanged(); } }

        private bool _isLoading;
        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }

        private PlaceholderViewModel _placeholder;
        public PlaceholderViewModel Placeholder { get { return _placeholder; } private set { _placeholder = value; OnPropertyChanged(); } }

        private User PeerUser;
        private ELOR.VKAPILib.Objects.Group PeerGroup;
        public ObservableCollection<MessageViewModel> ReceivedMessages { get; private set; } = new ObservableCollection<MessageViewModel>();
        public MessageViewModel LastMessage { get { return ReceivedMessages.Count > 0 ? ReceivedMessages.Last() : null; } }
        //private bool isGettingToLastMessage = false;

        public List<User> MembersUsers { get; private set; } = new List<User>();
        public List<ELOR.VKAPILib.Objects.Group> MembersGroups { get; private set; } = new List<ELOR.VKAPILib.Objects.Group>();

        Elapser<LongPollActivityInfo> ActivityStatusUsers = new Elapser<LongPollActivityInfo>();

        public Action<int, bool, bool> ScrollToMessageCallback;
        public Action<MessageViewModel> MessageAddedToLastCallback;

        public override string ToString()
        {
            return Title;
        }

        #region Static members

        public static ConversationViewModel CurrentFocused { get { return GetCurrentFocused(); } }

        private static ConversationViewModel GetCurrentFocused()
        {
            switch (ViewManagement.CurrentViewType)
            {
                case ViewType.Session: return VKSession.Current.SessionBase.SelectedConversation;
                case ViewType.SingleConversation: return ViewManagement.GetStandaloneConversationByWindow();
                default: return null;
            }
        }

        #endregion

        public ConversationViewModel(int id)
        {
            Id = id;
            SetupVM();
            MessageInput = new MessageInputViewModel(this);
        }

        public ConversationViewModel(Conversation conversation, bool asDataModel = false)
        {
            if (!asDataModel) SetupVM();
            BasicSetup(conversation, null, asDataModel);
        }

        bool isLPEventsRegistered = false;
        private void RegisterLongPollEvents()
        {
            if (isLPEventsRegistered) return;
            VKSession.Current.LongPoll.FlagsEvent += LongPoll_FlagsEvent;
            VKSession.Current.LongPoll.NewMessageReceived += LongPoll_NewMessageReceived;
            VKSession.Current.LongPoll.KeyboardChanged += LongPoll_KeyboardChanged;
            VKSession.Current.LongPoll.MessageEdit += LongPoll_MessageEdit;
            VKSession.Current.LongPoll.IncomingMessagesRead += LongPoll_IncomingMessagesRead;
            VKSession.Current.LongPoll.OutgoingMessagesRead += LongPoll_OutgoingMessagesRead;
            VKSession.Current.LongPoll.UserOnline += LongPoll_UserOnline;
            VKSession.Current.LongPoll.UserOffline += LongPoll_UserOffline;
            VKSession.Current.LongPoll.ActivityStatusChanged += LongPoll_ActivityStatusChanged;
            VKSession.Current.LongPoll.ConversationMarked += LongPoll_ConversationMarked;
            VKSession.Current.LongPoll.ConversationUnmarked += LongPoll_ConversationUnmarked;
            VKSession.Current.LongPoll.BotCallbackReceived += LongPoll_BotCallbackReceived;
            VKSession.Current.LongPoll.MajorIdChanged += LongPoll_MajorIdChanged;
            VKSession.Current.LongPoll.NotificationSettingsChanged += LongPoll_NotificationSettingsChanged;

            ActivityStatusUsers.Elapsed += (a, b) => UpdateActivityStatus();
            isLPEventsRegistered = true;
        }

        private void SetupVM()
        {
            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(IsLoading): ChangeSpinnerVisibility(); break;
                    case nameof(Online): OnlineUpdated(); break;
                    case nameof(Messages):
                        Messages.Inserted += async (c, d) =>
                        {
                            // Код для скроллинга к отправленному с этого приложения сообщению.
                            //if (Messages.Last().SentFromThisApp) {
                            //    await Task.Delay(20);  // Without this code the app has crashed. ¯\_(ツ)_/¯
                            //    ScrollToMessageCallback?.Invoke(Messages.Last().Id, false, true);
                            //}
                            if (Messages.Last() == d) MessageAddedToLastCallback?.Invoke(d);
                        };
                        Messages.CollectionChanged += (c, d) =>
                        {
                            SetPlaceholder();
                        };
                        break;
                    case nameof(Mentions):
                        if (Mentions != null)
                        {
                            MentionIcon = VKUILibrary.GetIconTemplate(Mentions.Count > 0 ? VKIconName.Icon28MentionOutline : VKIconName.Icon28BombOutline);
                            Mentions.CollectionChanged += (c, d) =>
                            {
                                OnPropertyChanged(nameof(Mentions));
                            };
                        }
                        else
                        {
                            MentionIcon = null;
                        }
                        break;
                    case nameof(MentionIcon):
                    case nameof(UnreadMessagesCount):
                        IsPinIconShowing = MentionIcon == null && UnreadMessagesCount == 0
                            && (MajorSortId != 0 && MajorSortId % 16 == 0);
                        break;
                }
            };

            ReceivedMessages.CollectionChanged += (c, d) =>
            {
                OnPropertyChanged(nameof(LastMessage));
            };
            ContextMenuCommand = new RelayCommand((o) => ShowConversationContextMenu((FrameworkElement)o));
            GetToPinnedMessageCommand = new RelayCommand((o) => GetToMessage(PinnedMessage, Locale.Get("pinned_message")));
            GetToLastMessageCommand = new RelayCommand((o) =>
            {
                if (Messages.Last().Id == LastMessage.Id)
                {
                    ScrollToMessageCallback?.Invoke(LastMessage.Id, true, true);
                    return;
                }
                if (ReceivedMessages.Count > 0)
                {
                    Messages = new MessagesCollection(ReceivedMessages.ToList());
                    foreach (MessageViewModel msg in Messages)
                    {
                        FixState(msg);
                    }
                    ScrollToMessageCallback?.Invoke(LastMessage.Id, true, true);
                    if (ReceivedMessages.Count < 30) LoadPreviousMessages();
                }
                else
                {
                    GetToMessage(LastMessage);
                }
            });
            NotificationToggleCommand = new RelayCommand((o) => APIHelper.ChangeConversationNotification(Id, IsMuted));
        }

        private void BasicSetup(Conversation conversation, UserOnlineInfoEx onlineInfo = null, bool isForDataModel = false)
        {
            PeerType = conversation.Peer.Type;
            Id = conversation.Peer.Id;
            UnreadMessagesCount = conversation.UnreadCount;
            CanWrite = conversation.CanWrite;
            InRead = conversation.InRead;
            OutRead = conversation.OutRead;
            IsMuted = conversation.PushSettings != null && conversation.PushSettings.DisabledForever ? true : false;
            IsMarkedAsUnread = conversation.IsMarkedUnread;
            MajorSortId = conversation.SortId.MajorId;
            if (conversation.Mentions != null) Mentions = new ThreadSafeObservableCollection<int>(conversation.Mentions);
            if (conversation.CurrentKeyboard != null && conversation.CurrentKeyboard.Buttons.Count > 0) CurrentKeyboard = conversation.CurrentKeyboard;
            if (MessageInput == null && !isForDataModel) MessageInput = new MessageInputViewModel(this);

            switch (PeerType)
            {
                case PeerType.Chat:
                    Title = conversation.ChatSettings.Title;
                    Avatar = conversation.ChatSettings.Photo != null ? conversation.ChatSettings.Photo.Uri : new Uri("https://vk.com/images/icons/im_multichat_200.png");
                    ChatSettings = conversation.ChatSettings;
                    if (!isForDataModel) UpdateSubtitleForChat();
                    if (!isForDataModel && ChatSettings?.PinnedMessage != null) PinnedMessage = new MessageViewModel(ChatSettings.PinnedMessage);
                    break;
                case PeerType.User:
                    PeerUser = CacheManager.GetUser(Id);
                    Title = PeerUser.FullName;
                    Avatar = PeerUser.Photo;
                    IsVerified = PeerUser.Verified;
                    Online = onlineInfo != null ? onlineInfo : PeerUser.OnlineInfo;
                    break;
                case PeerType.Group:
                    PeerGroup = CacheManager.GetGroup(Id);
                    Title = PeerGroup.Name;
                    Avatar = PeerGroup.Photo;
                    IsVerified = PeerGroup.Verified;
                    Subtitle = PeerGroup.Activity;
                    break;
            }
            if (!isForDataModel)
            {
                UpdateRestrictionInfo();
                RegisterLongPollEvents();
            }
        }

        private void UpdateSubtitleForChat()
        {
            if (ChatSettings.State == UserStateInChat.In)
            {
                Subtitle = String.Empty;
                if (ChatSettings.IsDisappearing) Subtitle = $"{Locale.Get("casper_chat").ToLower()}, ";
                Subtitle += String.Format(Locale.GetDeclensionForFormat(ChatSettings.MembersCount, "chatinfo_subtitle"), ChatSettings.MembersCount);
            }
            else
            {
                Subtitle = "";
            }
        }

        #region Commands

        private void Test1(object obj)
        {
            // Reserved for MVVM tests
        }

        private void Test2(object obj)
        {
            // Reserved for MVVM tests
        }

        private void ShowConversationContextMenu(FrameworkElement fe)
        {
            CellButton userinfo = new CellButton
            {
                Icon = PeerType == PeerType.Group ? VKIconName.Icon28Users3Outline : VKIconName.Icon28UserOutline,
                Text = PeerType == PeerType.Group ? Locale.Get("group") : Locale.Get("profile")
            };
            CellButton chatinfo = new CellButton
            {
                Icon = VKIconName.Icon28InfoOutline,
                Text = ChatSettings != null && ChatSettings.IsGroupChannel ? Locale.Get("about_channel") : Locale.Get("about_chat")
            };
            CellButton search = new CellButton
            {
                Icon = VKIconName.Icon28SearchOutline,
                Text = Locale.Get("conv_ctx_search")
            };
            CellButton attachments = new CellButton
            {
                Icon = VKIconName.Icon28PictureOutline,
                Text = Locale.Get("conv_ctx_attachments")
            };
            CellButton notification = new CellButton
            {
                Icon = IsMuted ? VKIconName.Icon28Notifications : VKIconName.Icon28NotificationDisableOutline,
                Text = IsMuted ? Locale.Get("conv_ctx_notifications_enable") : Locale.Get("conv_ctx_notifications_disable")
            };
            CellButton refresh = new CellButton
            {
                Icon = VKIconName.Icon28RefreshOutline,
                Text = Locale.Get("refresh")
            };
            CellButton returnto = new CellButton
            {
                Icon = VKIconName.Icon28ArrowUturnRightOutline,
                Text = ChatSettings != null && ChatSettings.IsGroupChannel ? Locale.Get("conv_ctx_return_channel") : Locale.Get("conv_ctx_return_chat")
            };

            userinfo.Click += (a, b) => Router.ShowCard(Id);
            chatinfo.Click += (a, b) => Router.ShowChatInfo(Id, ChatSettings.IsGroupChannel ? Locale.Get("about_channel") : Locale.Get("about_chat"));
            search.Click += (a, b) =>
            {
                SearchInConversation sic = new SearchInConversation(Id, Title);
                sic.Closed += (c, d) =>
                {
                    if (d != null && d is MessageViewModel mvm) GetToMessage(mvm);
                };
                sic.Show();
            };
            attachments.Click += (a, b) =>
            {
                ConversationAttachments cam = new ConversationAttachments(Id, Title);
                cam.Closed += (c, d) =>
                {
                    if (d != null && d is ConversationAttachment ca) GetToMessage(ca.MessageId);
                };
                cam.Show();
            };
            notification.Click += (a, b) => APIHelper.ChangeConversationNotification(Id, IsMuted);
            refresh.Click += (a, b) => LoadMessages();
            returnto.Click += (a, b) => Utils.ShowUnderConstructionInfo();

            MenuFlyout mf = new MenuFlyout();
            if (PeerType != PeerType.Chat) mf.Items.Add(userinfo);
            if (VKSession.Current.Type == SessionType.VKUser && PeerType == PeerType.Chat) mf.Items.Add(chatinfo);
            mf.Items.Add(search);
            mf.Items.Add(attachments);
            mf.Items.Add(notification);
            mf.Items.Add(refresh);
            if (PeerType == PeerType.Chat && ChatSettings.State == UserStateInChat.Left) mf.Items.Add(returnto);
            mf.ShowAt(fe);
        }

        #endregion

        bool opened = false;
        public void OnShowing()
        {
            Log.General.Info(String.Empty, new ValueSet { { "opened", opened } });
            if (opened) return;
            opened = true;
            LoadMessages();
        }

        private void UpdateRestrictionInfo()
        {
            if (CanWrite.Allowed)
            {
                RestrictionReason = String.Empty; return;
            }
            if (PeerType == PeerType.Chat)
            {
                if (ChatSettings.State != UserStateInChat.In)
                {
                    RestrictionReason = Locale.Get($"chat_{ChatSettings.State.ToString().ToLower()}");
                }
                else
                {
                    APIHelper.GetUnderstandableErrorMessage(CanWrite.Reason);
                }
            }
            else if (PeerType == PeerType.User)
            {
                switch (CanWrite.Reason)
                {
                    case 18:
                        if (PeerUser.Deactivated == DeactivationState.Deleted) RestrictionReason = Locale.Get("user_deleted");
                        if (PeerUser.Deactivated == DeactivationState.Banned) RestrictionReason = Locale.Get("user_blocked");
                        break;
                    case 900:
                        if (PeerUser.Blacklisted) RestrictionReason = Locale.Get("user_blacklisted", PeerUser.Sex);
                        if (PeerUser.BlacklistedByMe) RestrictionReason = Locale.Get("user_blacklisted_by_me", PeerUser.Sex);
                        break;
                    case 901:
                        RestrictionReason = Locale.Get("api_error_901");
                        break;
                    case 902:
                        RestrictionReason = Locale.Get("api_error_902");
                        break;
                    default:
                        APIHelper.GetUnderstandableErrorMessage(CanWrite.Reason, "restricted");
                        break;
                }
            }
            else if (PeerType == PeerType.Group)
            {
                switch (CanWrite.Reason)
                {
                    case 203:
                        RestrictionReason = Locale.Get("api_error_203");
                        break;
                    case 915:
                        RestrictionReason = Locale.Get("api_error_915");
                        break;
                    case 916:
                        RestrictionReason = Locale.Get("api_error_916");
                        break;
                    default:
                        APIHelper.GetUnderstandableErrorMessage(CanWrite.Reason, "restricted");
                        break;
                }
            }
        }

        private void SetPlaceholder()
        {
            if (Messages.Count > 0 || IsLoading)
            {
                Placeholder = null;
            }
            else
            {
                var defplac = CanWrite.Allowed ? PlaceholderViewModel.ForEmptyConversation : PlaceholderViewModel.ForEmptyRestrictedConversation;
                Placeholder = ChatSettings != null && ChatSettings.IsDisappearing ?
                    PlaceholderViewModel.ForEmptyCasperChat : defplac;
            }
        }

        private void ChangeSpinnerVisibility()
        {
            if (Messages.Count > 0)
            {
                SpinnerVisibility = Visibility.Collapsed;
            }
            else
            {
                SpinnerVisibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnlineUpdated()
        {
            User u = CacheManager.GetUser(Id);
            if (Online is UserOnlineInfoEx uoiex)
            {
                Subtitle = APIHelper.GetOnlineInfoString(uoiex, u.Sex);
            }
            else
            {
                Subtitle = APIHelper.GetOnlineInfoString(Online, u.Sex);
            }
        }

        private void UpdateActivityStatus()
        {
            var acts = ActivityStatusUsers.RegisteredObjects;
            int count = acts.Count();

            Debug.WriteLine($"UpdateActivityStatus: {String.Join(";", acts)}");

            if (count == 0)
            {
                ActivityStatus = String.Empty;
                return;
            }

            if (PeerType != PeerType.Chat)
            {
                if (count == 1)
                {
                    ActivityStatus = GetLocalizedActivityStatus(acts.FirstOrDefault().Status, 1);
                }
            }
            else
            {
                var typing = acts.Where(a => a.Status == LongPollActivityStatus.Typing).ToList();
                var voice = acts.Where(a => a.Status == LongPollActivityStatus.RecordingVoiceMessage).ToList();
                var photo = acts.Where(a => a.Status == LongPollActivityStatus.SendingPhoto).ToList();
                var video = acts.Where(a => a.Status == LongPollActivityStatus.SendingVideo).ToList();
                var file = acts.Where(a => a.Status == LongPollActivityStatus.SendingFile).ToList();
                List<List<LongPollActivityInfo>> groupedActivities = new List<List<LongPollActivityInfo>> {
                    typing, voice, photo, video, file
                };

                bool has3AndMoreDifferentTypes = groupedActivities.Where(a => a.Count > 0).Count() >= 3;

                string status = String.Empty;
                foreach (var act in groupedActivities)
                {
                    if (act.Count == 0) continue;
                    var type = act[0].Status;
                    string actstr = GetLocalizedActivityStatus(type, act.Count);

                    if (has3AndMoreDifferentTypes)
                    {
                        if (status.Length != 0) status += ", ";
                        status += $"{act.Count} {actstr}";
                    }
                    else
                    {
                        if (status.Length != 0) status += ", ";
                        List<int> ids = act.Select(s => s.MemberId).ToList();
                        status += $"{GetNamesForActivityStatus(ids, act.Count, act.Count == 1)} {actstr}";
                    }
                }

                ActivityStatus = status.Trim() + "...";
            }
        }

        private string GetLocalizedActivityStatus(LongPollActivityStatus status, int count)
        {
            string suffix = count == 1 ? "_s" : "_m";
            switch (status)
            {
                case LongPollActivityStatus.Typing: return Locale.Get($"im_status_typing{suffix}");
                case LongPollActivityStatus.RecordingVoiceMessage: return Locale.Get($"im_status_voice{suffix}");
                case LongPollActivityStatus.SendingPhoto: return Locale.Get($"im_status_photo{suffix}");
                case LongPollActivityStatus.SendingVideo: return Locale.Get($"im_status_video{suffix}");
                case LongPollActivityStatus.SendingFile: return Locale.Get($"im_status_file{suffix}");
            }
            return String.Empty;
        }

        private string GetNamesForActivityStatus(IReadOnlyList<int> ids, int count, bool showFullLastName)
        {
            string r = String.Empty;
            foreach (int id in ids)
            {
                if (id > 0)
                {
                    User u = CacheManager.GetUser(id);
                    if (u != null)
                    {
                        string lastName = showFullLastName ? u.LastName : u.LastName[0] + ".";
                        r = $"{u.FirstName} {lastName}";
                    }
                }
                else if (id < 0)
                {
                    var g = CacheManager.GetGroup(id);
                    if (g != null)
                    {
                        r = $"\"{g.Name}\"";
                    }
                }
            }
            if (!String.IsNullOrEmpty(r))
            {
                if (count > 1)
                {
                    r += $" {String.Format(Locale.GetForFormat("im_status_more"), count - 1)}";
                }
            }
            else
            {
                if (count > 0) r = String.Format(Locale.GetDeclensionForFormat(count, "chatinfo_subtitle"), count);
            }
            return r;
        }

        public void Dispose()
        {
            VKSession.Current.LongPoll.FlagsEvent -= LongPoll_FlagsEvent;
            VKSession.Current.LongPoll.NewMessageReceived -= LongPoll_NewMessageReceived;
            VKSession.Current.LongPoll.KeyboardChanged -= LongPoll_KeyboardChanged;
            VKSession.Current.LongPoll.MessageEdit -= LongPoll_MessageEdit;
            VKSession.Current.LongPoll.IncomingMessagesRead -= LongPoll_IncomingMessagesRead;
            VKSession.Current.LongPoll.OutgoingMessagesRead -= LongPoll_OutgoingMessagesRead;
            VKSession.Current.LongPoll.UserOnline -= LongPoll_UserOnline;
            VKSession.Current.LongPoll.UserOffline -= LongPoll_UserOffline;
            VKSession.Current.LongPoll.ActivityStatusChanged -= LongPoll_ActivityStatusChanged;
            VKSession.Current.LongPoll.ConversationMarked -= LongPoll_ConversationMarked;
            VKSession.Current.LongPoll.ConversationUnmarked -= LongPoll_ConversationUnmarked;
            VKSession.Current.LongPoll.BotCallbackReceived -= LongPoll_BotCallbackReceived;
            VKSession.Current.LongPoll.MajorIdChanged -= LongPoll_MajorIdChanged;
            VKSession.Current.LongPoll.NotificationSettingsChanged -= LongPoll_NotificationSettingsChanged;

            ActivityStatusUsers.Elapsed -= (a, b) => UpdateActivityStatus();
            isLPEventsRegistered = false;
            Messages.Clear();
            ReceivedMessages.Clear();
            MessageInput = null;
        }

        #region Messages

        public void GetToMessage(MessageViewModel msg, string modalTitle = null)
        {
            if (msg.Id == 0)
            {
                MessagesModal modal = new MessagesModal(msg, modalTitle == null ? Locale.Get("message") : modalTitle);
                modal.Show();
            }
            else
            {
                GetToMessage(msg.Id);
            }
        }

        public void GetToMessage(int messageId)
        {
            var m = from q in Messages where q.Id == messageId select q;
            if (m.Count() == 1)
            {
                Log.General.Info("Scrolling to message", new ValueSet { { "id", messageId } });
                ScrollToMessageCallback?.Invoke(m.FirstOrDefault().Id, true, true);
            }
            else
            {
                LoadMessages(messageId);
            }
        }

        public async void GetToMessageByConvMsgId(int conversationMessageId, string defaultMessageText, FrameworkElement elementForFlyout)
        {
            MessageViewModel msg = Messages.Where(m => m.ConversationMessageId == conversationMessageId).FirstOrDefault();
            if (msg != null)
            {
                GetToMessage(msg);
            }
            else
            {
                Log.General.Info("Need to get message from cmid", new ValueSet { { "cmid", conversationMessageId } });
                var resp = await VKSession.Current.API.Messages.GetByConversationMessageIdAsync(VKSession.Current.GroupId, Id, new List<int> { conversationMessageId });
                if (resp.Items.Count > 0)
                {
                    msg = new MessageViewModel(resp.Items[0]);
                    GetToMessage(msg);
                }
                else
                {
                    Flyout f = new Flyout
                    {
                        Content = new Windows.UI.Xaml.Controls.TextBlock
                        {
                            Text = VKTextParser.GetParsedText(defaultMessageText),
                        }
                    };
                    f.ShowAt(elementForFlyout);
                }
            }
        }

        public async void LoadMessages(int startMessageId = -1)
        {
            if (IsLoading) return;
            //isGettingToLastMessage = startMessageId == LastMessage?.Id;
            Placeholder = null;
            if (Messages != null)
            {
                Messages.Clear();
            }
            else
            {
                Messages = new MessagesCollection();
            }

            int count = Constants.MessagesCount;
            try
            {
                Log.General.Info(String.Empty, new ValueSet { { "start_message_id", startMessageId }, { "count", count } });
                IsLoading = true;
                int offset = -count / 2;
                MessagesHistoryEx mhr = await VKSession.Current.Execute.GetHistoryWithMembersAsync(VKSession.Current.GroupId, Id, offset, count, startMessageId, false, APIHelper.Fields);
                CacheManager.Add(mhr.Profiles);
                CacheManager.Add(mhr.Groups);
                CacheManager.Add(mhr.MentionedProfiles);
                CacheManager.Add(mhr.MentionedGroups);
                MembersUsers = mhr.Profiles;
                MembersGroups = mhr.Groups;
                BasicSetup(mhr.Conversation, mhr.OnlineInfo);
                mhr.Messages.Reverse();
                Messages = new MessagesCollection(mhr.Messages);

                foreach (MessageViewModel msg in Messages)
                {
                    FixState(msg);
                }
                if (startMessageId > 0) ScrollToMessageCallback?.Invoke(startMessageId, true, true);
                if (startMessageId == -1)
                {
                    ScrollToMessageCallback?.Invoke(Math.Min(InRead, OutRead), false, false);
                }
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => { LoadMessages(startMessageId); });
            }
            finally
            {
                IsLoading = false;
                //isGettingToLastMessage = false;
                SetPlaceholder();
            }
        }

        public async void LoadPreviousMessages()
        {
            if (Messages.Count == 0 || IsLoading) return;
            int count = Constants.MessagesCount;
            try
            {
                IsLoading = true;
                MessagesHistoryEx mhr = await VKSession.Current.Execute.GetHistoryWithMembersAsync(VKSession.Current.GroupId, Id, 1, count, Messages.First().Id, false, APIHelper.Fields, true);
                CacheManager.Add(mhr.MentionedProfiles);
                CacheManager.Add(mhr.MentionedGroups);
                mhr.Messages.Reverse();
                foreach (Message msg in mhr.Messages)
                {
                    MessageViewModel mvm = new MessageViewModel(msg);
                    FixState(mvm);
                    Messages.Insert(mvm);
                    await Task.Yield();
                }
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                {
                    LoadPreviousMessages();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async void LoadNextMessages()
        {
            if (Messages.Count == 0 || IsLoading) return;
            int count = Constants.MessagesCount;
            try
            {
                IsLoading = true;
                MessagesHistoryEx mhr = await VKSession.Current.Execute.GetHistoryWithMembersAsync(VKSession.Current.GroupId, Id, -count, count, Messages.Last().Id, false, APIHelper.Fields, false);
                CacheManager.Add(mhr.MentionedProfiles);
                CacheManager.Add(mhr.MentionedGroups);
                mhr.Messages.Reverse();
                foreach (Message msg in mhr.Messages)
                {
                    MessageViewModel mvm = new MessageViewModel(msg);
                    FixState(mvm);
                    Messages.Insert(mvm);
                    await Task.Yield();
                }
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                {
                    LoadNextMessages();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FixState(MessageViewModel msg)
        {
            int senderId = VKSession.Current.SessionId;
            bool isOutgoing = msg.SenderId == senderId;
            if (isOutgoing)
            {
                msg.State = msg.Id > OutRead ? MessageVMState.Unread : MessageVMState.Read;
            }
            else
            {
                msg.State = msg.Id > InRead ? MessageVMState.Unread : MessageVMState.Read;
            }
        }

        public void NullMessagesList()
        {
            Messages = null;
        }

        #endregion

        #region LongPoll events

        private void LongPoll_FlagsEvent(LPMessageFlagState state, int msgid, int peerId, int flags)
        {
            if (peerId != Id) return;
            if (state == LPMessageFlagState.Set)
            { // Установка
                if (Utils.CheckFlag(flags, 131072))
                { // Сообщение удалено
                    var y = (from z in Messages where z.Id == msgid select z).ToList();
                    if (y.Count == 1)
                    {
                        Messages.RemoveMessage(y[0]);
                        ReceivedMessages.Remove(y[0]);
                    }
                }
            }
        }

        private void LongPoll_NewMessageReceived(object sender, Message m)
        {
            if (m.PeerId == Id)
            {
                Debug.WriteLine($"New message in {Id}: id: {m.Id}.");
                int lmid = LastMessage == null ? 0 : LastMessage.Id;
                MessageViewModel msg = new MessageViewModel(m);
                ReceivedMessages.Add(msg);

                if (msg.Action != null) ProcessActionMessage(msg);

                bool ableToAddMessageInList = false;
                int? vid = Messages.LastOrDefault()?.Id;
                if (vid != null && vid == Int32.MaxValue)
                {
                    ableToAddMessageInList = Messages.Count == 0 || Messages[Messages.Count - 2].Id == lmid;
                }
                else
                {
                    ableToAddMessageInList = Messages.Count == 0 || Messages.Last().Id == lmid;
                }
                Debug.WriteLine($"{Id}: ableToAddMessageInList: {ableToAddMessageInList}");
                if (ableToAddMessageInList)
                {
                    // Сперва ищем в списке, есть ли сообщение, которое юзер отправил с этого приложения.
                    MessageViewModel yvm = Messages.Where(ms => ms.SenderId == VKSession.Current.SessionId &&
                        ms.RandomId == msg.RandomId &&
                        ms.Id == Int32.MaxValue &&
                        ms.State == MessageVMState.Sending).FirstOrDefault();
                    if (yvm != null)
                    {
                        yvm.Edit(m);
                        FixState(yvm);
                    }
                    else
                    {
                        Messages.Insert(msg);
                        FixState(msg);
                    }
                }

                if (m.FromId != VKSession.Current.SessionId)
                {
                    UnreadMessagesCount++;

                    var senderStatus = ActivityStatusUsers.RegisteredObjects.Where(u => m.FromId == u.MemberId).FirstOrDefault();
                    if (senderStatus != null) ActivityStatusUsers.Remove(senderStatus);
                }
            }
        }

        private void ProcessActionMessage(MessageViewModel msg)
        {
            var action = msg.Action;
            switch (action.Type)
            {
                case ActionType.ChatTitleUpdate:
                    Title = action.Text;
                    break;
                case ActionType.ChatPhotoUpdate:
                    Avatar = msg.Attachments[0].Photo.PreviewImageUri;
                    break;
                case ActionType.ChatPhotoRemove:
                    Avatar = new Uri("https://vk.com/images/icons/im_multichat_200.png");
                    break;
                case ActionType.ChatKickUser:
                    if (action.MemberId == VKSession.Current.SessionId)
                    {
                        ChatSettings.State = msg.SenderId == VKSession.Current.SessionId ? UserStateInChat.Left : UserStateInChat.Kicked;
                        CanWrite.Allowed = false;
                        UpdateRestrictionInfo();
                        PinnedMessage = null;
                    }
                    else
                    {
                        ChatSettings.MembersCount = ChatSettings.MembersCount - 1;
                        UpdateSubtitleForChat();
                    }
                    break;
                case ActionType.ChatInviteUser:
                    if (msg.SenderId == action.MemberId && msg.SenderId == VKSession.Current.SessionId)
                    {
                        ChatSettings.State = UserStateInChat.In;
                        CanWrite.Allowed = true;
                        UpdateRestrictionInfo();
                    }
                    else
                    {
                        ChatSettings.MembersCount = ChatSettings.MembersCount + 1;
                        UpdateSubtitleForChat();
                    }
                    break;
                case ActionType.ChatPinMessage:
                    UpdatePinnedMessage(action.ConversationMessageId);
                    break;
                case ActionType.ChatUnpinMessage:
                    PinnedMessage = null;
                    break;
            }
        }

        private async void UpdatePinnedMessage(int cmId)
        {
            var y = (from z in Messages where z.ConversationMessageId == cmId select z).ToList();
            if (y.Count > 1)
            {
                PinnedMessage = y[0];
            }
            else
            {
                var resp = await VKSession.Current.API.Messages.GetByConversationMessageIdAsync(VKSession.Current.GroupId, Id, new List<int> { cmId });
                PinnedMessage = new MessageViewModel(resp.Items[0]);
            }
        }

        private void LongPoll_KeyboardChanged(object sender, Tuple<int, BotKeyboard> e)
        {
            if (Id != e.Item1) return;
            CurrentKeyboard = e.Item2;
        }

        private void LongPoll_MessageEdit(object sender, Message m)
        {
            MessageViewModel msg = new MessageViewModel(m);
            if (msg.PeerId != Id) return;
            FixState(msg);

            MessageViewModel mvm = Messages.Where(ms => ms.Id == msg.Id).FirstOrDefault();
            if (mvm != null)
            {
                mvm.Edit(m);
                mvm.State = msg.State;
            }

            MessageViewModel rvm = ReceivedMessages.Where(ms => ms.Id == msg.Id).FirstOrDefault();
            if (rvm != null)
            {
                rvm.Edit(m);
                rvm.State = msg.State;
                if (LastMessage.Id == rvm.Id)
                {
                    OnPropertyChanged(nameof(LastMessage));
                }
            }

            if (m.FromId != VKSession.Current.SessionId)
            {
                var senderStatus = ActivityStatusUsers.RegisteredObjects.Where(u => m.FromId == u.MemberId).FirstOrDefault();
                if (senderStatus != null) ActivityStatusUsers.Remove(senderStatus);
            }
        }

        private void LongPoll_IncomingMessagesRead(object sender, Tuple<int, int, int> e)
        {
            if (e.Item1 == Id)
            {
                InRead = e.Item2;
                OutRead = e.Item2;
                UnreadMessagesCount = e.Item3;

                if (Mentions != null)
                {
                    if (Mentions.Count > 0)
                    {
                        var mentions = Mentions.ToList();
                        foreach (int id in mentions)
                        {
                            if (id <= InRead) Mentions.Remove(id);
                        }
                        if (Mentions.Count == 0) Mentions = null;
                    }
                }
            }
        }

        private void LongPoll_OutgoingMessagesRead(object sender, Tuple<int, int, int> e)
        {
            if (e.Item1 == Id)
            {
                InRead = e.Item2;
                OutRead = e.Item2;
            }
        }

        private void LongPoll_UserOnline(object sender, Tuple<int, int, bool> e)
        {
            if (PeerType != PeerType.User || Id != e.Item1) return;
            Online = new UserOnlineInfo() { Visible = true, isOnline = true, IsMobile = e.Item3 };
        }

        private void LongPoll_UserOffline(object sender, Tuple<int, bool> e)
        {
            if (PeerType != PeerType.User || Id != e.Item1) return;
            Online = new UserOnlineInfo() { Visible = true, isOnline = false };
        }

        private void LongPoll_ActivityStatusChanged(object sender, Tuple<int, List<LongPollActivityInfo>> e)
        {
            if (Id != e.Item1) return;
            double timeout = 7000;
            try
            {
                foreach (LongPollActivityInfo info in e.Item2)
                {
                    if (info.MemberId == VKSession.Current.SessionId) continue;
                    var exist = ActivityStatusUsers.RegisteredObjects.Where(u => u.MemberId == info.MemberId).FirstOrDefault();
                    if (exist != null) ActivityStatusUsers.Remove(exist);
                    ActivityStatusUsers.Add(info, timeout);
                }
                UpdateActivityStatus();
            }
            catch (Exception ex)
            {
                ActivityStatusUsers.Clear();
                ActivityStatus = String.Empty;
                Log.General.Warn($"Error while parsing user activity status, 0x{ex.HResult.ToString("x8")}");
            }
            UpdateActivityStatus();
        }

        private void LongPoll_ConversationMarked(object sender, Tuple<LPConversationMarkType, int, int> e)
        {
            if (e.Item2 != Id) return;
            switch (e.Item1)
            {
                case LPConversationMarkType.Mention:
                    if (Mentions == null) Mentions = new ThreadSafeObservableCollection<int>();
                    Mentions.Add(e.Item3);
                    break;
                case LPConversationMarkType.SelfDestructMessage:
                    if (Mentions == null) Mentions = new ThreadSafeObservableCollection<int>();
                    Mentions.Clear();
                    break;
                case LPConversationMarkType.Unread:
                    IsMarkedAsUnread = true;
                    break;
            }
        }

        private void LongPoll_ConversationUnmarked(object sender, Tuple<LPConversationMarkType, int> e)
        {
            if (e.Item2 != Id) return;
            if (e.Item1 != LPConversationMarkType.Unread)
            {
                MentionIcon = null;
            }
            else
            {
                IsMarkedAsUnread = false;
            }
        }

        private async void LongPoll_BotCallbackReceived(object sender, LPBotCallback e)
        {
            if (e.PeerId != Id) return;
            var action = e.Action;
            switch (e.Action.Type)
            {
                case LPBotCallbackActionType.OpenApp:
                    string link = String.IsNullOrEmpty(action.Hash) ? $"https://m.vk.com/app{action.AppId}" : $"https://m.vk.com/app{action.AppId}#{action.Hash}";
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(link));
                    break;
                case LPBotCallbackActionType.OpenLink:
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(action.Link));
                    break;
            }
        }

        private void LongPoll_MajorIdChanged(object sender, Dictionary<int, int> e)
        {
            if (e.ContainsKey(Id))
            {
                MajorSortId = e[Id];
            }
        }

        private void LongPoll_NotificationSettingsChanged(object sender, NotificationSettingsChangedInfo e)
        {
            if (e.PeerId != Id) return;
            IsMuted = e.DisabledUntil == -1 || e.DisabledUntil > 0;
        }

        #endregion
    }
}