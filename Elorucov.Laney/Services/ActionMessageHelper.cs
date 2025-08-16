using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services {
    public class ActionMessageHelper {
        public static TextBlock GenerateTextBlock(string text = null) {
            TextBlock txt = new TextBlock();
            txt.TextWrapping = TextWrapping.Wrap;
            txt.Foreground = new SolidColorBrush(Colors.Gray);
            txt.TextAlignment = TextAlignment.Center;
            txt.HorizontalAlignment = HorizontalAlignment.Center;

            Run r = new Run { Text = string.IsNullOrEmpty(text) ? string.Empty : text };
            txt.Inlines.Add(r);

            return txt;
        }

        static string hlxaml = "<Hyperlink xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Foreground=\"{ThemeResource ApplicationForegroundThemeBrush}\"/>";

        public static TextBlock GenerateTextBlock(string firstuser, long firstuserid, string act, string seconduser = null, long seconduserid = 0, bool isNonUserFrom = false, bool isNonUser = false, string suffix = null) {
            TextBlock txt = new TextBlock();
            txt.TextWrapping = TextWrapping.Wrap;
            txt.Foreground = new SolidColorBrush(Colors.Gray);
            txt.TextAlignment = TextAlignment.Center;
            txt.HorizontalAlignment = HorizontalAlignment.Center;

            Hyperlink hl1 = (Hyperlink)XamlReader.Load(hlxaml);
            hl1.Inlines.Add(new Run { Text = firstuser });
            hl1.FontWeight = FontWeights.SemiBold;
            hl1.UnderlineStyle = UnderlineStyle.None;
            hl1.Click += (a, b) => VKLinks.ShowPeerInfoModal(firstuserid);
            txt.Inlines.Add(hl1);

            Run r = new Run { Text = $" {act}" };
            txt.Inlines.Add(r);

            if (!string.IsNullOrEmpty(seconduser)) {
                Hyperlink hl2 = (Hyperlink)XamlReader.Load(hlxaml);
                hl2.Inlines.Add(new Run { Text = $" {seconduser}" });
                hl2.FontWeight = FontWeights.SemiBold;
                hl2.UnderlineStyle = UnderlineStyle.None;
                hl2.Click += (a, b) => VKLinks.ShowPeerInfoModal(seconduserid);
                txt.Inlines.Add(hl2);
            }

            if (!string.IsNullOrWhiteSpace(suffix)) {
                Run rs = new Run { Text = $" {suffix}" };
                txt.Inlines.Add(rs);
            }

            return txt;
        }

        public static TextBlock GenerateTextBlockForPinnedMessage(string firstuser, long firstuserid, string act, VkAPI.Objects.Action actMessage, string message, bool isNonUserFrom = false, bool isNonUser = false) {
            TextBlock txt = new TextBlock();
            txt.TextWrapping = TextWrapping.Wrap;
            txt.Foreground = new SolidColorBrush(Colors.Gray);
            txt.TextAlignment = TextAlignment.Center;
            txt.HorizontalAlignment = HorizontalAlignment.Center;

            Hyperlink hl1 = (Hyperlink)XamlReader.Load(hlxaml);
            hl1.Inlines.Add(new Run { Text = firstuser });
            hl1.FontWeight = FontWeights.SemiBold;
            hl1.UnderlineStyle = UnderlineStyle.None;
            hl1.Click += (a, b) => {
                if (!isNonUserFrom) {
                    VKLinks.ShowPeerInfoModal(firstuserid);
                } else {
                    VKLinks.ShowPeerInfoModal(firstuserid * -1);
                }
            };
            txt.Inlines.Add(hl1);

            Run r = new Run { Text = $" {act} " };
            txt.Inlines.Add(r);

            Hyperlink hl2 = (Hyperlink)XamlReader.Load(hlxaml);
            hl2.Inlines.Add(new Run { Text = message });
            hl2.FontWeight = FontWeights.SemiBold;
            hl2.UnderlineStyle = UnderlineStyle.None;
            hl2.Click += (a, b) => {
                AppSession.CurrentConversationVM.GoToMessage(actMessage.ConversationMessageId);
            };
            txt.Inlines.Add(hl2);

            return txt;
        }

        public static PhotoVideoThumbnail GetConversationAvatarThumbnail(Photo p) {
            double w = 160;
            double h = w / p.PreviewImageSize.Width * p.PreviewImageSize.Height;

            GalleryItem item = new GalleryItem(p);

            PhotoVideoThumbnail t = new PhotoVideoThumbnail();
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.Margin = new Thickness(0, 8, 0, 0);
            t.Width = w;
            t.Height = h;
            t.Preview = p;
            t.Click += (a, b) => {
                if (ViewManagement.GetWindowType() == WindowType.Main)
                    Pages.PhotoViewer.Show(new Tuple<List<GalleryItem>, GalleryItem>(new List<GalleryItem> { item }, item));
            };
            return t;
        }
    }
}