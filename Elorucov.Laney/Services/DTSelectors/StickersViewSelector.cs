using Elorucov.Laney.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Services.DTSelectors {
    public class StickersViewSelector : DataTemplateSelector {

        public DataTemplate GraffitiViewTemplate { get; set; }
        public DataTemplate StickersTemplate { get; set; }
        public DataTemplate ChatStickersTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) {
            var i = item as StickerFlyoutItem;
            if (i != null) {
                switch (i.Type) {
                    case StickerFlyoutItemType.GraffitiMenu: return GraffitiViewTemplate;
                    case StickerFlyoutItemType.StickerPackView: return StickersTemplate;
                    case StickerFlyoutItemType.UGCStickerPackView: return ChatStickersTemplate;
                }
            }
            return base.SelectTemplateCore(item, container);
        }
    }
}