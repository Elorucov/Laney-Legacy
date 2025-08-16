using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Helpers;
using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls
{
    public sealed partial class ClickableImage : UserControl
    {
        long ip = 0;
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
            "Image", typeof(IPreview), typeof(ClickableImage), new PropertyMetadata(default(object)));

        public IPreview Image
        {
            get { return (IPreview)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        DispatcherTimer timer;
        public event RoutedEventHandler Click;

        public ClickableImage()
        {
            this.InitializeComponent();
            SizeChanged += ChangeBitmapImageDecodePixelSizes;
            ip = RegisterPropertyChangedCallback(ImageProperty, LoadAndShowPreview);

            Unloaded += (a, b) =>
            {
                UnregisterPropertyChangedCallback(ImageProperty, ip);
                timer = null;
            };
        }

        private void RootSizeChanged(object sender, SizeChangedEventArgs e)
        {
            bool isWide = e.NewSize.Width > 112;
            VideoDuration.Visibility = isWide ? Visibility.Visible : Visibility.Collapsed;
            DocInfoContainer.Margin = isWide ? new Thickness(8, 8, 0, 0) : new Thickness(3, 3, 0, 0);
            VideoDurationContainer.Margin = isWide ? new Thickness(8, 8, 0, 0) : new Thickness(3, 3, 0, 0);
        }

        private void ChangeBitmapImageDecodePixelSizes(object sender, SizeChangedEventArgs e)
        {
            Size s = e.NewSize;
            bi.DecodePixelWidth = s.Width > s.Height ? (int)s.Width : 0;
            bi.DecodePixelHeight = s.Width < s.Height ? (int)s.Height : 0;
        }

        private void LoadAndShowPreview(DependencyObject sender, DependencyProperty dp)
        {
            PreviewImage.Opacity = 0;
            IPreview preview = (IPreview)GetValue(dp);
            bi.UriSource = preview.PreviewImageUri;
            if (preview is Video)
            {
                Video v = preview as Video;
                VideoDuration.Text = v.Live == 1 ?
                    "LIVE" :
                    v.DurationTime.ToNormalString();
                VideoDurationContainer.Visibility = Visibility.Visible;
            }
            else if (preview is Document)
            {
                Document d = preview as Document;
                DocFormat.Text = d.Extension.ToUpper();
                DocInfoContainer.Visibility = Visibility.Visible;
            }
        }

        private void RunLoadAnimation(object sender, RoutedEventArgs e)
        {
            LoadAnimation.Begin();
        }

        private void SetReleaseAnimationTimer(object sender, object e)
        {
            if (timer != null)
            {
                timer.Start();
            }
            else
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (a, b) =>
                {
                    timer.Stop();
                    ReleaseAnimation.Begin();
                };
                timer.Start();
            }
        }

        private void RootKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space) PressAnimation.Begin();
        }

        private void RootKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                e.Handled = true;
                InvokeClick();
            }
        }

        private void RootPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PressAnimation.Begin();
        }

        private void RootPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            InvokeClick();
        }

        private void InvokeClick()
        {
            if (timer != null) timer.Stop();
            ReleaseAnimation.Begin();
            Click?.Invoke(this, new RoutedEventArgs());
        }
    }
}
