using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Objects;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedView : Page {
        public FeedView() {
            this.InitializeComponent();
            feedDebug = AppParameters.FeedDebug;
            var host = Main.GetCurrent();
            BackButtonHW.Visibility = host.IsWideMode ? Visibility.Collapsed : Visibility.Visible;
            BackButtonHN.Visibility = host.IsWideMode ? Visibility.Collapsed : Visibility.Visible;
            Unloaded += (a, b) => {
                SizeChanged -= OnSizeChanged;
            };
        }

        private FeedViewModel ViewModel => DataContext as FeedViewModel;
        bool feedDebug;

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if (e.Parameter is Entity entity) {
                FeedViewModel fvm = AppSession.OpenedWallsAndFeeds.Where(f => f.Id == entity.Id).FirstOrDefault();
                if (fvm == null) {
                    fvm = new FeedViewModel(entity.Id, entity.Title);
                    AppSession.OpenedWallsAndFeeds.Add(fvm);
                }
                DataContext = fvm;
            }
        }

        private void GoBack(object sender, RoutedEventArgs e) {
            Main.GetCurrent().GoBack();

        }

        private void DoSearch(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            new System.Action(async () => { await ViewModel.SearchPostsAsync(true); })();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            BackButtonHW.Visibility = Main.GetCurrent().IsWideMode ? Visibility.Collapsed : Visibility.Visible;
            BackButtonHN.Visibility = Main.GetCurrent().IsWideMode ? Visibility.Collapsed : Visibility.Visible;
            if (e.NewSize.Width >= 720) {
                HeaderNarrow.Visibility = Visibility.Collapsed;
                HeaderWide.Visibility = Visibility.Visible;
            } else {
                HeaderWide.Visibility = Visibility.Collapsed;
                HeaderNarrow.Visibility = Visibility.Visible;
            }
        }

        double listViewWidth = 0;

        private void PostContainer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            if (listViewWidth == 0) {
                double w = PostsLV.ActualWidth > HeaderWide.MaxWidth ? HeaderWide.MaxWidth : PostsLV.ActualWidth;
                listViewWidth = 720 - 24 - 24 - 2;
            }

            Border b = sender as Border;
            if (args.NewValue != null && args.NewValue is WallPost post) {
                b.Child = new PostUI {
                    MaybeActualWidth = listViewWidth,
                    Post = post
                };
            } else {
                b.Child = null;
                return;
            }
        }

        ScrollViewer postsListScrollViewer;
        private void InitializeIncrementalLoading(object sender, RoutedEventArgs e) {
            if (feedDebug) {
                FindName(nameof(debug));
                dbgEntity.Text = $"ID: {ViewModel.Id}";
            }

            ListView lv = sender as ListView;
            postsListScrollViewer = lv.GetScrollViewerFromListView();
            postsListScrollViewer.ViewChanged += PostsListScrollChanged;
        }

        // required, because by default focus appears on Header.
        private async Task<bool> TryToFocusAsync(Control control, FocusState state) {
            byte attempts = 0;
            bool focused = false;
            while (attempts < 50) {
                focused = control.Focus(state);
                if (focused) {
                    Log.Info($"FeedView: focused on {control.GetType().Name} successfully. Failed attemts: {attempts}");
                    return true;
                }
                attempts++;
                await Task.Delay(50);
            }
            if (!focused) Log.Warn($"FeedView: unable to focus to {control.GetType().Name}! Failed attemts: {attempts}");
            return false;
        }

        private void PostsListScrollChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            double off = postsListScrollViewer.VerticalOffset;
            double sh = postsListScrollViewer.ScrollableHeight;
            double vh = postsListScrollViewer.ViewportHeight;
            if (feedDebug) {
                dbgOffset.Text = $"OFFSET: {Math.Round(off, 2)}";
                dbgScrollable.Text = $"SCRMAX: {Math.Round(sh, 2)}";
                dbgViewport.Text = $"VPORT:  {Math.Round(vh, 2)}";
            }

            if (off > sh - vh && ViewModel != null) {
                new System.Action(async () => {
                    if (ViewModel.SearchMode) {
                        await ViewModel.SearchPostsAsync();
                    } else {
                        await ViewModel.LoadPostsAsync();
                    }
                })();
            }
        }

        private void GoToSearchMode(object sender, RoutedEventArgs e) {
            ViewModel.SearchMode = true;
            Composer.Visibility = Visibility.Collapsed;
            new System.Action(async () => { await TryToFocusAsync(SearchBoxHN, FocusState.Keyboard); })();
            Composer.Visibility = Visibility.Visible;
        }

        private void ExitFromSearchMode(object sender, RoutedEventArgs e) {
            Composer.Visibility = Visibility.Collapsed;
            ViewModel.SearchMode = false;

            ViewModel.SearchQuery = string.Empty;
            new System.Action(async () => {
                await ViewModel.LoadPostsAsync();
                await TryToFocusAsync(BackButtonHN, FocusState.Keyboard);
            })();
            Composer.Visibility = Visibility.Visible;
        }
    }
}