using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class PhotoViewer : Page
    {
        public PhotoViewer()
        {
            this.InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplicationView av = ApplicationView.GetForCurrentView();
            ApplicationViewTitleBar tb = av.TitleBar;
            tb.BackgroundColor = Color.FromArgb(0, 0, 0, 0);
            tb.ForegroundColor = Color.FromArgb(255, 255, 255, 255);
            tb.ButtonBackgroundColor = Color.FromArgb(128, 0, 0, 0);
            tb.ButtonForegroundColor = Color.FromArgb(255, 255, 255, 255);
            tb.ButtonInactiveBackgroundColor = Color.FromArgb(128, 0, 0, 0);
            tb.ButtonInactiveForegroundColor = Color.FromArgb(255, 255, 255, 255);

            await Task.Delay(500); // При появление окна происходит ресайз, а окно всё-ещё не в полноэкранном режиме.
            var view = ApplicationView.GetForCurrentView();
            SizeChanged += (a, b) =>
            {
                if (!view.IsFullScreenMode) Window.Current.Close();
            };
            Window.Current.VisibilityChanged += (a, b) =>
            {
                Window.Current.Close();
            };
        }

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentIndex.Text = (MainFlipView.SelectedIndex + 1).ToString();
        }

        private void MainFlipView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Top.Visibility = Top.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            Bottom.Visibility = Bottom.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void ZoomImage(object sender, DoubleTappedRoutedEventArgs e)
        {
            ScrollViewer sv = sender as ScrollViewer;
            float mzf = sv.MinZoomFactor;
            var doubleTapPoint = e.GetPosition(sv);

            await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                bool res = sv.ChangeView(doubleTapPoint.X, doubleTapPoint.Y, sv.ZoomFactor == mzf ? mzf + mzf : mzf, false);
            });
        }
    }
}