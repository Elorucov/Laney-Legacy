using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.VKAPIExecute.Objects;
using Elorucov.Toolkit.UWP.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ChatLinkViewer : Modal
    {
        int peerId = 0;

        public ChatLinkViewer(ChatInfoEx chatInfo)
        {
            this.InitializeComponent();
            peerId = chatInfo.PeerId;
            Title = Locale.Get(chatInfo.IsChannel ? "chatinfo_invite_link_channel" : "chatinfo_invite_link_chat");
            Link.Header = Locale.Get(chatInfo.IsChannel ? "chatlinkviewer_hint_channel" : "chatlinkviewer_hint_chat");
            CreateButton.Visibility = !chatInfo.ACL.CanChangeInviteLink ? Visibility.Collapsed : Visibility.Visible;
            Loader.Height = chatInfo.ACL.CanChangeInviteLink ? 202 : 164;

            GetLink();
        }

        private async void GetLink(bool revoke = false)
        {
            try
            {
                ChatLink link = await VKSession.Current.API.Messages.GetInviteLinkAsync(VKSession.Current.GroupId, peerId, revoke);
                Link.Text = link.Link;

                Loader.Visibility = Visibility.Collapsed;
                Content.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                {
                    GetLink(revoke);
                }
                else
                {
                    if (!revoke) Hide();
                }
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Loader.Visibility = Visibility.Visible;
            Content.Visibility = Visibility.Collapsed;
            GetLink(true);
        }

        private void CopyLink(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(Link.Text);
            Clipboard.SetContent(dataPackage);
            Hide();
        }
    }
}
