using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.ObjectModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class ChatPermissionsEditorViewModel : CommonViewModel
    {
        private ObservableCollection<ChatPermissionItem> _permissions = new ObservableCollection<ChatPermissionItem>();
        private RelayCommand _valueChangedCommand;

        public ObservableCollection<ChatPermissionItem> Permissions { get { return _permissions; } set { _permissions = value; OnPropertyChanged(nameof(Permissions)); } }
        public RelayCommand ValueChangedCommand { get { return _valueChangedCommand; } set { _valueChangedCommand = value; OnPropertyChanged(); } }

        private int ChatId = 0;
        public ChatPermissions ChatPermissions { get; private set; }

        public ChatPermissionsEditorViewModel(int chatId, ChatPermissions permissions)
        {
            ChatId = chatId;
            ChatPermissions = permissions;

            Permissions.Add(new ChatPermissionItem("invite")
            {
                SettingIndex = GetIndexFromSetting(permissions.Invite),
                IconPath = new Uri("ms-appx:///Assets/ChatSettingsIcons/invite.svg"),
                IconBackground = new SolidColorBrush(Color.FromArgb(255, 75, 179, 75))
            });
            Permissions.Add(new ChatPermissionItem("change_info")
            {
                SettingIndex = GetIndexFromSetting(permissions.ChangeInfo),
                IconPath = new Uri("ms-appx:///Assets/ChatSettingsIcons/change_info.svg"),
                IconBackground = new SolidColorBrush(Color.FromArgb(255, 92, 156, 230))
            });
            Permissions.Add(new ChatPermissionItem("change_pin")
            {
                SettingIndex = GetIndexFromSetting(permissions.ChangePin),
                IconPath = new Uri("ms-appx:///Assets/ChatSettingsIcons/pin.svg"),
                IconBackground = new SolidColorBrush(Color.FromArgb(255, 92, 156, 230))
            });
            Permissions.Add(new ChatPermissionItem("use_mass_mentions")
            {
                SettingIndex = GetIndexFromSetting(permissions.UseMassMentions),
                IconPath = new Uri("ms-appx:///Assets/ChatSettingsIcons/mass_mention.svg"),
                IconBackground = new SolidColorBrush(Color.FromArgb(255, 92, 156, 230))
            });
            Permissions.Add(new ChatPermissionItem("see_invite_link")
            {
                SettingIndex = GetIndexFromSetting(permissions.SeeInviteLink),
                IconPath = new Uri("ms-appx:///Assets/ChatSettingsIcons/see_invite_link.svg"),
                IconBackground = new SolidColorBrush(Color.FromArgb(255, 92, 156, 230))
            });
            Permissions.Add(new ChatPermissionItem("call")
            {
                SettingIndex = GetIndexFromSetting(permissions.Call),
                IconPath = new Uri("ms-appx:///Assets/ChatSettingsIcons/call.svg"),
                IconBackground = new SolidColorBrush(Color.FromArgb(255, 75, 179, 75))
            });
            Permissions.Add(new ChatPermissionItem("change_admins")
            {
                SettingIndex = GetIndexFromSetting(permissions.ChangeAdmins),
                IconPath = new Uri("ms-appx:///Assets/ChatSettingsIcons/admin.svg"),
                IconBackground = new SolidColorBrush(Color.FromArgb(255, 75, 179, 75))
            });
        }

        public void ShowSettings(FrameworkElement owner, ChatPermissionItem item)
        {
            // TODO: use VK.VKUI.Flyouts.MenuFlyout, but first need to fix it.
            Windows.UI.Xaml.Controls.MenuFlyout mf = new Windows.UI.Xaml.Controls.MenuFlyout();
            mf.Placement = FlyoutPlacementMode.Bottom;

            ToggleMenuFlyoutItem all = new ToggleMenuFlyoutItem()
            {
                Text = ChatPermissionItem.GetChangersByIndex(0),
                IsChecked = item.SettingIndex == 0
            };
            ToggleMenuFlyoutItem owa = new ToggleMenuFlyoutItem()
            {
                Text = ChatPermissionItem.GetChangersByIndex(1),
                IsChecked = item.SettingIndex == 1
            };
            ToggleMenuFlyoutItem own = new ToggleMenuFlyoutItem()
            {
                Text = ChatPermissionItem.GetChangersByIndex(2),
                IsChecked = item.SettingIndex == 2
            };

            all.Click += (a, b) => ChangeSetting(item, 0);
            owa.Click += (a, b) => ChangeSetting(item, 1);
            own.Click += (a, b) => ChangeSetting(item, 2);

            if (item.SettingName != "change_admins") mf.Items.Add(all);
            mf.Items.Add(owa);
            mf.Items.Add(own);

            mf.ShowAt(owner);
        }

        //

        private int GetIndexFromSetting(ChatSettingsChangers changers)
        {
            switch (changers)
            {
                case ChatSettingsChangers.All: return 0;
                case ChatSettingsChangers.OwnerAndAdmins: return 1;
                case ChatSettingsChangers.Owner: return 2;
                default: return -1;
            }
        }

        private ChatSettingsChangers GetSettingFromIndex(int index)
        {
            switch (index)
            {
                case 1: return ChatSettingsChangers.OwnerAndAdmins;
                case 2: return ChatSettingsChangers.Owner;
                default: return ChatSettingsChangers.All;
            }
        }

        private async void ChangeSetting(ChatPermissionItem item, int value)
        {
            IsLoading = true;

            try
            {
                string val = "";
                switch (value)
                {
                    case 0: val = "all"; break;
                    case 1: val = "owner_and_admins"; break;
                    case 2: val = "owner"; break;
                }
                string req = $"{{\"{item.SettingName}\":\"{val}\"}}";

                var response = await VKSession.Current.API.Messages.EditChatAsync(ChatId, null, req);
                if (response)
                {
                    item.SettingIndex = value;

                    switch (item.SettingName)
                    {
                        case "invite": ChatPermissions.Invite = GetSettingFromIndex(value); break;
                        case "change_info": ChatPermissions.ChangeInfo = GetSettingFromIndex(value); break;
                        case "change_pin": ChatPermissions.ChangePin = GetSettingFromIndex(value); break;
                        case "use_mass_mentions": ChatPermissions.UseMassMentions = GetSettingFromIndex(value); break;
                        case "see_invite_link": ChatPermissions.SeeInviteLink = GetSettingFromIndex(value); break;
                        case "call": ChatPermissions.Call = GetSettingFromIndex(value); break;
                        case "change_admins": ChatPermissions.ChangeAdmins = GetSettingFromIndex(value); break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) ChangeSetting(item, value);
            }

            IsLoading = false;
        }
    }
}
