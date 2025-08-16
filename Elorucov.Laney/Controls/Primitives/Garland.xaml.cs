using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Primitives {
    public sealed partial class Garland : UserControl {
        public Garland() {
            this.InitializeComponent();
        }

        bool alreadyLoaded = false;
        DispatcherTimer timer;
        List<Color> colors = new List<Color> {
            Color.FromArgb(255, 0xf6, 0x79, 0xff),
            Color.FromArgb(255, 0xff, 0xd9, 0x1d),
            Color.FromArgb(255, 0xab, 0x47, 0xe9),
            Color.FromArgb(255, 0x00, 0x77, 0xff),
            Color.FromArgb(255, 0x17, 0xd6, 0x85),
            Color.FromArgb(255, 0xf6, 0x79, 0xff)
        };

        private void UserControl_Unloaded(object sender, RoutedEventArgs e) {
            timer?.Stop();
        }

        private void GarlandRoot_Loaded(object sender, RoutedEventArgs e) {
            if (alreadyLoaded) {
                if (timer != null && !timer.IsEnabled) timer?.Start();
                return;
            }
            alreadyLoaded = true;

            DataTemplate ftemplate = Resources["GarlandTemplate"] as DataTemplate;
            StackPanel froot = ftemplate.LoadContent() as StackPanel;
            froot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var gw = froot.DesiredSize.Width;

            DisplayInformation di = DisplayInformation.GetForCurrentView();
            var sw = di.ScreenWidthInRawPixels;

            var count = Convert.ToInt32(Math.Ceiling((double)sw / gw));
            for (int i = 0; i <= count; i++) {
                DataTemplate template = Resources["GarlandTemplate"] as DataTemplate;
                StackPanel root = template.LoadContent() as StackPanel;
                GarlandRoot.Children.Add(root);
                SetupColors(root);
            }

            timer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        int pos = 0; // lamp

        private void Timer_Tick(object sender, object e) {
            if (pos == int.MaxValue) pos = 0;
            pos++;

            foreach (StackPanel root in GarlandRoot.Children) SetupColors(root);
        }

        private void SetupColors(StackPanel root) {
            foreach (FrameworkElement el in root.Children) {
                var npos = pos % colors.Count;

                if (el is Canvas canvas && canvas.Children.Count == 3) {
                    Path shadow = canvas.Children[1] as Path;
                    Path lamp = canvas.Children[2] as Path;
                    Color color = colors[npos];

                    lamp.Fill = new SolidColorBrush(colors[npos]);
                    shadow.Fill = new SolidColorBrush(colors[npos]);
                    if (shadow.Tag == null) {
                        shadow.Loaded += ShadowPath_Loaded;
                    } else {
                        SetupShadow(shadow);
                    }
                }

                if (pos == int.MaxValue) pos = 0;
                pos++;
            }
        }

        private void ShadowPath_Loaded(object sender, RoutedEventArgs e) {
            Path shadow = sender as Path;
            shadow.Tag = true;
            shadow.Loaded -= ShadowPath_Loaded;
            SetupShadow(shadow);
        }

        private void SetupShadow(Path shadow) {
            SpriteVisual v = (SpriteVisual)ElementCompositionPreview.GetElementChildVisual(shadow);
            if (v != null && v.Size.X > 0 && v.Size.Y > 0) {
                (v.Shadow as DropShadow).Color = (shadow.Fill as SolidColorBrush).Color;
                return;
            }

            shadow.Width = shadow.Width + 6;
            shadow.Height = shadow.Height + 6;

            Compositor c = Window.Current.Compositor;
            v = c.CreateSpriteVisual();

            ElementCompositionPreview.SetElementChildVisual(shadow, v);

            v.Size = new Vector2((float)shadow.Width, (float)shadow.Height);
            v.Offset = new Vector3(0, 0, 0);

            DropShadow ds = c.CreateDropShadow();
            ds.Offset = new Vector3(0, 0, 0);
            ds.BlurRadius = 12.0f;
            ds.Color = (shadow.Fill as SolidColorBrush).Color;
            ds.Opacity = 1.0f;
            ds.Mask = shadow.GetAlphaMask();
            v.Shadow = ds;
        }
    }
}