using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Controls
{
    public class VideoMTC : MediaTransportControls
    {
        public VideoMTC()
        {
            this.DefaultStyleKey = typeof(VideoMTC);
        }

        public event RoutedEventHandler SettingsButtonClick;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Button settingsButton = (Button)GetTemplateChild("SettingsButton");
            settingsButton.Click += (a, b) => SettingsButtonClick?.Invoke(a, b);
        }
    }
}
