using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs.Folders {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConvsPicker : Page {
        public ConvsPicker() {
            this.InitializeComponent();
        }

        ObservableCollection<LConversation> Conversations = new ObservableCollection<LConversation>();
        ObservableCollection<LConversation> SelectedConversations;
        Action<ObservableCollection<LConversation>> PickedAction = null;
        bool canAddOrRemove = false;

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if (e.Parameter != null && e.Parameter is Tuple<ObservableCollection<LConversation>, Action<ObservableCollection<LConversation>>> nav) {
                SelectedConversations = nav.Item1;
                PickedAction = nav.Item2;
            }
            ConvsList.ItemsSource = Conversations;
            if (Conversations.Count == 0) new System.Action(async () => { await GetConversations(); })();
        }

        private void GoBack(object sender, RoutedEventArgs e) {
            Frame.GoBack(App.DefaultBackNavTransition);
        }

        private async Task GetConversations() {
            if (Loader.IsActive) return;
            canAddOrRemove = false;
            Conversations.Clear();
            Loader.IsActive = true;
            var ids = SelectedConversations.Select(c => c.Id);

            object response = await Messages.GetConversations(100, 0, null, 0);
            Loader.IsActive = false;
            if (response is ConversationsResponse cr) {
                AppSession.AddUsersToCache(cr.Profiles);
                AppSession.AddGroupsToCache(cr.Groups);
                foreach (var conv in cr.Items) {
                    var con = new LConversation(conv.Conversation);
                    Conversations.Add(con);
                    if (ids.Contains(con.Id)) ConvsList.SelectedItems.Add(con);
                }
                canAddOrRemove = true;
            } else {
                Functions.ShowHandledErrorDialog(response, async () => await GetConversations());
            }
        }

        private async Task SearchConversation(string query) {
            if (Loader.IsActive) return;
            canAddOrRemove = false;
            Conversations.Clear();
            Loader.IsActive = true;
            var ids = SelectedConversations.Select(c => c.Id);

            object response = await Messages.SearchConversations(query);
            Loader.IsActive = false;
            if (response is VKList<Conversation> cr) {
                AppSession.AddUsersToCache(cr.Profiles);
                AppSession.AddGroupsToCache(cr.Groups);
                foreach (var conv in cr.Items) {
                    var con = new LConversation(conv);
                    Conversations.Add(con);
                    if (ids.Contains(con.Id)) ConvsList.SelectedItems.Add(con);
                }
                canAddOrRemove = true;
            } else {
                Functions.ShowHandledErrorDialog(response, async () => await SearchConversation(query));
            }
        }

        private void DoneClicked(object sender, RoutedEventArgs e) {
            PickedAction?.Invoke(SelectedConversations);
            Frame.GoBack(App.DefaultBackNavTransition);
        }

        private void ConvsListSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!canAddOrRemove) return;

            var added = e.AddedItems.Select(c => c as LConversation).ToList();
            var removed = e.RemovedItems.Select(c => c as LConversation).ToList();

            foreach (LConversation con in added) {
                var conv = SelectedConversations.Where(c => c.Id == con.Id).FirstOrDefault();
                if (conv == null) SelectedConversations.Add(con);
            }

            foreach (LConversation con in removed) {
                var conv = SelectedConversations.Where(c => c.Id == con.Id).FirstOrDefault();
                if (conv != null) SelectedConversations.Remove(conv);
            }
        }

        private void OnSearchBoxSubmit(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            new System.Action(async () => {
                if (!string.IsNullOrEmpty(sender.Text)) {
                    await SearchConversation(sender.Text);
                } else {
                    await GetConversations();
                }
            })();
        }
    }
}