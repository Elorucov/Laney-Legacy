using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Elorucov.Laney.Controls {
    public class ChatListView : ListView {
        public ConversationViewModel ViewModel => DataContext as ConversationViewModel;

        public ScrollViewer ScrollingHost => (ScrollViewer)GetTemplateChild("ScrollViewer");
        public ItemsStackPanel ItemsStack => ItemsPanelRoot as ItemsStackPanel;

        private DisposableMutex _loadMoreLock = new DisposableMutex();
        private bool canTriggerIncrementalLoading = true;
        private bool isWindowActive = false;

        public ChatListView() {
            DefaultStyleKey = typeof(ListView);

            DataContextChanged += async (a, b) => {
                if (ViewModel != null) {
                    canTriggerIncrementalLoading = false;

                    RegisterContainerContentChangingEvent();
                    ViewModel.ScrollToMessageCallback = ScrollToMessage;

                    if (ViewModel.FirstVisibleMessage != null) {
                        await ScrollToItem(ViewModel.FirstVisibleMessage, VerticalAlignment.Top, false, null, ScrollIntoViewAlignment.Leading, true);
                    }

                    await Task.Delay(500);
                    canTriggerIncrementalLoading = true;
                }
            };

            Loaded += OnLoaded;
        }

        bool eventRegistered = false;
        private void RegisterContainerContentChangingEvent() {
            if (eventRegistered) return;
            if (!AppParameters.MessageRenderingPhase || ViewModel.ConversationId == int.MaxValue) this.ContainerContentChanging += ChatListView_ContainerContentChanging;
            eventRegistered = true;
        }

        private void ChatListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args) {
            if (args.InRecycleQueue) return;

            var msg = args.Item as LMessage;
            if (ViewModel == null) return;
            if (msg == null) {
                Log.Warn($"No message associated for ListView item container! InRecycleQueue: {args.InRecycleQueue}; Index: {args.ItemIndex}; Phase: {args.Phase}");
                return;
            }
            bool isGroupChannel = ViewModel.ChatSettings != null && ViewModel.ChatSettings.IsGroupChannel;

            if (args.ItemContainer != null &&
                args.ItemContainer is ListViewItem lvi &&
                lvi.ContentTemplateRoot is Microsoft.UI.Xaml.Controls.SwipeControl swc &&
                msg != null) {
                LMessage prev = null;

                if (ViewModel.ConversationId.IsChat()) {
                    MessagesCollection mc = ViewModel.Messages;
                    int idx = args.ItemIndex;
                    if (idx > 0 && mc != null && mc.Count > 0 && idx <= mc.Count) {
                        prev = mc[idx - 1];
                    }
                }

                if (isGroupChannel || msg.IsExpired || msg.Action != null || ViewModel.ConversationId == long.MaxValue) {
                    swc.RightItems = null;
                } else {
                    var replyCommand = new Microsoft.UI.Xaml.Controls.SwipeItem {
                        BehaviorOnInvoked = Microsoft.UI.Xaml.Controls.SwipeBehaviorOnInvoked.Close,
                        Background = new SolidColorBrush(Colors.Transparent),
                    };
                    replyCommand.Invoked += (a, b) => {
                        ViewModel.MessageFormViewModel.AddReplyMessage(msg);
                    };
                    swc.RightItems = new Microsoft.UI.Xaml.Controls.SwipeItems {
                        Mode = Microsoft.UI.Xaml.Controls.SwipeMode.Execute
                    };
                    swc.RightItems.Add(replyCommand);
                }

                Debug.WriteLine($"Build UI for {msg.PeerId}_{msg.ConversationMessageId}");
                Stopwatch sw = Stopwatch.StartNew();

                args.ItemContainer.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                args.ItemContainer.MinHeight = 0;
                args.ItemContainer.BorderThickness = new Thickness(0);

                try {
                    swc.Content = MessageUIHelper.Build(msg, prev, ScrollingHost);

                    // Disappearing message
                    if (msg.TTL > 0) {
                        TimeSpan expiration = DateTime.Now - msg.Date;
                        int remaining = msg.TTL - Convert.ToInt32(expiration.TotalSeconds);
                    }

                    sw.Stop();
                    string prevs = prev != null ? $" Prev: {prev.PeerId}_{prev.ConversationMessageId}" : string.Empty;
                    Debug.WriteLine($"UI for {msg.PeerId}_{msg.ConversationMessageId} built in {sw.ElapsedMilliseconds} ms.{prevs}");

                    msg.MessageEditedCallback = async () => {
                        if (msg.Action != null) await Task.Delay(500); // иногда в кэше не оказывается member_id
                        swc.Content = MessageUIHelper.Build(msg, prev, ScrollingHost);
                        if (msg.TTL > 0) {
                            TimeSpan expiration = DateTime.Now - msg.Date;
                            int remaining = msg.TTL - Convert.ToInt32(expiration.TotalSeconds);
                        }
                    };
                } catch (Exception ex) {
                    HyperlinkButton hbtn = new HyperlinkButton {
                        Padding = new Thickness(0),
                        FontSize = 13,
                        ContentTemplate = (DataTemplate)Application.Current.Resources["TextLikeHyperlinkBtnTemplate"],
                        Content = "An error occured when rendering this message. Click here for more info."
                    };
                    hbtn.Click += async (a, b) => {
                        await new MessageDialog($"{ex.Message}\n\nStackTrace:\n{ex.StackTrace}", $"HResult: 0x{ex.HResult.ToString("x8")}").ShowAsync();
                    };
                    swc.Content = new ContentControl {
                        Template = (ControlTemplate)Application.Current.Resources["ActionMessageTemplate"],
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Content = hbtn
                    };
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            SetScrollMode();
        }

        protected override void OnApplyTemplate() {
            ScrollingHost.ViewChanged += ScrollingHost_ViewChanged;

            // Костыль для того, чтобы сообщения не отмечались прочитанными при неактивном окне.
            Window.Current.Activated += (a, b) => {
                isWindowActive = b.WindowActivationState != Windows.UI.Core.CoreWindowActivationState.Deactivated;
                if (ItemsStack == null) return;
                if (ItemsStack.ItemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView && !isWindowActive)
                    ItemsStack.ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepScrollOffset;
            };
            base.OnApplyTemplate();
        }

        private void ScrollingHost_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (ScrollingHost == null || ItemsStack == null || ViewModel == null || !canTriggerIncrementalLoading) {
                return;
            }

            new Action(async () => {
                try {
                    using (await _loadMoreLock.WaitAsync()) {
                        if (ItemsStack.FirstVisibleIndex >= 0 && Items.Count > ItemsStack.FirstVisibleIndex) {
                            ViewModel.FirstVisibleMessage = (LMessage)Items[ItemsStack.FirstVisibleIndex];
                        } else {
                            ViewModel.FirstVisibleMessage = null;
                        }
                        ViewModel.LastVisibleMessage = ItemsStack.LastVisibleIndex >= 0 ? (LMessage)Items[ItemsStack.LastVisibleIndex] : null;
                        if (!e.IsIntermediate) {
                            if (ScrollingHost.VerticalOffset < ScrollingHost.ActualHeight) {
                                Debug.WriteLine("Load previous");
                                SetScrollMode(ItemsUpdatingScrollMode.KeepLastItemInView, true);
                                await ViewModel.LoadPreviousMessagesAsync();
                            } else if (ScrollingHost.VerticalOffset > ScrollingHost.ScrollableHeight - 10) {
                                if ((Items.Last() as LMessage).ConversationMessageId == ViewModel.LastMessage.ConversationMessageId) {
                                    SetScrollMode(ItemsUpdatingScrollMode.KeepLastItemInView, true);
                                } else {
                                    Debug.WriteLine("Load next");
                                    SetScrollMode(ItemsUpdatingScrollMode.KeepItemsInView, true);
                                    await ViewModel.LoadNextMessagesAsync();
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    Log.Error($"Error in ScrollingHost_ViewChanged! 0x{ex.HResult.ToString("x8")}");
                }
            })();
        }

        private ItemsUpdatingScrollMode? _pendingMode;
        private bool? _pendingForce;

        public void SetScrollMode() {
            if (_pendingMode is ItemsUpdatingScrollMode mode && _pendingForce is bool force) {
                _pendingMode = null;
                _pendingForce = null;

                SetScrollMode(mode, force);
            }
        }

        public void SetScrollMode(ItemsUpdatingScrollMode mode, bool force) {
            if (ItemsStack == null) {
                _pendingMode = mode;
                _pendingForce = force;

                return;
            }

            var scroll = ScrollingHost;
            if (scroll == null) {
                _pendingMode = mode;
                _pendingForce = force;

                return;
            }

            if (mode == ItemsUpdatingScrollMode.KeepItemsInView && (force || scroll.VerticalOffset < 200)) {
                Debug.WriteLine("Changed scrolling mode to KeepItemsInView");
                ItemsStack.ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepItemsInView;
            } else if (mode == ItemsUpdatingScrollMode.KeepLastItemInView && (force || scroll.ScrollableHeight - scroll.VerticalOffset < 200)) {
                Debug.WriteLine($"Changed scrolling mode to KeepLastItemInView. isWindowActive: {isWindowActive}");
                if (isWindowActive) ItemsStack.ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepLastItemInView;
            }
        }

        private void ScrollToMessage(int messageId, bool smoothScroll = false, bool isNewMessage = false) {
            new Action(async () => {
                try {
                    Debug.WriteLine($"ScrollToMessage callback {messageId}");
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                        var m = from q in ViewModel.Messages where q.ConversationMessageId == messageId select q;
                        if (m.Count() == 1) {
                            await Task.Delay(1);
                            Debug.WriteLine($"ScrollToMessage: scrolling...");
                            if (isNewMessage) {
                                if (ViewModel.LastVisibleMessage != null && messageId == ViewModel.LastVisibleMessage.ConversationMessageId) await ScrollToItem(m.First(), VerticalAlignment.Center, !isNewMessage, null, ScrollIntoViewAlignment.Leading, !smoothScroll);
                            } else {
                                await ScrollToItem(m.First(), VerticalAlignment.Center, !isNewMessage, null, ScrollIntoViewAlignment.Leading, !smoothScroll);
                            }
                        }
                    });
                } catch (Exception ex) {
                    Log.Error(ex, $"ScrollToMessage failed. MsgID: {messageId}");
                }
            })();
        }

        public async Task ScrollToItem(object item, VerticalAlignment alignment, bool highlight, double? pixel = null, ScrollIntoViewAlignment direction = ScrollIntoViewAlignment.Leading, bool disableAnimation = false) {
            var scrollViewer = ScrollingHost;
            if (scrollViewer == null) return;

            // We are going to try two times, as the first one seem to fail sometimes
            // leading the chat to the wrong scrolling position
            var iter = 2;

            var selectorItem = (SelectorItem)ContainerFromItem(item);
            var itemRoot = selectorItem?.ContentTemplateRoot as Microsoft.UI.Xaml.Controls.SwipeControl;
            try {
                while (selectorItem == null && iter > 0) {
                    Debug.WriteLine(string.Format("selectorItem == null, {0} try", iter + 1));

                    // call task-based ScrollIntoViewAsync to realize the item
                    await Task.Yield();
                    await this.ScrollIntoViewAsync(item, direction);

                    // this time the item shouldn't be null again
                    selectorItem = (SelectorItem)ContainerFromItem(item);
                    itemRoot = selectorItem?.ContentTemplateRoot as Microsoft.UI.Xaml.Controls.SwipeControl;
                    iter--;
                }
            } catch (Exception ex) {
                Log.Error($"ChatListView.ScrollToItem internal exception: 0x{ex.HResult.ToString("x8")} — {ex.Message}");
            }

            if (selectorItem == null) {
                Debug.WriteLine("selectorItem == null, abort");
                return;
            }

            // calculate the position object in order to know how much to scroll to
            var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
            var position = transform.TransformPoint(new Point(0, 0));

            if (alignment == VerticalAlignment.Top) {
                if (pixel is double adjust) {
                    position.Y -= adjust;
                }
            } else if (alignment == VerticalAlignment.Center) {
                if (selectorItem.ActualHeight < ActualHeight - 48) {
                    position.Y -= (ActualHeight - selectorItem.ActualHeight) / 2d;
                } else {
                    position.Y -= 48 + 4;
                }
            } else if (alignment == VerticalAlignment.Bottom) {
                position.Y -= ActualHeight - selectorItem.ActualHeight;

                if (pixel is double adjust) {
                    position.Y += adjust;
                }
            }

            // scroll to desired position with animation!
            if (!Theme.IsAnimationsEnabled) disableAnimation = true;
            scrollViewer.ChangeView(null, position.Y, null, disableAnimation);

            if (highlight) {
                Highlight(itemRoot != null ? (Control)itemRoot : (Control)selectorItem);
            }
        }

        private void Highlight(Control element) {
            element.Background = new SolidColorBrush { Color = (Color)element.Resources["SystemAccentColor"], Opacity = 0.7 };

            ColorAnimation ca = new ColorAnimation();
            ca.To = Colors.Transparent;
            ca.Duration = TimeSpan.FromSeconds(3);

            Storyboard.SetTarget(ca, element.Background);
            Storyboard.SetTargetProperty(ca, "Color");

            Storyboard sb = new Storyboard();
            sb.Children.Add(ca);

            sb.Begin();
        }
    }
}