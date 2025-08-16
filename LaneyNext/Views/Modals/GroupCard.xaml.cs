using Elorucov.Toolkit.UWP.Controls;
using Windows.UI.Xaml;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    public sealed partial class GroupCard : Modal
    {
        public GroupCard()
        {
            this.InitializeComponent();
        }

        private void ChangeVisualState(object sender, SizeChangedEventArgs e)
        {
            bool isWide = e.NewSize.Width >= 480;
            DetailedInfo.Columns = isWide ? 2 : 1;
            FullSizeDesired = !isWide;
        }
    }
}
