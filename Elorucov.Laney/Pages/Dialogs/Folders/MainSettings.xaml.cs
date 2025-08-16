using Elorucov.Laney.Controls;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs.Folders {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainSettings : Page {
        ObservableCollection<Folder> Folders = new ObservableCollection<Folder>();

        public MainSettings() {
            this.InitializeComponent();
            FolPosTop.IsChecked = !AppParameters.FoldersPlacement;
            FolPosSide.IsChecked = AppParameters.FoldersPlacement;
            FoldersList.ItemsSource = Folders;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            new System.Action(async () => { await GetFolders(); })();
        }

        private void SetFoldersViewTop(object sender, RoutedEventArgs e) {
            FolPosSide.IsChecked = false;
            Theme.ChangeFoldersView(false);
        }

        private void SetFoldersViewSide(object sender, RoutedEventArgs e) {
            FolPosTop.IsChecked = false;
            Theme.ChangeFoldersView(true);
        }

        private async Task GetFolders() {
            object response = await Messages.GetFolders();
            if (response is VKList<Folder> folders) {
                foreach (var folder in folders.Items) {
                    Folders.Add(folder);
                }
                Folders.CollectionChanged += Folders_CollectionChanged;
            } else {
                Functions.ShowHandledErrorDialog(response, async () => await GetFolders());
            }
        }

        private void Folders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            AddButton.Visibility = Folders.Count < 15 ? Visibility.Visible : Visibility.Collapsed;
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add) return;

            new System.Action(async () => {
                List<int> folderIds = Folders.Select(f => f.Id).ToList();
                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                var result = await ssp.ShowAsync(Messages.ReorderFolders(folderIds));
                Functions.ShowHandledErrorDialog(result);
            })();
        }

        private void GoToFolderCreationPage(object sender, RoutedEventArgs e) {
            Frame.Navigate(typeof(FolderEditor), null, App.DefaultNavTransition);
        }

        private void GoToFolderEditorPage(object sender, ItemClickEventArgs e) {
            Folder folder = e.ClickedItem as Folder;
            if (folder.Type != "default") {
                new System.Action(async () => {
                    await new ContentDialog {
                        Content = Locale.Get("unknown_attachment"),
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                    return;
                })();
            }
            Frame.Navigate(typeof(FolderEditor), folder, App.DefaultNavTransition);
        }

        private void ShowFolderContextMenu(UIElement sender, ContextRequestedEventArgs args) {
            Folder folder = (sender as FrameworkElement).DataContext as Folder;

            MenuFlyoutItem delete = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("delete") };
            delete.Click += async (a, b) => await TryDeleteFolder(folder);

            MenuFlyout mf = new MenuFlyout();
            mf.Items.Add(delete);
            mf.ShowAt(sender as FrameworkElement);
        }

        private void CheckHotkey(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Delete) {
                var f = FocusManager.GetFocusedElement();
                if (f != null && f is ListViewItem lvi) {
                    Folder folder = lvi.Content as Folder;
                    new System.Action(async () => { await TryDeleteFolder(folder); })();
                }
            }
        }

        private async Task TryDeleteFolder(Folder folder) {
            if (await APIHelper.TryDeleteFolderAsync(folder)) {
                Folders.Remove(folder);
            }
        }
    }
}