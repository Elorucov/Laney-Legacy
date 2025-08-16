using ELOR.VKAPILib.Objects;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ChatPermissionsEditor : Modal
    {
        public ChatPermissionsEditor(int chatId, ChatPermissions permissions)
        {
            this.InitializeComponent();
            DataContext = new ChatPermissionsEditorViewModel(chatId, permissions);
        }

        private ChatPermissionsEditorViewModel ViewModel { get { return DataContext as ChatPermissionsEditorViewModel; } }

        private void ShowOptionsContextMenu(object sender, ItemClickEventArgs e)
        {
            if (ViewModel.IsLoading) return;
            ChatPermissionItem item = e.ClickedItem as ChatPermissionItem;
            ListViewItem lv = SettingsList.ContainerFromItem(item) as ListViewItem;
            FrameworkElement el = (lv.ContentTemplateRoot as Grid).Children[0] as FrameworkElement;

            ViewModel.ShowSettings(el, item);
        }
    }
}
