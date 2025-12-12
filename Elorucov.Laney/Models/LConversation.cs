using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Models {

    public class LConversation : BaseViewModel {
        private long _id;
        private string _title;
        private Uri _photo;
        private PeerType _type = PeerType.Chat;
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
        private long _online;
        private int _inread;
        private int _outread;
        private ChatSettings _csettings;
        private CanWrite _canwrite;
        private BotKeyboard _currentKeyboard;
        private ObservableCollection<int> _mentions;
        private bool _hasMention;
        private DataTemplate _mentionIcon;
        private List<ChatMember> _chMembers;
        private bool _isMarkedUnread;
        private string _style;

        public long Id { get { return _id; } set { _id = value; OnPropertyChanged(); } }
        public string Title { get { return _title; } set { _title = value; OnPropertyChanged(); } }
        public Uri Photo { get { return _photo; } set { _photo = value; OnPropertyChanged(); } }
        public PeerType Type { get { return _type; } }
        public bool HasUnreadMessageFromCurrentUser { get { return _hasUnreadMessageFromCurrentUser; } set { _hasUnreadMessageFromCurrentUser = value; OnPropertyChanged(); } }
        public bool IsLastMessageOutgoing { get { return _isLastMessageOutgoing; } set { _isLastMessageOutgoing = value; OnPropertyChanged(); } }
        public bool IsPinned { get { return SortId.MajorId > 0 && SortId.MajorId % 16 == 0; } }
        public int UnreadMessagesCount { get { return _unreadMessagesCount; } set { _unreadMessagesCount = value; OnPropertyChanged(); } }
        public SortId SortId { get { return _sortId; } set { _sortId = value; OnPropertyChanged(); } }
        public bool IsVerified { get { return _isverified; } set { _isverified = value; OnPropertyChanged(); } }
        public bool IsDisappearing { get { return _isDisappearing; } set { _isDisappearing = value; OnPropertyChanged(); } }
        public bool IsDonut { get { return _isDonut; } set { _isDonut = value; OnPropertyChanged(); } }
        public bool IsMuted { get { return _isMuted; } set { _isMuted = value; OnPropertyChanged(); } }
        public bool IsArchived { get { return _isArchived; } set { _isArchived = value; OnPropertyChanged(); } }
        public LMessage PinnedMessage { get { return _pinnedMessage; } set { _pinnedMessage = value; OnPropertyChanged(); } }
        public int MembersCount { get { return _membersCount; } set { _membersCount = value; OnPropertyChanged(); } }
        public long Online { get { return _online; } set { _online = value; OnPropertyChanged(); } }
        public int InRead { get { return _inread; } set { _inread = value; OnPropertyChanged(); } }
        public int OutRead { get { return _outread; } set { _outread = value; OnPropertyChanged(); } }
        public ChatSettings ChatSettings { get { return _csettings; } set { _csettings = value; OnPropertyChanged(); } }
        public CanWrite CanWrite { get { return _canwrite; } set { _canwrite = value; OnPropertyChanged(); } }
        public BotKeyboard CurrentKeyboard { get { return _currentKeyboard; } set { _currentKeyboard = value; OnPropertyChanged(); } }
        public ObservableCollection<int> Mentions { get { return _mentions; } set { _mentions = value; OnPropertyChanged(); } }
        public DataTemplate MentionIcon { get { return _mentionIcon; } private set { _mentionIcon = value; OnPropertyChanged(); } }
        public bool HasMention { get { return _hasMention; } private set { _hasMention = value; OnPropertyChanged(); } }
        public List<ChatMember> ChatMembers { get { return _chMembers; } set { _chMembers = value; OnPropertyChanged(); } }
        public bool IsMarkedUnread { get { return _isMarkedUnread; } set { _isMarkedUnread = value; OnPropertyChanged(); } }
        public string Style { get { return _style; } set { _style = value; OnPropertyChanged(); } }

        public Conversation FromAPI { get; private set; }

        public LConversation() {
            Title = "Unknown";
        }

        public LConversation(Conversation c) {
            FromAPI = c;
            Do(c);
        }

        private void Do(Conversation c) {
            try {
                Id = c.Peer.Id;
                _type = c.Peer.Type;

                if (c.Peer.Type == PeerType.Chat) {
                    IsDisappearing = c.ChatSettings != null ? c.ChatSettings.IsDisappearing : false;
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
                        Title = u != null ? $"{u.FirstName} {u.LastName}" : "Untitled user";
                        Photo = u != null && u.HasPhoto ? u.Photo : null;
                        IsVerified = (u != null && u.Verified == 1);
                        if (u != null && u.Online != 0) {
                            if (u.OnlineMobile == 1) {
                                Online = VKClientsHelper.GetLPAppIdByAppId(u.OnlineAppId);
                            } else {
                                Online = 7;
                            }
                        }
                    }
                } else if (c.Peer.Type == PeerType.Group) {
                    var g = AppSession.GetCachedGroup(-c.Peer.Id);
                    Title = g != null ? g.Name : "Untitled group";
                    Photo = g != null && g.HasPhoto ? g.Photo : null;
                    IsVerified = (g != null && g.Verified == 1);
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
                if (c.CurrentKeyboard != null && c.CurrentKeyboard.Buttons.Count > 0) CurrentKeyboard = c.CurrentKeyboard;
                if (c.Mentions != null) Mentions = new ObservableCollection<int>(c.Mentions);
                UnreadMessagesCount = c.UnreadCount;
                IsMarkedUnread = c.IsMarkedUnread;
                IsArchived = c.IsArchived;
                Style = c.Style;
            } catch (Exception ex) { // Mitigating app crash without any info on screen that appears on some users...
                Log.Error($"Failed to initialize an instance of class LConversation for peer {Id}! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                Title = Title ?? Id.ToString();
            }
        }
    }
}