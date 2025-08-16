using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Network;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class StickerPackPreviewModal : Modal {
        public StickerPackPreviewModal(StockItem item) {
            this.InitializeComponent();
            new System.Action(async () => { await SetUp(item); })();
        }

        private Uri StickerPackUri;

        private async Task SetUp(StockItem item) {
            PackTitle.Text = item.Product.Title;
            PackAuthor.Text = item.Author;
            PackDescription.Text = item.Description;
            StickerPackUri = new Uri(item.Product.Url);

            BitmapImage prodBkgnd = new BitmapImage {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelHeight = 86
            };
            await prodBkgnd.SetUriSourceAsync(new Uri(item.Background));
            PackBackground.Fill = new ImageBrush { ImageSource = prodBkgnd, Stretch = Stretch.UniformToFill };

            BitmapImage prodImg = new BitmapImage {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelHeight = 72,
                DecodePixelWidth = 72
            };
            await prodImg.SetUriSourceAsync(new Uri(item.Photo));
            PackPhoto.Source = prodImg;

            foreach (Sticker sticker in item.Product.Stickers) {
                Uri uri = APIHelper.GetStickerUri(sticker, 128);
                if (uri == null) continue;
                BitmapImage img = new BitmapImage {
                    DecodePixelType = DecodePixelType.Logical,
                    DecodePixelWidth = 108,
                    DecodePixelHeight = 108
                };
                await img.SetUriSourceAsync(uri);
                StickersContainer.Children.Add(new Image { Source = img, Margin = new Windows.UI.Xaml.Thickness(6) });
            }
        }

        private void OnSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e) {
            double size = e.NewSize.Width / 4;
            size = Math.Floor(size);
            StickersContainer.ItemWidth = StickersContainer.ItemHeight = size;
        }

        private void OpenInBrowser(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            new System.Action(async () => {
                await Launcher.LaunchUriAsync(StickerPackUri);
            })();
            Hide(null);
        }
    }
}