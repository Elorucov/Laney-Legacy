using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SharingModal : Modal {
        public SharingModal() {
            this.InitializeComponent();
        }

        SharingModalViewModel ViewModel => DataContext as SharingModalViewModel;
        bool canSendImmediately;

        public SharingModal(long fromPeerId, List<LMessage> messages, bool canSendImmediately = false) {
            this.InitializeComponent();
            DataContext = new SharingModalViewModel(fromPeerId, messages);
            this.canSendImmediately = canSendImmediately;
            SetUpViewModel(DataContext as SharingModalViewModel);
        }

        public SharingModal(List<AttachmentBase> attachments, bool canSendImmediately = false) {
            this.InitializeComponent();
            DataContext = new SharingModalViewModel(attachments);
            this.canSendImmediately = canSendImmediately;
            SetUpViewModel(DataContext as SharingModalViewModel);
        }

        // Cannot normally bind Visibility property to SelectedConvs with NullToVisibility converter
        // а ещё элементы в SelectedConvs другого класса, отличный от Conversations.
        private void SetUpViewModel(SharingModalViewModel vm) {
            if (vm.SelectedConvs != null) vm.SelectedConvs.CollectionChanged += SelectedConvs_CollectionChanged;
            vm.PropertyChanged += (a, b) => {
                if (b.PropertyName == nameof(SharingModalViewModel.SelectedConvs))
                    if (vm.SelectedConvs != null) {
                        SelectedConvsRoot.Visibility = vm.SelectedConvs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                        SendBtn.IsEnabled = vm.SelectedConvs.Count > 0;
                        vm.SelectedConvs.CollectionChanged += SelectedConvs_CollectionChanged;
                    } else {
                        SelectedConvsRoot.Visibility = Visibility.Collapsed;
                        SendBtn.IsEnabled = false;
                    }
            };

            vm.OnConvsLoaded = () => {
                foreach (var entity in vm.SelectedConvs) {
                    var con = vm.Conversations.Where(c => c.Id == entity.Id).FirstOrDefault();
                    if (con != null) ConvsList.SelectedItems.Add(con);
                }
            };
        }

        private void SelectedConvs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            ObservableCollection<Entity> entities = sender as ObservableCollection<Entity>;
            SelectedConvsRoot.Visibility = entities.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            SendBtn.IsEnabled = entities.Count > 0;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e) {
            ConvsList.SelectionMode = ListViewSelectionMode.Multiple;
            ConvsList.IsItemClickEnabled = false;
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e) {
            ConvsList.SelectionMode = ListViewSelectionMode.None;
            ConvsList.IsItemClickEnabled = true;
        }

        private void ConvsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var added = e.AddedItems.Select(c => c as LConversation).ToList();
            var removed = e.RemovedItems.Select(c => c as LConversation).ToList();
            var vm = DataContext as SharingModalViewModel;

            foreach (LConversation con in added) {
                if (!con.CanWrite.Allowed) {
                    ConvsList.SelectedItems.Remove(con);
                    return;
                }

                var conv = vm.SelectedConvs.Where(c => c.Id == con.Id).FirstOrDefault();
                if (conv == null) {
                    vm.SelectedConvs.Add(con.ToEntity((o) => {
                        var entity1 = vm.SelectedConvs.Where(et => et.Id == con.Id).FirstOrDefault();
                        if (entity1 != null) {
                            vm.SelectedConvs.Remove(entity1);
                            // ConvsList.SelectedItems.Remove(con);
                        }
                    }));
                }
            }

            foreach (LConversation con in removed) {
                var conv = vm.SelectedConvs.Where(c => c.Id == con.Id).FirstOrDefault();
                if (conv != null) vm.SelectedConvs.Remove(conv);
            }
        }

        private void SelectedConvRemoveClick(object sender, RoutedEventArgs e) {
            FrameworkElement el = sender as FrameworkElement;
            Entity entity = el.DataContext as Entity;
            if (entity == null) return;

            LConversation con = ConvsList.SelectedItems.Cast<LConversation>().Where(c => c.Id == entity.Id).FirstOrDefault();
            if (con != null) ConvsList.SelectedItems.Remove(con);
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            new System.Action(async () => await ViewModel.SearchConversationsAsync())();
        }

        private void ConvsList_ItemClick(object sender, ItemClickEventArgs e) {
            LConversation conv = e.ClickedItem as LConversation;
            if (!conv.CanWrite.Allowed) {
                Tips.Show(Locale.Get("readonlyconv_overall"));
                return;
            }
            if (canSendImmediately) {
                new System.Action(async () => {
                    ViewModel.SelectedConvs.Clear();
                    ViewModel.SelectedConvs.Add(conv.ToEntity());
                    await ViewModel.SendMessageAsync();
                })();
            } else {
                Hide(conv.Id);
            }
        }
    }
}