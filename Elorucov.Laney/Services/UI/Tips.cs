using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services.UI {
    public class Tips {

        static Grid g;
        public static void Init(Grid grid) {
            g = grid;
        }

        public static void AddToAppRoot(TeachingTip tt) {
            g.Children.Add(tt);
            tt.Closed += (a, b) => {
                g.Children.Remove(tt);
            };
        }

        public static void Show(string title, string subtitle = null, string buttonText = null, Action buttonAction = null, FrameworkElement target = null) {
            if (g == null) return;

            TeachingTip tt = new TeachingTip {
                Title = title,
                PreferredPlacement = TeachingTipPlacementMode.Bottom,
                PlacementMargin = new Thickness(target == null ? 72 : 0),
                Target = target,
            };

            if (!string.IsNullOrEmpty(subtitle))
                tt.Content = new TextBlock { Text = subtitle, TextWrapping = TextWrapping.Wrap };

            if (!string.IsNullOrEmpty(buttonText) && buttonAction != null) {
                tt.ActionButtonContent = buttonText;
                tt.ActionButtonClick += (a, b) => {
                    buttonAction?.Invoke();
                    tt.IsOpen = false;
                };
            }

            DispatcherTimer tmr = new DispatcherTimer();
            tmr.Interval = TimeSpan.FromMilliseconds(3000);
            tmr.Tick += (a, b) => {
                tmr.Stop();
                tt.IsOpen = false;
            };
            tmr.Start();
            g.Children.Add(tt);
            tt.IsOpen = true;
            FixUI(tt);
            tt.Closed += (a, b) => {
                g.Children.Remove(tt);
            };
        }

        public static void FixUI(TeachingTip tt) {
            if (tt.Target == null) {
                tt.RequestedTheme = ElementTheme.Dark;
            } else {
                // Force set dark theme to TT's inner root element.
                // Баг в том, что если применить тёмную тему в самом tt,
                // когда у него есть Target, то и сам Target становится тёмной в светлой теме!
                new System.Action(async () => {
                    await Task.Delay(33);
                    var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
                    foreach (var popup in popups) {
                        if (popup.Child is Grid groot && groot.Children.Count > 0) {
                            var ttroot = groot.Children.FirstOrDefault();
                            if (ttroot != null && ttroot is Grid tog && tog.Name == "TailOcclusionGrid") {
                                tog.RequestedTheme = ElementTheme.Dark;
                                break;
                            }
                        }
                    }
                })();
            }
        }
    }
}
