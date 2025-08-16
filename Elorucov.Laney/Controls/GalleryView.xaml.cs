using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Controls {
    public sealed partial class GalleryView : UserControl {
        long id = 0;
        public GalleryView() {
            this.InitializeComponent(); id = RegisterPropertyChangedCallback(ItemProperty, CheckItemCallback);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(ItemProperty, id); };

        }

        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
                   "Item", typeof(GalleryItem), typeof(GalleryView), new PropertyMetadata(default(object)));

        public GalleryItem Item {
            get { return (GalleryItem)GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
        }

        private void CheckItemCallback(DependencyObject sender, DependencyProperty dp) {
            GalleryItem p = (GalleryItem)GetValue(dp);
            string type = p.OriginalObject is Document ? "doc" : "photo";
            da.Text = $"{type}{p.OwnerId}_{((AttachmentBase)p.OriginalObject).Id}";

            InitializeScrollViewer(sv);
            new System.Action(async () => { await SetImageAsync(p); })();
        }

        public delegate void ZoomFactorChangedDelegate(float minZoomFactor, float currentZoomFactor);
        public event ZoomFactorChangedDelegate ZoomFactorChanged;

        bool HaveSizes = false;

        private void InitializeScrollViewer(ScrollViewer sv) {
            FixViewboxSize();

            dbg.Visibility = AppParameters.ShowGalleryViewDebugInfo ? Visibility.Visible : Visibility.Collapsed;
            sv.Focus(FocusState.Keyboard);
            SetScrollViewerZoomFactor(sv);
            sv.ViewChanging += (a, b) => {
                ZoomFactorChanged?.Invoke(sv.MinZoomFactor, sv.ZoomFactor);
                df.Text = $"CurZF:  {Math.Round(sv.ZoomFactor, 4)}";
            };
            sv.DoubleTapped += async (c, d) => {
                bool res = false;
                float mzf = sv.MinZoomFactor;

                var doubleTapPoint = d.GetPosition(sv);

                await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {
                    res = sv.ChangeView(doubleTapPoint.X, doubleTapPoint.Y, sv.ZoomFactor == mzf ? mzf + mzf : mzf, false);
                });
            };
        }

        private void ChangeZoomFactor(object sender, SizeChangedEventArgs e) {
            SetScrollViewerZoomFactor(sv);
        }

        private void SetScrollViewerZoomFactor(ScrollViewer sv) {
            GalleryItem p = (GalleryItem)GetValue(ItemProperty);
            if (p.Size.Width != 0 && p.Size.Height != 0) {
                HaveSizes = true;
                SetMinZoomFactor(sv, p.Size.Width, p.Size.Height);
            }
        }

        private void SetMinZoomFactor(ScrollViewer sv, double pw, double ph) {
            double sw = sv.ActualWidth;
            double sh = sv.ActualHeight;

            double zf = 1;

            double ws = 0, hs = 0;
            if (pw != 0) ws = sw / pw;
            if (ph != 0) hs = sh / ph;

            zf = Math.Min(ws, hs);

            if (zf < 1) {
                float mzf = zf < 0.1 ? 0.1f : (float)zf;
                de.Text = $"MinZF:  {Math.Round(mzf, 4)}";
                sv.MinZoomFactor = mzf;
                sv.ChangeView(sv.ScrollableWidth / 2, sv.ScrollableHeight / 2, mzf);
            } else {
                de.Text = $"MinZF:  1 (no)";
                sv.MinZoomFactor = 1;
                sv.ChangeView(null, null, 1);
            }

            db.Text = $"Canvas: {sw}x{sh}";
            dc.Text = $"Photo:  {pw}x{ph}";
        }

        private async Task SetImageAsync(GalleryItem p) {
            if (p.Source == null) {
                FindName(nameof(errorIcon));
                return;
            }
            BitmapImage img = new BitmapImage();
            img.ImageOpened += (a, b) => {
                loader.Visibility = Visibility.Collapsed;
                if (!HaveSizes) SetMinZoomFactor(sv, img.PixelWidth, img.PixelHeight);
            };
            img.DecodePixelType = DecodePixelType.Physical;
            await img.SetUriSourceAsync(p.Source, true);
            imgz.Source = img;
        }

        private void RootSizeChanged(object sender, SizeChangedEventArgs e) {
            FixViewboxSize();
        }

        private void FixViewboxSize() {
            double w = Root.ActualWidth;
            double h = Root.ActualHeight;
            var a = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            dw.Text = $"Window: {w}x{h}";
            dp.Text = $"DPI:    {Math.Round(a, 2)}";

            sv.Width = w * a;
            sv.Height = h * a;
        }
    }
}