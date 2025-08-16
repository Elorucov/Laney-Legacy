using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class PhotoVideoThumbnail : UserControl {
        public static readonly DependencyProperty PreviewProperty = DependencyProperty.Register(
           "Preview", typeof(IPreview), typeof(PhotoVideoThumbnail), new PropertyMetadata(default(object)));

        public IPreview Preview {
            get { return (IPreview)GetValue(PreviewProperty); }
            set { SetValue(PreviewProperty, value); }
        }

        DispatcherTimer timer;

        public event RoutedEventHandler Click;

        public PhotoVideoThumbnail() {
            this.InitializeComponent();
            RegisterPropertyChangedCallback(PreviewProperty, LoadAndShowPreview);
        }

        private void RootSizeChanged(object sender, SizeChangedEventArgs e) {
            bool isWide = e.NewSize.Width > 112;
            DocSize.Visibility = isWide ? Visibility.Visible : Visibility.Collapsed;
            VideoDuration.Visibility = isWide ? Visibility.Visible : Visibility.Collapsed;
            DocInfoContainer.Margin = isWide ? new Thickness(8, 8, 0, 0) : new Thickness(3, 3, 0, 0);
            VideoDurationContainer.Margin = isWide ? new Thickness(8, 8, 0, 0) : new Thickness(3, 3, 0, 0);

            PreviewImage.Opacity = 0;
            Size s = e.NewSize;
            bi.DecodePixelWidth = s.Width > s.Height ? (int)s.Width : 0;
            bi.DecodePixelHeight = s.Width < s.Height ? (int)s.Height : 0;
        }

        private void LoadAndShowPreview(DependencyObject sender, DependencyProperty dp) {
            PreviewImage.Opacity = 0;
            IPreview preview = (IPreview)GetValue(dp);

            new System.Action(async () => {
                try {
                    if (preview.PreviewImageUri != null) {
                        await bi.SetUriSourceAsync(preview.PreviewImageUri);
                    } else {
                        Log.Error($"Thumbnail: No URL!");
                        FindName(nameof(ErrorInfo));
                    }
                } catch (Exception ex) {
                    Log.Error($"Thumbnail: an error occured while loading preview! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                    FindName(nameof(ErrorInfo));
                }
            })();

            if (preview is Video v) {
                if (v.Upcoming == 1) {
                    VideoDuration.Text = Locale.Get("soon");
                } else {
                    VideoDuration.Text = v.Live == 1 ?
                    Locale.Get("videouc_live").ToUpperInvariant() :
                    v.DurationTime.ToString(v.DurationTime.Hours > 0 ? "c" : @"m\:ss");
                }
                VideoDurationContainer.Visibility = Visibility.Visible;

                if (v.Restriction != null && (!string.IsNullOrEmpty(v.Restriction.Title) || !string.IsNullOrEmpty(v.Restriction.Text))) {
                    FindName(nameof(ErrorInfo));
                    ErrorText.Text = v.Restriction.Title;
                }
            } else if (preview is Document) {
                Document d = preview as Document;
                DocFormat.Text = d.Extension.ToUpper();
                DocSize.Text = $"· {Functions.GetFileSize(d.Size)}";
                DocInfoContainer.Visibility = Visibility.Visible;
            }
        }

        private void BlurPreview() {
            ElementCompositionPreview.SetIsTranslationEnabled(PreviewImage, true);
            var compositor = ElementCompositionPreview.GetElementVisual(PreviewImage).Compositor;

            var graphicsEffect = new GaussianBlurEffect {
                Name = "Blur",
                BlurAmount = 16.0f,
                BorderMode = EffectBorderMode.Hard,
                Source = new CompositionEffectSourceParameter("Backdrop")
            };

            var blurEffectFactory = compositor.CreateEffectFactory(graphicsEffect, new[] { "Blur.BlurAmount" });
            var brush = blurEffectFactory.CreateBrush();

            var destinationBrush = compositor.CreateBackdropBrush();
            brush.SetSourceParameter("Backdrop", destinationBrush);

            var blurSprite = compositor.CreateSpriteVisual();
            blurSprite.Size = new System.Numerics.Vector2((float)PreviewImage.ActualWidth, (float)PreviewImage.ActualHeight);
            blurSprite.Brush = brush;

            ElementCompositionPreview.SetElementChildVisual(PreviewImage, blurSprite);
        }

        private void RunLoadAnimation(object sender, RoutedEventArgs e) {
            LoadAnimation.Begin();
            if (Preview is Video v && v.Restriction != null && v.Restriction.Blur) {
                new System.Action(async () => {
                    await Task.Delay(200);
                    BlurPreview();
                })();
            }
        }

        private void SetReleaseAnimationTimer(object sender, object e) {
            if (timer != null) {
                timer.Start();
            } else {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (a, b) => {
                    timer.Stop();
                    ReleaseAnimation.Begin();
                };
                timer.Start();
            }
        }

        private void InvokeClick() {
            if (timer != null) timer.Stop();
            ReleaseAnimation.Begin();
            Click?.Invoke(this, new RoutedEventArgs());
        }

        private void ButtonClick(object sender, RoutedEventArgs e) {
            InvokeClick();
        }
    }
}