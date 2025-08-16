using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class ChatSettingsModal : Modal {
        private ObservableCollection<ChatSettingsItem> ChatSettings = new ObservableCollection<ChatSettingsItem>();
        bool isProcessing = false;

        int ChatId { get; set; }
        public ChatPermissions Permissions { get; private set; }

        public ChatSettingsModal() {
            this.InitializeComponent();
            FindName(nameof(Templates));
            FindName(nameof(Footer));

            SetupForOpen();
            SettingsList.ItemsSource = ChatSettings;
        }

        public ChatSettingsModal(int chatId, ChatPermissions permissions) {
            this.InitializeComponent();
            ChatId = chatId;
            Permissions = permissions;

            SetupPermissions();
            SettingsList.ItemsSource = ChatSettings;
        }

        private void SetupPermissions() {
            ChatSettings.Clear();
            ChatSettings.Add(new ChatSettingsItem("invite") {
                SettingIndex = GetIndexFromSetting(Permissions.Invite),
                Icon = (DataTemplate)App.Current.Resources["ChatSettingsInvite"]
            });
            ChatSettings.Add(new ChatSettingsItem("change_info") {
                SettingIndex = GetIndexFromSetting(Permissions.ChangeInfo),
                Icon = (DataTemplate)App.Current.Resources["ChatSettingsChangeInfo"]
            });
            ChatSettings.Add(new ChatSettingsItem("change_pin") {
                SettingIndex = GetIndexFromSetting(Permissions.ChangePin),
                Icon = (DataTemplate)App.Current.Resources["ChatSettingsChangePin"]
            });
            ChatSettings.Add(new ChatSettingsItem("use_mass_mentions") {
                SettingIndex = GetIndexFromSetting(Permissions.UseMassMentions),
                Icon = (DataTemplate)App.Current.Resources["ChatSettingsMassMention"]
            });
            ChatSettings.Add(new ChatSettingsItem("see_invite_link") {
                SettingIndex = GetIndexFromSetting(Permissions.SeeInviteLink),
                Icon = (DataTemplate)App.Current.Resources["ChatSettingsSeeInviteLink"]
            });
            ChatSettings.Add(new ChatSettingsItem("call") {
                SettingIndex = GetIndexFromSetting(Permissions.Call),
                Icon = (DataTemplate)App.Current.Resources["ChatSettingsCall"]
            });
            ChatSettings.Add(new ChatSettingsItem("change_admins") {
                SettingIndex = GetIndexFromSetting(Permissions.ChangeAdmins),
                Icon = (DataTemplate)App.Current.Resources["ChatSettingsAdmin"]
            });
            ChatSettings.Add(new ChatSettingsItem("change_style") {
                SettingIndex = GetIndexFromSetting(Permissions.ChangeStyle),
                Icon = (DataTemplate)App.Current.Resources["ChatSettingsChangeStyle"]
            });
        }

        private int GetIndexFromSetting(ChatSettingsChangers changers) {
            switch (changers) {
                case ChatSettingsChangers.All: return 0;
                case ChatSettingsChangers.OwnerAndAdmins: return 1;
                case ChatSettingsChangers.Owner: return 2;
                default: return -1;
            }
        }

        private ChatSettingsChangers GetSettingFromIndex(int index) {
            switch (index) {
                case 1: return ChatSettingsChangers.OwnerAndAdmins;
                case 2: return ChatSettingsChangers.Owner;
                default: return ChatSettingsChangers.All;
            }
        }

        private void ShowOptionsContextMenu(object sender, ItemClickEventArgs e) {
            if (isProcessing) return;
            ChatSettingsItem item = e.ClickedItem as ChatSettingsItem;
            ListViewItem lv = SettingsList.ContainerFromItem(item) as ListViewItem;
            FrameworkElement el = (lv.ContentTemplateRoot as Grid).Children[0] as FrameworkElement;

            MenuFlyout mf = new MenuFlyout();
            mf.Placement = FlyoutPlacementMode.Bottom;

            ToggleMenuFlyoutItem all = new ToggleMenuFlyoutItem() {
                Text = ChatSettingsItem.GetChangersByIndex(0),
                IsChecked = item.SettingIndex == 0
            };
            ToggleMenuFlyoutItem owa = new ToggleMenuFlyoutItem() {
                Text = ChatSettingsItem.GetChangersByIndex(1),
                IsChecked = item.SettingIndex == 1
            };
            ToggleMenuFlyoutItem own = new ToggleMenuFlyoutItem() {
                Text = ChatSettingsItem.GetChangersByIndex(2),
                IsChecked = item.SettingIndex == 2
            };

            all.Click += async (a, b) => await ChangeSetting(item, 0);
            owa.Click += async (a, b) => await ChangeSetting(item, 1);
            own.Click += async (a, b) => await ChangeSetting(item, 2);

            if (item.SettingName != "change_admins") mf.Items.Add(all);
            mf.Items.Add(owa);
            mf.Items.Add(own);

            mf.ShowAt(el);
        }

        private async Task ChangeSetting(ChatSettingsItem item, int value) {
            isProcessing = true;

            if (ChatId > 0) {
                string val = "";
                switch (value) {
                    case 0: val = "all"; break;
                    case 1: val = "owner_and_admins"; break;
                    case 2: val = "owner"; break;
                }
                string req = $"{{\"{item.SettingName}\":\"{val}\"}}";

                VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
                object r = await ssp.ShowAsync(Messages.EditChat(ChatId, null, req));
                if (r is bool b) {
                    ChangeSettingFinal(item, value);
                } else {
                    Functions.ShowHandledErrorTip(r);
                }
            } else {
                ChangeSettingFinal(item, value);
            }

            isProcessing = false;
        }

        private void ChangeSettingFinal(ChatSettingsItem item, int value) {
            item.SettingIndex = value;

            switch (item.SettingName) {
                case "invite": Permissions.Invite = GetSettingFromIndex(value); break;
                case "change_info": Permissions.ChangeInfo = GetSettingFromIndex(value); break;
                case "change_pin": Permissions.ChangePin = GetSettingFromIndex(value); break;
                case "use_mass_mentions": Permissions.UseMassMentions = GetSettingFromIndex(value); break;
                case "see_invite_link": Permissions.SeeInviteLink = GetSettingFromIndex(value); break;
                case "call": Permissions.Call = GetSettingFromIndex(value); break;
                case "change_admins": Permissions.ChangeAdmins = GetSettingFromIndex(value); break;
                case "change_style": Permissions.ChangeStyle = GetSettingFromIndex(value); break;
            }
        }

        private void SetupForOpen() {
            Permissions = new ChatPermissions {
                Invite = ChatSettingsChangers.All,
                ChangeInfo = ChatSettingsChangers.All,
                ChangePin = ChatSettingsChangers.All,
                UseMassMentions = ChatSettingsChangers.All,
                SeeInviteLink = ChatSettingsChangers.All,
                Call = ChatSettingsChangers.All,
                ChangeAdmins = ChatSettingsChangers.Owner,
                ChangeStyle = ChatSettingsChangers.All,
            };
            SetupPermissions();
        }

        private void SetupForClosed() {
            Permissions = new ChatPermissions {
                Invite = ChatSettingsChangers.Owner,
                ChangeInfo = ChatSettingsChangers.Owner,
                ChangePin = ChatSettingsChangers.Owner,
                UseMassMentions = ChatSettingsChangers.Owner,
                SeeInviteLink = ChatSettingsChangers.Owner,
                Call = ChatSettingsChangers.Owner,
                ChangeAdmins = ChatSettingsChangers.Owner,
                ChangeStyle = ChatSettingsChangers.Owner,
            };
            SetupPermissions();
        }

        private void SetupForOpen(object sender, RoutedEventArgs e) {
            SetupForOpen();
        }

        private void SetupForClosed(object sender, RoutedEventArgs e) {
            SetupForClosed();
        }

        private void SaveSettings(object sender, RoutedEventArgs e) {
            Hide(Permissions);
        }
    }
}