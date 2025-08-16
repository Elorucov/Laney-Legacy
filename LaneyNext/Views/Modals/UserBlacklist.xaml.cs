using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    public sealed partial class UserBlacklist : Modal
    {
        public UserBlacklist()
        {
            this.InitializeComponent();
            var vm = new UserBlacklistViewModel();
            vm.ShowSnackbarRequested += ShowSnackbarRequested;
            DataContext = vm;
        }

        private void ShowSnackbarRequested(object sender, VK.VKUI.Controls.Snackbar e)
        {
            LayoutRoot.Children.Add(e);
            e.Show(2000);
        }
    }
}