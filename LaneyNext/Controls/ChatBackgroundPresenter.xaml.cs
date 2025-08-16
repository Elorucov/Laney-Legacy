using Elorucov.Laney.Brushes;
using Elorucov.Laney.Core;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls
{
    public sealed partial class ChatBackgroundPresenter : UserControl
    {
        public ChatBackgroundPresenter()
        {
            this.InitializeComponent();

            Presenter.Height = Window.Current.Bounds.Height;
            Window.Current.SizeChanged += (a, b) => Presenter.Height = Window.Current.Bounds.Height;

            RenderBackground();
            ThemeManager.ChatBackgroundChanged += async (a, b) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => RenderBackground());
            };
        }

        private void RenderBackground()
        {
            int t = Settings.ChatBackgroundType;
            string b = Settings.ChatBackground;
            try
            {
                switch (t)
                {
                    case 1:
                        SolidColorBrush color = new SolidColorBrush(Helpers.UI.ColorHelper.ParseFromHex(b));
                        Presenter.Fill = color;
                        break;
                    case 2:
                        Uri uri = new Uri(b);
                        RenderImage(uri);
                        break;
                    default:
                        SolidColorBrush tcolor = new SolidColorBrush(Colors.Transparent);
                        Presenter.Fill = tcolor;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.General.Error($"Failed to render chat background! Type {t}; Bkg: {b}", ex);
            }
        }

        private void RenderImage(Uri uri)
        {
            if (Settings.ChatBackgroundImageStretch)
            {
                Presenter.Fill = new ImageBrush
                {
                    ImageSource = new BitmapImage(uri),
                    Stretch = Stretch.UniformToFill,
                };
            }
            else
            {
                Presenter.Fill = new TiledBrush
                {
                    Source = uri
                };
            }
        }
    }
}
