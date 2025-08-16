using Elorucov.Laney.Pages;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class VideoMessageControl : UserControl {
        VideoMessage Message;
        public Path Figure { get; private set; }

        public VideoMessageControl(VideoMessage message) {
            this.InitializeComponent();
            Message = message;

            Loaded += Setup;
        }

        private void Setup(object sender, RoutedEventArgs e) {
            Loaded -= Setup;
            if (Message == null) return;

            try {
                Duration.Text = Message.Duration >= 3600 ? Message.DurationTime.ToString("c") : Message.DurationTime.ToString(@"m\:ss");
                TranscriptButton.Visibility = string.IsNullOrWhiteSpace(Message.Transcript) ? Visibility.Collapsed : Visibility.Visible;

                string shapeRaw = "M216 108c0 59.647-48.353 108-108 108S0 167.647 0 108 48.353 0 108 0s108 48.353 108 108Z";
                if (AppSession.VideoMessageShapes != null) {
                    var shape = AppSession.VideoMessageShapes.Shapes.Where(s => s.Id == Message.ShapeId).FirstOrDefault();
                    if (shape != null) shapeRaw = shape.RawPath;
                }

                string stroke = Message.OwnerId == AppParameters.UserID ? "VKImBubbleOutgoingBrush" : "VKImBubbleIncomingBrush";
                string xaml = $"<Path xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" StrokeThickness=\"1\" Stroke=\"{{ThemeResource {stroke}}}\" Fill=\"Transparent\" Data=\"{shapeRaw}\"/>";
                FigureRoot.Children.Clear();

                Figure = (Path)XamlReader.Load(xaml);
                BitmapImage img = new BitmapImage {
                    DecodePixelType = DecodePixelType.Logical,
                    DecodePixelWidth = 216,
                    DecodePixelHeight = 216
                };

                new System.Action(async () => {
                    try {
                        int index = Math.Min(1, Message.FirstFrame.Count - 1);
                        await img.SetUriSourceAsync(Message.FirstFrame[index].Uri);
                        Figure.Fill = new ImageBrush {
                            ImageSource = img,
                            Stretch = Stretch.UniformToFill,
                            AlignmentX = AlignmentX.Center,
                            AlignmentY = AlignmentY.Center,
                        };
                    } catch (Exception ex) { // strange, but someone experienced crash in this Action block, ignoring parent try-catch, lol.
                        Log.Error(ex, $"{nameof(VideoMessageControl)}: An error occured when rendering video message! (image displaying phase)");
                    }
                })();

                FigureRoot.Children.Add(Figure);
            } catch (Exception ex) {
                Log.Error(ex, $"{nameof(VideoMessageControl)}: An error occured when rendering video message!");
            }
        }

        private void ShowTranscript(object sender, RoutedEventArgs e) {
            FrameworkElement element = sender as FrameworkElement;
            Flyout flyout = new Flyout {
                Content = new TextBlock {
                    Text = Message.Transcript,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(12)
                }
            };
            flyout.ShowAt(element);
        }

        private void PlayVideo(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await VideoPlayerView.Show(0, Message); })();
        }
    }
}