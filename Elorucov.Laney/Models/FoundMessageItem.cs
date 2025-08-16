using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Objects;
using System;

namespace Elorucov.Laney.Models {
    public class FoundMessageItem {
        public int Id { get; private set; }
        public long PeerId { get; private set; }
        public string PeerName { get; private set; }
        public Uri PeerAvatar { get; private set; }
        public string Text { get; private set; }
        public DateTime SentDate { get; private set; }
        public string NormalizedLastMessageTime { get { return APIHelper.GetNormalizedTime(SentDate); } }

        public FoundMessageItem(Message message, Conversation conversation = null) {
            LMessage msg = new LMessage(message);
            if (conversation.Peer.Type == PeerType.Chat) {
                PeerName = conversation.ChatSettings.Title;
                if (!string.IsNullOrEmpty(conversation.ChatSettings.Photo?.Medium)) PeerAvatar = new Uri(conversation.ChatSettings.Photo.Medium);

                string sname;
                if (message.FromId != AppParameters.UserID) {
                    var senderInfo = AppSession.GetNameAndAvatar(message.FromId, true);
                    sname = String.Join(" ", new string[] { senderInfo.Item1, senderInfo.Item2 });
                } else {
                    sname = Locale.Get("you");
                }
                Text = $"{sname}: {msg.ToString()}";
            } else {
                var info = AppSession.GetNameAndAvatar(conversation.Peer.Id);
                PeerName = String.Join(" ", new string[] { info.Item1, info.Item2 });
                PeerAvatar = info.Item3;

                string youSign = message.FromId == AppParameters.UserID ? $"{Locale.Get("you")}: " : string.Empty;
                Text = youSign + msg.ToString();
            }

            Id = message.ConversationMessageId;
            PeerId = message.PeerId;
            SentDate = message.DateTime;
        }
    }
}