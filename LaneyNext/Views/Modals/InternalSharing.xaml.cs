using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class InternalSharing : Modal
    {

        public InternalSharing(AttachmentBase attachment)
        {
            this.InitializeComponent();
            DataContext = new InternalSharingViewModel(attachment);
            SharingTypeTitle.Text = Locale.Get(VKSession.Current.Type == SessionType.VKGroup ? "intsharing_title_group" : "intsharing_title_user");
        }

        public InternalSharing(IEnumerable<MessageViewModel> forwardedMessages)
        {
            this.InitializeComponent();
            DataContext = new InternalSharingViewModel(forwardedMessages);
            SharingTypeTitle.Text = Locale.Get(VKSession.Current.Type == SessionType.VKGroup ? "intsharing_forward_title_group" : "intsharing_forward_title_user");
        }

        InternalSharingViewModel ViewModel { get { return DataContext as InternalSharingViewModel; } }

        private void ConvSelected(object sender, ItemClickEventArgs e)
        {
            Hide();
            ViewModel.ConvSelectedCommand.Execute(e.ClickedItem);
        }

        private void SearchBoxKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                ViewModel.SearchConversations();
            }
        }
    }
}