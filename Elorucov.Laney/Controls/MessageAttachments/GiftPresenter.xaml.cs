using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class GiftPresenter : UserControl {
        long id = 0;
        public GiftPresenter() {
            this.InitializeComponent();
            Loaded += (a, b) => { GiftImageContainer.Height = Width; };
            id = RegisterPropertyChangedCallback(GiftProperty, ChangedCallback);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(GiftProperty, id); };
        }

        public static readonly DependencyProperty GiftProperty = DependencyProperty.Register(
                   "Gift", typeof(Gift), typeof(GiftPresenter), new PropertyMetadata(default(object)));

        public Gift Gift {
            get { return (Gift)GetValue(GiftProperty); }
            set { SetValue(GiftProperty, value); }
        }

        private void ChangedCallback(DependencyObject sender, DependencyProperty dp) {
            new System.Action(async () => { await DisplayGiftAsync((Gift)GetValue(dp)); })();

        }

        private async Task DisplayGiftAsync(Gift gift) {
            await img.SetUriSourceAsync(gift.ThumbUri);
            ToolTipService.SetToolTip(this, $"ID: {gift.Id}");
        }
    }
}