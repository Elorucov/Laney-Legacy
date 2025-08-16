using Elorucov.VkAPI.Objects;
using VK.VKUI.Controls;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services {
    public static class DocumentIcons {
        public static VKIconName GetIcon(DocumentType type) {
            switch (type) {
                case DocumentType.Text: return VKIconName.Icon28ArticleOutline;
                case DocumentType.Archive: return VKIconName.Icon28ZipOutline;
                case DocumentType.GIF: return VKIconName.Icon28PictureOutline;
                case DocumentType.Image: return VKIconName.Icon28PictureOutline;
                case DocumentType.Audio: return VKIconName.Icon28MusicOutline;
                case DocumentType.Video: return VKIconName.Icon28VideoOutline;
                case DocumentType.EBook: return VKIconName.Icon28ArticleOutline;
                default: return VKIconName.Icon28DocumentOutline;
            }
        }

        public static SolidColorBrush GetIconBackground(DocumentType type) {
            switch (type) {
                case DocumentType.Text: return new SolidColorBrush(Color.FromArgb(255, 0, 122, 204));
                case DocumentType.Archive: return new SolidColorBrush(Color.FromArgb(255, 118, 185, 121));
                case DocumentType.GIF: return new SolidColorBrush(Color.FromArgb(255, 119, 165, 214));
                case DocumentType.Image: return new SolidColorBrush(Color.FromArgb(255, 119, 165, 214));
                case DocumentType.Audio: return new SolidColorBrush(Color.FromArgb(255, 186, 104, 200));
                case DocumentType.Video: return new SolidColorBrush(Color.FromArgb(255, 229, 115, 155));
                case DocumentType.EBook: return new SolidColorBrush(Color.FromArgb(255, 255, 174, 56));
                default: return new SolidColorBrush(Color.FromArgb(255, 119, 165, 214));
            }
        }
    }
}