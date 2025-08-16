using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using System;
using System.Collections.Generic;

namespace Elorucov.Laney.DataModels
{
    public enum ActionObjectType { Member, ConversationMessage }

    public class ActionMessage
    {
        public int InitiatorId { get; private set; }
        public string InitiatorDisplayName { get; private set; }
        public string ActionText { get; private set; }
        public ActionObjectType ObjectType { get; private set; }
        public int ObjectId { get; private set; }
        public string ObjectDisplayName { get; private set; }
        public string MessageText { get; private set; }
        public string Suffix { get; private set; }

        public ActionMessage(ELOR.VKAPILib.Objects.Action act, int fromId = 0)
        {
            string actionerName = "";
            Sex actionerSex = Sex.Male;
            string memberName = "";
            string memberNameGen = "";
            Sex memberSex = Sex.Male;

            if (fromId == 0) fromId = act.FromId;

            if (fromId > 0)
            {
                User u = CacheManager.GetUser(fromId);
                actionerName = u.FullName;
                actionerSex = u.Sex;
            }
            else if (fromId < 0)
            {
                Group g = CacheManager.GetGroup(fromId);
                actionerName = g.Name;
            }

            if (act.MemberId != 0) ObjectId = act.MemberId;
            if (act.MemberId > 0)
            {
                User u = CacheManager.GetUser(act.MemberId);
                memberName = u.FullName;
                memberNameGen = $"{u.FirstNameAcc} {u.LastNameAcc}";
                memberSex = u.Sex;
            }
            else if (act.MemberId < 0)
            {
                Group g = CacheManager.GetGroup(act.MemberId);
                memberName = g.Name;
                memberNameGen = g.Name;
            }
            else if (act.MemberId == 0 && act.ConversationMessageId > 0)
            {
                ObjectType = ActionObjectType.ConversationMessage;
                ObjectId = act.ConversationMessageId;
            }

            InitiatorId = fromId;
            InitiatorDisplayName = actionerName;

            string create = Locale.Get($"msg_action_create{actionerSex}");
            string invited = Locale.Get($"msg_action_invited{actionerSex}");
            string returned = Locale.Get($"msg_action_returnedToConv{actionerSex}");
            string invitedlink = Locale.Get($"msg_action_invitedByLink{actionerSex}");
            string left = Locale.Get($"msg_action_left{memberSex}");
            string kicked = Locale.Get($"msg_action_kick{actionerSex}");
            string photoupd = Locale.Get($"msg_action_photoUpdate{actionerSex}");
            string photorem = Locale.Get($"msg_action_photoRemove{actionerSex}");
            string pin = Locale.Get($"msg_action_pin{actionerSex}");
            string unpin = Locale.Get($"msg_action_unpin{actionerSex}");
            string rename = Locale.Get($"msg_action_rename{actionerSex}");
            string screenshot = Locale.Get($"msg_action_screenshot{actionerSex}");
            string acceptedmsgrequest = Locale.Get($"msg_action_acceptedMessageRequest{memberSex}");
            string inviteuserbycall = Locale.Get($"msg_action_inviteUserByCall{actionerSex}");
            string inviteuserbycalljoinlink = Locale.Get($"msg_action_inviteUserByCallJoinLink{actionerSex}");
            string inviteuserbycallsuffix = !String.IsNullOrWhiteSpace(Locale.Get($"msg_action_inviteUserByCall")) ? $" {Locale.Get($"msg_action_inviteUserByCall")}" : String.Empty;

            switch (act.Type)
            {
                case ActionType.ChatCreate:
                    ActionText = $"{create} \"{act.Text}\"";
                    break;
                case ActionType.ChatInviteUser:
                    ActionText = fromId == act.MemberId ? returned : invited;
                    if (fromId != act.MemberId)
                    {
                        ObjectId = act.MemberId;
                        ObjectDisplayName = memberNameGen;
                    }
                    break;
                case ActionType.ChatInviteUserByLink:
                    ActionText = invitedlink;
                    break;
                case ActionType.ChatKickUser:
                    ActionText = fromId == act.MemberId ? left : kicked;
                    if (fromId != act.MemberId)
                    {
                        ObjectId = act.MemberId;
                        ObjectDisplayName = memberNameGen;
                    }
                    break;
                case ActionType.ChatPhotoRemove: ActionText = photorem; break;
                case ActionType.ChatPhotoUpdate: ActionText = photoupd; break;
                case ActionType.ChatTitleUpdate: ActionText = $"{rename} \"{act.Text}\""; break;
                case ActionType.ChatPinMessage:
                    InitiatorId = act.MemberId;
                    InitiatorDisplayName = memberName;
                    ActionText = pin;
                    ObjectType = ActionObjectType.ConversationMessage;
                    ObjectId = act.ConversationMessageId;
                    ObjectDisplayName = Locale.Get("message").ToLower();
                    MessageText = act.Message;
                    break;
                case ActionType.ChatUnpinMessage:
                    InitiatorId = act.MemberId;
                    InitiatorDisplayName = memberName;
                    ActionText = unpin;
                    ObjectType = ActionObjectType.ConversationMessage;
                    ObjectId = act.ConversationMessageId;
                    ObjectDisplayName = Locale.Get("message").ToLower();
                    break;
                case ActionType.ChatScreenshot:
                    InitiatorId = act.MemberId;
                    InitiatorDisplayName = memberName;
                    ActionText = screenshot;
                    break;
                case ActionType.AcceptedMessageRequest:
                    ActionText = acceptedmsgrequest;
                    break;
                case ActionType.ChatInviteUserByCall:
                    ActionText = inviteuserbycall;
                    ObjectId = act.MemberId;
                    ObjectDisplayName = memberNameGen;
                    Suffix = inviteuserbycallsuffix;
                    break;
                case ActionType.ChatInviteUserByCallJoinLink:
                    ActionText = inviteuserbycalljoinlink;
                    break;
            }
        }

        public override string ToString()
        {
            return String.Join(" ", new List<string> { InitiatorDisplayName, ActionText, ObjectDisplayName }).Trim();
        }
    }
}
