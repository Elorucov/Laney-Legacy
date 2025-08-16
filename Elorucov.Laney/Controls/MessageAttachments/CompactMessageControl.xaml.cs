using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class CompactMessageControl : UserControl {
        public CompactMessageControl() {
            this.InitializeComponent();
            long mpId = RegisterPropertyChangedCallback(MessageProperty, async (a, b) => await RenderMessageAsync((LMessage)a.GetValue(b)));
            long pdId = RegisterPropertyChangedCallback(PaddingProperty, (a, b) => ChangePadding((Thickness)a.GetValue(b)));
            long bdId = RegisterPropertyChangedCallback(BorderThicknessProperty, (a, b) => ChangeLeftStripWidth((Thickness)a.GetValue(b)));
            long hfId = RegisterPropertyChangedCallback(UseHeaderForegroundColorProperty, (a, b) => ChangeSenderNameColor((bool)a.GetValue(b)));

            Unloaded += (a, b) => {
                if (mpId != 0) UnregisterPropertyChangedCallback(MessageProperty, mpId);
                if (pdId != 0) UnregisterPropertyChangedCallback(PaddingProperty, pdId);
                if (bdId != 0) UnregisterPropertyChangedCallback(BorderThicknessProperty, bdId);
                if (hfId != 0) UnregisterPropertyChangedCallback(UseHeaderForegroundColorProperty, bdId);
            };
            Loaded += async (a, b) => {
                ChangeSenderNameColor(UseHeaderForegroundColor);
                await RenderMessageAsync(Message);
            };
            Image.ImageOpened += (a, b) => ImageContainer.Visibility = Visibility.Visible;
            Image.ImageFailed += (a, b) => ImageContainer.Visibility = Visibility.Collapsed;
        }

        public static DependencyProperty MessageProperty = DependencyProperty.Register(nameof(Message), typeof(LMessage), typeof(CompactMessageControl), new PropertyMetadata(null));
        public LMessage Message {
            get { return (LMessage)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static DependencyProperty UseHeaderForegroundColorProperty = DependencyProperty.Register(nameof(UseHeaderForegroundColor), typeof(bool), typeof(CompactMessageControl), new PropertyMetadata(false));
        public bool UseHeaderForegroundColor {
            get { return (bool)GetValue(UseHeaderForegroundColorProperty); }
            set { SetValue(UseHeaderForegroundColorProperty, value); }
        }


        private void ChangeSenderNameColor(bool useHeader) {
            if (useHeader) {
                Sender.Style = (Style)Resources["ConvHeaderStyle"];
            } else {
                Sender.Style = (Style)Resources["AltAccentStyle"];
            }
        }

        private void ChangePadding(Thickness m) {
            ContentGrid.Margin = new Thickness(m.Left + BorderThickness.Left, m.Top, m.Right, m.Bottom);
        }

        private void ChangeLeftStripWidth(Thickness t) {
            LeftStrip.Width = t.Left;
            LeftStrip.RadiusX = t.Left / 2;
            LeftStrip.RadiusY = t.Left / 2;

            var m = ContentGrid.Margin;
            ContentGrid.Margin = new Thickness(m.Left + t.Left, m.Top, m.Right, m.Bottom);
        }

        private async Task RenderMessageAsync(LMessage msg) {
            if (msg == null) {
                ImageContainer.Visibility = Visibility.Collapsed;
                return;
            }
            string msgs = msg.ToString();
            Sender.Text = msg.SenderName;
            if (!string.IsNullOrEmpty(msgs)) MessageText.Text = msgs;

            Uri uri = null;
            Size size = new Size(128, 128);
            foreach (Attachment a in msg.Attachments) {
                if (a.Type == AttachmentType.Photo) {
                    uri = a.Photo.MinimalSizedPhoto.Uri;
                    size = a.Photo.MinimalSizedPhoto.Size;
                } else if (a.Type == AttachmentType.Video) {
                    uri = a.Video.PreviewImageUri;
                    size = a.Video.PreviewImageSize;
                } else if (a.Type == AttachmentType.Document && a.Document.Preview != null) {
                    uri = a.Document.Preview.Photo.MinimalSizedPhoto.Uri;
                    size = a.Document.Preview.Photo.MinimalSizedPhoto.Size;
                } else if (a.Type == AttachmentType.Sticker) {
                    uri = APIHelper.GetStickerUri(a.Sticker, 36);
                } else if (a.Type == AttachmentType.UGCSticker) {
                    uri = APIHelper.GetStickerUri(a.UGCSticker, 36);
                }
            }

            if (uri != null) {
                ImageContainer.Visibility = Visibility.Visible;
                double w = size.Width;
                double h = size.Height;
                if (w > h) {
                    Image.DecodePixelHeight = 36;
                    Image.DecodePixelWidth = (int)(36 / h * w);
                } else if (w < h) {
                    Image.DecodePixelWidth = 36;
                    Image.DecodePixelHeight = (int)(36 / w * h);
                } else {
                    Image.DecodePixelWidth = 36;
                    Image.DecodePixelHeight = 36;
                }
                await Image.SetUriSourceAsync(uri);
            } else {
                ImageContainer.Visibility = Visibility.Collapsed;
            }
        }
    }
}
