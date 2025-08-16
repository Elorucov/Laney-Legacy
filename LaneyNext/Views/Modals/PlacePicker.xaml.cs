using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class PlacePicker : Modal
    {
        public PlacePicker()
        {
            this.InitializeComponent();
            DataContext = new PlacePickerViewModel(this);
        }
    }
}
