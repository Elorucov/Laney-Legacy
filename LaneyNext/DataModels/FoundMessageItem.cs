using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;

namespace Elorucov.Laney.DataModels
{
    public class FoundMessageItem
    {
        public int Id { get; private set; }
        public int PeerId { get; private set; }
        public string PeerName { get; private set; }
        public Uri PeerAvatar { get; private set; }
        public string Text { get; private set; }
        public DateTime SentDate { get; private set; }

        public FoundMessageItem(Message message, Conversation conversation = null)
        {
            if (conversation.Peer.Type == PeerType.Chat)
            {
                PeerName = conversation.ChatSettings.Title;
                if (!String.IsNullOrEmpty(conversation.ChatSettings.Photo?.MediumUrl)) PeerAvatar = new Uri(conversation.ChatSettings.Photo.MediumUrl);

                string sname;
                if (message.FromId != VKSession.Current.SessionId)
                {
                    var senderInfo = CacheManager.GetNameAndAvatar(message.FromId);
                    sname = senderInfo.Item1;
                }
                else
                {
                    sname = Locale.Get("you");
                }
                Text = $"{sname}: {message.ToNormalString()}";

            }
            else
            {
                var info = CacheManager.GetNameAndAvatar(conversation.Peer.Id);
                PeerName = String.Join(" ", new string[] { info.Item1, info.Item2 });
                PeerAvatar = info.Item3;

                string youSign = message.FromId == VKSession.Current.SessionId ? $"{Locale.Get("you")}: " : String.Empty;
                Text = youSign + message.ToNormalString();
            }

            Id = message.Id;
            PeerId = message.PeerId;
            SentDate = message.DateTime;
        }
    }
}