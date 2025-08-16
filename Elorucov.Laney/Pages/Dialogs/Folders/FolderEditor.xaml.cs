using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
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
    public sealed partial class FolderEditor : Page {
        public FolderEditor() {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            PickedAction = new Action<ObservableCollection<LConversation>>(convs => {
                convs.SortDescending(c => c.SortId.MinorId);
                Conversations = convs;
            });
        }

        Folder currentFolder;
        List<long> initialConvsIds = new List<long>();
        ObservableCollection<LConversation> Conversations = new ObservableCollection<LConversation>();
        Action<ObservableCollection<LConversation>> PickedAction = null;
        System.Action DialogCloseAction;

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if (e.Parameter != null) {
                if (e.Parameter is Folder folder) {
                    currentFolder = folder;
                } else if (e.Parameter is Tuple<int, string, System.Action> data) {
                    currentFolder = new Folder {
                        Id = data.Item1,
                        Name = data.Item2
                    };
                    DialogCloseAction = data.Item3;
                }
            }
            PageName.Text = Locale.Get(currentFolder != null ? "folder_configure" : "folder_create_title");
            if (currentFolder != null) FolderName.Text = currentFolder.Name;
            if (currentFolder == null) ConvsList.ItemsSource = Conversations;
            if (Conversations.Count == 0 && currentFolder != null) new System.Action(async () => { await GetConversations(); })();
            Conversations.CollectionChanged += (a, b) => CheckCanSave();
            if (Frame.CanGoBack) FindName(nameof(BackButton));
        }

        private void GoBack(object sender, RoutedEventArgs e) {
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Frame.GoBack(App.DefaultBackNavTransition);
        }

        private async Task GetConversations() {
            if (Loader.IsActive) return;
            Loader.IsActive = true;
            AddButton.IsEnabled = false;

            object response = await Messages.GetConversations(200, 0, null, currentFolder.Id, API.WebToken);
            Loader.IsActive = false;
            if (response is ConversationsResponse cr) {
                AppSession.AddUsersToCache(cr.Profiles);
                AppSession.AddGroupsToCache(cr.Groups);
                foreach (var conv in cr.Items) {
                    initialConvsIds.Add(conv.Conversation.Peer.Id);
                    Conversations.Add(new LConversation(conv.Conversation));
                }
                ConvsList.ItemsSource = Conversations;
                AddButton.IsEnabled = true;
            } else {
                Functions.ShowHandledErrorDialog(response, async () => await GetConversations());
            }
        }

        private void RemoveFromFolder(object sender, RoutedEventArgs e) {
            LConversation cvm = (sender as FrameworkElement).DataContext as LConversation;
            Conversations.Remove(cvm);
        }

        private void GetToConvPickerPage(object sender, RoutedEventArgs e) {
            if (Loader.IsActive) return;
            Tuple<ObservableCollection<LConversation>, Action<ObservableCollection<LConversation>>> nav =
                new Tuple<ObservableCollection<LConversation>, Action<ObservableCollection<LConversation>>>(Conversations, PickedAction);
            Frame.Navigate(typeof(ConvsPicker), nav, App.DefaultNavTransition);
        }

        private void SaveChanges(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                List<long> actualConvsIds = Conversations.Select(c => c.Id).ToList();

                var toRemove = initialConvsIds.Except(actualConvsIds).ToList();
                var toAdd = actualConvsIds.Except(initialConvsIds).ToList();

                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                var response = await ssp.ShowAsync(currentFolder != null ?
                    Messages.UpdateFolder(currentFolder.Id, FolderName.Text, toAdd, toRemove) :
                    Messages.CreateFolder(FolderName.Text, toAdd));

                if ((response is bool b && b) || (response is FolderCreatedResponse fcr)) {
                    // Tips.Show(currentFolder != null ? "Updated" : "Created");
                    NavigationCacheMode = NavigationCacheMode.Disabled;
                    if (Frame.CanGoBack) {
                        Frame.GoBack(App.DefaultBackNavTransition);
                    } else {
                        DialogCloseAction?.Invoke();
                    }
                } else {
                    Functions.ShowHandledErrorDialog(response);
                }
            })();
        }

        private void CheckFolderName(TextBox sender, TextBoxTextChangingEventArgs args) {
            CheckCanSave();
        }

        private void CheckCanSave() {
            SaveButton.IsEnabled = FolderName.Text.Length > 0 && Conversations.Count > 0;
        }
    }
}