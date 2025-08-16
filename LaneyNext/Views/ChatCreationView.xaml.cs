using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ChatCreationView : Page
    {
        private ChatCreationViewModel ViewModel { get { return DataContext as ChatCreationViewModel; } }

        public ChatCreationView()
        {
            this.InitializeComponent();
            DataContext = new ChatCreationViewModel((id) =>
            {
                Frame.GoBack();
                VKSession.Current.SessionBase.SwitchToConversation(id);
            });
            ViewModel.SelectedFriendsForChat.CollectionChanged += SelectedFriendsForChat_CollectionChanged;
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsChatCreationMode)
            {
                ViewModel.IsChatCreationMode = false;
            }
            else if (ViewModel.IsFriendSearchMode)
            {
                ViewModel.SearchQuery = String.Empty;
                ViewModel.SearchFriend();
            }
            else
            {
                Frame.GoBack();
            }
        }

        private void SearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string query = (sender as TextBox).Text;
                ViewModel.SearchFriend();
            }
        }

        private void SwitchToChatCreationMode(object sender, RoutedEventArgs e)
        {
            ViewModel.IsChatCreationMode = true;
        }

        private void OnFriendClicked(object sender, ItemClickEventArgs e)
        {
            User u = e.ClickedItem as User;
            if (!ViewModel.IsChatCreationMode) VKSession.Current.SessionBase.SwitchToConversation(u.Id);
        }

        private void FriendsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var removed = e.RemovedItems.Cast<User>();
            var added = e.AddedItems.Cast<User>();
            foreach (var ur in removed)
            {
                if (ViewModel.SelectedFriendsForChat.Contains(ur)) ViewModel.SelectedFriendsForChat.Remove(ur);
            }
            foreach (var ua in added)
            {
                if (!ViewModel.SelectedFriendsForChat.Contains(ua)) ViewModel.SelectedFriendsForChat.Add(ua);
            }
        }

        private void SelectedFriendsForChat_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!ViewModel.IsChatCreationMode) return;

            FriendsList.SelectionChanged -= FriendsList_SelectionChanged;
            var list = FriendsList.Items.Cast<User>();
            foreach (var u in list)
            {
                bool contains = ViewModel.SelectedFriendsForChat.Contains(u);
                if (!contains)
                {
                    FriendsList.SelectedItems.Remove(u);
                }
                else
                {
                    FriendsList.SelectedItems.Add(u);
                }
            }

            FriendsList.SelectionChanged += FriendsList_SelectionChanged;
        }

        private void RemoveUserFromSelected(object sender, RoutedEventArgs e)
        {
            User u = (sender as FrameworkElement).DataContext as User;
            if (ViewModel.SelectedFriendsForChat.Contains(u)) ViewModel.SelectedFriendsForChat.Remove(u);
        }
    }
}
