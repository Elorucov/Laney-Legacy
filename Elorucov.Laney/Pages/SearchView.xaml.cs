using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchView : Page {
        SearchViewModel ViewModel { get { return DataContext as SearchViewModel; } }
        ScrollViewer FoundMessagesListScrollViewer;

        public SearchView() {
            this.InitializeComponent();
            DataContext = new SearchViewModel();
        }

        private void GoBack(object sender, RoutedEventArgs e) {
            Frame.GoBack(App.DefaultBackNavTransition);
        }
        private void DoSearch(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            new System.Action(async () => { await ViewModel.SearchAsync(); })();
        }

        private void SelectConversation(object sender, ItemClickEventArgs e) {
            Main.GetCurrent().ShowConversationPage((e.ClickedItem as LConversation).Id);
        }

        private void SelectMessage(object sender, ItemClickEventArgs e) {
            FoundMessageItem fmi = e.ClickedItem as FoundMessageItem;
            Main.GetCurrent().ShowConversationPage(fmi.PeerId, fmi.Id);
        }

        private void TryAutoFocusToSearchBox(object sender, RoutedEventArgs e) {
            SearchBox.PlaceholderText = Locale.Get("search_placeholder");
            SearchBox.Focus(FocusState.Keyboard);
        }

        private void GetScrollViewerForListViewOnce(object sender, RoutedEventArgs e) {
            (sender as FrameworkElement).Loaded -= GetScrollViewerForListViewOnce;
            if (FoundMessagesListScrollViewer == null) {
                FoundMessagesListScrollViewer = FoundMessagesList.GetScrollViewerFromListView();
                if (FoundMessagesListScrollViewer != null) {
                    FoundMessagesListScrollViewer.ViewChanged += FoundMessagesListScrollViewer_ViewChanged;
                }
            }
        }

        private void FoundMessagesListScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 4) {
                    if (ViewModel.CurrentTab == 1 && ViewModel.FoundMessages.Count != 0)
                        new System.Action(async () => { await ViewModel.SearchMessagesAsync(); })();
                }
            }
        }
    }
}