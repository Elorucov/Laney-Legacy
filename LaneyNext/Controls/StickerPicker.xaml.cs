using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls
{
    public sealed partial class StickerPicker : UserControl
    {
        private FlyoutBase Flyout;

        ObservableCollection<StickersPack> StickersPacks = new ObservableCollection<StickersPack>();

        public delegate void StickerSelectedDelegate(Sticker sticker);
        public event StickerSelectedDelegate StickerSelected;

        public StickerPicker(FlyoutBase flyout)
        {
            this.InitializeComponent();
            Flyout = flyout;
            Loaded += (a, b) => StickerSelected += (c) => Flyout.Hide();
        }

        private async void InitStickers(object sender, RoutedEventArgs e)
        {
            if (StickersPacks.Count == 0)
            {

                VKList<Sticker> recentStickers = await VKSession.Current.API.Messages.GetRecentStickersAsync();
                StickersPack recent = new StickersPack()
                {
                    Icon = VK.VKUI.VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24RecentOutline),
                    Title = Locale.Get("recent"),
                    Stickers = new ObservableCollection<Sticker>(recentStickers.Items)
                };
                StickersPacks.Add(recent);

                Dictionary<string, string> req = new Dictionary<string, string>();
                req.Add("type", "stickers");
                req.Add("filters", "purchased,active");
                req.Add("extended", "1");
                var ownedStickers = await VKSession.Current.API.CallMethodAsync<VKList<StoreItem>>("store.getProducts", req);

                foreach (var p in ownedStickers.Items)
                {
                    StickersPack pack = new StickersPack()
                    {
                        Title = p.Title,
                        Preview = p.Previews[0].Uri,
                        Stickers = new ObservableCollection<Sticker>(p.Stickers)
                    };
                    StickersPacks.Add(pack);
                }
            }

            Items.ItemsSource = StickersPacks;
            Items.SelectedIndex = 0;
            loader.Visibility = Visibility.Collapsed;
        }

        private void SelectSticker(object sender, ItemClickEventArgs e)
        {
            Sticker s = e.ClickedItem as Sticker;
            if (s.IsAllowed) StickerSelected?.Invoke(s);
        }
    }
}
