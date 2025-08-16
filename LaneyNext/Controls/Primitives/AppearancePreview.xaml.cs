using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.ViewModels.Controls.Primitives;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Primitives
{
    public sealed partial class AppearancePreview : UserControl
    {
        public AppearancePreview()
        {
            this.InitializeComponent();
            DataContext = new AppearancePreviewViewModel();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {

        }

        private void DrawMessage(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            MessageViewModel msg = args.NewValue as MessageViewModel;
            if (msg == null) return;
            MessageView view = new MessageView(msg, null, false, sender as Border, sender.ActualWidth);
        }
    }
}
