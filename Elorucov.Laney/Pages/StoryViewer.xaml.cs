using Elorucov.Laney.Controls;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class StoryViewer : OverlayModal {
        private int animationDuration = 370;
        private DispatcherTimer StoryTimer;
        MediaPlayer Player;

        private FrameworkElement FromControl;
        Story Story;
        bool IsReady = false;

        public StoryViewer(Story story, FrameworkElement from = null) {
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            InitializeComponent();
            Story = story;
            FromControl = from == null ? DefaultFromAnimator : from;
            Loaded += async (a, b) => {
                if (AppParameters.ShowStoryViewerDebugInfo) {
                    dbg.Visibility = Visibility.Visible;
                    d3.Visibility = Visibility.Visible;
                }

                SystemNavigationManager.GetForCurrentView().BackRequested += StoryViewer_BackRequested;
                await TitleAndStatusBar.ChangeColor(Color.FromArgb(255, 255, 255, 255));
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                    ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                }
                ResizeStoryRoot();
                Animate(FromControl);
                SizeChanged += (c, d) => ResizeStoryRoot();
                new System.Action(async () => { await SetupStoryAsync(); })();
            };
        }

        private void StoryViewer_BackRequested(object sender, BackRequestedEventArgs e) {
            new System.Action(async () => { await Close(); })();
        }

        private async Task Close(object data = null, bool alternativeAnimation = false) {
            try {
                Layer.IsHitTestVisible = false;
                if (StoryTimer != null) {
                    StoryTimer.Stop();
                    StoryTimer = null;
                }
                if (Player != null) {
                    Player.Dispose();
                    Player = null;
                }
                SystemNavigationManager.GetForCurrentView().BackRequested -= StoryViewer_BackRequested;
                Animate(alternativeAnimation ? DefaultFromAnimator : FromControl, true);
                if (Theme.IsAnimationsEnabled) await Task.Delay(animationDuration - 10);
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
                    if (DisplayInformation.GetForCurrentView().CurrentOrientation == DisplayOrientations.Portrait) {
                        ApplicationView.GetForCurrentView().ExitFullScreenMode();
                    }
                }
                StoryRoot.Children.Clear(); // to fix strange visual bug on video stories
                Hide(data);
            } catch (Exception ex) {
                Log.Error($"Error while closing storyviewer! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
        }

        private void ResizeStoryRoot() {
            double tm = 0;
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                tm = CoreApplication.GetCurrentView().TitleBar.Height;
            }
            double w = Container.ActualWidth;
            double h = Container.ActualHeight - (tm * 2);
            string dnw = "";
            double e = h / 16 * 9;
            if (w > e) { // wide
                dnw = "wide";
                StoryRoot.Width = h / 16 * 9;
                StoryRoot.Height = h;
            } else if (w < e) { // narrow
                dnw = "narrow";
                StoryRoot.Width = w;
                StoryRoot.Height = w / 9 * 16;
            } else if (w == e) { // equal
                dnw = "equal";
                StoryRoot.Width = w;
                StoryRoot.Height = h;
            }

            d1.Text = $"Container size: {w}x{h} — {dnw}\nStoryRoot size: {StoryRoot.Width}x{StoryRoot.Height}";
        }

        bool isCAEventRegistered = false;
        Visual dvisual;
        ConnectedAnimation ca;
        private void Animate(FrameworkElement sender, bool isOut = false) {
            if (sender != null && Theme.IsAnimationsEnabled) {
                dvisual = ElementCompositionPreview.GetElementVisual(StoryRoot);
                Compositor dcompositor = dvisual.Compositor;
                var cbef = dcompositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0), new Vector2(0.5f, 1));

                ConnectedAnimationService cas = ConnectedAnimationService.GetForCurrentView();
                cas.DefaultDuration = TimeSpan.FromMilliseconds(animationDuration);
                cas.DefaultEasingFunction = cbef;
                ca = cas.PrepareToAnimate("story", isOut ? StoryRoot : sender);
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) ca.Configuration = new BasicConnectedAnimationConfiguration();

                if (!isCAEventRegistered) {
                    ca.Completed += async (a, b) => {
                        if (!isOut) {
                            // Windows mobile bug
                            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                                StoryRoot.Visibility = Visibility.Collapsed;
                                await Task.Delay(50);
                                StoryRoot.Visibility = Visibility.Visible;
                            }
                        }
                    };
                    isCAEventRegistered = true;
                }

                Visual lvisual = ElementCompositionPreview.GetElementVisual(Layer);
                Compositor lcompositor = lvisual.Compositor;
                lvisual.Opacity = isOut ? 1 : 0;

                ScalarKeyFrameAnimation sfa = lcompositor.CreateScalarKeyFrameAnimation();
                sfa.InsertKeyFrame(1, isOut ? 0 : 1);
                sfa.Duration = TimeSpan.FromMilliseconds(animationDuration + 50);
                sfa.Direction = Windows.UI.Composition.AnimationDirection.Normal;
                sfa.IterationCount = 1;

                lvisual.StartAnimation("Opacity", sfa);
                ca.TryStart(isOut ? sender : StoryRoot);

                d3.Text = $"Animation duration: {animationDuration}ms";
            }
        }

        #region Story

        private async Task SetupStoryAsync() {
            string from = "";
            Uri ava = null;
            long ownerId = Story.OwnerId;
            if (ownerId.IsUser()) {
                User u = AppSession.GetCachedUser(ownerId);
                from = u != null ? u.FullName : $"id{ownerId}";
                ava = u.Photo;
            } else if (ownerId.IsGroup()) {
                Group u = AppSession.GetCachedGroup(ownerId);
                from = u != null ? u.Name : $"club{-ownerId}";
                ava = u.Photo;
            }

            OwnerName.Text = from;
            OwnerAva.DisplayName = from;
            if (ava != null) OwnerAva.ImageUri = ava;

            StoryLink link = Story.Link;
            if (link != null) {
                LinkButton.Content = link.Text;
                LinkButton.Visibility = Visibility.Visible;
                LinkButton.Click += async (a, b) => {
                    await Windows.System.Launcher.LaunchUriAsync(link.Uri);
                    await Close();
                };
            }

            if (Story.CanShare) {
                ShareButton.Visibility = Visibility.Visible;
                ShareButton.Click += async (a, b) => {
                    await Close(null, true);
                    Main.GetCurrent().StartForwardingAttachments(new List<AttachmentBase> { Story });
                };
            }

            if (Story.ClickableStickers != null) SetupClickableStickers(Story.ClickableStickers);

            switch (Story.Type) {
                case StoryType.Photo: await SetUpPhotoStoryAsync(); break;
                case StoryType.Video: await SetUpVideoStoryAsync(); break;
                default: await Close(); break;
            }
        }

        #region Clickable stickers

        private void SetupClickableStickers(ClickableStickersInfo clickableStickers) {
            ClickableStickersContainer.Width = clickableStickers.OriginalWidth;
            ClickableStickersContainer.Height = clickableStickers.OriginalHeight;
            foreach (ClickableSticker sticker in clickableStickers.ClickableStickers) {
                double x1 = StoryRoot.Width;
                double x2 = clickableStickers.OriginalWidth;
                double thickness = 2 / x1 * x2;

                Polygon polygon = new Polygon() {
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Stroke = new SolidColorBrush(AppParameters.StoryClickableStickerBorder ? Colors.Yellow : Colors.Transparent),
                    StrokeThickness = thickness,
                };

                foreach (Point p in sticker.ClickableArea) {
                    polygon.Points.Add(p);
                }

                Border border = new Border {
                    Width = 1,
                    Height = 1,
                    Background = new SolidColorBrush(Colors.Transparent)
                };

                double flyoutTop = sticker.ClickableArea.Select(p => p.Y).Min();
                double flyoutBottom = sticker.ClickableArea.Select(p => p.Y).Max();
                double flyoutLeft = sticker.ClickableArea.Select(p => p.X).Min();
                double flyoutRight = sticker.ClickableArea.Select(p => p.X).Max();
                double flyoutHorizontalCenter = ((flyoutRight - flyoutLeft) / 2) + flyoutLeft;
                double flyoutVerticalCenter = ((flyoutBottom - flyoutTop) / 2) + flyoutTop;

                Canvas.SetLeft(border, flyoutHorizontalCenter);
                Canvas.SetTop(border, flyoutVerticalCenter);

                polygon.Tapped += async (c, d) => {
                    Log.Info($"StoryViewer: user tapped to clickable sticker with type \"{sticker.Type}\"");
                    switch (sticker.Type) {
                        case "mention":
                            PauseStory();
                            ShowMentionFlyout(sticker.Mention, border);
                            break;
                        case "hashtag":
                            PauseStory();
                            ShowDefaultFlyout(border, CheckTooltipText(sticker.TooltipText, Locale.Get("story_clst_hashtag")), '', async () =>
                            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://vk.ru/feed?section=search&q={WebUtility.UrlEncode(sticker.Hashtag)}")));
                            break;
                        case "place":
                            PauseStory();
                            ShowDefaultFlyout(border, CheckTooltipText(sticker.TooltipText, Locale.Get("story_clst_place")), '', async () =>
                            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://m.vk.ru/place{sticker.PlaceId}"))); break;
                        case "sticker":
                            PauseStory();
                            await ShowStickerPackInfoAsync(sticker.StickerPackId);
                            break;
                        case "link":
                            PauseStory();
                            ShowDefaultFlyout(border, CheckTooltipText(sticker.TooltipText, Locale.Get("story_clst_link")), '', async () => {
                                await Close();
                                await VKLinks.LaunchLinkAsync(sticker.LinkObject.Uri);
                            });
                            break;
                        case "post":
                            PauseStory();
                            ShowDefaultFlyout(border, CheckTooltipText(sticker.TooltipText, Locale.Get("story_clst_post")), '', async () =>
                            await ShowPostAsync(sticker.PostOwnerId, sticker.PostId));
                            break;
                        case "poll":
                            PauseStory();
                            await ShowPollAsync(sticker.Poll);
                            break;
                        case "market_item":
                            PauseStory();
                            ShowDefaultFlyout(border, CheckTooltipText(sticker.TooltipText, Locale.Get("story_clst_market")), '', async () =>
                            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://vk.ru/product{sticker.MarketItem.OwnerId}_{sticker.MarketItem.Id}")));
                            break;
                        case "owner":
                            PauseStory();
                            ShowUserOrGroupInfo(sticker.OwnerId, border);
                            break;
                        case "story_reply": // TODO: реализовать получение истории по id.
                            break;
                        default: break;
                    }
                };

                ClickableStickersContainer.Children.Add(polygon);
                ClickableStickersContainer.Children.Add(border);
            }
        }

        private string CheckTooltipText(string tooltipText, string fallback) {
            return string.IsNullOrEmpty(tooltipText) ? fallback : tooltipText;
        }

        private void ShowMentionFlyout(string mention, Border border) {
            long id = VKTextParser.GetMentionId(mention);
            ShowUserOrGroupInfo(id, border);
        }

        private void ShowUserOrGroupInfo(long id, Border border) {
            VKLinks.ShowPeerInfoModal(id, () => PlayStory());
        }

        private void ShowDefaultFlyout(Border target, string text, char symbol, System.Action action) {
            MenuFlyout mf = new MenuFlyout { Placement = FlyoutPlacementMode.Top };
            MenuFlyoutItem mfi = new MenuFlyoutItem {
                Text = text,
                Icon = new FixedFontIcon { Glyph = symbol.ToString() }
            };
            mfi.Click += (a, b) => action?.Invoke();
            mf.Items.Add(mfi);
            mf.Closed += (a, b) => PlayStory();
            mf.ShowAt(target);
        }

        private async Task ShowPollAsync(Poll poll) {
            await VKLinks.ShowPollAsync(poll.OwnerId, poll.Id, (a, b) => PlayStory());
        }

        private async Task ShowPostAsync(long ownerId, long id) {
            await VKLinks.ShowWallPostAsync(ownerId, id, null, (a, b) => PlayStory());
        }

        private async Task ShowStickerPackInfoAsync(long stickerPackId) {
            if (!AppSession.ActiveStickerPacks.Contains(stickerPackId)) {
                await VKLinks.ShowStickerPackInfoAsync(stickerPackId, (a, b) => PlayStory());
            }
        }

        #endregion

        private async Task SetUpPhotoStoryAsync() {
            BitmapImage image = new BitmapImage();
            PhotoStoryBackground.ImageSource = image;
            await image.SetUriSourceAsync(Story.Photo.PreviewImageUri, true);

            // Load process
            Uri uri = Story.Photo.MaximalSizedPhoto.Uri;
            try {
                HttpResponseMessage hmsg = await LNet.GetAsync(uri, dontSendCookies: true);
                hmsg.EnsureSuccessStatusCode();

                using (var stream = await hmsg.Content.ReadAsStreamAsync()) {
                    using (var memStream = new MemoryStream()) {
                        await stream.CopyToAsync(memStream);
                        memStream.Position = 0;
                        image.SetSource(memStream.AsRandomAccessStream());

                        // Loaded, now show photo and run timer.
                        StoryLoadScreen.Visibility = Visibility.Collapsed;

                        double timerms = 4000;
                        double timerinterval = 40;
                        StoryProgress.Maximum = timerms;
                        StoryTimer = new DispatcherTimer();
                        StoryTimer.Interval = TimeSpan.FromMilliseconds(timerinterval);
                        StoryTimer.Tick += async (a, b) => {
                            if (StoryProgress.Value >= timerms) {
                                await Close();
                                return;
                            }
                            StoryProgress.Value += timerinterval;
                        };
                        StoryTimer.Start();
                        IsReady = true;
                    }
                }
            } catch (Exception ex) {
                Log.Error($"StoryViewer.SetUpPhotoStory error 0x{ex.HResult.ToString("x8")}: {ex.Message}\nUrl: {uri}");
            }
        }

        private async Task SetUpVideoStoryAsync() {
            BitmapImage image = new BitmapImage();
            PhotoStoryBackground.ImageSource = image;
            await image.SetUriSourceAsync(Story.Video.FirstFrameForStory.Uri);

            // Setup VideoStory
            VideoFiles files = Story.Video.Files;
            string url = files.MP4p240;
            if (!string.IsNullOrEmpty(files.MP4p360)) url = files.MP4p360;
            if (!string.IsNullOrEmpty(files.MP4p480)) url = files.MP4p480;
            if (!string.IsNullOrEmpty(files.MP4p720)) url = files.MP4p720;
            if (!string.IsNullOrEmpty(files.MP4p1080)) url = files.MP4p1080;

            Player = new MediaPlayer();

            Player.Source = MediaSource.CreateFromUri(new Uri(url));

            Player.PlaybackSession.PlaybackStateChanged += PlaybackStateChanged;
            Player.MediaEnded += MediaPlayer_MediaEnded;
            VideoStory.SetMediaPlayer(Player);
            Player.Play();
            IsReady = true;
        }

        private void PlaybackStateChanged(MediaPlaybackSession sender, object args) {
            new System.Action(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    switch (sender.PlaybackState) {
                        case MediaPlaybackState.Playing: ShowVideo(); break;
                        default: if (StoryTimer != null) StoryTimer.Stop(); break;
                    }
                });
            })();
        }

        private void ShowVideo() {
            VideoStory.Visibility = Visibility.Visible;
            StoryLoadScreen.Visibility = Visibility.Collapsed;
            if (StoryTimer == null) {
                double timerms = Player.PlaybackSession.NaturalDuration.TotalMilliseconds;
                double timerinterval = 50;
                StoryProgress.Maximum = timerms;
                StoryTimer = new DispatcherTimer();
                StoryTimer.Interval = TimeSpan.FromMilliseconds(timerinterval);
                StoryTimer.Tick += (a, b) => {
                    StoryProgress.Value = Player.PlaybackSession.Position.TotalMilliseconds;
                };
            }
            StoryTimer.Start();
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args) {
            new System.Action(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                    Player.PlaybackSession.PlaybackStateChanged -= PlaybackStateChanged;
                    Player.MediaEnded -= MediaPlayer_MediaEnded;
                    await Close();
                });
            })();
        }

        private void PauseStory() {
            if (Story == null) return;
            if (Story.Type == StoryType.Photo) {
                StoryTimer?.Stop();
            } else if (Story.Type == StoryType.Video) {
                Player?.Pause();
            }
        }

        private void PlayStory() {
            if (Story == null) return;
            if (Story.Type == StoryType.Photo) {
                StoryTimer?.Start();
            } else if (Story.Type == StoryType.Video) {
                Player?.Play();
            }
        }

        #endregion

        #region Events

        private void Close(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await Close(); })();
        }

        private void LayerTapped(object sender, TappedRoutedEventArgs e) {
            new System.Action(async () => { await Close(); })();
        }

        private void RootPointerPressed(object sender, PointerRoutedEventArgs e) {
            if (!IsReady) return;
            ControlsHideAnimation.Begin();
            PauseStory();
        }

        private void RootPointerReleased(object sender, PointerRoutedEventArgs e) {
            if (!IsReady) return;
            ControlsShowAnimation.Begin();
            PlayStory();
        }

        #endregion

        #region Static members

        public static void Show(Story story, FrameworkElement sender = null) {
            Log.Info($"StoryViewer (static) > Opening story viewer...");
            StoryViewer sv = new StoryViewer(story, sender);
            sv.Closed += async (m, n) => {
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                    await Theme.UpdateTitleBarColors(App.UISettings);
                }
            };
            sv.Show();
        }

        #endregion
    }
}