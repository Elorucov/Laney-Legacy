using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ChatPreviewCard : Modal
    {
        public ChatPreviewCard(ChatPreviewResponse resp)
        {
            this.InitializeComponent();
            Setup(resp.Preview, resp.Profiles, resp.Groups);
        }

        private void Setup(ChatPreview preview, List<User> profiles, List<Group> groups)
        {
            Uri avatar = preview.Photo != null ?
                preview.Photo.Photo200 :
                new Uri("https://vk.com/images/icons/im_multichat_200.png");

            ChatAvatar.Fill = new ImageBrush { ImageSource = new BitmapImage(avatar) };
            ChatTitle.Text = preview.Title;
            Subtitle.Text = Locale.Get(preview.IsGroupChannel ? "channel_invitation" : "chat_invitation");

            if (!preview.IsGroupChannel && (profiles.Count > 0 || groups.Count > 0))
            {
                MembersInfo.Visibility = Visibility.Visible;
                SetMembersNamesAndAvatars(preview, profiles, groups);
            }

            if (preview.LocalId > 0)
            {
                MainButton.Content = Locale.Get(preview.IsGroupChannel ? "channel_go" : "chat_go");
            }
            else
            {
                MainButton.Content = Locale.Get(preview.IsGroupChannel ? "channel_join" : "chat_join");
            }
        }

        private void SetMembersNamesAndAvatars(ChatPreview preview, List<User> profiles, List<Group> groups)
        {
            List<UserAvatarItem> avatars = new List<UserAvatarItem>();
            List<string> names = new List<string>();

            List<int> members = preview.Members;
            int c = Math.Min(3, preview.MembersCount);
            for (int i = 0; i < Math.Min(3, c); i++)
            {
                int id = members[i];
                if (id > 0 && profiles != null)
                {
                    User u = (from z in profiles where z.Id == id select z).FirstOrDefault();
                    if (u != null)
                    {
                        avatars.Add(new UserAvatarItem { Name = u.FullName, Image = new BitmapImage(u.Photo) });
                        names.Add(u.FirstName);
                    }
                }
                else if (id < 0 && groups != null)
                {
                    Group g = (from z in groups where z.Id == id * -1 select z).FirstOrDefault();
                    if (g != null)
                    {
                        avatars.Add(new UserAvatarItem { Name = g.Name, Image = new BitmapImage(g.Photo) });
                        names.Add(g.Name);
                    }
                }
            }

            MembersAvatars.Avatars = new System.Collections.ObjectModel.ObservableCollection<UserAvatarItem>(avatars);
            if (preview.MembersCount > 3) MembersAvatars.OverrideAvatarsCount = preview.MembersCount;

            string namespretty = "";

            for (int i = 0; i < c; i++)
            {
                namespretty += $"{names[i]}, ";
            }
            namespretty = namespretty.Substring(0, namespretty.Length - 2);
            if (preview.MembersCount > c)
            {
                int more = preview.MembersCount - c;
                namespretty += $" {Locale.Get("chatpreview_member_1")} {more} {Locale.GetDeclension(more, "chatpreview_member_2")}.";
            }
            else
            {
                namespretty += ".";
            }

            Members.Text = namespretty;
        }

        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            Hide(true);
        }
    }
}
