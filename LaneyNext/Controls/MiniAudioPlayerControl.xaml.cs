using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls
{
    public sealed partial class MiniAudioPlayerControl : UserControl
    {
        public MiniAudioPlayerControl()
        {
            this.InitializeComponent();
        }

        public event RoutedEventHandler Click;
        public event RoutedEventHandler CloseButtonClick;

        private void InvokeClick(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void InvokeCloseButtonClicked(object sender, RoutedEventArgs e)
        {
            CloseButtonClick?.Invoke(this, e);
        }
    }
}
