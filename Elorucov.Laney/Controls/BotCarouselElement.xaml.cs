using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class BotCarouselElement : UserControl {
        public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(
           "Element", typeof(CarouselElement), typeof(BotCarouselElement), new PropertyMetadata(default(object)));

        public CarouselElement Element {
            get { return (CarouselElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        public static readonly DependencyProperty OwnerMessageIdProperty = DependencyProperty.Register(
            "OwnerMessageId", typeof(int), typeof(BotCarouselElement), new PropertyMetadata(0));

        public int OwnerMessageId {
            get { return (int)GetValue(OwnerMessageIdProperty); }
            set { SetValue(OwnerMessageIdProperty, value); }
        }

        public event EventHandler<BotButtonAction> ElementButtonClick;
        public event EventHandler<BotButtonAction> Click;

        public BotCarouselElement() {
            this.InitializeComponent();
            RegisterPropertyChangedCallback(ElementProperty, async (a, b) => { await DrawElementAsync((CarouselElement)GetValue(b)); });
        }

        private async Task DrawElementAsync(CarouselElement element) {
            if (element.Photo != null) {
                await CardImage.SetUriSourceAsync(element.Photo.MaximalSizedPhoto.Uri);
            } else {
                CardImageContainer.Visibility = Visibility.Collapsed;
            }

            ShowText(element.Title, CardTitle);
            ShowText(element.Description, CardDescription);

            if (element.Buttons != null && element.Buttons.Count > 0) {
                ButtonsContainer.Children.Clear();
                foreach (BotButton btn in element.Buttons) {
                    Button b = VKButtonHelper.GenerateButton(OwnerMessageId, btn, this, ElementButtonClick, true);
                    b.Margin = new Thickness(4);
                    ButtonsContainer.Children.Add(b);
                }
            } else {
                ButtonsContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowText(string text, TextBlock control) {
            if (!string.IsNullOrEmpty(text)) {
                control.Visibility = Visibility.Visible;
                control.Text = text;
            }
        }

        private void ElementClicked(object sender, RoutedEventArgs e) {
            if (Element.Action != null) Click?.Invoke(this, Element.Action);
        }
    }
}
