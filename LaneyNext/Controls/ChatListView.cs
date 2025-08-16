using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Elorucov.Laney.Controls
{
    public class ChatListView : ListView
    {
        public ConversationViewModel ViewModel => DataContext as ConversationViewModel;

        private Dictionary<ConversationViewModel, MessageViewModel> CurrentPositions = new Dictionary<ConversationViewModel, MessageViewModel>();
        public ScrollViewer ScrollingHost { get; private set; }
        public ItemsStackPanel ItemsStack { get; private set; }

        public event EventHandler<Tuple<double, double>> Scrolling;

        private DisposableMutex _loadMoreLock = new DisposableMutex();

        public ChatListView()
        {
            DefaultStyleKey = typeof(ListView);

            DataContextChanged += async (a, b) =>
            {
                if (ViewModel == null) return;
                ViewModel.ScrollToMessageCallback = ScrollToMessage;
                ViewModel.MessageAddedToLastCallback = MessageAddedToLast;
                if (CurrentPositions.ContainsKey(ViewModel))
                {
                    await ScrollToItem(CurrentPositions[ViewModel], VerticalAlignment.Bottom, false, null, ScrollIntoViewAlignment.Leading, true);
                }
            };

            Loaded += OnLoaded;
            this.ContainerContentChanging += ChatListView_ContainerContentChanging;
        }

        private void ChatListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var msg = args.Item as MessageViewModel;
            if (ViewModel == null) return;
            if (args.InRecycleQueue && msg != null)
            {
                msg.NeedRedrawCallback = null;
            }

            if (args.ItemContainer != null && args.ItemContainer.ContentTemplateRoot != null &&
                args.ItemContainer is ListViewItem && msg != null)
            {
                MessageViewModel prev = null;

                if (ViewModel.ChatSettings != null)
                {
                    MessagesCollection mc = ViewModel.Messages;
                    if (args.ItemIndex > 0 && mc != null && mc.Count > 0 && args.ItemIndex < mc.Count)
                    {
                        prev = mc[args.ItemIndex - 1];
                    }
                }

                args.ItemContainer.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                args.ItemContainer.MinHeight = 0;
                args.ItemContainer.BorderThickness = new Thickness(0);

                Border container = args.ItemContainer.ContentTemplateRoot as Border;
                MessageView mw = new MessageView(msg, prev, false, container, ActualWidth);
                args.ItemContainer.Tag = mw;
                msg.NeedRedrawCallback = async () =>
                {
                    await container.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        MessageView mv = new MessageView(msg, prev, false, container, ActualWidth);
                        args.ItemContainer.Tag = mv;
                    });
                };

                args.ItemContainer.Unloaded += (a, b) => mw.Dispose();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var panel = ItemsPanelRoot as ItemsStackPanel;
            if (panel != null)
            {
                if (ViewModel != null)
                {
                    ViewModel.ScrollToMessageCallback = ScrollToMessage;
                    ViewModel.MessageAddedToLastCallback = MessageAddedToLast;
                }
                ItemsStack = panel;

                SetScrollMode();
            }
        }

        protected override void OnApplyTemplate()
        {
            ScrollingHost = (ScrollViewer)GetTemplateChild("ScrollViewer");
            ScrollingHost.ViewChanged += ScrollingHost_ViewChanged;

            base.OnApplyTemplate();
        }

        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
        }

        private async void ScrollingHost_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (ScrollingHost == null || ItemsStack == null || ViewModel == null)
            {
                return;
            }

            Scrolling?.Invoke(this, new Tuple<double, double>(ScrollingHost.ScrollableHeight, ScrollingHost.VerticalOffset));

            using (await _loadMoreLock.WaitAsync())
            {
                // Save position
                CheckFirstAndLastVisibleMessages();
                MessageViewModel p = GetMessageByCoords(new Point(ActualWidth / 2, ActualHeight - 12));

                if (CurrentPositions.ContainsKey(ViewModel))
                {
                    if (p != null) CurrentPositions[ViewModel] = p;
                }
                else
                {
                    if (p != null) CurrentPositions.Add(ViewModel, p);
                }

                if (!e.IsIntermediate)
                {
                    if (ScrollingHost.VerticalOffset < ScrollingHost.ActualHeight)
                    {
                        Debug.WriteLine("Load previous");
                        SetScrollMode(ItemsUpdatingScrollMode.KeepLastItemInView, true);
                        ViewModel.LoadPreviousMessages();
                    }
                    else if (ScrollingHost.VerticalOffset > ScrollingHost.ScrollableHeight - 200)
                    {
                        int id = (Items.Last() as MessageViewModel).Id;
                        if (ViewModel.LastMessage != null && (id == ViewModel.LastMessage.Id || id == Int32.MaxValue))
                        {
                            SetScrollMode(ItemsUpdatingScrollMode.KeepLastItemInView, true);
                        }
                        else
                        {
                            Debug.WriteLine("Load next");
                            SetScrollMode(ItemsUpdatingScrollMode.KeepItemsInView, true);
                            ViewModel.LoadNextMessages();
                        }
                    }
                }
            }
        }

        private void CheckFirstAndLastVisibleMessages(bool setNull = true)
        {
            MessageViewModel f = GetMessageByCoords(new Point(ActualWidth / 2, 12));
            MessageViewModel l = GetMessageByCoords(new Point(ActualWidth / 2, ActualHeight - 12));

            if (setNull)
            {
                ViewModel.FirstVisibleMessage = f;
                ViewModel.LastVisibleMessage = l;
            }
            else
            {
                if (f != null) ViewModel.FirstVisibleMessage = f;
                if (l != null) ViewModel.LastVisibleMessage = l;
            }
        }

        private MessageViewModel GetMessageByCoords(Point point)
        {
            GeneralTransform gt = TransformToVisual(Window.Current.Content);
            Point p = gt.TransformPoint(point);
            var c = this.GetControlByCoordinates<ListViewItem>(p);
            return c != null && c.Content != null && c.Content is MessageViewModel mvm ? mvm : null;
        }

        private ItemsUpdatingScrollMode? _pendingMode;
        private bool? _pendingForce;

        public void SetScrollMode()
        {
            if (_pendingMode is ItemsUpdatingScrollMode mode && _pendingForce is bool force)
            {
                _pendingMode = null;
                _pendingForce = null;

                SetScrollMode(mode, force);
            }
        }

        public void SetScrollMode(ItemsUpdatingScrollMode mode, bool force)
        {
            var panel = ItemsPanelRoot as ItemsStackPanel;
            if (panel == null)
            {
                _pendingMode = mode;
                _pendingForce = force;

                return;
            }

            var scroll = ScrollingHost;
            if (scroll == null)
            {
                _pendingMode = mode;
                _pendingForce = force;

                return;
            }

            if (mode == ItemsUpdatingScrollMode.KeepItemsInView && (force || scroll.VerticalOffset < 200))
            {
                Debug.WriteLine("Changed scrolling mode to KeepItemsInView");

                panel.ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepItemsInView;
            }
            else if (mode == ItemsUpdatingScrollMode.KeepLastItemInView && (force || scroll.ScrollableHeight - scroll.VerticalOffset < 200))
            {
                Debug.WriteLine("Changed scrolling mode to KeepLastItemInView");

                panel.ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepLastItemInView;
            }
        }

        private async void ScrollToMessage(int messageId, bool highlight = true, bool smoothScroll = false)
        {
            Debug.WriteLine($"ScrollToMessage callback {messageId}");
            var m = from q in ViewModel.Messages where q.Id == messageId select q;
            if (m.Count() == 1)
            {
                await Task.Delay(1);
                Debug.WriteLine($"ScrollToMessage: scrolling...");
                await ScrollToItem(m.First(), VerticalAlignment.Center, highlight, null, ScrollIntoViewAlignment.Leading, !smoothScroll);
            }
        }

        private async void MessageAddedToLast(MessageViewModel msg)
        {
            bool isInForeground = ViewManagement.IsWindowInForeground;
            Debug.WriteLine($"MessageAddedToLast callback {msg.Id}; isInForeground: {isInForeground}");
            if (!isInForeground) return;

            await Task.Delay(10); // Without this code the app has crashed. ¯\_(ツ)_/¯
            if (ViewModel.Messages.Count > 1)
            {
                MessageViewModel prevlast = ViewModel.Messages[ViewModel.Messages.Count - 2];
                if (ViewModel.LastVisibleMessage == prevlast)
                {
                    await Task.Delay(1);
                    await ScrollToItem(msg, VerticalAlignment.Bottom, false);
                }
                else if (ViewModel.LastVisibleMessage == msg)
                {
                    await Task.Delay(1);
                    await ScrollToItem(prevlast, VerticalAlignment.Bottom, false, null, ScrollIntoViewAlignment.Leading, true);
                    await Task.Delay(1);
                    await ScrollToItem(msg, VerticalAlignment.Bottom, false);
                }
            }
        }

        public async Task ScrollToItem(object item, VerticalAlignment alignment, bool highlight, double? pixel = null, ScrollIntoViewAlignment direction = ScrollIntoViewAlignment.Leading, bool disableAnimation = false)
        {
            var scrollViewer = ScrollingHost;
            if (scrollViewer == null) return;

            // We are going to try two times, as the first one seem to fail sometimes
            // leading the chat to the wrong scrolling position
            var iter = 2;

            var selectorItem = ContainerFromItem(item) as SelectorItem;
            while (selectorItem == null && iter > 0)
            {
                Debug.WriteLine(string.Format("selectorItem == null, {0} try", iter + 1));

                // call task-based ScrollIntoViewAsync to realize the item
                await this.ScrollIntoViewAsync(item, direction);

                // this time the item shouldn't be null again
                selectorItem = (SelectorItem)ContainerFromItem(item);
                iter--;
            }

            if (selectorItem == null)
            {
                Debug.WriteLine("selectorItem == null, abort");
                return;
            }

            // calculate the position object in order to know how much to scroll to
            var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
            var position = transform.TransformPoint(new Point(0, 0));

            if (alignment == VerticalAlignment.Top)
            {
                if (pixel is double adjust)
                {
                    position.Y -= adjust;
                }
            }
            else if (alignment == VerticalAlignment.Center)
            {
                if (selectorItem.ActualHeight < ActualHeight - 48)
                {
                    position.Y -= (ActualHeight - selectorItem.ActualHeight) / 2d;
                }
                else
                {
                    position.Y -= 48 + 4;
                }
            }
            else if (alignment == VerticalAlignment.Bottom)
            {
                position.Y -= ActualHeight - selectorItem.ActualHeight;

                if (pixel is double adjust)
                {
                    position.Y += adjust;
                }
            }

            // scroll to desired position with animation!
            scrollViewer.ChangeView(null, position.Y, null, disableAnimation);

            if (highlight)
            {
                Highlight(selectorItem);
            }
        }

        private void Highlight(SelectorItem si)
        {
            si.Background = new SolidColorBrush { Color = (Color)Application.Current.Resources["SystemAccentColor"], Opacity = 0.5 };

            ColorAnimation ca = new ColorAnimation();
            ca.To = Colors.Transparent;
            ca.Duration = TimeSpan.FromSeconds(3);

            Storyboard.SetTarget(ca, si.Background);
            Storyboard.SetTargetProperty(ca, "Color");

            Storyboard sb = new Storyboard();
            sb.Children.Add(ca);

            sb.Begin();
        }
    }
}
