using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Shapes;

namespace Elorucov.Laney.Helpers.UI
{
    public class ShadowHelper
    {
        public static void DrawShadow(FrameworkElement element, Rectangle mask, Vector3 offset, float blurRadius, float opacity)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
            SpriteVisual _visual = compositor.CreateSpriteVisual();
            _visual.Size = element.RenderSize.ToVector2();
            _visual.Offset = new Vector3(0, 0, 0);

            DropShadow _shadow = compositor.CreateDropShadow();
            _shadow.Offset = offset;
            _shadow.BlurRadius = blurRadius;
            _shadow.Color = Colors.Black;
            _shadow.Opacity = opacity;
            _shadow.Mask = mask.GetAlphaMask();
            _visual.Shadow = _shadow;
            ElementCompositionPreview.SetElementChildVisual(mask, _visual);

            mask.SizeChanged += (a, b) =>
            {
                if (_visual != null)
                {
                    _visual.Size = element.RenderSize.ToVector2();
                    _shadow.Mask = mask.GetAlphaMask();
                }
            };
        }
    }
}
