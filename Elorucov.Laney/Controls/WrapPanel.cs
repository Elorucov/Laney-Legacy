using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Source from https://github.com/yazdipour/UWPChipsX/blob/master/ChipsControl/WrapPanel.cs

namespace Elorucov.Laney.Controls {
    public sealed class WrapPanel : Panel {
        public double HorizontalSpacing {
            get => (double)GetValue(HorizontalSpacingProperty);
            set => SetValue(HorizontalSpacingProperty, value);
        }

        public static DependencyProperty HorizontalSpacingProperty { get; } =
            DependencyProperty.Register(
                nameof(HorizontalSpacing),
                typeof(double),
                typeof(WrapPanel),
                new PropertyMetadata(0d, LayoutPropertyChanged));

        public double VerticalSpacing {
            get => (double)GetValue(VerticalSpacingProperty);
            set => SetValue(VerticalSpacingProperty, value);
        }

        public static DependencyProperty VerticalSpacingProperty { get; } =
            DependencyProperty.Register(
                nameof(VerticalSpacing),
                typeof(double),
                typeof(WrapPanel),
                new PropertyMetadata(0d, LayoutPropertyChanged));

        public Orientation Orientation {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static DependencyProperty OrientationProperty { get; } =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(WrapPanel),
                new PropertyMetadata(Orientation.Horizontal, LayoutPropertyChanged));

        private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is WrapPanel wp)) return;
            wp.InvalidateMeasure();
            wp.InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize) {
            var totalMeasure = UvMeasure.Zero;
            var parentMeasure = new UvMeasure(Orientation, availableSize.Width, availableSize.Height);
            var spacingMeasure = new UvMeasure(Orientation, HorizontalSpacing, VerticalSpacing);
            var lineMeasure = UvMeasure.Zero;

            foreach (var child in Children.Where(c => c.Visibility == Visibility.Visible)) {
                child.Measure(availableSize);

                var currentMeasure = new UvMeasure(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);

                if (parentMeasure.U > currentMeasure.U + lineMeasure.U + spacingMeasure.U) {
                    lineMeasure.U += currentMeasure.U + spacingMeasure.U;
                    lineMeasure.V = Math.Max(lineMeasure.V, currentMeasure.V);
                } else {
                    totalMeasure.U = Math.Max(lineMeasure.U, totalMeasure.U);
                    totalMeasure.V += lineMeasure.V + spacingMeasure.V;

                    if (parentMeasure.U > currentMeasure.U) {
                        lineMeasure = currentMeasure;
                    } else {
                        totalMeasure.U = Math.Max(currentMeasure.U, totalMeasure.U);
                        totalMeasure.V += currentMeasure.V;
                        lineMeasure = UvMeasure.Zero;
                    }
                }
            }

            totalMeasure.U = Math.Max(lineMeasure.U, totalMeasure.U);
            totalMeasure.V += lineMeasure.V;

            totalMeasure.U = Math.Ceiling(totalMeasure.U);

            return Orientation == Orientation.Horizontal
                ? new Size(totalMeasure.U, totalMeasure.V)
                : new Size(totalMeasure.V, totalMeasure.U);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var parentMeasure = new UvMeasure(Orientation, finalSize.Width, finalSize.Height);
            var spacingMeasure = new UvMeasure(Orientation, HorizontalSpacing, VerticalSpacing);
            var position = UvMeasure.Zero;

            double currentV = 0;
            foreach (var child in Children.Where(c => c.Visibility == Visibility.Visible)) {
                var desiredMeasure = new UvMeasure(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
                if (desiredMeasure.U + position.U > parentMeasure.U) {
                    position.U = 0;
                    position.V += currentV + spacingMeasure.V;
                    currentV = 0;
                }

                child.Arrange(Orientation == Orientation.Horizontal
                    ? new Rect(position.U, position.V, child.DesiredSize.Width, child.DesiredSize.Height)
                    : new Rect(position.V, position.U, child.DesiredSize.Width, child.DesiredSize.Height));

                position.U += desiredMeasure.U + spacingMeasure.U;
                currentV = Math.Max(desiredMeasure.V, currentV);
            }

            return finalSize;
        }

        private struct UvMeasure {
            internal static readonly UvMeasure Zero = default(UvMeasure);
            internal double U { get; set; }
            internal double V { get; set; }

            public UvMeasure(Orientation orientation, double width, double height) {
                if (orientation == Orientation.Horizontal) {
                    U = width;
                    V = height;
                } else {
                    U = height;
                    V = width;
                }
            }
        }
    }
}