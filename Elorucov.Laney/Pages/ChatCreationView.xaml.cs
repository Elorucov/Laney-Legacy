using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Objects;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatCreationView : Page {
        ChatCreationViewModel ViewModel { get { return DataContext as ChatCreationViewModel; } }

        public ChatCreationView() {
            this.InitializeComponent();
            DataContext = new ChatCreationViewModel(() => Frame.GoBack());
        }

        private void GoBack(object sender, RoutedEventArgs e) {
            Frame.GoBack(App.DefaultBackNavTransition);
        }

        private void UpdateSelectedFriends(object sender, SelectionChangedEventArgs e) {
            foreach (User u in e.AddedItems.Cast<User>()) {
                ViewModel.AddFriendToSelected(u);
            }
            foreach (User u in e.RemovedItems.Cast<User>()) {
                ViewModel.RemoveFriendFromSelected(u);
            }
        }

        private void RemoveFromSelected(object sender, RoutedEventArgs e) {
            User u = (sender as FrameworkElement).DataContext as User;
            FriendsList.SelectedItems.Remove(u);
        }

        private void GoToChat(object sender, RoutedEventArgs e) {
            Button button = sender as Button;
            User user = button.DataContext as User;
            Main.GetCurrent().ShowConversationPage(user.Id);
        }
    }
}