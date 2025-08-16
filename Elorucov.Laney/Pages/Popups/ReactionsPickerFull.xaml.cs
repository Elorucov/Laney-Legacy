using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Popups {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReactionsPickerFull : OverlayModal {

        private int selectedReactionId;
        private long PeerId;
        private int ConvMsgId;

        public ReactionsPickerFull(Point position, int selected, long peerId, int cmId) {
            this.InitializeComponent();
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8)) {
                Root.Translation += new Vector3(0, 0, 32);
                Root.Shadow = new ThemeShadow();
            }

            selectedReactionId = selected;
            PeerId = peerId;
            ConvMsgId = cmId;

            double x = position.X;
            double y = position.Y;
            var bounds = Window.Current.Bounds;

            if (x > bounds.Width - Root.Width) x = bounds.Width - Root.Width - 12;
            if (y > bounds.Height - Root.Height) y = bounds.Height - Root.Height - 24;

            Root.Margin = new Thickness(x, y, 0, 0);

            Window.Current.SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Loaded -= OnLoaded;
            new Action(async () => { await Setup(); })();
        }

        private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e) {
            Window.Current.SizeChanged -= OnSizeChanged;
            new Action(async () => { await HideWithAnimation(); })();
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e) {
            if (e.OriginalSource is ContentPresenter p && p.Name == "OverlayModalFrame") new Action(async () => { await HideWithAnimation(); })();
        }

        private async Task Setup() {
            double count = AppSession.AvailableReactions.Count;
            double rows = Math.Ceiling(count / 8);
            double finalHeight = (36 * rows) + 6;
            ExpandingAnimationDA.To = finalHeight;
            CollapsingAnimationDA.From = finalHeight;

            foreach (var rid in AppSession.SortedReactions) {
                // Делаем таймаут и запускаем анимацию после отображения 8 первых реакций,
                // чтобы анимация не лагала при подгрузке остальных реакций.
                if (ReactionsIcons.Children.Count == 8) {
                    ExpandingAnimation.Begin();
                    await Task.Delay(150);
                }

                HyperlinkButton hb = new HyperlinkButton {
                    BorderThickness = new Thickness(0),
                    Width = 36, Height = 36,
                    Padding = new Thickness(2),
                    Content = new Image {
                        Width = 32, Height = 32,
                        Source = new SvgImageSource {
                            UriSource = Reaction.GetImagePathById(rid),
                        },
                        Stretch = Stretch.Uniform
                    },
                };

                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) hb.CornerRadius = new CornerRadius(18);

                hb.Click += async (a, b) => {
                    await SendReaction(rid);
                    await HideWithAnimation();
                };

                Border br = new Border {
                    Width = 36, Height = 36,
                    CornerRadius = new CornerRadius(18),
                    Child = hb
                };
                if (selectedReactionId == rid) br.Background = (Brush)App.Current.Resources["AccentAcrylicBackgroundFillColorDefaultBrush"];

                ReactionsIcons.Children.Add(br);
            }
        }

        private async Task SendReaction(int rid) {
            var response = selectedReactionId == rid ? await Messages.DeleteReaction(PeerId, ConvMsgId) : await Messages.SendReaction(PeerId, ConvMsgId, rid);
            Functions.ShowHandledErrorTip(response);
        }

        private async Task HideWithAnimation() {
            CollapsingAnimation.Begin();
            await Task.Delay(250);
            Hide();
        }
    }
}