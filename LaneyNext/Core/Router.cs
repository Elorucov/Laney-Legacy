using ELOR.VKAPILib.Objects;
using ELOR.VKAPILib.Objects.Messages;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Laney.Views.Modals;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.Foundation.Collections;
using Windows.System;

namespace Elorucov.Laney.Core
{
    public enum VKLinkType
    {
        OtherOrNonVK, User, Group, Wall, Poll, ConversationInvite, WriteVkMe, Write, ScreenName
    }

    public class Router
    {
        #region Regex and parsing VK links

        private static readonly Regex userReg = new Regex("(http(s)?://)?(m.)?vk.com/id[0-9]+", RegexOptions.Compiled);
        private static readonly Regex groupReg = new Regex("(http(s)?://)?(m.)?vk.com/(club|public|event)[0-9]+", RegexOptions.Compiled);
        private static readonly Regex wallReg = new Regex("(http(s)?://)?(m.)?vk.com/wall[-0-9]+_[0-9]+", RegexOptions.Compiled);
        private static readonly Regex pollReg = new Regex("(http(s)?://)?(m.)?vk.com/poll[-0-9]+_[0-9]+", RegexOptions.Compiled);
        private static readonly Regex convInvReg = new Regex(@"(http(s)?://)?vk.me/join/[a-zA-Z0-9_\s\-]+", RegexOptions.Compiled);
        private static readonly Regex vkmeReg = new Regex(@"(http(s)?://)?vk.me/(?!app$)[a-zA-Z0-9._\-]+", RegexOptions.Compiled);
        private static readonly Regex writeReg = new Regex("(http(s)?://)?vk.com/write[-0-9]+", RegexOptions.Compiled);
        private static readonly Regex vkIgnorableReg = new Regex(@"(http(s)?://)?vk.com/(account|albums[0-9]+|app(s|[0-9]+)|audio(s|[-0-9]+_[0-9]+)|audio_playlist[-0-9]+_[0-9]+|blog|bookmarks|bug(s|tracker|[1-9][0-9]+)|business_notify|community_manage|doc(s|[-0-9]+_[0-9]+)|edit|events|fave|feed|friends|games|gifts([0-9])?|groups|groups_create|help|im|lives|mail|music|pages|payments|podcasts|press|promocode|purchase_subscription|restore|search|search(\???(?:&?[^=&]*=[^=&]*)*)?|services|settings|shopping|stats|stickers|stories_archive|support|transfers|vkpay|video([-0-9]+_[0-9]+)?)|write", RegexOptions.Compiled);
        private static readonly Regex vkReg = new Regex(@"(http(s)?://)?vk.com/[a-zA-Z0-9_\-]+", RegexOptions.Compiled);

        private static readonly Regex idsReg = new Regex(@"(?![A-Za-z]\S)[-0-9]+", RegexOptions.Compiled);
        private static readonly Regex screenNameReg = new Regex(@"(?<=(http(s)?://)?vk.(com|me)/)([A-Za-z0-9._/-]+)", RegexOptions.Compiled);
        private static readonly Regex writeIdReg = new Regex(@"(?!(http(s)?://)?vk.com/write)[-0-9]+", RegexOptions.Compiled);

        public static VKLinkType GetLinkType(string url)
        {
            if (userReg.IsMatch(url)) return VKLinkType.User;
            if (groupReg.IsMatch(url)) return VKLinkType.Group;
            if (wallReg.IsMatch(url)) return VKLinkType.Wall;
            if (pollReg.IsMatch(url)) return VKLinkType.Poll;
            if (convInvReg.IsMatch(url)) return VKLinkType.ConversationInvite;
            if (vkmeReg.IsMatch(url)) return VKLinkType.WriteVkMe;
            if (writeReg.IsMatch(url)) return VKLinkType.Write;
            if (vkIgnorableReg.IsMatch(url)) return VKLinkType.OtherOrNonVK;
            if (vkReg.IsMatch(url)) return VKLinkType.ScreenName;
            return VKLinkType.OtherOrNonVK;
        }

        public static async Task<Tuple<VKLinkType, string>> LaunchLinkAsync(Uri uri)
        {
            return await LaunchLinkAsync(uri.AbsoluteUri);
        }

        public static async Task<Tuple<VKLinkType, string>> LaunchLinkAsync(string url)
        {
            VKLinkType type = GetLinkType(url);
            string id = null;

            var ids = idsReg.Matches(url);
            var snm = screenNameReg.Matches(url);

            Log.General.Info("Trying to launch VK link", new ValueSet { { "link", url }, { "type", type.ToString() },
                { "ids_matches", ids.Count }, { "snm_matches", snm.Count }, });

            switch (type)
            {
                case VKLinkType.User:
                    id = ids[0].Value;
                    ShowUserCard(Int32.Parse(id));
                    break;
                case VKLinkType.Group:
                    id = ids[0].Value;
                    ShowGroupCard(Int32.Parse(id));
                    break;
                case VKLinkType.Wall: // TODO: Wallpost viewer in app
                    id = $"{ids[0].Value}_{ids[1].Value}";
                    await Launcher.LaunchUriAsync(new Uri(url));
                    break;
                case VKLinkType.Poll:
                    TryLaunchPollViewer(Int32.Parse(ids[0].Value), Int32.Parse(ids[1].Value));
                    break;
                case VKLinkType.ConversationInvite:
                    id = url;
                    if (VKSession.Current.Type == SessionType.VKGroup) break; // TODO: info about unavailable features for groups
                    ShowChatPreview(url);
                    break;
                case VKLinkType.WriteVkMe:
                    id = snm[0].Value;
                    TryResolveScreenNameAndOpenConv(id, url);
                    break;
                case VKLinkType.Write:
                    var wr = writeIdReg.Match(url);
                    id = wr.Value;
                    VKSession.Current.SessionBase.SwitchToConversation(Int32.Parse(id));
                    break;
                case VKLinkType.ScreenName:
                    id = snm[0].Value;
                    TryResolveScreenName(id, url);
                    break;
                case VKLinkType.OtherOrNonVK:
                    id = url;
                    await Launcher.LaunchUriAsync(new Uri(url));
                    break;
            }
            await Task.Delay(1);
            return new Tuple<VKLinkType, string>(type, id);
        }

        private static async void TryLaunchPollViewer(int ownerId, int id)
        {
            try
            {
                Log.General.Info(String.Empty, new ValueSet { { "owner_id", ownerId.ToString() }, { "id", id.ToString() } });
                ScreenSpinner<Poll> ssp = new ScreenSpinner<Poll>();
                Poll poll = await ssp.ShowAsync(VKSession.Current.API.Polls.GetByIdAsync(ownerId, id, true, APIHelper.UserFields));
                PollViewer pv = new PollViewer(poll);
                pv.Show();
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex.InnerException)) TryLaunchPollViewer(ownerId, id);
            }
        }

        private static async void TryResolveScreenName(string name, string fallbackUrl)
        {
            try
            {
                Log.General.Info(String.Empty, new ValueSet { { "name", name }, { "fallback", fallbackUrl } });
                ScreenSpinner<ResolveScreenNameResult> ssp = new ScreenSpinner<ResolveScreenNameResult>();
                ResolveScreenNameResult result = await ssp.ShowAsync(VKSession.Current.API.Utils.ResolveScreenNameAsync(name));
                if (result != null && result.ObjectId > 0)
                {
                    switch (result.Type)
                    {
                        case ScreenNameType.User:
                            ShowUserCard(result.ObjectId);
                            break;
                        case ScreenNameType.Group:
                            ShowGroupCard(result.ObjectId);
                            break;
                        default:
                            await Launcher.LaunchUriAsync(new Uri(fallbackUrl));
                            break;
                    }
                }
                else
                {
                    await Launcher.LaunchUriAsync(new Uri(fallbackUrl));
                }
            }
            catch (AggregateException ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex.InnerException)) TryResolveScreenName(name, fallbackUrl);
            }
        }

        private static async void TryResolveScreenNameAndOpenConv(string name, string fallbackUrl)
        {
            try
            {
                Log.General.Info(String.Empty, new ValueSet { { "name", name }, { "fallback", fallbackUrl } });
                var resp = await VKSession.Current.API.Utils.ResolveScreenNameAsync(name);

                ScreenSpinner<ResolveScreenNameResult> ssp = new ScreenSpinner<ResolveScreenNameResult>();
                ResolveScreenNameResult result = await ssp.ShowAsync(VKSession.Current.API.Utils.ResolveScreenNameAsync(name));
                if (result != null && result.ObjectId > 0)
                {
                    switch (result.Type)
                    {
                        case ScreenNameType.User:
                            VKSession.Current.SessionBase.SwitchToConversation(result.ObjectId);
                            break;
                        case ScreenNameType.Group:
                            VKSession.Current.SessionBase.SwitchToConversation(-result.ObjectId);
                            break;
                        default:
                            await Launcher.LaunchUriAsync(new Uri(fallbackUrl));
                            break;
                    }
                }
            }
            catch (AggregateException ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex.InnerException)) TryResolveScreenNameAndOpenConv(name, fallbackUrl);
            }
        }

        #endregion

        public static void ShowCard(int id)
        {
            if (id == 0) return;
            if (id > 0) ShowUserCard(id);
            if (id < 0) ShowGroupCard(id);
        }

        public static void ShowUserCard(int userId)
        {
            UserCard card = new UserCard();
            card.DataContext = new UserCardViewModel(userId);
            card.Show();
        }

        public static void ShowGroupCard(int groupId)
        {
            if (groupId < 0) groupId = -groupId;
            GroupCard card = new GroupCard();
            card.DataContext = new GroupCardViewModel(groupId);
            card.Show();
        }

        public static void ShowChatInfo(int chatId, string title = null)
        {
            if (chatId > 2000000000) chatId = chatId - 2000000000;
            ChatInfo card = new ChatInfo();
            if (!String.IsNullOrEmpty(title)) card.Title = title;
            card.DataContext = new ChatInfoViewModel(card, chatId);
            card.Show();
        }

        private static async void ShowChatPreview(string url)
        {
            try
            {
                ScreenSpinner<ChatPreviewResponse> ssp = new ScreenSpinner<ChatPreviewResponse>();
                ChatPreviewResponse preview = await ssp.ShowAsync(VKSession.Current.API.Messages.GetChatPreviewAsync(url, APIHelper.Fields));
                if (preview != null)
                {
                    ChatPreviewCard cpc = new ChatPreviewCard(preview);
                    cpc.Closed += (a, b) =>
                    {
                        if (b is bool join && join)
                        {
                            if (preview.Preview.LocalId > 0)
                            {
                                VKSession.Current.SessionBase.SwitchToConversation(2000000000 + preview.Preview.LocalId);
                            }
                            else
                            {
                                JoinChat(url);
                            }
                        }
                    };
                    cpc.Show();
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is APIException apiex && apiex.Code == 100)
                {
                    await new Alert
                    {
                        Header = Locale.Get("error"),
                        Text = Locale.Get("chatpreview_invalid_link"),
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                    return;
                }
                await ExceptionHelper.ShowErrorDialogAsync(ex.InnerException);
            }
        }

        private static async void JoinChat(string url)
        {
            try
            {
                ScreenSpinner<JoinChatResponse> ssp = new ScreenSpinner<JoinChatResponse>();
                JoinChatResponse response = await VKSession.Current.API.Messages.JoinChatByInviteLinkAsync(url);
                VKSession.Current.SessionBase.SwitchToConversation(2000000000 + response.ChatId);
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync(ex);
            }
        }
    }
}