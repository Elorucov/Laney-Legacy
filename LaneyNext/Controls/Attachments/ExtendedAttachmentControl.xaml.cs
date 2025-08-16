using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Attachments
{
    public sealed partial class ExtendedAttachmentControl : UserControl
    {
        long ip = 0;
        long tp = 0;
        long cp = 0;
        long dp = 0;
        long bp = 0;

        public ExtendedAttachmentControl()
        {
            this.InitializeComponent();
            Loaded += (a, b) =>
            {
                ip = RegisterPropertyChangedCallback(ImageProperty, (c, d) => SetImage((PhotoSizes)GetValue(d)));
                tp = RegisterPropertyChangedCallback(TitleProperty, (c, d) => SetContents());
                cp = RegisterPropertyChangedCallback(CaptionProperty, (c, d) => SetContents());
                dp = RegisterPropertyChangedCallback(DescriptionProperty, (c, d) => SetDescription(GetValue(d).ToString()));
                bp = RegisterPropertyChangedCallback(ButtonProperty, (c, d) => SetContents());
                SetImage(Image);
                SetContents();
                SetDescription(Description);
            };
            Unloaded += (a, b) =>
            {
                UnregisterPropertyChangedCallback(ImageProperty, ip);
                UnregisterPropertyChangedCallback(TitleProperty, tp);
                UnregisterPropertyChangedCallback(DescriptionProperty, dp);
                UnregisterPropertyChangedCallback(ButtonProperty, bp);
            };
        }

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
                   "Image", typeof(PhotoSizes), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public PhotoSizes Image
        {
            get { return (PhotoSizes)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
                   "Title", typeof(string), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
                   "Caption", typeof(string), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
                   "Description", typeof(string), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty LinkProperty = DependencyProperty.Register(
                   "Link", typeof(Uri), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public Uri Link
        {
            get { return (Uri)GetValue(LinkProperty); }
            set { SetValue(LinkProperty, value); }
        }

        public static readonly DependencyProperty ButtonProperty = DependencyProperty.Register(
                   "Button", typeof(LinkButton), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public LinkButton Button
        {
            get { return (LinkButton)GetValue(ButtonProperty); }
            set { SetValue(ButtonProperty, value); }
        }

        public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register(
                "ButtonText", typeof(string), typeof(ExtendedAttachmentControl), new PropertyMetadata(default(object)));

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public event RoutedEventHandler ButtonClick;

        private void SetDescription(string desc)
        {
            ToolTipService.SetToolTip(this, !String.IsNullOrEmpty(desc) ? desc : null);
        }

        private void SetImage(PhotoSizes ps)
        {
            if (ps == null)
            {
                ImageContainer.Visibility = Visibility.Collapsed;
                return;
            }

            double w = ps.Width;
            double h = ps.Height;
            if (w > h)
            {
                Preview.DecodePixelHeight = 80;
                Preview.DecodePixelWidth = (int)(80 / h * w);
            }
            else if (w < h)
            {
                Preview.DecodePixelWidth = 80;
                Preview.DecodePixelHeight = (int)(80 / w * h);
            }
            else
            {
                Preview.DecodePixelWidth = 80;
                Preview.DecodePixelHeight = 80;
            }
            ImageContainer.Visibility = Visibility.Visible;
            Preview.UriSource = ps.Uri;
        }

        private void SetContents()
        {
            LinkButton.Visibility = Button != null || !String.IsNullOrEmpty(ButtonText) ? Visibility.Visible : Visibility.Collapsed;
            bool buttonVisible = LinkButton.Visibility == Visibility.Visible;

            TitleString.MaxLines = buttonVisible ? 1 : 2;
            TitleString.Text = Title;
            DescString.MaxLines = buttonVisible ? 1 : 2;
            DescString.Text = !String.IsNullOrEmpty(Caption) ? Caption : String.Empty;
            DescString.Visibility = String.IsNullOrEmpty(Caption) ? Visibility.Collapsed : Visibility.Visible;

            if (Button != null)
            {
                LinkButton.Content = Button.Title;
            }
            else
            {
                LinkButton.Content = ButtonText;
            }
        }

        private async void LaunchLink(object sender, RoutedEventArgs e)
        {
            if (Link != null) await Router.LaunchLinkAsync(Link);
        }

        private async void LaunchLinkFromButton(object sender, RoutedEventArgs e)
        {
            if (Button != null)
            {
                await Router.LaunchLinkAsync(Button.Action.Uri);
            }
            else
            {
                ButtonClick?.Invoke(this, e);
            }
        }
    }
}