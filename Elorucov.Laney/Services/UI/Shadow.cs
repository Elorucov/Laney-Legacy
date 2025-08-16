using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Elorucov.Laney.Services.UI {
    public class Shadow {
        public static void Draw(FrameworkElement control, Shape shadowTarget, float blurRadius, float opacity, Vector3? offset = null) {
            try {
                var compositor = Window.Current.Compositor;
                SpriteVisual _visual = (SpriteVisual)ElementCompositionPreview.GetElementChildVisual(shadowTarget);
                if (_visual == null) {
                    _visual = compositor.CreateSpriteVisual();
                    ElementCompositionPreview.SetElementChildVisual(shadowTarget, _visual);
                }
                _visual.Size = control.RenderSize.ToVector2();
                _visual.Offset = new Vector3(0, 0, 0);

                DropShadow _shadow = compositor.CreateDropShadow();
                _shadow.Offset = offset.HasValue ? offset.Value : new Vector3(0, 0, 0);
                _shadow.BlurRadius = blurRadius;
                _shadow.Color = Colors.Black;
                _shadow.Opacity = opacity;
                _shadow.Mask = shadowTarget.GetAlphaMask();
                _visual.Shadow = _shadow;
            } catch (Exception ex) { // Возможно, у кого-то могут быть неизвестные ошибки в нём, судя по логам
                Log.Error(ex, $"Cannot draw a shadow!");
            }
        }

        public static void TryDrawUsingThemeShadow(FrameworkElement control, Shape shadowTarget, List<UIElement> receivers, float blurRadius) {
            if (Functions.IsWin11()) {
                try {
                    if (shadowTarget.Parent != null && shadowTarget.Parent is Panel p) {
                        p.Children.Remove(shadowTarget);
                    } else {
                        shadowTarget.Visibility = Visibility.Collapsed;
                    }
                    ThemeShadow shadow = new ThemeShadow();
                    foreach (var receiver in receivers) {
                        shadow.Receivers.Add(receiver);
                    }
                    control.Shadow = shadow;
                    float z = blurRadius * 2;
                    control.Translation = new Vector3(0, 0, z);
                } catch (Exception ex) {
                    Log.Warn($"TryDrawUsingThemeShadow failed, using legacy Composition API to draw f**king shadow... 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                    Draw(control, shadowTarget, blurRadius, 0.1f, new Vector3(0, blurRadius / 2, 0));
                    control.SizeChanged += (c, d) => {
                        Draw(control, shadowTarget, blurRadius, 0.1f, new Vector3(0, blurRadius / 2, 0));
                    };
                }
            } else {
                Draw(control, shadowTarget, blurRadius, 0.1f, new Vector3(0, blurRadius / 2, 0));
                control.SizeChanged += (c, d) => {
                    Draw(control, shadowTarget, blurRadius, 0.1f, new Vector3(0, blurRadius / 2, 0));
                };
            }
        }
    }
}