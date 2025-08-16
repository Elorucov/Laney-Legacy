using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Primitives
{
    public sealed partial class PollBackgroundPreview : UserControl
    {
        public PollBackgroundPreview()
        {
            this.InitializeComponent();
            RegisterPropertyChangedCallback(PollBackgroundProperty, (c, d) => PBackground.Background = GetValue(d) as Brush);
            RegisterPropertyChangedCallback(PollBackgroundColorProperty, (c, d) => PBackgroundColor.Background = GetValue(d) as SolidColorBrush);
        }

        public static readonly DependencyProperty PollBackgroundProperty = DependencyProperty.Register(
           "PollBackground", typeof(Brush), typeof(PollBackgroundPreview), new PropertyMetadata(default(object)));

        public Brush PollBackground
        {
            get { return (Brush)GetValue(PollBackgroundProperty); }
            set { SetValue(PollBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PollBackgroundColorProperty = DependencyProperty.Register(
           "PollBackgroundColor", typeof(SolidColorBrush), typeof(PollBackgroundPreview), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(32, 128, 128, 128))));

        public SolidColorBrush PollBackgroundColor
        {
            get { return (SolidColorBrush)GetValue(PollBackgroundColorProperty); }
            set { SetValue(PollBackgroundColorProperty, value); }
        }
    }
}