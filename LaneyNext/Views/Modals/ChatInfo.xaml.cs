using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Toolkit.UWP.Controls;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    public sealed partial class ChatInfo : Modal
    {
        public ChatInfo()
        {
            this.InitializeComponent();
        }

        private void ShowMemberInfo(object sender, ItemClickEventArgs e)
        {
            Entity entity = e.ClickedItem as Entity;
            Router.ShowCard(entity.Id);
            this.Hide();
        }
    }
}
