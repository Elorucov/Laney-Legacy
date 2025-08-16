using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class DefaultAttachmentControl : UserControl {

        long itp = 0;
        long tp = 0;
        long dp = 0;

        public event RoutedEventHandler Click;

        public static DependencyProperty IconTemplateProperty = DependencyProperty.Register(nameof(IconTemplate), typeof(DataTemplate), typeof(DefaultAttachmentControl), new PropertyMetadata(null));
        public DataTemplate IconTemplate {
            get { return (DataTemplate)GetValue(IconTemplateProperty); }
            set { SetValue(IconTemplateProperty, value); }
        }

        public static DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(DefaultAttachmentControl), new PropertyMetadata(null));
        public string Title {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(DefaultAttachmentControl), new PropertyMetadata(null));
        public string Description {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public DefaultAttachmentControl() {
            this.InitializeComponent();
            itp = RegisterPropertyChangedCallback(IconTemplateProperty, (o, p) => SetIcon(p));
            tp = RegisterPropertyChangedCallback(TitleProperty, (o, p) => SetTitle(p));
            dp = RegisterPropertyChangedCallback(DescriptionProperty, (o, p) => SetDescription(p));

            Unloaded += (a, b) => {
                UnregisterPropertyChangedCallback(IconTemplateProperty, itp);
                UnregisterPropertyChangedCallback(TitleProperty, tp);
                UnregisterPropertyChangedCallback(DescriptionProperty, dp);
            };
        }

        private void SetIcon(DependencyProperty p) {
            DataTemplate dt = (DataTemplate)GetValue(p);
            IconPresenter.ContentTemplate = dt;
        }

        private void SetTitle(DependencyProperty p) {
            TitleString.Text = GetValue(p).ToString();
        }

        private void SetDescription(DependencyProperty p) {
            DescString.Text = GetValue(p).ToString();
        }

        private void HandleClick(object sender, RoutedEventArgs e) {
            Click?.Invoke(this, e);
        }
    }
}
