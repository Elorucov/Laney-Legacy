using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Elorucov.Laney.Models {
    public enum SentMessageState {
        Loading, Unread, Read, Error, Deleted
    }

    public enum LMessageType {
        VKMessage, VKAction
    }

    public class LMessage : BaseViewModel, IComparable {
        // private Message _fromAPI;
        private LMessageType _type;
        private int _id;
        private int _conversationMessageId;
        private DateTime _date;
        private DateTime? _updateTime;
        private DateTime? _pinnedAt;
        private long _peerId;
        private long _fromId;
        private bool _isUser = false;
        private long _senderId;
        private string _senderName;
        private Uri _senderAvatar;
        private string _text;
        private List<Attachment> _attachments = new List<Attachment>();
        private Geo _geo;
        private BotKeyboard _keyboard;
        private bool _isImportant;
        private VkAPI.Objects.Action _action;
        private List<LMessage> _forwardedMessages = new List<LMessage>();
        private LMessage _replyMessage;
        private BotTemplate _template;
        private int _ttl;
        private bool _isExpired;
        private bool _hasOnlyEmojis;
        private bool _isUnavailable;
        private int _selectedReactionId;
        private ObservableCollection<Reaction> _reactions = new ObservableCollection<Reaction>();
        private MessageFormatData _formatData;
        private SentMessageState _uiSentMessageState;

        public LMessageType Type { get { return _type; } private set { _type = value; OnPropertyChanged(); } }
        public int Id { get { return _id; } set { _id = value; OnPropertyChanged(); } }
        public int ConversationMessageId { get { return _conversationMessageId; } set { _conversationMessageId = value; OnPropertyChanged(); } }
        public DateTime Date { get { return _date; } set { _date = value; OnPropertyChanged(); } }
        public DateTime? UpdateTime { get { return _updateTime; } set { _updateTime = value; OnPropertyChanged(); } }
        public DateTime? PinnedAt { get { return _pinnedAt; } set { _pinnedAt = value; OnPropertyChanged(); } }
        public long PeerId { get { return _peerId; } set { _peerId = value; OnPropertyChanged(); } }
        public long FromId { get { return _fromId; } set { _fromId = value; OnPropertyChanged(); } }
        public bool IsUser { get { return _isUser; } }
        public long SenderId { get { return _senderId; } set { _senderId = value; OnPropertyChanged(); } }
        public string SenderName { get { return _senderName; } set { _senderName = value; OnPropertyChanged(); } }
        public Uri SenderAvatar { get { return _senderAvatar; } set { _senderAvatar = value; OnPropertyChanged(); } }
        public string Text { get { return _text; } set { _text = value; OnPropertyChanged(); } }
        public List<Attachment> Attachments { get { return _attachments; } set { _attachments = value; OnPropertyChanged(); } }
        public Geo Geo { get { return _geo; } set { _geo = value; OnPropertyChanged(); } }
        public BotKeyboard Keyboard { get { return _keyboard; } set { _keyboard = value; OnPropertyChanged(); } }
        public bool IsImportant { get { return _isImportant; } set { _isImportant = value; OnPropertyChanged(); } }
        public VkAPI.Objects.Action Action { get { return _action; } set { _action = value; OnPropertyChanged(); } }
        public List<LMessage> ForwardedMessages { get { return _forwardedMessages; } set { _forwardedMessages = value; OnPropertyChanged(); } }
        public LMessage ReplyMessage { get { return _replyMessage; } set { _replyMessage = value; OnPropertyChanged(); } }
        public BotTemplate Template { get { return _template; } set { _template = value; OnPropertyChanged(); } }
        public int TTL { get { return _ttl; } set { _ttl = value; OnPropertyChanged(); } }
        public bool IsExpired { get { return _isExpired; } set { _isExpired = value; OnPropertyChanged(); } }
        public bool HasOnlyEmojis { get { return _hasOnlyEmojis; } private set { _hasOnlyEmojis = value; OnPropertyChanged(); } }
        public bool IsUnavailable { get { return _isUnavailable; } private set { _isUnavailable = value; OnPropertyChanged(); } }
        public int SelectedReactionId { get { return _selectedReactionId; } set { _selectedReactionId = value; OnPropertyChanged(); } }
        public ObservableCollection<Reaction> Reactions { get { return _reactions; } private set { _reactions = value; OnPropertyChanged(); } }
        public MessageFormatData FormatData { get { return _formatData; } private set { _formatData = value; OnPropertyChanged(); } }
        public SentMessageState UISentMessageState { get { return _uiSentMessageState; } set { _uiSentMessageState = value; OnPropertyChanged(); } }
        public TextParsingResult ParsedTextInfo { get; private set; }

        public System.Action MessageEditedCallback;

        public LMessage(Message msg = null) {
            if (msg != null) Setup(msg);
        }

        public void Edit(Message msg) {
            Setup(msg);
            MessageEditedCallback?.Invoke();
        }

        public void NotifyReRender() {
            MessageEditedCallback?.Invoke();
        }

        private void Setup(Message msg) {
            SenderId = msg.FromId;
            if (SenderId.IsUser()) {
                var u = AppSession.GetCachedUser(msg.FromId);
                if (u != null) {
                    SenderName = $"{u.FirstName} {u.LastName}";
                    SenderAvatar = u.Photo;
                    _isUser = true;
                }
            } else if (SenderId.IsGroup()) {
                var g = AppSession.GetCachedGroup(-msg.FromId);
                if (g != null) {
                    SenderName = g.Name;
                    SenderAvatar = g.Photo;
                }
            }
            if (string.IsNullOrEmpty(SenderName)) SenderName = "...";

            Id = msg.Id;
            ConversationMessageId = msg.ConversationMessageId;
            Date = msg.DateTime;
            UpdateTime = msg.UpdateTimeUnix != 0 ? (DateTime?)msg.UpdateTime : null;
            PinnedAt = msg.PinnedAtUnix != 0 ? (DateTime?)msg.PinnedAt : null;
            PeerId = msg.PeerId;
            FromId = msg.FromId;
            Text = msg.Text;
            Geo = msg.Geo;
            Keyboard = msg.Keyboard;
            Attachments = msg.Attachments;
            IsImportant = msg.Important;
            Action = msg.Action;
            Template = msg.Template;
            TTL = Math.Max(msg.ExpireTTL, msg.TTL);
            IsExpired = msg.IsExpired;
            IsUnavailable = msg.IsUnavailable;
            FormatData = msg.FormatData;
            if (msg.ReplyMessage != null) ReplyMessage = new LMessage(msg.ReplyMessage);

            if (msg.ForwardedMessages != null) {
                ForwardedMessages.Clear();
                foreach (var fmsg in msg.ForwardedMessages) {
                    ForwardedMessages.Add(new LMessage(fmsg));
                }
            }

            HasOnlyEmojis = !string.IsNullOrEmpty(Text) && Functions.CheckEmoji(Text);

            SelectedReactionId = msg.ReactionId;
            if (msg.Reactions != null && msg.Reactions.Count > 0) {
                Reactions.Clear(); // на всякий случай
                foreach (var mr in msg.Reactions) {
                    List<Entity> members = new List<Entity>();
                    foreach (var mid in mr.UserIds) {
                        var info = AppSession.GetNameAndAvatar(mid);
                        if (info != null) {
                            members.Add(new Entity(mid, String.Join(" ", new string[] { info.Item1, info.Item2 }), null, info.Item3));
                        } else {
                            Log.Warn($"Member {mid} is not found in cache!");
                        }
                    }

                    Reaction reaction = new Reaction(mr.ReactionId);
                    reaction.Count = mr.Count;
                    reaction.Members = mr.UserIds;
                    Reactions.Add(reaction);
                }
            }

            // Action message
            if (Action != null) {
                Type = LMessageType.VKAction;
                Action.FromId = FromId;
            } else if (!string.IsNullOrEmpty(Text)) {
                ParsedTextInfo = VKTextParser.ParseText(Text, FormatData);
            }

            if (msg.IsPartial) UISentMessageState = SentMessageState.Loading;
        }

        public int CompareTo(object obj) {
            LMessage mvm = obj as LMessage;
            return ConversationMessageId.CompareTo(mvm.ConversationMessageId);
        }

        public override string ToString() {
            if (Action != null) return APIHelper.GetActionMessageInfo(Action);
            if (IsExpired) return Locale.Get("msg_disappeared_title");
            if (!string.IsNullOrEmpty(Text)) return ParsedTextInfo.PlainText;
            if (_attachments?.Count > 0) {
                int count = _attachments.Count;
                if (_attachments.All(a => a.Type == _attachments[0].Type) && Geo == null) {
                    string type = _attachments[0].TypeString;
                    switch (_attachments[0].Type) {
                        case AttachmentType.Audio:
                        case AttachmentType.AudioMessage:
                        case AttachmentType.AudioPlaylist:
                        case AttachmentType.Document:
                        case AttachmentType.Photo:
                        case AttachmentType.Video:
                        case AttachmentType.Market:
                        case AttachmentType.MiniApp:
                        case AttachmentType.Widget: return $"{count} {Locale.GetDeclension(count, $"atch_{type}")}";
                        case AttachmentType.Call:
                        case AttachmentType.Curator:
                        case AttachmentType.Event:
                        case AttachmentType.Gift:
                        case AttachmentType.Graffiti:
                        case AttachmentType.GroupCallInProgress:
                        case AttachmentType.Link:
                        case AttachmentType.Narrative:
                        case AttachmentType.Podcast:
                        case AttachmentType.Poll:
                        case AttachmentType.Sticker:
                        case AttachmentType.UGCSticker:
                        case AttachmentType.Story:
                        case AttachmentType.Wall:
                        case AttachmentType.WallReply:
                        case AttachmentType.Artist:
                        case AttachmentType.Article:
                        case AttachmentType.VideoMessage:
                        case AttachmentType.MoneyRequest:
                        case AttachmentType.MoneyTransfer: return Locale.Get($"atch_{type}").Capitalize();
                        default: return $"{count} {Locale.GetDeclension(count, "attachment").ToLower()}";
                    }
                } else {
                    if (Geo != null && count > 0) count++;
                    return $"{count} {Locale.GetDeclension(count, "attachment").ToLower()}";
                }
            }
            if (Geo != null) return Locale.Get("atch_geo").Capitalize();
            if (_forwardedMessages?.Count > 0) {
                int c = _forwardedMessages.Count;
                return $"{c} {Locale.GetDeclension(c, "forwarded_msgs_link").ToLower()}";
            }

            return Locale.Get("empty_message");
        }

        #region Sample message

        public static LMessage GetSampleMessage(int id, bool isOutgoing, string message, bool hasPhoto = false, bool hasReactions = false) {
            Attachment p1 = new Attachment {
                TypeString = "photo",
                Photo = new Photo {
                    AlbumId = -3,
                    DateUnix = 1718795054,
                    Id = 457273447,
                    OwnerId = 172894294,
                    Text = "Laney has no easter eggs...",
                    Sizes = new List<PhotoSizes> {
                        new PhotoSizes {
                            Height = 538,
                            Type = "y",
                            Width = 807,
                            Url = "https://sun9-63.userapi.com/s/v1/ig2/8DjlT5NOTYXgNsShcEjrvnY3jcuuEuDch7sqk64-ayKGtRQ_pdxX2HmclCTD-z4vuI6JgmBNhMevZXl3Q3vrSmhP.jpg?quality=96&as=32x21,48x32,72x48,108x72,160x107,240x160,360x240,480x320,540x360,640x427,720x480,1080x720,1280x853,1440x960,2560x1707&from=bu&cs=807x538"
                        },
                        new PhotoSizes {
                            Height = 1707,
                            Type = "w",
                            Width = 2560,
                            Url = "https://sun9-63.userapi.com/s/v1/ig2/8DjlT5NOTYXgNsShcEjrvnY3jcuuEuDch7sqk64-ayKGtRQ_pdxX2HmclCTD-z4vuI6JgmBNhMevZXl3Q3vrSmhP.jpg?quality=96&as=32x21,48x32,72x48,108x72,160x107,240x160,360x240,480x320,540x360,640x427,720x480,1080x720,1280x853,1440x960,2560x1707&from=bu&cs=2560x1707"
                        }
                    }
                }
            };
            Attachment p2 = new Attachment {
                TypeString = "photo",
                Photo = new Photo {
                    AlbumId = -3,
                    DateUnix = 1718795060,
                    Id = 457273449,
                    OwnerId = 172894294,
                    Text = "Lorem ipsum dolor sit amet...",
                    Sizes = new List<PhotoSizes> {
                        new PhotoSizes {
                            Height = 807,
                            Type = "y",
                            Width = 454,
                            Url = "https://sun9-63.userapi.com/s/v1/ig2/Nxv89Y-YAK0xYQz1NDu4qNFC25PsSUoX9xH2SgksJPErPkZYXtb6zM1Y4LhiXelpf3Mm_awxF3n9GZjhbV7iRMUe.jpg?quality=96&as=32x57,48x85,72x128,108x192,160x284,240x427,360x640,480x853,540x960,640x1138,720x1280,1080x1920&from=bu&cs=454x807"
                        },
                        new PhotoSizes {
                            Height = 1920,
                            Type = "w",
                            Width = 1080,
                            Url = "https://sun9-63.userapi.com/s/v1/ig2/Nxv89Y-YAK0xYQz1NDu4qNFC25PsSUoX9xH2SgksJPErPkZYXtb6zM1Y4LhiXelpf3Mm_awxF3n9GZjhbV7iRMUe.jpg?quality=96&as=32x57,48x85,72x128,108x192,160x284,240x427,360x640,480x853,540x960,640x1138,720x1280,1080x1920&from=bu&cs=1080x1920"
                        }
                    }
                }
            };

            return new LMessage {
                Id = id,
                ConversationMessageId = id,
                PeerId = 2000999999,
                SenderId = isOutgoing ? AppParameters.UserID : 3,
                SenderName = isOutgoing ? "Продвинутый пользователь ЭВМ" : "Elchin Orujov",
                SenderAvatar = new Uri("https://sun91-2.userapi.com/s/v1/ig2/73U16T9XhAN1QxGBZVtU-RRRKOTbR8J1557k3iA6D3Nqh_4NrAbH75_sFwJfA0-QWGGmxTWEEvxtvkxDtUinxgO8.jpg?size=100x100&quality=96&crop=240,0,1182,1182&ava=1"),
                Text = message,
                ParsedTextInfo = VKTextParser.ParseText(message, null),
                Date = DateTime.Now.Date.AddHours(8).AddMinutes(30).AddSeconds(id),
                UISentMessageState = SentMessageState.Read,
                Attachments = hasPhoto ? new List<Attachment> { p1, p2 } : new List<Attachment>(),
                SelectedReactionId = hasReactions ? 1 : 0,
                Reactions = hasReactions ? new ObservableCollection<Reaction> {
                    new Reaction(1) {
                        Count = 1,
                        Members = new List<long> { AppParameters.UserID  }
                    }
                } : new ObservableCollection<Reaction>()
            };
        }

        #endregion
    }
}