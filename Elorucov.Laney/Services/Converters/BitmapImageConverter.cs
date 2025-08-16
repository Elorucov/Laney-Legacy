using Elorucov.Laney.Services.Network;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Services.Converters {
    public class BitmapImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            Uri uri = null;
            if (value == null) return null;
            if (value is string s && Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute)) {
                uri = new Uri(s);
            } else if (value is Uri u) uri = u;

            bool isAva = false;
            bool isStickerInPanel = false;
            bool isStickerPreviewInPanel = false;
            bool isSharingModalPreview = false;

            if (parameter != null && parameter is string p) {
                isAva = p == "ava";
                isStickerInPanel = p == "sticker1";
                isStickerPreviewInPanel = p == "sticker2";
                isSharingModalPreview = p == "smp";
            }

            if (isAva && APIHelper.PlaceholderAvatars.Contains(uri)) return null;

            BitmapImage image = new BitmapImage {
                DecodePixelType = DecodePixelType.Logical
            };
            if (isAva) {
                image.DecodePixelWidth = 44;
                image.DecodePixelHeight = 44;
            }
            if (isStickerInPanel) {
                image.DecodePixelWidth = 64;
                image.DecodePixelHeight = 64;
            }
            if (isStickerPreviewInPanel) {
                image.DecodePixelWidth = 24;
                image.DecodePixelHeight = 24;
            }
            if (isSharingModalPreview) {
                image.DecodePixelWidth = 32;
                image.DecodePixelHeight = 32;
            }
            new Action(async () => { await image.SetUriSourceAsync(uri, true); })();
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}