using Elorucov.Laney.Services.Logger;
using System;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Elorucov.Laney.Services.UI {
    public class CompositionHelper {
        public static void SetupExpressionAnimation(Compositor compositor, ScrollViewer scrollViewer, UIElement target, string expression, string property) {
            try {
                var sprops = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);

                var exp = compositor.CreateExpressionAnimation();
                exp.SetReferenceParameter("Scroll", sprops);
                exp.Expression = expression;

                Visual visual = ElementCompositionPreview.GetElementVisual(target);
                visual.StartAnimation(property, exp);
            } catch (Exception ex) {
                Log.Error(ex, $"SetupExpressionAnimation error!");
            }
        }

        public static void SetupExpressionClipAnimation(Compositor compositor, ScrollViewer scrollViewer, InsetClip clip, string expression, string property) {
            try {
                var sprops = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);

                var exp = compositor.CreateExpressionAnimation();
                exp.SetReferenceParameter("Scroll", sprops);
                exp.Expression = expression;

                clip.StartAnimation(property, exp);
            } catch (Exception ex) {
                Log.Error(ex, $"SetupExpressionClipAnimation error!");
            }
        }

        public static void StopAnimation(Compositor compositor, UIElement target, string property) {
            try {
                Visual visual = ElementCompositionPreview.GetElementVisual(target);
                visual.StopAnimation(property);
            } catch (Exception ex) {
                Log.Error(ex, $"StopAnimation error!");
            }
        }
    }
}