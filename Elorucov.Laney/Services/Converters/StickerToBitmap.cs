using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Services.Converters {
    public class StickerToBitmap : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            BitmapImage image = null;
            new System.Action(async () => {
                if (value is Sticker sticker) {
                    if (sticker.StickerId <= 0) {
                        image = new BitmapImage(new Uri("ms-appx:///Assets/NonVector/StickerKeywordBot.png"));
                    } else {
                        int size = parameter != null && parameter is string s ? int.Parse(s) : 128;
                        Uri uri = APIHelper.GetStickerUri(sticker, size);
                        if (uri != null) {
                            image = new BitmapImage();
                            await image.SetUriSourceAsync(uri);
                        }
                    }
                } else if (value is UGCSticker ugc) {
                    int size = parameter != null && parameter is string s ? int.Parse(s) : 128;
                    Uri uri = APIHelper.GetStickerUri(ugc, size);
                    if (uri != null) {
                        image = new BitmapImage();
                        await image.SetUriSourceAsync(uri);
                    }
                }
            })();
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}