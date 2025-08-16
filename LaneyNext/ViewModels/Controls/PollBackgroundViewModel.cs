using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Helpers.UI;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.ViewModels.Controls
{
    public class PollBackgroundViewModel : BaseViewModel
    {
        private Brush _background;
        private SolidColorBrush _backgroundColor;
        private ElementTheme _theme;

        public Brush Background { get { return _background; } set { _background = value; OnPropertyChanged(); } }
        public SolidColorBrush BackgroundColor { get { return _backgroundColor; } set { _backgroundColor = value; OnPropertyChanged(); } }
        public ElementTheme Theme { get { return _theme; } set { _theme = value; OnPropertyChanged(); } }

        public int Id { get; private set; } = 0;

        public PollBackgroundViewModel(PollBackground pb, ElementTheme theme = ElementTheme.Default)
        {
            Id = pb.Id;
            Theme = theme;
            BackgroundColor = pb.Type != PollBackgroundType.Unknown ? new SolidColorBrush(ColorHelper.ParseFromHex(pb.ColorHEX)) : null;
            if (pb.Type == PollBackgroundType.Gradient)
            {
                GradientStopCollection gsc = new GradientStopCollection();
                foreach (var p in pb.Points)
                {
                    gsc.Add(new GradientStop { Offset = p.Position, Color = ColorHelper.ParseFromHex(p.ColorHEX) });
                }
                LinearGradientBrush lgb = new LinearGradientBrush(gsc, pb.Angle);
                lgb.EndPoint = new Point(-lgb.EndPoint.X, -lgb.EndPoint.Y);
                Background = lgb;
            }
        }
    }
}