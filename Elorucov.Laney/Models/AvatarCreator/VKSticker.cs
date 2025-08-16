using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Models.AvatarCreator {
    public class VKSticker : IAvatarCreatorItem {
        public Sticker Sticker { get; private set; }

        public VKSticker(Sticker sticker) {
            Sticker = sticker;
        }

        // extraFlag — draw sticker with border
        public async Task<FrameworkElement> RenderAsync(RenderMode mode, bool extraFlag) {
            int size = 128;
            if (mode == RenderMode.InCanvas) {
                size = 256;
            } else {
                var scale = DisplayInformation.GetForCurrentView().ResolutionScale;
                if (scale == ResolutionScale.Scale100Percent) size = 64;
            }

            Uri stickerUrl = new Uri($"https://vk.com/sticker/1-{Sticker.StickerId}-{size}");
            Uri stickerWithBackgroundUrl = new Uri($"https://vk.com/sticker/1-{Sticker.StickerId}-{size}b");
            int controlSize = mode == RenderMode.InCanvas ? 224 : 64;

            if (Sticker.Vmoji != null) {
                StickerImage si = Sticker.Images.Where(i => i.Width == 512).FirstOrDefault();
                if (si != null) stickerUrl = si.Uri;
                StickerImage sib = Sticker.ImagesWithBackground.Where(i => i.Width == 256).FirstOrDefault();
                if (sib != null) stickerWithBackgroundUrl = sib.Uri;
            }

            BitmapImage bimage = new BitmapImage {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelWidth = controlSize,
                DecodePixelHeight = controlSize
            };
            await bimage.SetUriSourceAsync(extraFlag ? stickerWithBackgroundUrl : stickerUrl);

            Image image = new Image {
                Source = bimage,
                Width = controlSize,
                Height = controlSize,
            };
            if (mode == RenderMode.InCanvas) {
                Canvas.SetLeft(image, 48);
                Canvas.SetTop(image, 48);
            }

            return image;
        }
    }
}
