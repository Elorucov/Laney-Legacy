using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Helpers.UI;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls
{
    public sealed partial class CarouselElementControl : UserControl
    {
        public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(
           "Element", typeof(CarouselElement), typeof(CarouselElementControl), new PropertyMetadata(default(object)));

        public CarouselElement Element
        {
            get { return (CarouselElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        public static readonly DependencyProperty OwnerMessageIdProperty = DependencyProperty.Register(
            "OwnerMessageId", typeof(int), typeof(CarouselElementControl), new PropertyMetadata(0));

        public int OwnerMessageId
        {
            get { return (int)GetValue(OwnerMessageIdProperty); }
            set { SetValue(OwnerMessageIdProperty, value); }
        }

        public event EventHandler<BotButtonAction> ElementButtonClick;
        public event EventHandler<BotButtonAction> Click;

        public CarouselElementControl()
        {
            this.InitializeComponent();
            RegisterPropertyChangedCallback(ElementProperty, (a, b) => { DrawElement((CarouselElement)GetValue(b)); });
        }

        private void DrawElement(CarouselElement element)
        {
            if (element.Photo != null)
            {
                CardImage.UriSource = element.Photo.MaximalSizedPhoto.Uri;
            }
            else
            {
                CardImageContainer.Visibility = Visibility.Collapsed;
            }

            CardTitle.Text = element.Title;
            CardDescription.Text = element.Description;

            if (element.Buttons != null && element.Buttons.Count > 0)
            {
                ButtonsContainer.Children.Clear();
                foreach (BotButton btn in element.Buttons)
                {
                    Button b = MessageKeyboardHelper.GenerateButton(OwnerMessageId, btn, this, ElementButtonClick);
                    b.Margin = new Thickness(4);
                    ButtonsContainer.Children.Add(b);
                }
            }
            else
            {
                ButtonsContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void ElementClicked(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, Element.Action);
        }
    }
}
