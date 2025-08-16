using Elorucov.Laney.Services.Common;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Objects;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class WallPostModal : Modal {
        public WallPostModal(WallPost post) {
            this.InitializeComponent();
            Title = Locale.Get("wallpost");
            double width = Window.Current.Bounds.Width > MaxWidth ? MaxWidth : Window.Current.Bounds.Width;
            PostView.MaybeActualWidth = width - 24;
            PostView.Post = post;
        }
    }
}