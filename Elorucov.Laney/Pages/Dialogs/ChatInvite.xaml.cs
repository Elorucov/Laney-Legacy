using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class ChatInvite : Modal {
        private string chatLink;
        private ChatPreviewResponse previewResponse;
        private ChatPreview preview;

        public ChatInvite(ChatPreviewResponse cpr, string link) {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            CornersRadius = 18;
            chatLink = link;
            previewResponse = cpr;
        }

        private void GetChatPreview(object sender, RoutedEventArgs e) {
            preview = previewResponse.Preview;
            Log.Info($"{GetType().Name} > Chat preview: is channel: {preview.IsGroupChannel}, joined: {preview.Joined}, local id: {preview.LocalId}");

            Name.Text = preview.Title;

            new System.Action(async () => {
                BitmapImage cphoto = new BitmapImage() {
                    DecodePixelType = DecodePixelType.Logical,
                    DecodePixelHeight = 80
                };
                await cphoto.SetUriSourceAsync(preview.Photo != null ? preview.Photo.Photo200 : new Uri("https://vk.com/images/icons/im_multichat_200.png"));

                ChatPhoto.Fill = new ImageBrush {
                    ImageSource = cphoto
                };

                MainView.Visibility = Visibility.Visible;
                if (!preview.IsGroupChannel) {
                    Subtitle.Text = Locale.Get("chat_invitation");
                    JoinBtn.Content = Locale.Get("chat_join");
                    await SetMembersNamesAndAvatarsAsync(previewResponse);
                } else {
                    Subtitle.Text = Locale.Get("channel_invitation");
                    JoinBtn.Content = Locale.Get("channel_join");
                    MembersAvatars.Visibility = Visibility.Collapsed;
                    MembersNames.Text = $"{preview.MembersCount} {Locale.GetDeclension(preview.MembersCount, "chatinvitedlg_more")}";
                }
            })();

            if (preview.Button != null) {
                JoinBtn.Content = preview.Button.Title;
            }
        }

        private async Task SetMembersNamesAndAvatarsAsync(ChatPreviewResponse cpr) {
            ObservableCollection<UserAvatarItem> avatars = new ObservableCollection<UserAvatarItem>();
            List<string> names = new List<string>();

            List<long> members = cpr.Preview.Members;
            int c = Math.Min(3, cpr.Preview.MembersCount);
            for (int i = 0; i < Math.Min(3, c); i++) {
                long id = members[i];
                if (id.IsUser() && cpr.Profiles != null) {
                    User u = (from z in cpr.Profiles where z.Id == id select z).FirstOrDefault();
                    if (u != null) {
                        BitmapImage ava = new BitmapImage();
                        await ava.SetUriSourceAsync(u.Photo);
                        avatars.Add(new UserAvatarItem { Name = u.FullName, Image = ava });
                        names.Add(u.FirstName);
                    }
                } else if (id.IsGroup() && cpr.Groups != null) {
                    Group g = (from z in cpr.Groups where z.Id == id * -1 select z).FirstOrDefault();
                    if (g != null) {
                        BitmapImage ava = new BitmapImage();
                        await ava.SetUriSourceAsync(g.Photo);
                        avatars.Add(new UserAvatarItem { Name = g.Name, Image = ava });
                        names.Add(g.Name);
                    }
                }
            }

            MembersAvatars.Avatars = avatars;
            if (cpr.Preview.MembersCount > 3) MembersAvatars.OverrideAvatarsCount = cpr.Preview.MembersCount;

            string namespretty = "";

            for (int i = 0; i < c; i++) {
                namespretty += $"{names[i]}, ";
            }
            namespretty = namespretty.Substring(0, namespretty.Length - 2);
            if (preview.MembersCount > c) {
                int more = preview.MembersCount - c;
                namespretty += $" {Locale.Get("chatinvitedlg_and")} {more} {Locale.GetDeclension(more, "chatinvitedlg_more")}.";
            } else {
                namespretty += ".";
            }

            MembersNames.Text = namespretty;
        }

        private void Close(object sender, RoutedEventArgs e) {
            Hide();
        }

        private void JoinChat(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                if (preview.Button != null && preview.Button.Action.Uri != null) {
                    await Windows.System.Launcher.LaunchUriAsync(preview.Button.Action.Uri);
                    return;
                }

                Button button = sender as Button;
                button.IsEnabled = false;
                if (preview.LocalId > 0) {
                    Hide(2000000000 + preview.LocalId);
                } else {
                    object resp = await Messages.JoinChatByInviteLink(chatLink);
                    if (resp is long id) {
                        Hide(2000000000 + id);
                    } else {
                        button.IsEnabled = true;
                        Functions.ShowHandledErrorDialog(resp);
                    }
                }
            })();
        }
    }
}