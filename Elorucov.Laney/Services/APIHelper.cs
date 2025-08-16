using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.PushNotifications;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Microsoft.QueryStringDotNET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Elorucov.Laney.Services {
    public class APIHelper {
        public static List<Uri> PlaceholderAvatars { get; private set; } = new List<Uri> {
            new Uri("https://vk.com/images/camera_50.png"),
            new Uri("https://vk.com/images/community_50.png"),
            new Uri("https://vk.com/images/icons/im_multichat_50.png"),
            new Uri("https://vk.com/images/camera_100.png"),
            new Uri("https://vk.com/images/community_100.png"),
            new Uri("https://vk.com/images/icons/im_multichat_100.png"),
            new Uri("https://vk.com/images/camera_200.png"),
            new Uri("https://vk.com/images/community_200.png"),
            new Uri("https://vk.com/images/icons/im_multichat_200.png"),
        };

        public static string GetOAuthUrl(int appId) {
            return $"https://oauth.vk.com/authorize?client_id={appId}&display=mobile&redirect_uri=https://oauth.vk.com/blank.html&scope={AppParameters.Scope}&response_type=token&revoke=1&v={API.Version}";
        }

        public static async Task ShowAPIErrorDialogAsync(VKError resp, System.Action retryAction = null) {
            Logger.Log.Error($"API Error {resp.error_code} (dialog): {resp.error_msg}");
            MessageDialog dlg = new MessageDialog(!string.IsNullOrEmpty(resp.error_text) ? resp.error_text : resp.error_msg, $"{Locale.Get("api_error")} ({resp.error_code})");
            if (retryAction != null) dlg.Commands.Add(new Windows.UI.Popups.UICommand(Locale.Get("retry"), (a) => retryAction?.Invoke()));
            dlg.Commands.Add(new Windows.UI.Popups.UICommand(Locale.Get("close")));
            dlg.DefaultCommandIndex = 0;
            dlg.CancelCommandIndex = 1;
            var result = await dlg.ShowAsync();
        }

        public static void ShowAPIErrorTip(VKError resp, string additionalInfo = null) {
            string extra = !string.IsNullOrEmpty(additionalInfo) ? $"\n{additionalInfo}" : string.Empty;
            Logger.Log.Error($"API Error {resp.error_code} (tip): {resp.error_msg}{extra}");
            string msg = !string.IsNullOrEmpty(resp.error_text) ? resp.error_text : resp.error_msg;
            Tips.Show($"{Locale.Get("api_error")} ({resp.error_code})", msg + extra);
        }

        public static string ConvertDateToVKFormat(DateTime d) {
            string ds = d.Day <= 9 ? $"0{d.Day}" : d.Day.ToString();
            string ms = d.Month <= 9 ? $"0{d.Month}" : d.Month.ToString();
            return $"{ds}{ms}{d.Year}";
        }

        public static string GetNormalizedTime(DateTime dt, bool showTime = false) {
            if (dt.Year < 2006) return string.Empty;

            dt = dt.ToLocalTime();
            DateTime dn = DateTime.Now;

            DateTime dayn = dn.Date;
            DateTime daym = dt.Date;

            if (dayn == daym) {
                return dt.ToString("t");
            } else if (daym == dayn.AddDays(-1)) {
                return showTime ? $"{Locale.Get("msgprev_yesterday")} {dt.ToString("t")}" : Locale.Get("msgprev_yesterday");
            } else {
                string time = showTime ? $" {dt.ToString("t")}" : "";
                return (DateTime.Now.Year - dt.Year) < 1 ? $"{dt.ToString("d MMM")}{time}" : $"{dt.ToString("d MMM yyyy")}{time}";
            }
        }

        public static void SetOnlinePeriodically() {
            if (App.OnlineTimer == null) {
                int period = 1000 * 60 * 3;
                App.OnlineTimer = new Timer(async (o) => {
                    Logger.Log.Info($"Setting online...");
                    await Account.SetOnline();
                }, null, period, period);
            }
        }

        public static string GetNormalizedDate(DateTime dt) {
            dt = dt.ToLocalTime();
            DateTime dn = DateTime.Now;

            DateTime dayn = dn.Date;
            DateTime daym = dt.Date;

            if (dayn == daym) {
                return Locale.Get("msg_today");
            } else if (daym == dayn.AddDays(-1)) {
                return Locale.Get("msgprev_yesterday");
            } else {
                return ((DateTime.Now.Year - dt.Year) < 1 ? $"{dt.ToString("M")}" : $"{dt.ToString("M")} {dt.Year}").ToLower();
            }
        }

        public static string GetNormalizedBirthDate(string bdate) {
            string[] a = bdate.Split('.');
            DateTime dt = a.Length == 3 ? new DateTime(int.Parse(a[2]), int.Parse(a[1]), int.Parse(a[0])) : new DateTime(1604, int.Parse(a[1]), int.Parse(a[0]));
            return a.Length == 3 ? $"{dt.ToString("M")} {dt.Year}" : dt.ToString("M");
        }

        public static string GetActionMessageInfo(VkAPI.Objects.Action msg, bool fullLastName = false) {
            string str = $"{Locale.Get("unknown_attachment")}: {msg.Type}";
            string ActionUserName = "";
            string MemberName = "";
            string MemberNameGen = "";
            Sex ActionUserSex = Sex.Male;

            if (msg.FromId.IsUser()) {
                var u = AppSession.GetCachedUser(msg.FromId);
                if (u == null) {
                    ActionUserName = "...";
                    ActionUserSex = Sex.Male;
                } else {
                    string ln = fullLastName ? $" {u.LastName}" : string.Empty;
                    ActionUserName = $"{u.FirstName}{ln}";
                    ActionUserSex = u.Sex;
                }
            } else if (msg.FromId.IsGroup()) {
                var u = AppSession.GetCachedGroup(msg.FromId * -1);
                if (u == null) {
                    ActionUserName = "...";
                } else {
                    ActionUserName = u.Name;
                }
                ActionUserSex = Sex.Male;
            }

            if (msg.MemberId.IsUser()) {
                var u = AppSession.GetCachedUser(msg.MemberId);
                if (u == null) {
                    MemberName = "...";
                    MemberNameGen = "...";
                } else {
                    string ln = fullLastName ? $" {u.LastName}" : string.Empty;
                    string lng = fullLastName ? $" {u.LastNameAcc}" : string.Empty;
                    MemberName = $"{u.FirstName}{ln}";
                    MemberNameGen = $"{u.FirstNameAcc}{lng}";
                }
            } else if (msg.MemberId.IsGroup()) {
                var u = AppSession.GetCachedGroup(msg.MemberId * -1);
                if (u == null) {
                    MemberName = "...";
                    MemberNameGen = "...";
                } else {
                    MemberName = u.Name;
                    MemberNameGen = u.Name;
                }
            }

            if (ActionUserSex == Sex.No) ActionUserSex = Sex.Male;

            // Grammar
            string create = Locale.Get($"ActionCreate{ActionUserSex}");
            string invited = Locale.Get($"ActionInvited{ActionUserSex}");
            string returned = Locale.Get($"ActionReturnedToConv{ActionUserSex}");
            string invitedlink = Locale.Get($"ActionInvitedByLink{ActionUserSex}");
            string left = Locale.Get($"ActionLeft{ActionUserSex}");
            string kick = Locale.Get($"ActionKick{ActionUserSex}");
            string photoupd = Locale.Get($"ActionPhotoUpdate{ActionUserSex}");
            string photorem = Locale.Get($"ActionPhotoRemove{ActionUserSex}");
            string pin = Locale.Get($"ActionPin{ActionUserSex}");
            string unpin = Locale.Get($"ActionUnPin{ActionUserSex}");
            string rename = Locale.Get($"ActionRename{ActionUserSex}");
            string screenshot = Locale.Get($"ActionScreenshot{ActionUserSex}");
            string inviteuserbycall = Locale.Get($"ActionInviteUserByCall{ActionUserSex}");
            string inviteuserbycalljoinlink = Locale.Get($"ActionInviteUserByCallJoinLink{ActionUserSex}");
            string inviteuserbycallsuffix = !string.IsNullOrWhiteSpace(Locale.Get($"ActionInviteUserByCall")) ? $" {Locale.Get($"ActionInviteUserByCall")}" : string.Empty;
            string acceptedmessagerequest = Locale.Get($"ActionAcceptedMessageRequest{ActionUserSex}");
            string styleupdate = string.IsNullOrEmpty(msg.Style) ? Locale.Get($"ActionStyleReset{ActionUserSex}") : $"{Locale.Get($"ActionStyleUpdate{ActionUserSex}")} «{ChatThemeService.GetLocalizedStyleName(msg.Style)}»";

            string normalizedTitle = string.Empty;
            switch (msg.Type) {
                case "chat_create": normalizedTitle = Locale.Get("lang") == "en" ? $"\"{msg.Text}\"" : $"«{msg.Text}»"; break;
                case "chat_title_update": normalizedTitle = Locale.Get("lang") == "en" ? $"\"{msg.OldText}\" → \"{msg.Text}\"" : $"«{msg.OldText}» → «{msg.Text}»"; break;
            }

            // Messages
            switch (msg.Type) {
                case "chat_create": str = $"{ActionUserName} {create} {normalizedTitle}"; break;
                case "chat_invite_user": str = msg.FromId == msg.MemberId ? $"{MemberName} {returned}" : $"{ActionUserName} {invited} {MemberNameGen}"; break;
                case "chat_invite_user_by_link": str = $"{ActionUserName} {invitedlink}"; break;
                case "chat_kick_user": str = msg.FromId == msg.MemberId ? $"{MemberName} {left}" : $"{ActionUserName} {kick} {MemberNameGen}"; break;
                case "chat_photo_remove": str = $"{ActionUserName} {photorem}"; break;
                case "chat_photo_update": str = $"{ActionUserName} {photoupd}"; break;
                case "chat_pin_message": str = $"{MemberName} {pin}"; break;
                case "chat_title_update": str = $"{ActionUserName} {rename} {normalizedTitle}"; break;
                case "chat_unpin_message": str = $"{MemberName} {unpin}"; break;
                case "chat_screenshot": str = $"{MemberName} {screenshot}"; break;
                case "chat_invite_user_by_call": str = $"{ActionUserName} {inviteuserbycall} {MemberNameGen}{inviteuserbycallsuffix}"; break;
                case "chat_invite_user_by_call_join_link": str = $"{MemberName} {inviteuserbycalljoinlink}"; break;
                case "accepted_message_request": str = $"{MemberName} {acceptedmessagerequest}"; break;
                case "conversation_style_update": str = $"{ActionUserName} {styleupdate}"; break;
                case "custom": str = msg.Message; break;
            }

            return str;
        }

        public static FrameworkElement GetActionMessageInfoUI(VkAPI.Objects.Action msg, Photo p = null) {
            ContentControl cc = new ContentControl {
                Template = (ControlTemplate)Application.Current.Resources["ActionMessageTemplate"],
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            StackPanel sp = new StackPanel();
            TextBlock serviceTextBlock;

            string ActionUserName = "";
            long ActionUserId = msg.FromId;
            string MemberName = "";
            string MemberNameGen = "";
            long MemberId = 0;
            Sex ActionUserSex = Sex.Male;
            bool isNonUser = true;
            bool isNonUserFrom = true;

            if (msg.FromId.IsUser()) {
                var u = AppSession.GetCachedUser(msg.FromId);
                if (u == null) {
                    ActionUserName = "...";
                    ActionUserSex = Sex.Male;
                } else {
                    ActionUserName = $"{u.FirstName} {u.LastName}";
                    ActionUserSex = u.Sex;
                }
                isNonUserFrom = false;
            } else if (msg.FromId.IsGroup()) // Group
            {
                var u = AppSession.GetCachedGroup(msg.FromId * -1);
                if (u == null) {
                    ActionUserName = "...";
                    ActionUserSex = Sex.Male;
                } else {
                    ActionUserName = u.Name;
                }
            }

            MemberId = msg.MemberId;
            if (msg.MemberId.IsUser()) {
                var u = AppSession.GetCachedUser(msg.MemberId);
                if (u == null) {
                    MemberId = msg.MemberId;
                    MemberName = "...";
                    MemberNameGen = "...";
                } else {
                    MemberName = $"{u.FirstName} {u.LastName}";
                    MemberNameGen = $"{u.FirstNameAcc} {u.LastNameAcc}";
                    MemberId = u.Id;
                    isNonUser = false;
                }
            } else if (msg.MemberId.IsGroup()) {
                var g = AppSession.GetCachedGroup(msg.MemberId);
                if (g == null) {
                    MemberName = "...";
                    MemberNameGen = "...";
                } else {
                    MemberName = g.Name;
                    MemberNameGen = g.Name;
                }
            }

            if (ActionUserSex == Sex.No) ActionUserSex = Sex.Male;

            // Grammar
            string create = Locale.Get($"ActionCreate{ActionUserSex}");
            string invited = Locale.Get($"ActionInvited{ActionUserSex}");
            string returned = Locale.Get($"ActionReturnedToConv{ActionUserSex}");
            string invitedlink = Locale.Get($"ActionInvitedByLink{ActionUserSex}");
            string left = Locale.Get($"ActionLeft{ActionUserSex}");
            string kick = Locale.Get($"ActionKick{ActionUserSex}");
            string photoupd = Locale.Get($"ActionPhotoUpdate{ActionUserSex}");
            string photorem = Locale.Get($"ActionPhotoRemove{ActionUserSex}");
            string pin = Locale.Get($"ActionPin{ActionUserSex}");
            string unpin = Locale.Get($"ActionUnPin{ActionUserSex}");
            string rename = Locale.Get($"ActionRename2{ActionUserSex}");
            string screenshot = Locale.Get($"ActionScreenshot{ActionUserSex}");
            string inviteuserbycall = Locale.Get($"ActionInviteUserByCall{ActionUserSex}");
            string inviteuserbycalljoinlink = Locale.Get($"ActionInviteUserByCallJoinLink{ActionUserSex}");
            string acceptedmessagerequest = Locale.Get($"ActionAcceptedMessageRequest{ActionUserSex}");
            string styleupdate = string.IsNullOrEmpty(msg.Style) ? $"{Locale.Get($"ActionStyleReset{ActionUserSex}")}." : $"{Locale.Get($"ActionStyleUpdate{ActionUserSex}")} «{ChatThemeService.GetLocalizedStyleName(msg.Style)}».";

            var pinstrarr = pin.Split(' ').ToList();
            string pinstrend = pinstrarr.Last();
            pinstrarr.Remove(pinstrarr.Last());
            string pinstr = String.Join(" ", pinstrarr);

            string normalizedTitle = string.Empty;
            switch (msg.Type) {
                case "chat_create": normalizedTitle = Locale.Get("lang") == "en" ? $"\"{msg.Text}\"" : $"«{msg.Text}»"; break;
                case "chat_title_update": normalizedTitle = Locale.Get("lang") == "en" ? $"\"{msg.OldText}\" → \"{msg.Text}\"" : $"«{msg.OldText}» → «{msg.Text}»"; break;
            }

            // Messages
            switch (msg.Type) {
                case "chat_create": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, $"{create} {normalizedTitle}", null, 0, isNonUserFrom); break;
                case "chat_invite_user": serviceTextBlock = msg.FromId == msg.MemberId ? ActionMessageHelper.GenerateTextBlock(MemberName, MemberId, returned) : ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, invited, MemberNameGen, MemberId, isNonUserFrom, isNonUser); break;
                case "chat_invite_user_by_link": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, invitedlink); break;
                case "chat_kick_user": serviceTextBlock = msg.FromId == msg.MemberId ? ActionMessageHelper.GenerateTextBlock(MemberName, MemberId, left) : ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, kick, MemberNameGen, MemberId, isNonUserFrom, isNonUser); break;
                case "chat_photo_remove": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, photorem, null, 0, isNonUserFrom); break;
                case "chat_photo_update": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, photoupd, null, 0, isNonUserFrom); break;
                case "chat_pin_message": serviceTextBlock = ActionMessageHelper.GenerateTextBlockForPinnedMessage(MemberName, MemberId, pinstr, msg, pinstrend, isNonUserFrom); break;
                case "chat_title_update": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, $"{rename} {normalizedTitle}", null, 0, isNonUserFrom); break;
                case "chat_unpin_message": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(MemberName, MemberId, unpin, null, 0, isNonUserFrom); break;
                case "chat_screenshot": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(MemberName, MemberId, screenshot, null, 0, isNonUserFrom); break;
                case "chat_invite_user_by_call": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, inviteuserbycall, MemberNameGen, MemberId, isNonUserFrom, isNonUser, Locale.Get($"ActionInviteUserByCall")); break;
                case "chat_invite_user_by_call_join_link": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, inviteuserbycalljoinlink, null, 0, isNonUserFrom); break;
                case "accepted_message_request": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, acceptedmessagerequest, null, 0, isNonUserFrom); break;
                case "conversation_style_update": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(ActionUserName, ActionUserId, styleupdate, null, 0, isNonUserFrom, isNonUserFrom, null); break;
                case "custom": serviceTextBlock = ActionMessageHelper.GenerateTextBlock(msg.Message); break;
                default: serviceTextBlock = ActionMessageHelper.GenerateTextBlock($"{Locale.Get("unknown_attachment")}: {msg.Type}"); break;
            }

            cc.Content = serviceTextBlock;

            sp.Children.Add(cc);
            if (msg.Type == "chat_photo_update" && p != null) {
                sp.Children.Add(ActionMessageHelper.GetConversationAvatarThumbnail(p));
            }

            return sp;
        }

        public static async Task<bool> TryDeleteFolderAsync(Folder folder) {
            return await TryDeleteFolderAsync(folder.Id, folder.Name);
        }

        public static async Task<bool> TryDeleteFolderAsync(ConversationsFolder folder) {
            return await TryDeleteFolderAsync(folder.Id, folder.Name);
        }

        private static async Task<bool> TryDeleteFolderAsync(int folderId, string folderName) {
            ContentDialog dlg = new ContentDialog {
                Title = String.Format(Locale.GetForFormat("folder_delete_dialog_title"), folderName),
                Content = Locale.Get("folder_delete_dialog_content"),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no"),
                DefaultButton = ContentDialogButton.Secondary
            };
            if (await dlg.ShowAsync() == ContentDialogResult.Primary) {
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                var result = await ssp.ShowAsync(Messages.DeleteFolder(folderId));
                if (result is bool b && b) {
                    return true;
                } else {
                    Functions.ShowHandledErrorDialog(result);
                    return false;
                }
            }
            return false;
        }


        public static async Task AddConvToFolderAsync(long peerId, int folderId) {
            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            var response = await ssp.ShowAsync(Messages.UpdateFolder(folderId, null, new List<long> { peerId }, null));
            Functions.ShowHandledErrorDialog(response, async () => await AddConvToFolderAsync(peerId, folderId));
        }

        public static async Task RemoveConvFromFolderAsync(long peerId, int folderId) {
            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            var response = await ssp.ShowAsync(Messages.UpdateFolder(folderId, null, null, new List<long> { peerId }));
            Functions.ShowHandledErrorDialog(response, async () => await RemoveConvFromFolderAsync(peerId, folderId));
        }

        public static IFileUploader GetUploadMethod(string type, Uri uri, StorageFile file) {
            switch (AppParameters.FileUploaderProvider) {
                case 1: return new VKFileUploaderViaHttpClient(type, uri, file);
                case 2: return new VKFileUploader(type, uri, file);
                default: return GetDefaultFileUploader(type, uri, file);
            }
        }

        private static IFileUploader GetDefaultFileUploader(string type, Uri uri, StorageFile file) {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                return new VKFileUploader(type, uri, file);
            } else {
                return new VKFileUploaderViaHttpClient(type, uri, file);
            }
        }

        public static string GetFontIconForAttachment(string stype) {
            var type = Attachment.GetAttachmentEnum(stype);
            switch (type) {
                case AttachmentType.Album: return "";
                case AttachmentType.Article: return "";
                case AttachmentType.Artist: return "";
                case AttachmentType.Audio: return "";
                case AttachmentType.AudioMessage: return "";
                case AttachmentType.AudioPlaylist: return "";
                case AttachmentType.Call: return "";
                case AttachmentType.Curator: return "";
                case AttachmentType.Document: return "";
                case AttachmentType.DonutLink: return "";
                case AttachmentType.Event: return "";
                case AttachmentType.Gift: return "";
                case AttachmentType.Graffiti: return "";
                case AttachmentType.Link: return "";
                case AttachmentType.Market: return "";
                case AttachmentType.MiniApp: return "";
                case AttachmentType.MoneyRequest: return "";
                case AttachmentType.MoneyTransfer: return "";
                case AttachmentType.Narrative: return "";
                case AttachmentType.Photo: return "";
                case AttachmentType.Podcast: return "";
                case AttachmentType.Poll: return "";
                case AttachmentType.Sticker: return "";
                case AttachmentType.Story: return "";
                case AttachmentType.Textlive: return "";
                case AttachmentType.TextpostPublish: return "";
                case AttachmentType.UGCSticker: return "";
                case AttachmentType.Video: return "";
                case AttachmentType.VideoMessage: return "";
                case AttachmentType.Wall: return "";
                case AttachmentType.WallReply: return "";
                case AttachmentType.Widget: return "";
                default: return "";
            }
        }

        // Обратите внимание: не для всех типов вложений есть переводы с ключами _nom, _gen и _plu,
        // особенно это касается тех вложений, которые не возвращаются сторонним клиентам и не учитываются в статистике.
        public static string GetHumanReadableAttachmentName(string stype, int count) {
            string name = Locale.GetDeclension(count, $"atch_{stype}");
            if (name.Length == 0 || name[0] == '%') return stype;
            return name;
        }

        public static async Task Logout(VKSession switchToSession = null) {
            Logger.Log.Info($"Logging out.");

            await TitleAndStatusBar.SetTitleText();
            LNetExtensions.ClearImagesCache();
            AppParameters.AccessToken = null;
            AppParameters.WebToken = null;
            AppParameters.UserID = 0;
            AppParameters.Passcode = null;
            AppParameters.WindowsHelloInsteadPasscode = false;
            if (App.OnlineTimer != null) {
                App.OnlineTimer.Dispose();
                App.OnlineTimer = null;
            }
            API.Uninitialize();

            ClearWebViewCookies();

            AudioPlayerViewModel.CloseMainInstance();
            AudioPlayerViewModel.CloseVoiceMessageInstance();
            await MyPeople.ContactsPanel.ClearAsync();
            await VKNotificationHelper.DisconnectAsync();
            VKNotificationHelper.UnregisterBackgroundTask();
            LongPoll.LongPoll.Stop();
            LongPoll.VKQueue.Stop();
            ToastNotificationManager.GetDefault()?.History.Clear();
            BadgeUpdateManager.CreateBadgeUpdaterForApplication()?.Clear();
            SearchViewModel.ClearImportantConversations();

            foreach (var t in BackgroundTaskRegistration.AllTasks) {
                t.Value.Unregister(true);
            }

            AppSession.Clear();
            GC.Collect();

            Frame frm = Window.Current.Content as Frame;
            frm.Navigate(typeof(WelcomePage), switchToSession);
        }

        public static void ClearWebViewCookies() {
            HttpBaseProtocolFilter f = new HttpBaseProtocolFilter();
            HttpCookieCollection cv = f.CookieManager.GetCookies(new Uri("https://vk.com"));
            HttpCookieCollection cl = f.CookieManager.GetCookies(new Uri("https://login.vk.com"));
            HttpCookieCollection co = f.CookieManager.GetCookies(new Uri("https://oauth.vk.com"));
            cv.Concat(cl).Concat(co);

            foreach (HttpCookie hc in cv) {
                f.CookieManager.DeleteCookie(hc);
            }
        }

        public static Uri GetStickerUri(ISticker sticker, int size, bool isDarkForced = false) {
            Uri uri = null;

            int scale = 1;
            int rscale = (int)DisplayInformation.GetForCurrentView().ResolutionScale;
            if (rscale >= 125) scale = 2;
            size = size * scale;
            Debug.WriteLine($"GetStickerUri: scale: {scale}; rscale: {rscale}; size: {size}");

            string theme = isDarkForced || Theme.IsDarkTheme() ? "b" : "";

            if (sticker is Sticker st && st.IsPartial) {
                int[] sizes = new int[] { 64, 128, 256, 352, 512 };
                int ssize = 0;
                foreach (int s in sizes) {
                    ssize = s;
                    if (s >= size) break;
                }
                uri = new Uri($"https://vk.com/sticker/1-{st.StickerId}-{ssize}{theme}");
            } else {
                var imgs = sticker.Images;
                if (imgs == null || imgs.Count == 0) return null;
                if (sticker is Sticker s && (isDarkForced || Theme.IsDarkTheme())) imgs = s.ImagesWithBackground;
                var si = imgs.Where(i => i.Width >= size).FirstOrDefault();
                if (si == null) si = imgs.LastOrDefault();
                uri = si.Uri;
            }
            return uri;
        }

        public static async Task<Tuple<Uri, string>> GetOauthHashAsync() {
            Dictionary<string, string> headers = new Dictionary<string, string> {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36 Edg/115.0.1901.203" }
            };

            var resp = await LNet.GetAsync(new Uri(APIHelper.GetOAuthUrl(AppParameters.ApplicationID)), headers: headers);
            var q = QueryString.Parse(resp.RequestMessage.RequestUri.Query.Substring(1));
            if (q.Contains("return_auth_hash"))
                return new Tuple<Uri, string>(resp.RequestMessage.RequestUri, q["return_auth_hash"]);
            return null;
        }

        public static async Task<object> DoConnectCodeAuthAsync(string superAppToken, int appId, int oauthScope, string returnAuthHash) {
            var hr = await LNet.PostAsync(new Uri($"https://login.vk.com"), new Dictionary<string, string> {
                { "act", "connect_code_auth" },
                { "token", superAppToken },
                { "app_id", appId.ToString() },
                { "oauth_scope", oauthScope.ToString() },
                { "oauth_force_hash","1" },
                { "is_registration", "0" },
                { "oauth_response_type", "token" },
                { "is_oauth_migrated_flow", "1" },
                { "version", "1" },
                { "to", "aHR0cHM6Ly9vYXV0aC52ay5jb20vYmxhbmsuaHRtbA==" }
            }, new Dictionary<string, string> {
                { "Origin", $"https://id.vk.com" }
            });
            var result = await hr.Content.ReadAsStringAsync();

            JObject response = JObject.Parse(result);
            string type = response["type"].Value<string>();
            if (type == "okay") {
                var cookies = LNet.Cookies.GetCookies(new Uri("https://login.vk.com")).Cast<Cookie>();
                // var cookies = hr.Headers.Where(h => h.Key == "Set-Cookie").Select(h => h.Value).FirstOrDefault();
                return await DoConnectAuthAsync(cookies, appId, oauthScope, returnAuthHash);
            } else if (type == "error") {
                string errtext = response["error_text"].Value<string>();
                Logger.Log.Error($"DoConnectCodeAuthAsync: VK returned an error! {errtext}");
                await new MessageDialog($"Error from VK ID (connect_code_auth): {errtext}", Locale.Get("global_error")).ShowAsync();
            } else {
                await new MessageDialog($"Error from VK ID (connect_code_auth). Try again.\n\n{result}", Locale.Get("global_error")).ShowAsync();
            }
            return null;
        }

        private static async Task<object> DoConnectAuthAsync(IEnumerable<Cookie> cookies, int appId, int oauthScope, string returnAuthHash) {
            //CookieContainer cc = new CookieContainer();
            //foreach (var cookie in cookies) {
            //    string[] c = cookie.Split("=");

            //    cc.Add(new Cookie(cookie));
            //}

            string cookiestr = String.Join("\n", cookies);
            var hr = await LNet.PostAsync(new Uri($"https://login.vk.com"), new Dictionary<string, string> {
                { "act", "connect_internal" },
                { "app_id", appId.ToString() },
                { "uuid", "" },
                { "service_group","" },
                { "device_id", Functions.GetDeviceId() },
                { "oauth_version", "1" },
                { "version", "1" }
            }, new Dictionary<string, string> {
                { "Origin", $"https://id.vk.com" }
            });
            var result = await hr.Content.ReadAsStringAsync();

            JObject response = JObject.Parse(result);
            string type = response["type"].Value<string>();
            if (type == "okay") {
                var data = (JObject)response["data"];
                if (data.ContainsKey("access_token") && data.ContainsKey("auth_user_hash")) {
                    string token = data["access_token"].Value<string>();
                    string authUserHash = data["auth_user_hash"].Value<string>();

                    return await Auth.GetOauthToken(token, appId, oauthScope, returnAuthHash, authUserHash);
                } else {
                    Logger.Log.Error($"DoConnectAuthAsync: Cannot get access_token and auth_user_hash!");
                    await new MessageDialog($"Error from VK ID (connect_internal): Cannot get access_token and auth_user_hash!", Locale.Get("global_error")).ShowAsync();
                }
            } else if (type == "error") {
                string errtext = response["error_text"].Value<string>();
                Logger.Log.Error($"DoConnectAuthAsync: VK returned an error! {errtext}");
                await new MessageDialog($"Error from VK ID (connect_internal): {errtext}", Locale.Get("global_error")).ShowAsync();
            } else {
                await new MessageDialog($"Error from VK ID (connect_internal). Try again.\n\n{result}", Locale.Get("global_error")).ShowAsync();
            }
            return null;
        }

        public static async Task SaveRefreshedTokenAsync(bool isSuccess, string token, long expiresIn) {
            Logger.Log.Info($"Refresh token result: {isSuccess}");
            if (isSuccess) {
                AppParameters.WebToken = token;
                AppParameters.WebTokenExpires = DateTimeOffset.Now.ToUnixTimeSeconds() + expiresIn;
                try {
                    await Task.Delay(3000); // required
                    var session = new VKSession(AppParameters.UserID, AppParameters.AccessToken, token, AppParameters.WebTokenExpires, AppParameters.ExchangeToken, AppParameters.UserName, AppParameters.UserAvatar);
                    if (!string.IsNullOrEmpty(AppParameters.Passcode)) session.LocalPasscode = AppParameters.Passcode;
                    await VKSessionManager.AddOrUpdateSessionAsync(session);
                } catch (Exception ex) {
                    Functions.ShowHandledErrorTip(ex);
                }
            } else {
                await new MessageDialog("Cannot refresh token!", Locale.Get("api_error")).ShowAsync();
            }
        }

        public static async Task ClearChatHistoryAsync(long peerId, System.Action successCallback = null) {
            string confirm = string.Empty;
            if (peerId.IsUser()) {
                var u = AppSession.GetCachedUser(peerId);
                confirm = u != null ?
                String.Format(Locale.GetForFormat("conv_delete_confirmation_user"), $"{u.FirstNameIns} {u.LastNameIns}") :
                Locale.Get("conv_delete_confirmation_chat");
            } else if (peerId.IsGroup()) {
                var g = AppSession.GetCachedGroup(peerId);
                confirm = g != null ?
                String.Format(Locale.GetForFormat("conv_delete_confirmation_group"), g.Name) :
                Locale.Get("conv_delete_confirmation_chat");
            } else if (peerId.IsChat()) {
                confirm = Locale.Get("conv_delete_confirmation_chat");
            }

            ContentDialog dlg = new ContentDialog {
                Title = Locale.Get("convctx_delete"),
                Content = $"{confirm} {Locale.Get("action_cannot_be_undone")}",
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no"),
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object response = await ssp.ShowAsync(Messages.DeleteConversation(peerId));
                if (AppSession.CurrentOpenedConversationId == peerId) Main.GetCurrent().SwitchToLeftFrame();
                successCallback?.Invoke();
                Functions.ShowHandledErrorTip(response);
            }
        }

        public static async Task DeleteChatForAllAsync(int chatId, string chatName, System.Action successCallback = null) {
            TextBlock tb = new TextBlock { TextWrapping = TextWrapping.Wrap };
            tb.Inlines.Add(new Run { Text = chatName });
            tb.Inlines.Add(new LineBreak());
            tb.Inlines.Add(new Run { FontWeight = FontWeights.Bold, Text = Locale.Get("action_cannot_be_undone") });

            ContentDialog dlg = new ContentDialog {
                Title = Locale.Get("chat_delete_for_all_confirm"),
                Content = tb,
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no"),
                DefaultButton = ContentDialogButton.Secondary,
            };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                object r = await Messages.DropChatForAll(chatId);
                Functions.ShowHandledErrorTip(r);

                if (r is bool) {
                    Tips.Show(Locale.Get("chat_deleted"));
                    if (AppSession.CurrentOpenedConversationId == chatId + 2000000000) Main.GetCurrent().SwitchToLeftFrame();
                    successCallback?.Invoke();
                }
            }
        }
    }
}