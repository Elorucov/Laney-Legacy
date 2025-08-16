using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.Attachments
{
    public sealed partial class StoryControl : UserControl
    {
        long id = 0;
        public StoryControl()
        {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(StoryProperty, ChangedCallback);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(StoryProperty, id); };
        }

        public event RoutedEventHandler Click;

        public static readonly DependencyProperty StoryProperty = DependencyProperty.Register(
                   "Story", typeof(Story), typeof(StoryControl), new PropertyMetadata(default(object)));

        public Story Story
        {
            get { return (Story)GetValue(StoryProperty); }
            set { SetValue(StoryProperty, value); }
        }

        private void ChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if ((Story)GetValue(dp) != null)
            {
                SetStory((Story)GetValue(dp));
            }
        }

        private void SetStory(Story story)
        {
            if (story.IsDeleted)
            {
                StoryRestriction.Visibility = Visibility.Visible;
                StoryRestrictionText.Text = Locale.Get("story_restriction_deleted");
                return;
            }

            StoryStatus.Text = story.OwnerId > 0 ? Locale.Get("story_from_user") : Locale.Get("story_from_group");

            if (story.OwnerId > 0)
            {
                User u = CacheManager.GetUser(story.OwnerId);
                if (u != null) StoryAuthor.Text = $"{u.FirstNameGen} {u.LastNameGen}";
            }
            else if (story.OwnerId < 0)
            {
                Group g = CacheManager.GetGroup(story.OwnerId);
                if (g != null) StoryAuthor.Text = $"«{g.Name}»";
            }

            if (story.IsExpired)
            {
                StoryRestriction.Visibility = Visibility.Visible;
                StoryRestrictionText.Text = Locale.Get("story_restriction_expired");
                return;
            }

            if (story.CanSee == 0)
            {
                StoryRestriction.Visibility = Visibility.Visible;
                StoryRestrictionText.Text = Locale.Get("story_restriction_private");
                return;
            }

            Uri preview = null;
            switch (story.Type)
            {
                case StoryType.Photo: preview = story.Photo.PreviewImageUri; break;
                case StoryType.Video: preview = story.Video.FirstFrameForStory.Uri; break;
            }

            if (preview == null) return;
            ImageContainer.Background = new ImageBrush
            {
                ImageSource = new BitmapImage(preview)
                {
                    DecodePixelType = DecodePixelType.Logical
                },
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
        }

        private void HandleClick(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
    }
}
