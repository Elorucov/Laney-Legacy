using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels.Settings;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Settings
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Interface : SettingsPageBase
    {
        public Interface()
        {
            this.InitializeComponent();
            CategoryId = Constants.SettingsInterfaceId;
            DataContext = new InterfaceViewModel();
        }

        private void GoToBackgroundPicker(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Frame.Navigate(typeof(InterfaceViews.BackgroundPickerView));
        }
    }
}
