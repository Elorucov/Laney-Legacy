using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class StoryUC : UserControl {

        long id = 0;
        public StoryUC() {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(StoryProperty, ChangedCallback);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(StoryProperty, id); };
        }

        public event RoutedEventHandler Click;

        public static readonly DependencyProperty StoryProperty = DependencyProperty.Register(
                   "Story", typeof(Story), typeof(StoryUC), new PropertyMetadata(default(object)));

        public Story Story {
            get { return (Story)GetValue(StoryProperty); }
            set { SetValue(StoryProperty, value); }
        }

        private void ChangedCallback(DependencyObject sender, DependencyProperty dp) {
            if ((Story)GetValue(dp) != null) {
                new System.Action(async () => { await SetStoryAsync((Story)GetValue(dp)); })();
            }
        }

        private async Task SetStoryAsync(Story story) {
            if (story.IsDeleted) {
                StoryRestriction.Visibility = Visibility.Visible;
                StoryRestrictionText.Text = Locale.Get("story_restriction_deleted");
                return;
            }

            StoryStatus.Text = story.OwnerId.IsUser() ? Locale.Get("story_from_user") : Locale.Get("story_from_group");

            if (story.OwnerId.IsUser()) {
                User u = AppSession.GetCachedUser(story.OwnerId);
                if (u != null) StoryAuthor.Text = $"{u.FirstNameGen} {u.LastNameGen}";
            } else if (story.OwnerId.IsGroup()) {
                Group g = AppSession.GetCachedGroup(story.OwnerId * -1);
                if (g != null) StoryAuthor.Text = $"«{g.Name}»";
            }

            if (story.IsExpired) {
                StoryRestriction.Visibility = Visibility.Visible;
                StoryRestrictionText.Text = Locale.Get("story_restriction_expired");
                return;
            }

            if (!story.CanSee) {
                StoryRestriction.Visibility = Visibility.Visible;
                StoryRestrictionText.Text = Locale.Get("story_restriction_private");
                return;
            }

            Uri preview = null;
            switch (story.Type) {
                case StoryType.Photo: preview = story.Photo.PreviewImageUri; break;
                case StoryType.Video: preview = story.Video.FirstFrameForStory.Uri; break;
            }

            if (preview == null) return;

            BitmapImage image = new BitmapImage(preview) {
                DecodePixelWidth = Convert.ToInt32(Root.Width),
                DecodePixelType = DecodePixelType.Logical
            };
            await image.SetUriSourceAsync(preview);
            ImageContainer.Background = new ImageBrush {
                ImageSource = image
            };
        }

        private void HandleClick(object sender, RoutedEventArgs e) {
            Click?.Invoke(this, e);
        }
    }
}