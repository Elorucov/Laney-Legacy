using Elorucov.Laney.Models.UI;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class ChatBackgroundControl : UserControl {
        long id = 0;
        long sid = 0;

        public static readonly DependencyProperty ChatBackgroundProperty = DependencyProperty.Register(
            nameof(ChatBackground), typeof(Background), typeof(ChatBackgroundControl), new PropertyMetadata(default));

        public Background ChatBackground {
            get { return (Background)GetValue(ChatBackgroundProperty); }
            set { SetValue(ChatBackgroundProperty, value); }
        }

        public static readonly DependencyProperty VectorScaleProperty = DependencyProperty.Register(
    nameof(VectorScale), typeof(double), typeof(ChatBackgroundControl), new PropertyMetadata(1.0));

        public double VectorScale {
            get { return (double)GetValue(VectorScaleProperty); }
            set { SetValue(VectorScaleProperty, value); }
        }

        SpriteVisual blurVisual;
        SpriteVisual svgVisual;
        GaussianBlurEffect effect;
        CompositionEffectBrush brush;
        Blur blur; // for vector background
        const double RADIUS_DIVIDE = 2.5;

        public ChatBackgroundControl() {
            this.InitializeComponent();
            Loaded += ChatBackgroundControl_Loaded;
        }

        private void ChatBackgroundControl_Loaded(object sender, RoutedEventArgs e) {
            SetUp(this, ChatBackgroundProperty);
            Theme.ThemeChanged += Theme_ThemeChanged;
            App.UISettings.ColorValuesChanged += OnColorValuesChanged;

            id = RegisterPropertyChangedCallback(ChatBackgroundProperty, SetUp);
            sid = RegisterPropertyChangedCallback(VectorScaleProperty, SetUp);
            Unloaded += (a, b) => {
                Theme.ThemeChanged -= Theme_ThemeChanged;
                App.UISettings.ColorValuesChanged -= OnColorValuesChanged;
                Loaded -= ChatBackgroundControl_Loaded;
                if (id != 0) UnregisterPropertyChangedCallback(ChatBackgroundProperty, id);
                if (sid != 0) UnregisterPropertyChangedCallback(VectorScaleProperty, sid);
            };
        }

        private void Theme_ThemeChanged(object sender, bool e) {
            SetUp(this, ChatBackgroundProperty);
        }

        private void OnColorValuesChanged(UISettings sender, object args) {
            new Action(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    SetUp(this, ChatBackgroundProperty);
                });
            })();
        }

        private void SetUp(DependencyObject sender, DependencyProperty dp) {
            EllipsesRoot.Children.Clear();
            Gradient.Fill = null;
            ElementCompositionPreview.SetElementChildVisual(BlurLayer, null);
            ElementCompositionPreview.SetElementChildVisual(SVGBackgroundLayer, null);
            SVGBackgroundLayer.Fill = null;
            OpacityLayer.Opacity = 0;
            blur = null;
            blurVisual = null;
            svgVisual = null;
            effect = null;
            brush = null;

            Background background = (Background)GetValue(ChatBackgroundProperty);
            if (background == null) {
                Debug.WriteLine($"ChatBackgroundControl > no background to set up!");
                return;
            } else {
                Debug.WriteLine($"ChatBackgroundControl > setting up \"{background.Id}\"...");
            }

            new Action(async () => {
                await SetupBackground(Theme.IsDarkTheme() ? background.Dark : background.Light);
            })();
        }

        private async Task SetupBackground(BackgroundSources source) {
            try {
                if (source.Type == "vector" && source.Vector != null) {
                    SetupGradient(source.Vector.Gradient);
                    double radius = source.Vector.Blur != null ? source.Vector.Blur.Radius : 0;
                    SetupEllipses(source.Vector.ColorEllipses, radius);
                    SetupBlur(source.Vector.Blur, source.Vector.ColorEllipses.Count > 0);
                    await SetupSVGBackground(source.Vector.SVG);
                } else if (source.Type == "raster" && source.Raster != null) {
                    BitmapImage img = new BitmapImage();
                    Uri uri = new Uri(source.Raster.Url);
                    if (uri.Scheme != "https") {
                        img.UriSource = uri;
                        SVGBackgroundLayer.Fill = new ImageBrush {
                            Stretch = Stretch.UniformToFill,
                            AlignmentX = AlignmentX.Center,
                            AlignmentY = AlignmentY.Center,
                            ImageSource = img
                        };
                    } else {
                        img.ImageFailed += (a, b) => {
                            Log.Error($"{GetType().Name}.SetupBackground > Background download failed! File: {source.Raster.Url}; err: {b.ErrorMessage}");
                        };
                        img.ImageOpened += (a, b) => {
                            SVGBackgroundLayer.Fill = new ImageBrush {
                                Stretch = Stretch.UniformToFill,
                                AlignmentX = AlignmentX.Center,
                                AlignmentY = AlignmentY.Center,
                                ImageSource = img
                            };
                        };
                        await img.SetUriSourceAsync(uri);
                    }
                }
            } catch (Exception ex) {
                Log.Error($"{GetType().Name}.SetupBackground > Error 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
        }

        private void SetupGradient(Gradient gradient) {
            if (gradient == null) return;

            LinearGradientBrush brush = new LinearGradientBrush {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };

            brush.RelativeTransform = new RotateTransform {
                CenterX = 0.5,
                CenterY = 0.5,
                Angle = gradient.Angle
            };

            double offsetStep = 1.0 / (gradient.Colors.Count - 1);
            double currentOffset = 1;

            foreach (var color in gradient.Colors) {
                brush.GradientStops.Add(new GradientStop {
                    Offset = currentOffset,
                    Color = ChatThemeService.ParseHex(color)
                });
                currentOffset = currentOffset - offsetStep;
            }

            Gradient.Fill = brush;
        }

        private void SetupEllipses(List<ColorEllipse> ellipses, double blurRadius) {
            if (ellipses == null || ellipses.Count == 0) return;

            blurRadius = blurRadius / RADIUS_DIVIDE;
            foreach (ColorEllipse e in ellipses) {
                Ellipse ellipse = new Ellipse {
                    Width = e.RadiusX * EllipsesRoot.Width + (blurRadius / 2.5),
                    Height = e.RadiusY * EllipsesRoot.Height + (blurRadius / 2.5),
                    Fill = new SolidColorBrush(ChatThemeService.ParseHex(e.Color))
                };
                Canvas.SetLeft(ellipse, e.X * EllipsesRoot.Width - (ellipse.Width / 2.5));
                Canvas.SetTop(ellipse, e.Y * EllipsesRoot.Height - (ellipse.Height / 2.5));
                EllipsesRoot.Children.Add(ellipse);
            }
        }

        private void SetupBlur(Blur blur, bool hasEllipses) {
            if (blur == null) return;
            OpacityLayer.Fill = new SolidColorBrush(ChatThemeService.ParseHex(blur.Color));
            OpacityLayer.Opacity = blur.Opacity;

            if (blur.Opacity == 1) return;

            this.blur = blur;
            var visual = ElementCompositionPreview.GetElementVisual(BlurLayer);
            var compositor = visual.Compositor;
            blurVisual = compositor.CreateSpriteVisual();
            blurVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);

            // Blur amout more than 250 is not allowed in UWP!
            effect = new GaussianBlurEffect {
                Name = "Blur",
                BlurAmount = Math.Min(((float)blur.Radius / (float)RADIUS_DIVIDE) / 640f * (float)ActualWidth, 250f),
                BorderMode = EffectBorderMode.Hard,
                Optimization = EffectOptimization.Speed,
                Source = new CompositionEffectSourceParameter("source")
            };

            var bb = compositor.CreateBackdropBrush();
            var factory = compositor.CreateEffectFactory(effect, new[] { "Blur.BlurAmount" });
            brush = factory.CreateBrush();
            blurVisual.Brush = brush;
            brush.SetSourceParameter("source", compositor.CreateBackdropBrush());

            ElementCompositionPreview.SetElementChildVisual(BlurLayer, blurVisual);
        }

        private async Task SetupSVGBackground(VectorBackgroundSource svg) {
            bool isModernWindows = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7);
            var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel * VectorScale;

            double nw = svg.Width;
            double nh = svg.Height;

            string xml = string.Empty;
            try {
                var response = await LNet.GetAsync(new Uri(svg.Url));
                xml = await response.Content.ReadAsStringAsync();
            } catch (Exception ex) {
                Log.Error($"{GetType().Name}.SetupSVGBackground > Error loading file! File: {svg.Url}, HResult: 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                return;
            }

            // Replacing width and height in svg (bad way)
            if (scale > 1) {
                nw = svg.Width * scale;
                nh = svg.Height * scale;
                xml = xml.Replace($"width=\"{svg.Width}\"", $"width=\"{nw}\"");
                xml = xml.Replace($"height=\"{svg.Height}\"", $"height=\"{nh}\"");
            }

            var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            var canvasDevice = CanvasDevice.GetSharedDevice();
            CanvasSvgDocument doc = null;
            try {
                doc = CanvasSvgDocument.LoadFromXml(canvasDevice, xml);
            } catch (Exception ex) {
                Log.Error($"{GetType().Name}.SetupSVGBackground > Error parsing file! File: {svg.Url}, HResult: 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                return;
            }

            var graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, canvasDevice);

            var drawingSurface = graphicsDevice.CreateDrawingSurface(new Size(nw, nh),
                DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
            using (var ds = CanvasComposition.CreateDrawingSession(drawingSurface)) {
                ds.Clear(Colors.Transparent);
                ds.DrawSvg(doc, new Size(nw, nh));
            }

            var surfaceBrush = compositor.CreateSurfaceBrush(drawingSurface);
            surfaceBrush.Stretch = CompositionStretch.None;
            if (scale > 1) surfaceBrush.Scale = new Vector2(1 / (float)scale);

            var border = new BorderEffect {
                ExtendX = CanvasEdgeBehavior.Wrap,
                ExtendY = CanvasEdgeBehavior.Wrap,
                Source = new CompositionEffectSourceParameter("source")
            };

            var fxFactory = compositor.CreateEffectFactory(border);
            var fxBrush = fxFactory.CreateBrush();
            fxBrush.SetSourceParameter("source", surfaceBrush);

            CompositionEffectBrush cebrush = fxBrush;
            if (isModernWindows && blurVisual?.Brush != null && EllipsesRoot.Children.Count > 0) {
                var blend = new BlendEffect {
                    Background = new CompositionEffectSourceParameter("Main"),
                    Foreground = new CompositionEffectSourceParameter("Tint"),
                    Mode = BlendEffectMode.Overlay
                };

                var blendFactory = compositor.CreateEffectFactory(blend);
                cebrush = blendFactory.CreateBrush();
                cebrush.SetSourceParameter("Main", fxBrush);
                if (blurVisual != null) cebrush.SetSourceParameter("Tint", blurVisual.Brush);
            }

            svgVisual = compositor.CreateSpriteVisual();
            svgVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
            svgVisual.Opacity = isModernWindows ? (float)svg.Opacity : 0.35f;
            svgVisual.Brush = cebrush;

            ElementCompositionPreview.SetElementChildVisual(SVGBackgroundLayer, svgVisual);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            clipRec.Rect = new Rect(0, 0, VectorColorsRoot.ActualWidth, VectorColorsRoot.ActualHeight);

            if (blurVisual != null && blur != null) {
                blurVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);

                float blurAmount = Math.Min(((float)blur.Radius / (float)RADIUS_DIVIDE) / 640f * (float)ActualWidth, 250f);
                brush.Properties.InsertScalar("Blur.BlurAmount", blurAmount);
            }

            if (svgVisual != null) {
                svgVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
            }
        }
    }
}