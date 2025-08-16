using ELOR.VKAPILib.Methods;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.VKAPIExecute;
using Elorucov.Laney.VKAPIExecute.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Helpers
{
    public class APIHelper
    {

        public static readonly List<string> Fields = new List<string>() { "photo_200", "photo_100", "photo_50",
            "ban_info", "blacklisted", "blacklisted_by_me", "can_message", "can_write_private_message", "friend_status",
            "is_messages_blocked", "online_info", "domain", "verified", "sex", "activity",
            "first_name_gen", "first_name_dat", "first_name_acc", "first_name_ins", "first_name_abl",
            "last_name_gen", "last_name_dat", "last_name_acc", "last_name_ins", "last_name_abl"
        };

        public static readonly List<string> UserFields = new List<string>() { "photo_200", "photo_100", "photo_50", "sex",
            "blacklisted", "blacklisted_by_me", "can_write_private_message", "friend_status", "online_info", "domain", "verified"
        };

        public static readonly List<string> GroupFields = new List<string>() { "photo_200", "photo_100", "photo_50", "description",
            "ban_info", "can_message", "is_messages_blocked", "domain", "activity", "status", "verified", "activity"
        };

        #region Errors

        public static string GetUnderstandableErrorMessage(int code)
        {
            return Locale.GetOrEmpty($"api_error_{code}");
        }

        public static string GetUnderstandableErrorMessage(APIException ex)
        {
            string uem = GetUnderstandableErrorMessage(ex.Code);
            return String.IsNullOrEmpty(uem) ? $"{ex.Message} (code: {ex.Code})" : uem;
        }

        public static string GetUnderstandableErrorMessage(int code, string message)
        {
            string uem = GetUnderstandableErrorMessage(code);
            return String.IsNullOrEmpty(uem) ? $"{message} (code: {code})" : uem;
        }

        #endregion

        public static string GetAttachmentsString(IEnumerable<Attachment> attachments)
        {
            string str = "";
            if (attachments != null && attachments.Count() > 0)
            {
                int audc = 0; int aumc = 0; int podc = 0; int calc = 0; int gcpc = 0; int docc = 0; int gifc = 0; int graf = 0; int polc = 0; int linc = 0; int marc = 0; int phoc = 0; int stic = 0; int vidc = 0; int walc = 0; int wrpc = 0; int stry = 0;
                foreach (var att in attachments)
                {
                    switch (att.Type)
                    {
                        case AttachmentType.Audio: audc++; break;
                        case AttachmentType.AudioMessage: aumc++; break;
                        case AttachmentType.Podcast: podc++; break;
                        case AttachmentType.Call: calc++; break;
                        case AttachmentType.GroupCallInProgress: gcpc++; break;
                        case AttachmentType.Document: docc++; break;
                        case AttachmentType.Gift: gifc++; break;
                        case AttachmentType.Graffiti: graf++; break;
                        case AttachmentType.Link: linc++; break;
                        case AttachmentType.Poll: polc++; break;
                        case AttachmentType.Market: marc++; break;
                        case AttachmentType.Photo: phoc++; break;
                        case AttachmentType.Sticker: stic++; break;
                        case AttachmentType.Video: vidc++; break;
                        case AttachmentType.Wall: walc++; break;
                        case AttachmentType.WallReply: wrpc++; break;
                        case AttachmentType.Story: stry++; break;
                    }
                }

                if (audc > 0) str += $"{audc} {Locale.GetDeclension(audc, "msg_attachment_audio")}, ";
                if (aumc > 0) str += $"{aumc} {Locale.GetDeclension(aumc, "msg_attachment_audiomsg")}, ";
                if (podc > 0) str += $"{aumc} {Locale.GetDeclension(aumc, "msg_attachment_podcast")}, ";
                if (calc > 0) str += $"{Locale.Get("msg_attachment_call")}, ";
                if (gcpc > 0) str += $"{Locale.Get("msg_attachment_group_call_in_progress")}, ";
                if (docc > 0) str += $"{docc} {Locale.GetDeclension(docc, "msg_attachment_doc")}, ";
                if (polc > 0) str += $"{Locale.Get("msg_attachment_poll")}, ";
                if (gifc > 0) str += $"{Locale.Get("msg_attachment_gift")}, ";
                if (graf > 0) str += $"{Locale.Get("msg_attachment_graffiti")}, ";
                if (linc > 0) str += $"{Locale.Get("msg_attachment_link")}, ";
                if (marc > 0) str += $"{marc} {Locale.GetDeclension(marc, "msg_attachment_market")}, ";
                if (phoc > 0) str += $"{phoc} {Locale.GetDeclension(phoc, "msg_attachment_photo")}, ";
                if (stic > 0) str += $"{Locale.Get("msg_attachment_sticker")}, ";
                if (vidc > 0) str += $"{vidc} {Locale.GetDeclension(vidc, "msg_attachment_video")}, ";
                if (walc > 0) str += $"{Locale.Get("msg_attachment_wall")}, ";
                if (wrpc > 0) str += $"{Locale.Get("msg_attachment_wallreply")}, ";
                if (stry > 0) str += $"{Locale.Get("msg_attachment_story")}, ";
            }
            return str;
        }

        public static string GetOnlineInfoString(UserOnlineInfo o, Sex sex)
        {
            string result = String.Empty;
            if (o == null) return result;
            string s = sex == Sex.Female ? "_f" : "_m";
            if (o.Visible)
            {
                if (o.isOnline)
                {
                    if (o.AppId > 0)
                    {
                        result = String.Format(Locale.GetForFormat("online_via"), o.AppId);
                    }
                    else
                    {
                        result = Locale.Get("online");
                    }
                }
                else
                {
                    result = o.LastSeen > new DateTime(2007, 1, 1) ?
                        String.Format(Locale.GetForFormat($"offline_last_seen{s}"), o.LastSeen.ToTimeAndDate()) :
                        Locale.Get("offline");
                }
            }
            else
            {
                result = Locale.Get($"offline{s}_{o.Status.ToEnumMemberAttrValue()}");
            }
            return result;
        }

        public static string GetOnlineInfoString(UserOnlineInfoEx o, Sex sex)
        {
            string result = String.Empty;
            if (o == null) return result;
            string s = sex == Sex.Female ? "_f" : "_m";
            if (o.Visible)
            {
                if (o.isOnline)
                {
                    if (o.AppId > 0)
                    {
                        result = String.Format(Locale.GetForFormat("online_via"), o.AppName);
                    }
                    else
                    {
                        result = Locale.Get("online");
                    }
                }
                else
                {
                    result = o.LastSeen > new DateTime(2007, 1, 1) ?
                        String.Format(Locale.GetForFormat($"offline_last_seen{s}"), o.LastSeen.ToTimeAndDate()) :
                        Locale.Get("offline");
                }
            }
            else
            {
                result = Locale.Get($"offline{s}_{o.Status.ToEnumMemberAttrValue()}");
            }
            return result;
        }

        public static string GetNormalizedBirthDate(string bdate)
        {
            if (String.IsNullOrEmpty(bdate)) return String.Empty;
            string[] a = bdate.Split('.');
            DateTime dt = a.Length == 3 ? new DateTime(Int32.Parse(a[2]), Int32.Parse(a[1]), Int32.Parse(a[0])) : new DateTime(1604, Int32.Parse(a[1]), Int32.Parse(a[0]));
            return a.Length == 3 ? $"{dt.ToString("M")} {dt.Year}" : dt.ToString("M");
        }

        public static string GetNameFromId(int id)
        {
            string n = "Unknown";
            try
            {
                if (id > 0)
                {
                    User u = CacheManager.GetUser(id);
                    n = u.FullName;
                }
                else if (id < 0)
                {
                    Group g = CacheManager.GetGroup(id);
                    n = g.Name;
                }
            }
            catch { }
            return n;
        }

        public static Sex GetSexFromId(int id)
        {
            try
            {
                if (id > 0)
                {
                    User u = CacheManager.GetUser(id);
                    return u.Sex;
                }
                else if (id < 0)
                {
                    return Sex.Train;
                }
            }
            catch { }
            return Sex.Train;
        }

        public static SolidColorBrush GetDocumentIconBackground(DocumentType type)
        {
            switch (type)
            {
                case DocumentType.Text: return new SolidColorBrush(Color.FromArgb(255, 0, 122, 204));
                case DocumentType.Archive: return new SolidColorBrush(Color.FromArgb(255, 118, 185, 121));
                case DocumentType.GIF: return new SolidColorBrush(Color.FromArgb(255, 119, 165, 214));
                case DocumentType.Image: return new SolidColorBrush(Color.FromArgb(255, 119, 165, 214));
                case DocumentType.Audio: return new SolidColorBrush(Color.FromArgb(255, 186, 104, 200));
                case DocumentType.Video: return new SolidColorBrush(Color.FromArgb(255, 229, 115, 155));
                case DocumentType.EBook: return new SolidColorBrush(Color.FromArgb(255, 255, 174, 56));
                default: return new SolidColorBrush(Color.FromArgb(255, 119, 165, 214));
            }
        }

        public static DataTemplate GetDocumentIcon(DocumentType type)
        {
            switch (type)
            {
                case DocumentType.Text: return (DataTemplate)Application.Current.Resources["Icon28ArticleOutline"];
                case DocumentType.Archive: return (DataTemplate)Application.Current.Resources["Icon28CubeBoxOutline"];
                case DocumentType.GIF: return (DataTemplate)Application.Current.Resources["Icon28PictureOutline"];
                case DocumentType.Image: return (DataTemplate)Application.Current.Resources["Icon28PictureOutline"];
                case DocumentType.Audio: return (DataTemplate)Application.Current.Resources["Icon28MusicOutline"];
                case DocumentType.Video: return (DataTemplate)Application.Current.Resources["Icon28VideoOutline"];
                case DocumentType.EBook: return (DataTemplate)Application.Current.Resources["Icon28ArticleOutline"];
                default: return (DataTemplate)Application.Current.Resources["Icon28DocumentOutline"];
            }
        }

        public static bool IsPinnedConversation(Conversation conv)
        {
            return conv.SortId.MajorId != 0 && conv.SortId.MajorId % 16 == 0;
        }

        #region Context menu actions for conversation

        public static async void ChangeConversationNotification(int peerId, bool isEnabled)
        {
            int time = isEnabled ? 0 : -1;
            try
            {
                await VKSession.Current.API.Account.SetSilenceModeAsync(time, peerId, isEnabled);
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) ChangeConversationNotification(peerId, isEnabled);
            }
        }

        public static async Task<bool> DeleteConversationAsync(int peerId)
        {
            Alert alert = new Alert
            {
                Header = Locale.Get("delete_conversation_title"),
                Text = Locale.Get("delete_conversation_description"),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                try
                {
                    await VKSession.Current.API.Messages.DeleteConversationAsync(peerId);
                    return true;
                }
                catch (Exception ex)
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                    {
                        return await DeleteConversationAsync(peerId);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public static async Task<bool> LeaveFromChatAsync(int chatId, bool isChannel)
        {
            string type = isChannel ? "unsubscribe" : "leave";
            Alert alert = new Alert
            {
                Header = Locale.Get($"chatinfo_{type}"),
                Text = Locale.Get($"chatinfo_{type}_confirm"),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                try
                {
                    await VKSession.Current.API.Messages.RemoveChatUserAsync(chatId, VKSession.Current.SessionId);
                    return true;
                }
                catch (Exception ex)
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                    {
                        return await LeaveFromChatAsync(chatId, isChannel);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public static async Task<bool> LeaveAndDeleteChatAsync(int peerId)
        {
            Alert alert = new Alert
            {
                Header = Locale.Get("leave_and_delete"),
                Text = Locale.Get("leave_and_delete_confirm"),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                try
                {
                    await (VKSession.Current.API.Execute as Execute).LeaveAndDeleteChatAsync(peerId);
                    return true;
                }
                catch (Exception ex)
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                    {
                        return await LeaveAndDeleteChatAsync(peerId);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public static async Task<bool> DeleteConvAndDenyGroupAsync(int groupId)
        {
            Alert alert = new Alert
            {
                Header = Locale.Get("delete_conversation_title"),
                Text = Locale.Get("delete_conversation_description"),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                try
                {
                    await (VKSession.Current.API.Execute as Execute).DeleteConvAndDenyGroupAsync(groupId);
                    return true;
                }
                catch (Exception ex)
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                    {
                        return await DeleteConvAndDenyGroupAsync(groupId);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        #endregion

        #region Typing and other activities

        static Dictionary<int, Tuple<ActivityType, DateTime>> activities = new Dictionary<int, Tuple<ActivityType, DateTime>>();

        public static async void SendActivity(ActivityType type, int peerId)
        {
            if (Settings.DontSendActivity || peerId == 0) return;

            if (activities.ContainsKey(peerId))
            {
                var activity = activities[peerId];
                bool cantSend = true;

                var sec = (DateTime.Now - activity.Item2).TotalSeconds;
                cantSend = activity.Item1 == type && sec <= 5;
                Debug.WriteLine($"SendActivity — diff: {sec}s., peer: {peerId}, prev: {activity.Item1}, type: {type}, break: {cantSend}");

                if (!cantSend)
                {
                    activities.Remove(peerId);
                    activities.Add(peerId, new Tuple<ActivityType, DateTime>(type, DateTime.Now));
                    await VKSession.Current.API.Messages.SetActivityAsync(VKSession.Current.GroupId, peerId, type);
                }
            }
            else
            {
                activities.Add(peerId, new Tuple<ActivityType, DateTime>(type, DateTime.Now));
                await VKSession.Current.API.Messages.SetActivityAsync(VKSession.Current.GroupId, peerId, type);
            }
        }

        #endregion
    }
}