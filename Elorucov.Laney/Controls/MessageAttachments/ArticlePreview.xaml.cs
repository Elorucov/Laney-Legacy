using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using VK.VKUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class ArticlePreview : UserControl {
        long ap = 0;

        public ArticlePreview() {
            this.InitializeComponent();
            Loaded += async (a, b) => {
                ap = RegisterPropertyChangedCallback(ArticleProperty, async (c, d) => await SetUpAsync((Article)GetValue(d)));
                await SetUpAsync(Article);
            };
            Unloaded += (a, b) => {
                UnregisterPropertyChangedCallback(ArticleProperty, ap);
            };
        }

        public static readonly DependencyProperty ArticleProperty = DependencyProperty.Register(
                   nameof(Article), typeof(Article), typeof(ArticlePreview), new PropertyMetadata(default(object)));

        public Article Article {
            get { return (Article)GetValue(ArticleProperty); }
            set { SetValue(ArticleProperty, value); }
        }

        private async Task SetUpAsync(Article article) {
            if (article == null) return;
            if (article.State != "available") {
                FindName(nameof(UnavailablePreview));
                switch (article.State) {
                    case "deleted":
                        ReasonIcon.Id = VKIconName.Icon56DeleteOutlineIos;
                        Reason.Text = Locale.Get("article_deleted");
                        break;
                    case "paid":
                        ReasonIcon.Id = VKIconName.Icon56LockOutline;
                        Reason.Text = Locale.Get("article_paid");
                        break;
                    case "protected":
                        ReasonIcon.Id = VKIconName.Icon56LockOutline;
                        Reason.Text = Locale.Get("article_protected");
                        break;
                    default:
                        ReasonIcon.Id = VKIconName.Icon28BlockOutline;
                        Reason.Text = Locale.Get("article_unavailable");
                        break;
                }
            } else {
                FindName(nameof(AvailablePreview));
                Title.Text = article.Title;
                ToolTipService.SetToolTip(this, article.Subtitle);

                if (article.Photo != null) {
                    var img = new BitmapImage() {
                        DecodePixelType = DecodePixelType.Logical,
                        DecodePixelHeight = 170,
                    };

                    await img.SetUriSourceAsync(article.Photo.PreviewImageUri);
                    AvailablePreview.Background = new ImageBrush {
                        ImageSource = img,
                        Stretch = Stretch.UniformToFill,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center
                    };
                }
            }
        }

        private void Open(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await VKLinks.LaunchLinkAsync(new Uri(Article.ViewUrl)); })();
        }
    }
}