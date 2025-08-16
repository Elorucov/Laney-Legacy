using Elorucov.Laney.Controls.MessageAttachments;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class PostUI : UserControl {
        public static DependencyProperty PostProperty = DependencyProperty.Register(nameof(Post), typeof(WallPost), typeof(PostUI), new PropertyMetadata(null));

        public WallPost Post {
            get { return (WallPost)GetValue(PostProperty); }
            set { SetValue(PostProperty, value); }
        }

        public double MaybeActualWidth { get; set; }

        bool dontTriggerSizeChanged = false;
        long id = 0;

        public PostUI() {
            this.InitializeComponent();

            if (AppParameters.FeedDebug) FindName(nameof(debug));
            if (ViewManagement.GetWindowType() != WindowType.Main) RepostButton.Visibility = Visibility.Collapsed;

            CoreApplication.GetCurrentView().CoreWindow.ResizeStarted += CoreWindow_ResizeStarted;
            CoreApplication.GetCurrentView().CoreWindow.ResizeCompleted += CoreWindow_ResizeCompleted;
            SizeChanged += OnSizeChanged;
            Loaded += PostUI_Loaded;
            Unloaded += PostUI_Unloaded;

            id = RegisterPropertyChangedCallback(PostProperty, async (a, p) => await SetUpAsync((WallPost)GetValue(p)));
            new System.Action(async () => { await SetUpAsync(Post); })();
        }

        private void PostUI_Loaded(object sender, RoutedEventArgs e) {
            Loaded -= PostUI_Loaded;
            if (debug != null) {
                dbgWidth.Text = $"AW: {ActualWidth}px;";
            }
        }

        private void PostUI_Unloaded(object sender, RoutedEventArgs e) {
            UnregisterPropertyChangedCallback(PostProperty, id);
            CoreApplication.GetCurrentView().CoreWindow.ResizeStarted -= CoreWindow_ResizeStarted;
            CoreApplication.GetCurrentView().CoreWindow.ResizeCompleted -= CoreWindow_ResizeCompleted;
            SizeChanged -= OnSizeChanged;
            Unloaded -= PostUI_Unloaded;
        }

        private async Task SetUpAsync(WallPost post) {
            if (post == null) {
                IsHitTestVisible = false;
                Opacity = 0;
                return;
            }

            IsHitTestVisible = true;
            Opacity = 1;
            if (debug != null) {
                dbgId.Text = $"{post.OwnerId}_{post.Id}";
                dbgMaybeWidth.Text = $"MW: {MaybeActualWidth}px";
            }

            // Объект header возвращается не во всех аккаунтах. Ёбанный A/B.
            if (post.Header != null) {
                Tuple<string, string, Uri> nameAndAva = null;
                if (post.Header?.Title?.SourceId != 0) {
                    nameAndAva = AppSession.GetNameAndAvatar(post.Header.Title.SourceId);
                    AuthorName.Text = String.Join(" ", nameAndAva.Item1, nameAndAva.Item2);
                    Ava.DisplayName = AuthorName.Text;
                }
                if (post.Header?.Photo?.SourceId != 0) {
                    if (post.Header.Photo.SourceId != post.Header?.Title?.SourceId) nameAndAva = AppSession.GetNameAndAvatar(post.Header.Photo.SourceId);
                    Ava.ImageUri = nameAndAva.Item3;
                }

                PostTime.Text = post.Date.ToTimeAndDate();
                if (!string.IsNullOrEmpty(post.Header?.Description?.Text.Text)) {
                    PostTime.Text += $" • {post.Header.Description.Text.Text}";
                }
            } else {
                long oid = post.FromId;
                if (oid == 0) oid = post.OwnerId; // FromId = 0 в постах из ответа newsfeed.get и, возможно, wall.get
                if (post.OwnerId.IsUser()) {
                    var u = AppSession.GetCachedUser(post.OwnerId);
                    if (u != null) {
                        AuthorName.Text = $"{u.FirstName} {u.LastName}";
                        Ava.DisplayName = $"{u.FirstName} {u.LastName}";
                        await Ava.SetUriSourceAsync(u.Photo);
                    }
                } else if (post.OwnerId.IsGroup()) {
                    var g = AppSession.GetCachedGroup(-post.OwnerId);
                    if (g != null) {
                        AuthorName.Text = g.Name;
                        Ava.DisplayName = g.Name;
                        await Ava.SetUriSourceAsync(g.Photo);
                    }
                } else {
                    AuthorName.Text = post.FromId.ToString();
                }

                // Time and to_id
                PostTime.Text = post.Date.ToTimeAndDate();
                if (post.OwnerOrToId != 0 && post.OwnerOrToId != post.FromId) {
                    if (post.OwnerOrToId.IsUser()) {
                        var u = AppSession.GetCachedUser(post.OwnerOrToId);
                        if (u != null) {
                            PostTime.Text += $" {Locale.Get("in")} {u.FirstName} {u.LastName}";
                        }
                    } else if (post.OwnerOrToId.IsGroup()) {
                        var g = AppSession.GetCachedGroup(-post.OwnerOrToId);
                        if (g != null) {
                            PostTime.Text += $" {Locale.Get("in")} {g.Name}";
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(PostTime.Text)) PostTime.Text = post.Date.ToTimeAndDate();

            PinnedIndicator.Visibility = post.IsPinned ? Visibility.Visible : Visibility.Collapsed;
            if (post.FriendsOnly || post.BestFriendsOnly) {
                PrivacyIndicator.Visibility = Visibility.Visible;
                PrivacyIndicatorIcon.Glyph = post.BestFriendsOnly ? "" : "";
            }

            // Donut
            if (post.Donut != null && post.Donut.IsDonut) {
                FindName(nameof(DonutPlaceholder));
                DonutPlaceholderText.Text = post.Donut.Placeholder.Text;
                return;
            }

            // Text
            if (!string.IsNullOrEmpty(post.Text)) {
                FindName(nameof(PostText));
                PostText.FontSize = post.ZoomText ? 24 : AppParameters.MessageFontSize;

                VKTextParser.SetText(post.Text, PostText, MessageUIHelper.OnLinkClicked);
            }

            // Attachments
            AddAttachments(post.Id, post.Attachments);

            // Reposts
            if (post.CopyHistory != null) {
                foreach (var wp in post.CopyHistory) {
                    string def = MessageUIHelper.GetNameOrDefaultString(wp.FromId, wp.Text);
                    DefaultAttachmentControl dac = new DefaultAttachmentControl {
                        IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Newsfeed"],
                        Title = Locale.Get("wallpost").Capitalize(),
                        Description = def
                    };
                    dac.Click += (a, b) => {
                        WallPostModal wpm = new WallPostModal(wp);
                        wpm.Show();
                    };
                    AttachmentsContainer.Children.Add(dac);
                }
            }

            foreach (FrameworkElement element in AttachmentsContainer.Children) {
                element.Margin = new Thickness(0, 0, 0, 12);
            }

            // Place, signer, copyright
            if (post.Geo != null) {
                FindName(nameof(Place));
                PlaceName.Text = post.Geo.Place.Title;
            }

            if (post.Header == null && post.SignerId.IsUser()) {
                FindName(nameof(Signer));
                var u = AppSession.GetCachedUser(post.SignerId);
                if (u != null) {
                    SignerName.Text += $"{u.FirstName} {u.LastName}";
                }
            }

            if (post.Header == null && post.Copyright != null) {
                FindName(nameof(CopyrightLink));
                Copyright.Text = $"{Locale.Get("source")}: {post.Copyright.Name}";
            }

            // Counters
            if (post.Likes == null && post.Reposts == null && post.Views == null) return;

            FindName(nameof(PostCounters));

            if (post.Likes != null) {
                FindName(nameof(LikeCounter));
                SetLike(post.Likes.Count, post.Likes.UserLikes);
            }

            if (post.Comments != null && (post.Comments.CanPost && post.Comments.CanView)) {
                FindName(nameof(CommentButton));
                if (post.Comments.Count > 0) {
                    FindName(nameof(CommentCounter));
                    CommentCounter.Text = post.Comments.Count.ToString();
                }
            }

            if (post.Likes != null && post.Likes.RepostDisabled == false) {
                FindName(nameof(RepostButton));
                if (post.Reposts?.Count > 0) {
                    FindName(nameof(RepostCounter));
                    RepostCounter.Text = post.Reposts.Count.ToString();
                }
            }

            if (post.Views?.Count > 0) {
                FindName(nameof(ViewsButton));
                ViewsCounter.Text = post.Views.Count.ToString();
            }
        }

        private void AddAttachments(long id, List<Attachment> attachments) {
            if (attachments == null || attachments.Count == 0) return;
            double width = ActualWidth;
            if (debug != null) {
                dbgWidth.Text = $"AW: {width}px;";
                dbgMaybeWidth.Text = $"MW: {MaybeActualWidth}px";
            }
            if (width == 0) width = MaybeActualWidth;
            if (width > 480) width = 480;

            AttachmentsContainer.Children.Clear();
            MessageUIHelper.AddAttachmentsToPanel(id, AttachmentsContainer, attachments,
                width, new Thickness(0, 0, 0, 12), atchsOwner: AuthorName.Text,
                graffitiControlWidth: width);

            foreach (FrameworkElement element in AttachmentsContainer.Children) {
                element.Margin = new Thickness(0, 0, 0, 12);
            }
        }

        private void SetLike(int count, bool isLiked) {
            LikeCounter.Text = count.ToString();
            LikeCounter.Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (isLiked) {
                LikeIcon.Id = VK.VKUI.Controls.VKIconName.Icon24Like;
                LikeButton.Style = (Style)Application.Current.Resources["VKPostPressedButtonStyle"];
            } else {
                LikeIcon.Id = VK.VKUI.Controls.VKIconName.Icon24LikeOutline;
                LikeButton.Style = (Style)Application.Current.Resources["VKPostButtonStyle"];
            }
        }

        private void ShowUserOrGroupInfo(long id) {
            VKLinks.ShowPeerInfoModal(id);
        }

        private void CoreWindow_ResizeStarted(Windows.UI.Core.CoreWindow sender, object args) {
            dontTriggerSizeChanged = true;
        }

        private void CoreWindow_ResizeCompleted(Windows.UI.Core.CoreWindow sender, object args) {
            if (Post.Attachments?.Count > 0) AddAttachments(Post.Id, Post.Attachments);
            dontTriggerSizeChanged = false;
        }

        double oldWidth = 0;
        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            if (dontTriggerSizeChanged || e.NewSize.Width == oldWidth) return;
            oldWidth = e.NewSize.Width;
            if (Post?.Attachments?.Count > 0) AddAttachments(Post.Id, Post.Attachments);
        }

        private void OpenDonutPage(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                await Launcher.LaunchUriAsync(new Uri($"https://vk.com/club{Post.OwnerOrToId}?act=donut_payment&source=wall_placeholder"));
            })();
        }

        private void ShowAuthorInfo(object sender, RoutedEventArgs e) {
            ShowUserOrGroupInfo(Post.FromId);
        }

        private void ShowSignerInfo(object sender, RoutedEventArgs e) {
            ShowUserOrGroupInfo(Post.SignerId);
        }

        private void GoToSource(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await VKLinks.LaunchLinkAsync(new Uri(Post.Copyright.Link)); })();
        }

        private void OpenPostInBrowser(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await Launcher.LaunchUriAsync(new Uri($"https://vk.com/wall{Post.OwnerOrToId}_{Post.Id}")); })();
        }

        private void LikeButtonClicked(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                bool isPreviouslyLiked = Post.Likes.UserLikes;
                SetLike(isPreviouslyLiked ? Post.Likes.Count - 1 : Post.Likes.Count + 1, !Post.Likes.UserLikes);
                object resp = !isPreviouslyLiked ?
                    await VkAPI.Methods.Likes.Add("post", Post.OwnerOrToId, Post.Id, Post.AccessKey) :
                    await VkAPI.Methods.Likes.Delete("post", Post.OwnerOrToId, Post.Id, Post.AccessKey);
                if (resp is int count) {
                    SetLike(count, !isPreviouslyLiked);
                    Post.Likes.Count = count;
                    Post.Likes.UserLikes = !Post.Likes.UserLikes;
                    Post.Likes.CanLike = !Post.Likes.CanLike;
                } else {
                    Functions.ShowHandledErrorTip(resp, $"Owner: {Post.OwnerId}; From: {Post.FromId}; To: {Post.ToId}");
                    SetLike(Post.Likes.Count, isPreviouslyLiked);
                }
            })();
        }

        private void SharePost(object sender, RoutedEventArgs e) {
            // Main.GetCurrent().StartForwardingAttachments(new List<AttachmentBase> { Post });

            SharingModal sm = new SharingModal(new List<AttachmentBase> { Post }, true);
            sm.Show();
        }

        private void OpenMap(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                try {
                    string[] coords = Post.Geo.Coordinates.Split(' ');
                    bool result = await Launcher.LaunchUriAsync(new Uri($"bingmaps:cp={coords[0]}~{coords[1]}"));
                    if (!result) Tips.Show(Locale.Get("global_error"));
                } catch (Exception ex) {
                    Functions.ShowHandledErrorDialog(ex);
                }
            })();
        }
    }
}