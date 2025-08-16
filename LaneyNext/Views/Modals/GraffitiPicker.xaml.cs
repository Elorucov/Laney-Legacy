using ELOR.VKAPILib.Objects;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class GraffitiPicker : Modal
    {
        public GraffitiPicker()
        {
            this.InitializeComponent();
            DataContext = new GraffitiPickerViewModel();
        }

        private void PickGraffiti(object sender, ItemClickEventArgs e)
        {
            (DataContext as GraffitiPickerViewModel).AttachGraffiti(e.ClickedItem as Document);
            Hide();
        }
    }
}
