using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elorucov.Laney.ViewModels
{

    public enum MessageVMState
    {
        Sending, Unread, Read, Deleted, Failed
    }

    public enum MessageVMType
    {
        Message, Action
    }

    public enum SenderType
    {
        Unknown, User, Group
    }

    public class MessageViewModel : BaseViewModel, IComparable
    {
        private MessageVMType _type;
        private int _id;
        private int _randomId;
        private int _conversationMessageId;
        private DateTime _sentDateTime;
        private DateTime? _editDateTime;
        private bool _isImportant;
        private int _peerId;
        private int _adminAuthorId;
        private int _senderId;
        private SenderType _senderType;
        private string _senderName;
        private Uri _senderAvatar;
        private string _text;
        private ThreadSafeObservableCollection<Attachment> _attachments = new ThreadSafeObservableCollection<Attachment>();
        private Geo _location;
        private ThreadSafeObservableCollection<MessageViewModel> _forwardedMessages = new ThreadSafeObservableCollection<MessageViewModel>();
        private MessageViewModel _replyMessage;
        private ELOR.VKAPILib.Objects.Action _action;
        private BotKeyboard _keyboard;
        private BotTemplate _template;
        private string _payload;
        private int _ttl;
        private bool _isExpired;
        private MessageVMState _state;
        private Exception _exception;

        public MessageVMType Type { get { return _type; } private set { _type = value; OnPropertyChanged(); } }
        public int Id { get { return _id; } private set { _id = value; OnPropertyChanged(); } }
        public int RandomId { get { return _randomId; } private set { _randomId = value; OnPropertyChanged(); } }
        public int ConversationMessageId { get { return _conversationMessageId; } private set { _conversationMessageId = value; OnPropertyChanged(); } }
        public DateTime SentDateTime { get { return _sentDateTime; } private set { _sentDateTime = value; OnPropertyChanged(); } }
        public DateTime? EditDateTime { get { return _editDateTime; } private set { _editDateTime = value; OnPropertyChanged(); } }
        public bool IsImportant { get { return _isImportant; } set { _isImportant = value; OnPropertyChanged(); } }
        public int PeerId { get { return _peerId; } private set { _peerId = value; OnPropertyChanged(); } }
        public int AdminAuthorId { get { return _adminAuthorId; } private set { _adminAuthorId = value; OnPropertyChanged(); } }
        public int SenderId { get { return _senderId; } private set { _senderId = value; OnPropertyChanged(); } }
        public SenderType SenderType { get { return _senderType; } private set { _senderType = value; OnPropertyChanged(); } }
        public string SenderName { get { return _senderName; } private set { _senderName = value; OnPropertyChanged(); } }
        public Uri SenderAvatar { get { return _senderAvatar; } private set { _senderAvatar = value; OnPropertyChanged(); } }
        public string Text { get { return _text; } private set { _text = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<Attachment> Attachments { get { return _attachments; } private set { _attachments = value; OnPropertyChanged(); } }
        public Geo Location { get { return _location; } private set { _location = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<MessageViewModel> ForwardedMessages { get { return _forwardedMessages; } private set { _forwardedMessages = value; OnPropertyChanged(); } }
        public MessageViewModel ReplyMessage { get { return _replyMessage; } private set { _replyMessage = value; OnPropertyChanged(); } }
        public ELOR.VKAPILib.Objects.Action Action { get { return _action; } private set { _action = value; OnPropertyChanged(); } }
        public BotKeyboard Keyboard { get { return _keyboard; } private set { _keyboard = value; OnPropertyChanged(); } }
        public BotTemplate Template { get { return _template; } private set { _template = value; OnPropertyChanged(); } }
        public string Payload { get { return _payload; } private set { _payload = value; OnPropertyChanged(); } }
        public int TTL { get { return _ttl; } private set { _ttl = value; OnPropertyChanged(); } }
        public bool IsExpired { get { return _isExpired; } set { _isExpired = value; OnPropertyChanged(); } }
        public MessageVMState State { get { return _state; } set { _state = value; OnPropertyChanged(); } }
        public Exception Exception { get { return _exception; } private set { _exception = value; OnPropertyChanged(); } }

        public bool ForceDisplayAsOutgoing { get; private set; }
        public int ForwardedMessagesFromGroupId { get; private set; }
        private List<AttachmentBase> AttachmentsForSent = new List<AttachmentBase>();
        public System.Action NeedRedrawCallback;

        private MessageViewModel() { }

        public MessageViewModel(Message msg)
        {
            Setup(msg);
        }

        public void Edit(Message msg)
        {
            Setup(msg);
            NeedRedrawCallback?.Invoke();
        }

        private void Setup(Message msg)
        {
            Type = msg.Action != null ? MessageVMType.Action : MessageVMType.Message;
            Id = msg.Id;
            RandomId = msg.RandomId;
            ConversationMessageId = msg.ConversationMessageId;
            SentDateTime = msg.DateTime;
            EditDateTime = msg.UpdateTimeUnix != 0 ? (DateTime?)msg.UpdateTime : null;
            IsImportant = msg.Important;
            PeerId = msg.PeerId;
            AdminAuthorId = msg.AdminAuthorId;
            SenderId = msg.FromId;
            Text = msg.Text;
            Attachments = new ThreadSafeObservableCollection<Attachment>(msg.Attachments.AsEnumerable());
            Location = msg.Geo;
            if (msg.ReplyMessage != null) ReplyMessage = new MessageViewModel(msg.ReplyMessage);
            Action = msg.Action;
            Keyboard = msg.Keyboard;
            Template = msg.Template;
            Payload = msg.PayLoad;
            TTL = Math.Max(msg.ExpireTTL, msg.TTL);
            IsExpired = msg.IsExpired;

            if (msg.ForwardedMessages != null)
            {
                ForwardedMessages.Clear();
                foreach (var fmsg in msg.ForwardedMessages)
                {
                    ForwardedMessages.Add(new MessageViewModel(fmsg));
                }
            }

            SetSenderNameAndAvatar();
        }

        private void SetSenderNameAndAvatar()
        {
            if (SenderId > 0) SenderType = SenderType.User;
            if (SenderId < 0) SenderType = SenderType.Group;

            switch (SenderType)
            {
                case SenderType.User:
                    User u = CacheManager.GetUser(SenderId);
                    SenderName = u == null ? $"ID{SenderId}" : u.FullName;
                    if (u != null) SenderAvatar = u.Photo;
                    break;
                case SenderType.Group:
                    if (SenderId < -2000000000)
                    {
                        SenderName = "E-Mail";
                        SenderAvatar = null;
                    }
                    else
                    {
                        Group g = CacheManager.GetGroup(SenderId);
                        SenderName = g == null ? $"Group{SenderId}" : g.Name;
                        if (g != null) SenderAvatar = g.Photo;
                    }
                    break;
            }
        }

        public async void SendOrEditMessage()
        {
            bool isEdit = Id != Int32.MaxValue;

            MessageVMState prevState = State;
            string text = !String.IsNullOrEmpty(Text) ? Text.Replace("\r\n", "\r").Replace("\r", "\r\n").Trim() : null;

            int reply = ReplyMessage != null ? ReplyMessage.Id : 0;

            List<string> attachments = new List<string>();
            if (AttachmentsForSent != null) attachments = AttachmentsForSent.Select(b => b.ToString()).ToList();
            var fwds = ForwardedMessages.Select(m => m.Id).ToList();

            List<string> gfwds = new List<string>();
            if (ForwardedMessagesFromGroupId > 0)
            {
                foreach (var m in ForwardedMessages)
                {
                    gfwds.Add($"-{ForwardedMessagesFromGroupId}_{m.Id}");
                }
                fwds = new List<int>();
            }

            double glat = Location != null ? Location.Coordinates.Latitude : 0;
            double glong = Location != null ? Location.Coordinates.Longitude : 0;

            var stickers = Attachments.Where(b => b.Type == AttachmentType.Sticker);
            int sticker = stickers.Count() == 1 ? stickers.First().Sticker.StickerId : 0;

            try
            {
                State = MessageVMState.Sending;
                if (!isEdit)
                {
                    await VKSession.Current.API.Messages.SendAsync(VKSession.Current.GroupId, PeerId, RandomId,
                        text, glat, glong, attachments, reply, fwds, gfwds, sticker, null, Payload);
                }
                else
                {
                    bool result = await VKSession.Current.API.Messages.EditAsync(VKSession.Current.GroupId, PeerId, Id, text,
                        glat, glong, attachments, true, true, false);
                    if (!result) State = MessageVMState.Failed;
                }
                AttachmentsForSent = null;
            }
            catch (Exception ex)
            {
                Log.General.Error($"Error sending/editing message! Id: {Id}", ex);
                Exception = ex;
                State = MessageVMState.Failed;
            }
        }

        public override string ToString()
        {
            string s = $"mid{Id} cid{ConversationMessageId} {SenderName}";
            return s;
        }

        public int CompareTo(object obj)
        {
            MessageViewModel mvm = obj as MessageViewModel;
            return Id.CompareTo(mvm.Id);
        }

        #region Static members

        private static Random Random = new Random();

        public static MessageViewModel Build(int id, DateTime sentTime, int peerId, MessageViewModel reply, string text, List<AttachmentBase> attachments, ThreadSafeObservableCollection<MessageViewModel> fwdmessages, Tuple<double, double> place, Sticker sticker = null, string payload = null, int fwdMsgFromGroupId = 0)
        {
            MessageViewModel mvm = new MessageViewModel();
            mvm.Id = id;
            mvm.SenderId = VKSession.Current.SessionId;
            if (VKSession.Current.Type == SessionType.VKGroup) mvm.AdminAuthorId = VKSession.Current.Id;
            mvm.Type = MessageVMType.Message;
            mvm.PeerId = peerId;
            mvm.SentDateTime = sentTime;
            if (id != Int32.MaxValue) mvm.EditDateTime = DateTime.Now;
            mvm.RandomId = Random.Next(1, Int32.MaxValue);
            mvm.ReplyMessage = reply;
            mvm.Text = text;
            if (fwdmessages != null) mvm.ForwardedMessages = fwdmessages;
            mvm.Payload = payload;

            if (place != null) mvm.Location = new Geo
            {
                Coordinates = new GeoCoordinates
                {
                    Latitude = place.Item1,
                    Longitude = place.Item2
                }
            };

            mvm.ForwardedMessagesFromGroupId = fwdMsgFromGroupId;

            mvm.AttachmentsForSent = attachments;
            mvm.Attachments = new ThreadSafeObservableCollection<Attachment>();
            if (attachments != null)
            {
                foreach (AttachmentBase ab in attachments)
                {
                    Attachment a = new Attachment();
                    switch (ab.GetType().Name)
                    {
                        case nameof(Photo): a.Photo = (Photo)ab; a.TypeString = "photo"; break;
                        case nameof(Video): a.Video = (Video)ab; a.TypeString = "video"; break;
                        case nameof(Document): a.Document = (Document)ab; a.TypeString = "doc"; break;
                        case nameof(Poll): a.Poll = (Poll)ab; a.TypeString = "poll"; break;
                        case nameof(AudioMessage): a.AudioMessage = (AudioMessage)ab; a.TypeString = "audio_message"; break;
                        case nameof(Audio): a.Audio = (Audio)ab; a.TypeString = "audio"; break;
                        case nameof(Graffiti): a.Graffiti = (Graffiti)ab; a.TypeString = "graffiti"; break;
                        case nameof(Story): a.Story = (Story)ab; a.TypeString = "story"; break;
                    }
                    mvm.Attachments.Add(a);
                }
            }

            if (sticker != null) mvm.Attachments.Add(new Attachment
            {
                TypeString = "sticker",
                Sticker = sticker
            });

            mvm.SetSenderNameAndAvatar();
            return mvm;
        }

        public static MessagesCollection GetSampleMessages()
        {
            List<MessageViewModel> messages = new List<MessageViewModel>();
            messages.Add(new MessageViewModel
            {
                ForceDisplayAsOutgoing = true,
                Id = 1,
                ConversationMessageId = 1,
                PeerId = 2000000001,
                SenderId = 999999992,
                SenderName = "エルキン オールジョブ",
                SenderType = SenderType.User,
                SentDateTime = new DateTime(2020, 09, 15, 13, 00, 00),
                Text = "Когда 2.0?",
                State = MessageVMState.Read
            });
            messages.Add(new MessageViewModel
            {
                Id = 2,
                ConversationMessageId = 2,
                PeerId = 2000000001,
                SenderId = 999999991,
                SenderName = "Григорий Клюшников",
                SenderAvatar = new Uri("https://sun1-20.userapi.com/impf/c840236/v840236023/829df/CVL3-pl_LUo.jpg?size=200x0&quality=88&crop=787,852,533,533&sign=b64c2f1010d0bb9f1bdb1ad8a3f94c6f&c_uniq_tag=vc1Au5UBVdZfaIMQ4Ll-x2PrNhYZtjldEnSHSp4BUpM&ava=1"),
                SenderType = SenderType.User,
                SentDateTime = new DateTime(2020, 09, 15, 13, 01, 00),
                Text = "Когда [id172894294|Эльчинк] доделает и оттестирует. 👌🏻",
                State = MessageVMState.Read
            });
            return new MessagesCollection(messages);
        }

        #endregion
    }
}