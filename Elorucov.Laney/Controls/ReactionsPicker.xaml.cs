using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.Popups;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Methods;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class ReactionsPicker : UserControl {
        private MenuFlyout flyout;
        private Point pos;
        private int selectedReactionId;
        private long PeerId;
        private int ConvMsgId;

        ReactionsPickerFull rpf = null;

        public ReactionsPicker() {
            this.InitializeComponent();
        }

        ReactionsPicker(MenuFlyout mf, int selected, long peerId, int cmId) {
            this.InitializeComponent();
            selectedReactionId = selected;
            flyout = mf;
            flyout.Opened += MenuFlyoutOpened;
            PeerId = peerId;
            ConvMsgId = cmId;
        }

        private void MenuFlyoutOpened(object sender, object e) {
            if (AppSession.ReactionsAssets == null) return;
            // await Task.Delay(125);

            double windowWidth = Window.Current.Bounds.Width;
            double reactionsWidth = Root.Width;
            double reactionsHeight = Root.Height;

            var first = flyout.Items.FirstOrDefault();
            var transform = first.TransformToVisual(Window.Current.Content);
            pos = transform.TransformPoint(new Point());

            var x = pos.X;
            var y = pos.Y - reactionsHeight - 15;

            if (x > windowWidth - reactionsWidth) x = windowWidth - reactionsWidth;

            pos = new Point(x, y); // нужен для ReactionsPickerFull

            Popup p = new Popup {
                Child = this,
                Margin = new Thickness(x, y, 0, 0),
                AllowFocusOnInteraction = false,
                IsOpen = true
            };

            flyout.Closing += async (a, b) => {
                flyout.Opened -= MenuFlyoutOpened;
                if (rpf != null) {
                    await Task.Delay(150);

                }
                p.IsOpen = false;
                p.Child = null;
            };
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (Functions.IsWin11()) {
                Root.Translation += new Vector3(0, 0, 8);
                Root.Shadow = new ThemeShadow();
            }

            foreach (var rid in AppSession.SortedReactions) {
                bool needAddExpandButtonAndBreak = AppSession.SortedReactions.Count > 8 && ReactionsIcons.Children.Count >= 7;

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

                if (needAddExpandButtonAndBreak) hb.Content = new FixedFontIcon {
                    Glyph = "",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 1, 0, 0)
                };


                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) hb.CornerRadius = new CornerRadius(18);

                hb.Click += (a, b) => {
                    if (needAddExpandButtonAndBreak) {
                        rpf = new ReactionsPickerFull(pos, selectedReactionId, PeerId, ConvMsgId);
                        rpf.Show();
                    } else {
                        SendReaction(rid);
                    }

                    flyout.Hide();
                };

                Border br = new Border {
                    Width = 36, Height = 36,
                    CornerRadius = new CornerRadius(18),
                    Child = hb
                };
                if (selectedReactionId == rid && !needAddExpandButtonAndBreak) br.Background = (Brush)App.Current.Resources["AccentAcrylicBackgroundFillColorDefaultBrush"];

                ReactionsIcons.Children.Add(br);
                if (needAddExpandButtonAndBreak) break;
            }
        }

        private void SendReaction(int rid) {
            new System.Action(async () => {
                var response = selectedReactionId == rid ? await Messages.DeleteReaction(PeerId, ConvMsgId) : await Messages.SendReaction(PeerId, ConvMsgId, rid);
                Functions.ShowHandledErrorTip(response);
            })();
        }

        //

        public static void RegisterForMenuFlyout(MenuFlyout mf, long peerId, int cmId, int selectedReactionId = 0) {
            new ReactionsPicker(mf, selectedReactionId, peerId, cmId);
        }
    }
}
