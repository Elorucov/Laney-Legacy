using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Views.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using VK.VKUI.Controls;
using Windows.ApplicationModel.Core;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Elorucov.Laney.Views
{
    public sealed partial class StoryViewer : OverlayModal
    {
        private int animationDuration = Core.Settings.StoryViewerSlowDownAnimation ? 5000 : 370;
        private DispatcherTimer StoryTimer;
        MediaPlayer Player;

        private FrameworkElement FromControl;
        Story Story;
        bool IsReady = false;

        public StoryViewer(Story story, FrameworkElement from = null)
        {
            this.InitializeComponent();
            Story = story;
            FromControl = from == null ? DefaultFromAnimator : from;

            Loaded += (a, b) =>
            {
                SystemNavigationManager.GetForCurrentView().BackRequested += StoryViewer_BackRequested;
                ResizeStoryRoot();
                Animate(FromControl);
                SizeChanged += (c, d) => ResizeStoryRoot();
                SetupStory();
            };
        }

        #region Story UI

        private void ResizeStoryRoot()
        {
            double tm = 0;
            if (OSHelper.IsDesktop)
            {
                tm = CoreApplication.GetCurrentView().TitleBar.Height;
            }
            double w = Container.ActualWidth;
            double h = Container.ActualHeight - (tm * 2);
            double e = h / 16 * 9;
            if (w > e)
            { // wide
                StoryCard.Width = h / 16 * 9;
                StoryCard.Height = h;
            }
            else if (w < e)
            { // narrow
                StoryCard.Width = w;
                StoryCard.Height = w / 9 * 16;
            }
            else if (w == e)
            { // equal
                StoryCard.Width = w;
                StoryCard.Height = h;
            }
        }

        Visual dvisual;
        ConnectedAnimation ca;
        private void Animate(FrameworkElement sender, bool isOut = false)
        {
            if (sender != null)
            {
                dvisual = ElementCompositionPreview.GetElementVisual(StoryCard);
                Compositor dcompositor = dvisual.Compositor;
                var cbef = dcompositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0), new Vector2(0.5f, 1));

                ConnectedAnimationService cas = ConnectedAnimationService.GetForCurrentView();
                cas.DefaultDuration = TimeSpan.FromMilliseconds(animationDuration);
                cas.DefaultEasingFunction = cbef;
                ca = cas.PrepareToAnimate("story", isOut ? StoryCard : sender);

                Visual lvisual = ElementCompositionPreview.GetElementVisual(OverlayBackground);
                Compositor lcompositor = lvisual.Compositor;
                lvisual.Opacity = isOut ? 1 : 0;

                ScalarKeyFrameAnimation sfa = lcompositor.CreateScalarKeyFrameAnimation();
                sfa.InsertKeyFrame(1, isOut ? 0 : 1);
                sfa.Duration = TimeSpan.FromMilliseconds(animationDuration + 50);
                sfa.Direction = Windows.UI.Composition.AnimationDirection.Normal;
                sfa.IterationCount = 1;

                lvisual.StartAnimation("Opacity", sfa);
                ca.TryStart(isOut ? sender : StoryCard);
            }
        }

        #endregion

        #region Story object

        private void SetupStory()
        {
            string from = "";
            Uri ava = null;
            var owner = CacheManager.GetNameAndAvatar(Story.OwnerId);
            from = String.Join(" ", new List<string> { owner.Item1, owner.Item2 });
            ava = owner.Item3;

            OwnerName.Text = from;
            OwnerAva.DisplayName = from;
            if (ava != null) OwnerAva.ImageUri = ava;

            StoryLink link = Story.Link;
            if (link != null)
            {
                LinkButton.Content = link.Text;
                LinkButton.Visibility = Visibility.Visible;
                LinkButton.Click += async (a, b) =>
                {
                    await Router.LaunchLinkAsync(link.Uri);
                    Close();
                };
            }

            if (Story.CanShare == 1)
            { // TODO: bool
                ShareButton.Visibility = Visibility.Visible;
                ShareButton.Click += (a, b) =>
                {
                    Close();

                    InternalSharing ish = new InternalSharing(Story);
                    ish.Show();
                };
            }

            if (Story.ClickableStickers != null) SetupClickableStickers(Story.ClickableStickers);

            switch (Story.Type)
            {
                case StoryType.Photo: SetUpPhotoStory(); break;
                case StoryType.Video: SetUpVideoStory(); break;
                default: Close(); break;
            }
        }

        private async void SetUpPhotoStory()
        {
            PhotoStoryBackground.ImageSource = new BitmapImage(Story.Photo.PreviewImageUri);

            // Load process
            BitmapImage photo = new BitmapImage();
            var rq = (HttpWebRequest)WebRequest.Create(Story.Photo.MaximalSizedPhoto.Uri);
            rq.Method = "GET";

            var rs = (HttpWebResponse)await rq.GetResponseAsync();

            MemoryStream ms = new MemoryStream();
            var str = rs.GetResponseStream();
            str.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            using (InMemoryRandomAccessStream mras = new InMemoryRandomAccessStream())
            {
                using (DataWriter dw = new DataWriter(mras.GetOutputStreamAt(0)))
                {
                    dw.WriteBytes(ms.ToArray());
                    await dw.StoreAsync();
                }
                photo.SetSource(mras);
            }

            // Loaded, now show photo and run timer.
            PhotoStoryBackground.ImageSource = photo;
            StoryLoadScreen.Visibility = Visibility.Collapsed;

            double timerms = 5000;
            StoryProgress.Maximum = timerms;
            Stopwatch sw = new Stopwatch();

            StoryTimer = new DispatcherTimer();
            StoryTimer.Interval = TimeSpan.FromMilliseconds(32);
            StoryTimer.Tick += (a, b) =>
            {
                if (sw.ElapsedMilliseconds >= timerms)
                {
                    sw.Stop();
                    Close();
                    return;
                }
                StoryProgress.Value = sw.ElapsedMilliseconds;
            };
            StoryTimer.Start();
            sw.Start();
            IsReady = true;
        }

        private void SetUpVideoStory()
        {
            PhotoStoryBackground.ImageSource = new BitmapImage(Story.Video.FirstFrameForStory.Uri);

            // Setup VideoStory
            VideoFiles files = Story.Video.Files;
            string url = files.MP4p360;
            if (!String.IsNullOrEmpty(files.MP4p480)) url = files.MP4p480;
            if (!String.IsNullOrEmpty(files.MP4p720)) url = files.MP4p720;
            if (!String.IsNullOrEmpty(files.MP4p1080)) url = files.MP4p1080;

            Player = new MediaPlayer();
            Player.Source = MediaSource.CreateFromUri(new Uri(url));
            Player.PlaybackSession.PlaybackStateChanged += PlaybackStateChanged;
            Player.MediaEnded += MediaPlayer_MediaEnded;
            VideoStory.SetMediaPlayer(Player);
            Player.Play();
            IsReady = true;
        }

        private async void PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (sender.PlaybackState)
                {
                    case MediaPlaybackState.Playing: ShowVideo(); break;
                    default: if (StoryTimer != null) StoryTimer.Stop(); break;
                }
            });
        }

        private void ShowVideo()
        {
            VideoStory.Visibility = Visibility.Visible;
            StoryLoadScreen.Visibility = Visibility.Collapsed;
            if (StoryTimer == null)
            {
                double timerms = Player.PlaybackSession.NaturalDuration.TotalMilliseconds;
                double timerinterval = 50;
                StoryProgress.Maximum = timerms;
                StoryTimer = new DispatcherTimer();
                StoryTimer.Interval = TimeSpan.FromMilliseconds(timerinterval);
                StoryTimer.Tick += (a, b) =>
                {
                    StoryProgress.Value = Player.PlaybackSession.Position.TotalMilliseconds;
                };
            }
            StoryTimer.Start();
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Player.PlaybackSession.PlaybackStateChanged -= PlaybackStateChanged;
                Player.MediaEnded -= MediaPlayer_MediaEnded;
                Close();
            });
        }

        private void PauseStory()
        {
            if (Story == null) return;
            if (Story.Type == StoryType.Photo)
            {
                StoryTimer?.Stop();
            }
            else if (Story.Type == StoryType.Video)
            {
                Player?.Pause();
            }
        }

        private void PlayStory()
        {
            if (Story == null) return;
            if (Story.Type == StoryType.Photo)
            {
                StoryTimer?.Start();
            }
            else if (Story.Type == StoryType.Video)
            {
                Player?.Play();
            }
        }

        #endregion

        #region Clickable stickers

        private void SetupClickableStickers(ClickableStickersInfo clickableStickers)
        {
            if (Core.Settings.StoryViewerNoLightThemeForFlyouts) ClickableStickersContainer.RequestedTheme = ElementTheme.Default;
            ClickableStickersContainer.Width = clickableStickers.OriginalWidth;
            ClickableStickersContainer.Height = clickableStickers.OriginalHeight;
            foreach (ClickableSticker sticker in clickableStickers.ClickableStickers)
            {
                double x1 = StoryCard.Width;
                double x2 = clickableStickers.OriginalWidth;
                double thickness = 2 / x1 * x2;

                Polygon polygon = new Polygon()
                {
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Stroke = new SolidColorBrush(Core.Settings.StoryViewerClickableStickerBorder ? Colors.Yellow : Colors.Transparent),
                    StrokeThickness = thickness,
                };

                foreach (var p in sticker.ClickableArea)
                {
                    polygon.Points.Add(p.ToPoint());
                }

                Border focusableBorder = new Border
                {
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

                Canvas.SetLeft(focusableBorder, flyoutHorizontalCenter);
                Canvas.SetTop(focusableBorder, flyoutVerticalCenter);

                polygon.Tapped += (c, d) =>
                {
                    Log.General.Info($"User tapped to clickable sticker with type: {sticker.Type}");
                    switch (sticker.Type)
                    {
                        case "mention":
                            PauseStory();
                            ShowUserOrGroupFlyout(sticker.Mention, focusableBorder);
                            break;
                        case "hashtag":
                            PauseStory();
                            ShowDefaultFlyout(focusableBorder, Locale.Get("story_clst_hashtag"), VKIconName.Icon28SearchOutline, async () =>
                            await Router.LaunchLinkAsync(new Uri($"https://vk.com/feed?section=search&q={WebUtility.UrlEncode(sticker.Hashtag)}")));
                            break;
                        case "place":
                            PauseStory();
                            ShowDefaultFlyout(focusableBorder, Locale.Get("story_clst_place"), VKIconName.Icon28PlaceOutline, async () =>
                            await Router.LaunchLinkAsync(new Uri($"https://m.vk.com/place{sticker.PlaceId}"))); break;
                        case "sticker": break; // TODO: реализовать отображение наборов стикеров (не только для историй, но и для сообщений)
                        case "link":
                            PauseStory();
                            ShowDefaultFlyout(focusableBorder, Locale.Get("story_clst_link"), VKIconName.Icon28LinkOutline, async () =>
                            await Router.LaunchLinkAsync(sticker.LinkObject.Uri));
                            break;
                        case "post":
                            PauseStory();
                            ShowDefaultFlyout(focusableBorder, Locale.Get("story_clst_post"), VKIconName.Icon28LinkOutline, async () =>
                            await Router.LaunchLinkAsync(new Uri($"https://vk.com/wall{sticker.PostOwnerId}_{sticker.PostId}")));
                            break;
                        case "poll":
                            PauseStory();
                            PollViewer pv = new PollViewer(sticker.Poll);
                            pv.Closed += (a, b) => PlayStory();
                            pv.Show();
                            break;
                        case "market_item":
                            PauseStory();
                            ShowDefaultFlyout(focusableBorder, Locale.Get("story_clst_market"), VKIconName.Icon28MarketOutline, async () =>
                            await Router.LaunchLinkAsync(new Uri($"https://vk.com/product{sticker.MarketItem.OwnerId}_{sticker.MarketItem.Id}")));
                            break;
                        case "owner":
                            PauseStory();
                            ShowUserOrGroupFlyout(focusableBorder, sticker.OwnerId);
                            break;
                        case "story_reply": // TODO.
                            break;
                        default: break;
                    }
                };

                ClickableStickersContainer.Children.Add(polygon);
                ClickableStickersContainer.Children.Add(focusableBorder);
            }
        }

        private void ShowUserOrGroupFlyout(string mention, Border border)
        {
            int id = VKTextParser.GetMentionId(mention);
            ShowUserOrGroupFlyout(border, id);
        }

        private void ShowUserOrGroupFlyout(Border target, int id)
        {
            var info = CacheManager.GetNameAndAvatar(id);

            // Зачем ждать получения объекта юзера/группы из апи
            // только для отображения названия и авы,
            // если можно отобразить окно сразу же после получения.
            if (info == null)
            {
                Close();
                Router.ShowCard(id);
                return;
            }

            string fullName = String.Join(" ", new string[] { info.Item1, info.Item2 });

            VK.VKUI.Popups.Flyout flyout = new VK.VKUI.Popups.Flyout
            {
                Placement = FlyoutPlacementMode.Top,
                PresenterStyle = (Style)Application.Current.Resources["FlyoutWithoutPadding"]
            };
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            sp.Children.Add(new Avatar
            {
                DisplayName = fullName,
                ImageUri = info.Item3,
                Width = 32,
                Height = 32,
                Margin = new Thickness(8)
            });
            sp.Children.Add(new TextBlock
            {
                Text = fullName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 15,
                LineHeight = 19,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                Margin = new Thickness(0, 10, 12, 11),
                VerticalAlignment = VerticalAlignment.Center
            });

            Button button = new Button
            {
                Style = (Style)Application.Current.Resources["TransparentButtonStyle"],
                Padding = new Thickness(0),
                Height = 48,
                Content = sp
            };

            button.Click += (a, b) =>
            {
                Close();
                Router.ShowCard(id);
            };
            flyout.Content = button;
            flyout.Closed += (a, b) => PlayStory();
            flyout.ShowAt(target);
        }

        private void ShowDefaultFlyout(Border target, string text, VKIconName icon, System.Action action)
        {
            VK.VKUI.Popups.MenuFlyout mf = new VK.VKUI.Popups.MenuFlyout { Placement = FlyoutPlacementMode.Top };
            CellButton cb = new CellButton
            {
                Text = text,
                Icon = icon
            };
            cb.Click += (a, b) => action?.Invoke();
            mf.Items.Add(cb);
            mf.Closed += (a, b) => PlayStory();
            mf.ShowAt(target);
        }

        #endregion

        #region Events

        private void StoryViewer_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Close();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OverlayTapped(object sender, TappedRoutedEventArgs e)
        {
            Close();
        }

        private void CardPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsReady) return;
            ControlsHideAnimation.Begin();
            PauseStory();
        }

        private void CardPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!IsReady) return;
            ControlsShowAnimation.Begin();
            PlayStory();
        }

        #endregion

        private async void Close(object data = null, bool alternativeAnimation = false)
        {
            IsHitTestVisible = false;
            if (StoryTimer != null)
            {
                StoryTimer.Stop();
                StoryTimer = null;
            }
            if (Player != null)
            {
                Player.Dispose();
                Player = null;
            }
            SystemNavigationManager.GetForCurrentView().BackRequested -= StoryViewer_BackRequested;
            Animate(alternativeAnimation ? DefaultFromAnimator : FromControl, true);
            await Task.Delay(animationDuration - 10);
            StoryCard.Children.Clear(); // to fix strange visual bug on video stories
            Hide(data);
        }

        #region Static

        public static void Show(Story story, FrameworkElement from = null)
        {
            if (story.IsExpired || story.IsDeleted || story.IsRestricted) return;
            StoryViewer sv = new StoryViewer(story, from);
            sv.Show();
        }

        #endregion
    }
}