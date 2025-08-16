using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Attachments
{
    public sealed partial class CompactMessageControl : UserControl
    {
        public CompactMessageControl()
        {
            this.InitializeComponent();
            long mpId = RegisterPropertyChangedCallback(MessageProperty, (a, b) => RenderMessage((MessageViewModel)a.GetValue(MessageProperty)));

            Unloaded += (a, b) =>
            {
                if (mpId != 0) UnregisterPropertyChangedCallback(MessageProperty, mpId);
            };
            Loaded += (a, b) =>
            {
                RenderMessage(Message);
            };
        }

        public static DependencyProperty MessageProperty = DependencyProperty.Register(nameof(Message), typeof(MessageViewModel), typeof(CompactMessageControl), new PropertyMetadata(null));
        public MessageViewModel Message
        {
            get { return (MessageViewModel)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        private void RenderMessage(MessageViewModel msg)
        {
            if (msg == null) return;
            Sender.Text = msg.SenderName;
            MessageText.Text = msg.ToNormalString();

            Uri uri = null;
            Size size = new Size(128, 128);
            foreach (Attachment a in msg.Attachments)
            {
                if (a.Type == AttachmentType.Photo)
                {
                    uri = a.Photo.MinimalSizedPhoto.Uri;
                    size = a.Photo.MinimalSizedPhoto.Size.ToWinSize();
                }
                else if (a.Type == AttachmentType.Video)
                {
                    uri = a.Video.PreviewImageUri;
                    size = a.Video.PreviewImageSize.ToWinSize();
                }
                else if (a.Type == AttachmentType.Document && a.Document.Preview != null)
                {
                    uri = a.Document.Preview.Photo.MinimalSizedPhoto.Uri;
                    size = a.Document.Preview.Photo.MinimalSizedPhoto.Size.ToWinSize();
                }
                else if (a.Type == AttachmentType.Sticker)
                {
                    uri = a.Sticker.Images[0].Uri;
                }
            }

            if (uri != null)
            {
                double w = size.Width;
                double h = size.Height;
                if (w > h)
                {
                    Image.DecodePixelHeight = 36;
                    Image.DecodePixelWidth = (int)(36 / h * w);
                }
                else if (w < h)
                {
                    Image.DecodePixelWidth = 36;
                    Image.DecodePixelHeight = (int)(36 / w * h);
                }
                else
                {
                    Image.DecodePixelWidth = 36;
                    Image.DecodePixelHeight = 36;
                }
                Image.UriSource = uri;
                ImageContainer.Visibility = Visibility.Visible;
            }
            else
            {
                ImageContainer.Visibility = Visibility.Collapsed;
            }
        }
    }
}
