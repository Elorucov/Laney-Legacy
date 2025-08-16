using Elorucov.Laney.Services.Common;
using Elorucov.Laney.ViewModel.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class OutboundAttachmentUI : UserControl {
        public OutboundAttachmentUI() {
            this.InitializeComponent();
        }

        public OutboundAttachmentViewModel ViewModel => DataContext as OutboundAttachmentViewModel;

        public event RoutedEventHandler OnRemoveButtonClick;

        private void OnRemoveClick(object sender, RoutedEventArgs e) {
            OnRemoveButtonClick?.Invoke(this, e);
        }

        private void OnErrorInfoClick(object sender, RoutedEventArgs e) {
            if (ViewModel == null) return;
            Control target = sender as Control;

            Flyout flyout = new Flyout {
                Content = new TextBlock {
                    Margin = new Thickness(12),
                    TextWrapping = TextWrapping.Wrap,
                    Text = $"{Locale.Get("load_error")}\n{ViewModel.ErrorInfo?.Message}"
                },
                Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Top
            };
            flyout.ShowAt(target);
        }
    }
}
