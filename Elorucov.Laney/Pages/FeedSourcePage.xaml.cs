using Elorucov.Laney.Models;
using Elorucov.Laney.ViewModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedSourcePage : Page {
        public FeedSourcePage() {
            this.InitializeComponent();
            DataContext = new FeedSourceViewModel();
        }

        FeedSourceViewModel ViewModel => DataContext as FeedSourceViewModel;

        private void GoBack(object sender, RoutedEventArgs e) {
            Frame.GoBack(App.DefaultBackNavTransition);
        }

        private void DoSearch(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            new System.Action(async () => { await ViewModel.SearchAsync(); })();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e) {
            Entity entity = e.ClickedItem as Entity;
            if (entity != null) {
                switch (entity.Id) {
                    case Constants.FEED_SOURCE_PICK_FRIENDS:
                    case Constants.FEED_SOURCE_PICK_COMMUNITIES:
                    case Constants.FEED_SOURCE_PICK_ADMINED_COMMUNITIES:
                        break;
                    default:
                        Main.GetCurrent().ToggleContentLayerVisibility(false);
                        Main.GetCurrent().NavigateToPage(typeof(FeedView), entity, true);
                        break;
                }
            }
        }
    }
}
