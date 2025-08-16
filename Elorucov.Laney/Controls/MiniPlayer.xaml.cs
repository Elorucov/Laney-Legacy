using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class MiniPlayer : UserControl {
        public MiniPlayer() {
            this.InitializeComponent();
        }

        public event RoutedEventHandler Click;
        public event RoutedEventHandler CloseButtonClick;

        private void InvokeClick(object sender, RoutedEventArgs e) {
            Click?.Invoke(this, e);
        }

        private void InvokeCloseButtonClicked(object sender, RoutedEventArgs e) {
            CloseButtonClick?.Invoke(this, e);
        }
    }
}