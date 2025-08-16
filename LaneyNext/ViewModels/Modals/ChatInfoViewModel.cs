using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Views.Modals;
using Elorucov.Laney.VKAPIExecute.Objects;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VK.VKUI;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using Windows.UI.Xaml;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class ChatInfoViewModel : CommonViewModel
    {
        private ChatInfoEx _chat;
        private string _subtitle;
        private bool _chatControlButtonsVisible;
        private ObservableCollection<Entity> _members = new ObservableCollection<Entity>();
        private RelayCommand _addMemberCommand;
        private RelayCommand _leaveCommand;
        private RelayCommand _renameCommand;
        private RelayCommand _changeAvatarCommand;
        private RelayCommand _showChatLinkCommand;
        private RelayCommand _editChatPermissionsCommand;

        public ChatInfoEx Chat { get { return _chat; } set { _chat = value; OnPropertyChanged(); } }
        public string Subtitle { get { return _subtitle; } set { _subtitle = value; OnPropertyChanged(); } }
        public bool ChatControlButtonsVisible { get { return _chatControlButtonsVisible; } set { _chatControlButtonsVisible = value; OnPropertyChanged(); } }
        public ObservableCollection<Entity> Members { get { return _members; } set { _members = value; OnPropertyChanged(); } }
        public RelayCommand AddMemberCommand { get { return _addMemberCommand; } set { _addMemberCommand = value; OnPropertyChanged(); } }
        public RelayCommand LeaveCommand { get { return _leaveCommand; } set { _leaveCommand = value; OnPropertyChanged(); } }
        public RelayCommand RenameCommand { get { return _renameCommand; } set { _renameCommand = value; OnPropertyChanged(); } }
        public RelayCommand ChangeAvatarCommand { get { return _changeAvatarCommand; } set { _changeAvatarCommand = value; OnPropertyChanged(); } }
        public RelayCommand ShowChatLinkCommand { get { return _showChatLinkCommand; } set { _showChatLinkCommand = value; OnPropertyChanged(); } }
        public RelayCommand EditChatPermissionsCommand { get { return _editChatPermissionsCommand; } set { _editChatPermissionsCommand = value; OnPropertyChanged(); } }

        Modal OwnerModal;
        List<ChatMember> OriginalMembers = new List<ChatMember>();

        public ChatInfoViewModel(Modal ownerModal, int chatId)
        {
            OwnerModal = ownerModal;

            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(Chat): SetInfos(); break;
                }
            };

            AddMemberCommand = new RelayCommand(o =>
            {
                List<int> membersIds = (from i in Members select i.Id).ToList();
                InviteFriendToChat modal = new InviteFriendToChat(Chat.ChatId, membersIds);
                modal.Closed += (a, b) =>
                {
                    if (b is bool hide && hide) OwnerModal.Hide();
                };
                modal.Show();
            });

            ShowChatLinkCommand = new RelayCommand(o =>
            {
                ChatLinkViewer clv = new ChatLinkViewer(Chat);
                clv.Show();
            });

            EditChatPermissionsCommand = new RelayCommand(o =>
            {
                ChatPermissionsEditor cpe = new ChatPermissionsEditor(Chat.ChatId, Chat.Permissions);
                cpe.Closed += (a, b) =>
                {
                    Chat.Permissions = (cpe.DataContext as ChatPermissionsEditorViewModel).ChatPermissions;
                };
                cpe.Show();
            });

            LeaveCommand = new RelayCommand(async o => await APIHelper.LeaveFromChatAsync(Chat.ChatId, Chat.IsChannel));

            GetChatInfo(chatId);
        }

        private async void GetChatInfo(int chatId)
        {
            try
            {
                Placeholder = null;
                IsLoading = true;
                Chat = await VKSession.Current.Execute.GetChatInfoAsync(chatId, APIHelper.Fields);
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => GetChatInfo(chatId));
            }
            IsLoading = false;
        }

        private void SetInfos()
        {
            if (Chat == null) return;
            switch (Chat.State)
            {
                case UserStateInChat.In:
                    Subtitle = Chat.OnlineCount > 0 ?
                        String.Format(Locale.GetDeclensionForFormat(Chat.MembersCount, "chatinfo_subtitle_online"), Chat.MembersCount, Chat.OnlineCount) :
                        String.Format(Locale.GetDeclensionForFormat(Chat.MembersCount, "chatinfo_subtitle"), Chat.MembersCount);
                    break;
                case UserStateInChat.Kicked:
                    Subtitle = Locale.Get("chat_kicked");
                    break;
                case UserStateInChat.Left:
                    Subtitle = Locale.Get("chat_left");
                    break;
            }
            ChatControlButtonsVisible = Chat.State == UserStateInChat.In;

            Members.Clear();
            if (Chat.Members != null && !Chat.IsChannel)
            {
                CacheManager.Add(Chat.Members.Profiles);
                CacheManager.Add(Chat.Members.Groups);

                foreach (ChatMember m in Chat.Members.Items)
                {
                    Entity e = GetEntityForMember(m);
                    Members.Add(e);
                }
            }
        }

        private Entity GetEntityForMember(ChatMember m)
        {
            Entity e = new Entity();
            e.Id = m.MemberId;
            if (m.MemberId > 0)
            {
                User u = CacheManager.GetUser(m.MemberId);
                e.Title = u.FullName;
                e.Image = u.Photo;
            }
            else if (m.MemberId < 0)
            {
                ELOR.VKAPILib.Objects.Group g = CacheManager.GetGroup(m.MemberId);
                e.Title = g.Name;
                e.Image = g.Photo;
            }

            e.Subtitle = String.Format(Locale.Get("chatinfo_invited_by", APIHelper.GetSexFromId(m.InvitedBy)), APIHelper.GetNameFromId(m.InvitedBy));
            if (m.IsAdmin) e.Subtitle = Locale.Get("chatinfo_admin");
            if (m.MemberId == Chat.OwnerId) e.Subtitle = Locale.Get("chatinfo_owner");

            if (m.MemberId != Chat.OwnerId && m.CanKick)
            {
                e.ExtraButtonIcon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28MoreHorizontal);
                e.ExtraButtonCommand = new RelayCommand(a => ShowMemberContextMenu(m, e, (FrameworkElement)a));
            }
            return e;
        }

        private void ShowMemberContextMenu(ChatMember cmb, Entity e, FrameworkElement el)
        {
            var a = OriginalMembers.Where(z => z.IsAdmin && z.MemberId == VKSession.Current.SessionId);
            bool canChangeAdmin = Chat.Permissions != null;
            if (canChangeAdmin) canChangeAdmin = Chat.Permissions.ChangeAdmins == ChatSettingsChangers.Owner ||
                (Chat.Permissions.ChangeAdmins == ChatSettingsChangers.OwnerAndAdmins && a.Count() > 0);

            MenuFlyout mf = new MenuFlyout();
            if (canChangeAdmin)
            {
                CellButton amfi = new CellButton
                {
                    Text = cmb.IsAdmin ? Locale.Get("chatifno_memctx_remadmin") : Locale.Get("chatifno_memctx_addadmin"),
                    Icon = cmb.IsAdmin ? VKIconName.Icon28RemoveCircleOutline : VKIconName.Icon28AddCircleOutline
                };
                amfi.Click += (c, d) =>
                {
                    SetMemberRole(cmb, e);
                };
                mf.Items.Add(amfi);

                if (cmb.CanKick)
                {
                    CellButton dmfi = new CellButton { Text = Locale.Get("chatifno_memctx_kick"), Icon = VKIconName.Icon28CancelOutline };
                    dmfi.Click += (c, d) => ConfirmRemovingMember(cmb, e);
                    mf.Items.Add(dmfi);
                }

                if (mf.Items.Count > 0) mf.ShowAt(el);
            }
        }

        private async void SetMemberRole(ChatMember cmb, Entity e)
        {
            try
            {
                await VKSession.Current.API.Messages.SetMemberRoleAsync(VKSession.Current.GroupId, 2000000000 + Chat.ChatId, cmb.MemberId, cmb.IsAdmin ? "member" : "admin");
                cmb.IsAdmin = !cmb.IsAdmin;
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) SetMemberRole(cmb, e);
            }
        }

        private async void ConfirmRemovingMember(ChatMember cmb, Entity e)
        {
            int memberId = cmb.MemberId;
            string name = String.Empty;
            if (memberId > 0)
            {
                User u = CacheManager.GetUser(memberId);
                name = u.FirstNameAcc;
            }
            else if (memberId < 0)
            {
                ELOR.VKAPILib.Objects.Group g = CacheManager.GetGroup(memberId);
                name = $"\"{g.Name}\"";
            }

            Alert alert = new Alert
            {
                Header = Locale.Get("chatifno_memctx_kick"),
                Text = String.Format(Locale.GetForFormat("chatinfo_kick_confirm"), name),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary) RemoveMember(cmb, e);
        }

        private async void RemoveMember(ChatMember cmb, Entity e)
        {
            try
            {
                await VKSession.Current.API.Messages.RemoveChatUserAsync(Chat.ChatId, cmb.MemberId);
                OriginalMembers.Remove(cmb);
                Members.Remove(e);
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) RemoveMember(cmb, e);
            }
        }
    }
}