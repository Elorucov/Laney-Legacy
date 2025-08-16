using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class ExtendedAttachmentControl : UserControl {
        long ip = 0;
        long idp = 0;
        long tp = 0;
        long cp = 0;
        long dp = 0;
        long bp = 0;

        public ExtendedAttachmentControl() {
            this.InitializeComponent();
            Loaded += async (a, b) => {
                ip = RegisterPropertyChangedCallback(ImageProperty, async (c, d) => await SetImage((PhotoSizes)GetValue(d)));
                idp = RegisterPropertyChangedCallback(ImageDirectProperty, async (c, d) => await SetImage((Uri)GetValue(d)));
                tp = RegisterPropertyChangedCallback(TitleProperty, (c, d) => SetContents());
                cp = RegisterPropertyChangedCallback(CaptionProperty, (c, d) => SetContents());
                dp = RegisterPropertyChangedCallback(DescriptionProperty, (c, d) => SetDescription());
                bp = RegisterPropertyChangedCallback(ButtonProperty, (c, d) => SetContents());
                SetContents();
                SetDescription();
                await SetImage(Image);
                await SetImage(ImageDirect);
            };
            Unloaded += (a, b) => {
                UnregisterPropertyChangedCallback(ImageProperty, ip);
                UnregisterPropertyChangedCallback(ImageDirectProperty, idp);
                UnregisterPropertyChangedCallback(TitleProperty, tp);
                UnregisterPropertyChangedCallback(CaptionProperty, cp);
                UnregisterPropertyChangedCallback(DescriptionProperty, dp);
                UnregisterPropertyChangedCallback(ButtonProperty, bp);
            };
        }

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
                   "Image", typeof(PhotoSizes), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public PhotoSizes Image {
            get { return (PhotoSizes)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageDirectProperty = DependencyProperty.Register(
                   "ImageDirect", typeof(Uri), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public Uri ImageDirect {
            get { return (Uri)GetValue(ImageDirectProperty); }
            set { SetValue(ImageDirectProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
                   "Title", typeof(string), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public string Title {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
                   "Caption", typeof(string), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public string Caption {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
                   "Description", typeof(string), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public string Description {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty LinkProperty = DependencyProperty.Register(
                   "Link", typeof(Uri), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public Uri Link {
            get { return (Uri)GetValue(LinkProperty); }
            set { SetValue(LinkProperty, value); }
        }

        public static readonly DependencyProperty ButtonProperty = DependencyProperty.Register(
                   "Button", typeof(LinkButton), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public LinkButton Button {
            get { return (LinkButton)GetValue(ButtonProperty); }
            set { SetValue(ButtonProperty, value); }
        }

        public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register(
           "ButtonText", typeof(string), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public string ButtonText {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public event RoutedEventHandler ButtonClick;

        private void SetDescription() {
            ToolTipService.SetToolTip(this, String.Join("\n", new string[] { Title, Description, Caption }));
        }

        private async Task SetImage(PhotoSizes ps) {
            ImageContainer.Visibility = ps == null && ImageDirect == null ? Visibility.Collapsed : Visibility.Visible;
            if (ps == null) return;

            var img = new BitmapImage() {
                DecodePixelType = DecodePixelType.Logical
            };

            await img.SetUriSourceAsync(ps.Uri);
            Preview.Source = img;
        }

        private async Task SetImage(Uri uri) {
            ImageContainer.Visibility = uri == null && Image == null ? Visibility.Collapsed : Visibility.Visible;
            if (uri == null) return;

            var img = new BitmapImage() {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelWidth = Convert.ToInt32(ImageContainer.Width)
            };

            await img.SetUriSourceAsync(uri);
            Preview.Source = img;
        }

        private void SetContents() {
            LinkButton.Visibility = Button != null || !string.IsNullOrEmpty(ButtonText) ? Visibility.Visible : Visibility.Collapsed;
            bool buttonVisible = LinkButton.Visibility == Visibility.Visible;

            TitleString.MaxLines = buttonVisible ? 1 : 2;
            TitleString.Text = Title;
            DescString.MaxLines = buttonVisible ? 1 : 2;
            DescString.Text = !string.IsNullOrEmpty(Caption) ? Caption : string.Empty;
            DescString.Visibility = string.IsNullOrEmpty(Caption) ? Visibility.Collapsed : Visibility.Visible;

            if (Button != null) {
                LinkButton.Content = Button.Title;
            } else {
                LinkButton.Content = ButtonText;
            }
        }

        private void LaunchLink(object sender, RoutedEventArgs e) {
            if (Link != null) new System.Action(async () => { await VKLinks.LaunchLinkAsync(Link); })();
        }

        private void LaunchLinkFromButton(object sender, RoutedEventArgs e) {
            if (Button != null) {
                new System.Action(async () => { await VKLinks.LaunchLinkAsync(Button.Action.Uri); })();
            } else {
                ButtonClick?.Invoke(this, e);
            }
        }
    }
}
