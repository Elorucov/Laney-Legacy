using ELOR.VKAPILib.Objects;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Attachments
{
    public sealed partial class StickerPresenter : UserControl
    {
        long id = 0;
        public StickerPresenter()
        {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(StickerProperty, ChangedCallback);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(StickerProperty, id); };
        }

        public static readonly DependencyProperty StickerProperty = DependencyProperty.Register(
                   "Sticker", typeof(Sticker), typeof(StickerPresenter), new PropertyMetadata(default(object)));

        public Sticker Sticker
        {
            get { return (Sticker)GetValue(StickerProperty); }
            set { SetValue(StickerProperty, value); }
        }

        private void ChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            DisplaySticker((Sticker)GetValue(dp));
        }

        private void DisplaySticker(Sticker sticker)
        {
            img.UriSource = sticker.Images != null ? sticker.Images[3].Uri : new Uri($"https://vk.com/sticker/1-{sticker.StickerId}-128b");
        }
    }
}
