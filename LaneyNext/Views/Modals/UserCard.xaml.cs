using Elorucov.Toolkit.UWP.Controls;
using Windows.UI.Xaml;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class UserCard : Modal
    {
        public UserCard()
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