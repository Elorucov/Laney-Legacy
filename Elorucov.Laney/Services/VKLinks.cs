using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Microsoft.QueryStringDotNET;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Services {
    public enum VKLinkType {
        None, System, Wall, Poll, AudioPlaylist, Artist, Article, Invite, User, Group, Write
    }

    public class VKLinks {
        private static readonly Regex _numReg = new Regex("[0-9]+");
        private static readonly Regex _userReg = new Regex("/id[0-9]+");
        private static readonly Regex _groupReg = new Regex("/(club|public|event)[0-9]+");
        private static readonly Regex _wallReg = new Regex("/wall[-0-9]+_[0-9]+");
        private static readonly Regex _pollReg = new Regex("/poll[-0-9]+_[0-9]+");
        private static readonly Regex _audioPlaylistReg = new Regex("act=audio_playlist[-0-9]+_[0-9]+");
        private static readonly Regex _artistReg = new Regex(@"/artist/[a-zA-Z0-9_\s\-]+");
        private static readonly Regex _articleReg = new Regex(@"/@[a-zA-Z0-9_\s\-]+");
        private static readonly Regex _inviteReg = new Regex(@"/join/[a-zA-Z0-9_\s\-]+");
        // public потому что юзается в App.xaml.cs
        public static readonly Regex _writeReg = new Regex(@"/write([-0-9]+)", RegexOptions.Compiled);

        public static VKLinkType IsMobileVKPage(Uri uri) {
            if (uri.Scheme != "https") return VKLinkType.None;
            if (uri.Host == "login.vk.com" || uri.Host == "login.vk.ru") return VKLinkType.System;
            if (uri.Host == "vk.me" && _inviteReg.IsMatch(uri.AbsolutePath)) return VKLinkType.Invite;
            if (uri.Host != "vk.com" && uri.Host != "m.vk.com" && uri.Host != "vk.ru" && uri.Host != "m.vk.ru") return VKLinkType.None;
            if (_userReg.IsMatch(uri.AbsolutePath)) return VKLinkType.User;
            if (_groupReg.IsMatch(uri.AbsolutePath)) return VKLinkType.Group;
            if (uri.AbsolutePath == "/login") return VKLinkType.System;
            if (_wallReg.IsMatch(uri.AbsolutePath)) return VKLinkType.Wall;
            if (_pollReg.IsMatch(uri.AbsolutePath)) return VKLinkType.Poll;
            if (_artistReg.IsMatch(uri.AbsolutePath)) return VKLinkType.Artist;
            if (_articleReg.IsMatch(uri.AbsolutePath)) return VKLinkType.Article;
            if (uri.AbsolutePath == "/audio" && _audioPlaylistReg.IsMatch(uri.Query.Substring(1))) return VKLinkType.AudioPlaylist;
            if (_writeReg.IsMatch(uri.AbsolutePath)) return VKLinkType.Write;
            return VKLinkType.None;
        }

        public static async Task LaunchLinkAsync(Uri uri) {
            VKLinkType t = IsMobileVKPage(uri);
            if (ViewManagement.GetWindowType() != WindowType.Main) {
                await CheckLinkAndLaunch(uri);
                return;
            }
            switch (t) {
                case VKLinkType.None: await CheckLinkAndLaunch(uri); break;
                case VKLinkType.Invite:
                    await OpenChatInvitationModalAsync(uri);
                    break;
                case VKLinkType.User:
                    CheckUserIdAndShowFlyout(uri);
                    break;
                case VKLinkType.Group:
                    CheckGroupIdAndShowFlyout(uri);
                    break;
                case VKLinkType.Wall:
                    await ParseWallLinkAndShowAsync(uri);
                    break;
                case VKLinkType.Poll:
                    await ParsePollLinkAndShowAsync(uri);
                    break;
                case VKLinkType.Write:
                    GetToConversation(uri);
                    break;
                default:
                    await LaunchLinkInWebAsync(uri);
                    break;
            }
        }

        private static async Task CheckLinkAndLaunch(Uri uri) {
            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Utils.CheckLink(uri.AbsoluteUri));
            if (resp is CheckLinkResult result) {
                if (result.Status == CheckLinkStatus.Banned) {
                    ContentDialog dlg = new ContentDialog {
                        Title = Locale.Get("bannedlink_title"),
                        Content = Locale.Get("bannedlink_desc"),
                        PrimaryButtonText = Locale.Get("continue"),
                        SecondaryButtonText = Locale.Get("cancel"),
                        DefaultButton = ContentDialogButton.Secondary
                    };

                    var dlgbutton = await dlg.ShowAsync();
                    if (dlgbutton == ContentDialogResult.Primary) await Launcher.LaunchUriAsync(new Uri(result.Link));
                } else {
                    await Launcher.LaunchUriAsync(new Uri(result.Link));
                }
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        private static async Task LaunchLinkInWebAsync(Uri uri) {
            if (AppParameters.OpenVKLinksInLaney) {
                ModalWebView mvw = new ModalWebView(uri);
                mvw.Show();
            } else {
                await CheckLinkAndLaunch(uri);
            }
        }

        private static void CheckUserIdAndShowFlyout(Uri uri) {
            var k = _numReg.Match(uri.AbsoluteUri);

            int id = 0;
            if (int.TryParse(k.Value, out id)) ShowPeerInfoModal(id);
        }

        private static void CheckGroupIdAndShowFlyout(Uri uri) {
            var k = _numReg.Match(uri.AbsoluteUri);
            int id = 0;
            if (int.TryParse(k.Value, out id)) ShowPeerInfoModal(id * -1);
        }

        private static void GetToInvitedChat(object sender, object e) {
            if (e is int) {
                Main.GetCurrent().ShowConversationPage((int)e);
            }
        }

        private static async Task ParsePollLinkAndShowAsync(Uri uri) {
            string[] s = uri.AbsolutePath.Substring(5).Split('_');
            int id = 0;
            int ownerId = 0;
            if (s.Length >= 2 && int.TryParse(s[0], out ownerId) && int.TryParse(s[1], out id)) {
                await ShowPollAsync(ownerId, id);
            } else {
                await LaunchLinkInWebAsync(uri);
            }
        }

        public static async Task ShowPollAsync(long ownerId, long id, EventHandler<object> onClosed = null) {
            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Polls.GetById(ownerId, id));
            if (resp is Poll poll) {
                PollViewer pv = new PollViewer(poll);
                if (onClosed != null) pv.Closed += onClosed;
                pv.Show();
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        private static async Task ParseWallLinkAndShowAsync(Uri uri) {
            string[] s = uri.AbsolutePath.Substring(5).Split('_');
            int id = 0;
            int ownerId = 0;
            if (s.Length >= 2 && int.TryParse(s[0], out ownerId) && int.TryParse(s[1], out id)) {
                await ShowWallPostAsync(ownerId, id);
            } else {
                await LaunchLinkInWebAsync(uri);
            }
        }

        public static async Task ShowWallPostAsync(WallPost post, EventHandler<object> onClosed = null) {
            if (post.Textlive == null) {
                ScreenSpinner<object> ssp = new ScreenSpinner<object>();
                object resp = await ssp.ShowAsync(Wall.GetById(post.OwnerOrToId, post.Id, post.AccessKey));
                if (resp is VKList<WallPost> posts) {
                    AppSession.AddUsersToCache(posts.Profiles);
                    AppSession.AddGroupsToCache(posts.Groups);
                    if (posts.Items.Count == 1) {
                        post = posts.Items[0];
                    }
                } else {
                    Functions.ShowHandledErrorDialog(resp);
                }
            }

            WallPostModal wpm = new WallPostModal(post);
            if (onClosed != null) wpm.Closed += onClosed;
            wpm.Show();
        }

        public static async Task ShowWallPostAsync(long ownerId, long id, string accessKey = null, EventHandler<object> onClosed = null) {
            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Wall.GetById(ownerId, id, accessKey));
            if (resp is VKList<WallPost> posts) {
                AppSession.AddUsersToCache(posts.Profiles);
                AppSession.AddGroupsToCache(posts.Groups);
                if (posts.Items.Count == 1) {
                    WallPostModal wpm = new WallPostModal(posts.Items[0]);
                    if (onClosed != null) wpm.Closed += onClosed;
                    wpm.Show();
                } else {
                    Tips.Show(Locale.Get("global_error"));
                }
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        public static async Task ShowStickerPackInfoAsync(long productId, EventHandler<object> onClosed = null) {
            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Store.GetStockItemByProductId("stickers", productId));
            if (resp is StockItem item) {
                if (item.Product.Type != "stickers") {
                    Log.Warn($"ShowStickerPackInfo: Product type is {item.Product.Type}");
                    Tips.Show(Locale.Get("global_error"), $"Product type is {item.Product.Type}");
                    if (onClosed != null) onClosed?.Invoke(null, null);
                    return;
                }
                StickerPackPreviewModal modal = new StickerPackPreviewModal(item);
                if (onClosed != null) modal.Closed += onClosed;
                modal.Show();
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        private static async Task OpenChatInvitationModalAsync(Uri uri) {
            string link = uri.ToString();
            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Messages.GetChatPreview(link));
            if (resp is ChatPreviewResponse cpr) {
                if (cpr.Preview.LocalId > 0) {
                    Main.GetCurrent().ShowConversationPage(cpr.Preview.LocalId + 2000000000);
                    return;
                }

                ChatInvite modal = new ChatInvite(cpr, link);
                modal.Closed += GetToInvitedChat;
                modal.Show();
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        private static void GetToConversation(Uri uri) {
            var match = _writeReg.Match(uri.AbsolutePath);
            string vkRef = null;
            string vkRefSource = null;
            if (!string.IsNullOrEmpty(uri.Query)) {
                string q = uri.Query.Substring(1, uri.Query.Length - 1);
                var query = QueryString.Parse(q);
                vkRef = query.Contains("ref") ? query["ref"] : null;
                vkRefSource = query.Contains("ref_source") ? query["ref_source"] : null;
            }

            if (match.Success) {
                int id = 0;
                bool result = int.TryParse(match.Groups[1].Value, out id);
                if (result) Main.GetCurrent().ShowConversationPage(id, -1, false, vkRef, vkRefSource);
            }
        }

        public static void ShowPeerInfoModal(long peerId, System.Action onClosed = null) {
            PeerProfile modal = new PeerProfile(peerId);
            if (onClosed != null) modal.Closed += (a, b) => onClosed.Invoke();
            modal.Show();
        }
    }
}