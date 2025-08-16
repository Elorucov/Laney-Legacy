using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SearchView : Page
    {
        SearchViewModel ViewModel { get { return DataContext as SearchViewModel; } set { DataContext = value; } }
        public SearchView()
        {
            this.InitializeComponent();
            ViewModel = new SearchViewModel();
            Loaded += (a, b) =>
            {
                SearchBox.Focus(FocusState.Keyboard);
            };
        }

        private void InitScrollViewer(object sender, RoutedEventArgs e)
        {
            ScrollViewer ListScrollViewer = (sender as ListView).GetScrollViewer();
            ListScrollViewer.RegisterIncrementalLoadingEvent(() => ViewModel.SearchMessages());
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void SearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ViewModel.DoSearch();
            }
        }

        private void ClickedOnConversation(object sender, ItemClickEventArgs e)
        {
            VKSession.Current.SessionBase.SwitchToConversation((e.ClickedItem as ConversationViewModel).Id);
        }

        private void ClickedOnMessage(object sender, ItemClickEventArgs e)
        {
            FoundMessageItem fmi = e.ClickedItem as FoundMessageItem;
            VKSession.Current.SessionBase.SwitchToConversation(fmi.PeerId, fmi.Id);
        }
    }
}
