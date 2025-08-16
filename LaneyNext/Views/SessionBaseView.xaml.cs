using ELOR.VKAPILib.Methods;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.Views.Modals;
using System;
using System.Collections.Generic;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SessionBaseView : Page
    {
        private SessionBaseViewModel SessionBase { get { return VKSession.Current.SessionBase; } }
        private Shell AppShell { get { return Tag as Shell; } }

        public SessionBaseView()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            Loaded += (a, b) =>
            {
                if (AppShell != null)
                {
                    MainHeader.Margin = new Thickness(0, AppShell.TitleBarHeight, 0, 0);
                    AppShell.TitleBarHeightChanged += (c, d) => MainHeader.Margin = new Thickness(0, d, 0, 0);
                    ConvsList.GetScrollViewer().RegisterIncrementalLoadingEvent(() => SessionBase.GetConversations());
                }

                ctAll.IsChecked = true;

                SessionBase.PropertyChanged += (c, d) =>
                {
                    switch (d.PropertyName)
                    {
                        case nameof(SessionBase.SelectedConversation):
                            ChangeSelectionToCurrentConversation();
                            AppShell.SwitchFrame(true);
                            break;
                    }
                };
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Shell shell) Tag = shell;
        }

        private void ChangeSelectionToCurrentConversation()
        {
            if (SessionBase.SelectedConversation == null)
            {
                AppShell.SwitchFrame(false);
                return;
            }

            ConvsList.SelectedIndex = ConvsList.Items.IndexOf(SessionBase.SelectedConversation);
            if (SessionBase.PinnedConversations.Count > 0)
                PinnedConvs.SelectedIndex = PinnedConvs.Items.IndexOf(SessionBase.SelectedConversation);
        }

        private void OpenConvContextMenu(object sender, RightTappedRoutedEventArgs e)
        {
            ConversationViewModel cvm = null;
            FrameworkElement fel = e.OriginalSource as FrameworkElement;

            if (e.OriginalSource is ListViewItem)
            {
                cvm = (((ListViewItem)fel).ContentTemplateRoot as Control).DataContext as ConversationViewModel;
            }
            else
            {
                cvm = (e.OriginalSource as FrameworkElement).DataContext as ConversationViewModel;
            }

            if (cvm == null) return;

            FrameworkElement uel = e.OriginalSource as FrameworkElement;
            VK.VKUI.Popups.MenuFlyout mf = BuildMenuFlyout(cvm);
            if (mf.Items.Count > 0) mf.ShowAt(uel);
        }

        private VK.VKUI.Popups.MenuFlyout BuildMenuFlyout(ConversationViewModel cvm)
        {
            CellButton unread = new CellButton
            {
                Icon = VKIconName.Icon28MessageUnreadTop,
                Text = Locale.Get("conv_ctx_mark_as_unread")
            };

            CellButton read = new CellButton
            {
                Icon = VKIconName.Icon28MessageOutline,
                Text = Locale.Get("conv_ctx_mark_as_read")
            };

            CellButton pincontpanel = new CellButton
            {
                Icon = VKIconName.Icon28PinOutline,
                Text = Locale.Get("conv_ctx_pin_to_contactpanel")
            };

            CellButton unpincontpanel = new CellButton
            {
                Icon = VKIconName.Icon28PinOutline,
                Text = Locale.Get("conv_ctx_pin_to_contactpanel")
            };

            CellButton notification = new CellButton
            {
                Icon = cvm.IsMuted ? VKIconName.Icon28Notifications : VKIconName.Icon28NotificationDisableOutline,
                Text = cvm.IsMuted ? Locale.Get("conv_ctx_notifications_enable") : Locale.Get("conv_ctx_notifications_disable")
            };

            CellButton clearhistory = new CellButton
            {
                Icon = VKIconName.Icon28DeleteOutline,
                IconBrush = (SolidColorBrush)Application.Current.Resources["VKDestructiveBrush"],
                Text = Locale.Get("conv_ctx_clear")
            };

            CellButton cleardeny = new CellButton
            {
                Icon = VKIconName.Icon28DeleteOutline,
                IconBrush = (SolidColorBrush)Application.Current.Resources["VKDestructiveBrush"],
                Text = Locale.Get("conv_ctx_clear_block")
            };

            CellButton clearchat = new CellButton
            {
                Icon = VKIconName.Icon28DeleteOutline,
                IconBrush = (SolidColorBrush)Application.Current.Resources["VKDestructiveBrush"],
                Text = Locale.Get("conv_ctx_clear_leave")
            };

            CellButton kickself = new CellButton
            {
                Icon = VKIconName.Icon28DoorArrowRightOutline,
                IconBrush = (SolidColorBrush)Application.Current.Resources["VKDestructiveBrush"],
                Text = cvm.ChatSettings != null && cvm.ChatSettings.IsGroupChannel ? Locale.Get("chatinfo_unsubscribe") : Locale.Get("chatinfo_leave")
            };

            CellButton returnto = new CellButton
            {
                Icon = VKIconName.Icon28ArrowUturnRightOutline,
                Text = cvm.ChatSettings != null && cvm.ChatSettings.IsGroupChannel ? Locale.Get("conv_ctx_return_channel") : Locale.Get("conv_ctx_return_chat")
            };

            unread.Click += async (a, b) =>
                await VKSession.Current.API.Messages.MarkAsUnreadConversationAsync(VKSession.Current.GroupId, cvm.Id);

            read.Click += async (a, b) =>
                await VKSession.Current.API.Messages.MarkAsReadAsync(VKSession.Current.GroupId, cvm.Id, 0, true);

            pincontpanel.Click += (a, b) => Utils.ShowUnderConstructionInfo();
            unpincontpanel.Click += (a, b) => Utils.ShowUnderConstructionInfo();
            notification.Click += (a, b) => APIHelper.ChangeConversationNotification(cvm.Id, cvm.IsMuted);

            clearhistory.Click += async (a, b) => await APIHelper.DeleteConversationAsync(cvm.Id);
            cleardeny.Click += async (a, b) => await APIHelper.DeleteConvAndDenyGroupAsync(-cvm.Id);
            clearchat.Click += async (a, b) => await APIHelper.LeaveAndDeleteChatAsync(cvm.Id);
            kickself.Click += async (a, b) => await APIHelper.LeaveFromChatAsync(cvm.Id - 2000000000, cvm.ChatSettings.IsGroupChannel);

            bool isUnread = cvm.IsMarkedAsUnread || cvm.UnreadMessagesCount > 0;

            VK.VKUI.Popups.MenuFlyout mf = new VK.VKUI.Popups.MenuFlyout();
            if (cvm.Id != VKSession.Current.SessionId && !isUnread) mf.Items.Add(unread);
            if (cvm.Id != VKSession.Current.SessionId && isUnread) mf.Items.Add(read);
            // if (cvm.PeerType == PeerType.User) mf.Items.Add(pincontpanel);
            if (cvm.Id != VKSession.Current.SessionId) mf.Items.Add(notification);
            if (cvm.PeerType == PeerType.User || cvm.PeerType == PeerType.Group || (cvm.PeerType == PeerType.Chat && cvm.ChatSettings.State != UserStateInChat.In)) mf.Items.Add(clearhistory);
            if (cvm.PeerType == PeerType.Group) mf.Items.Add(cleardeny);
            if ((cvm.PeerType == PeerType.Chat && cvm.ChatSettings.State == UserStateInChat.In) && !cvm.ChatSettings.IsGroupChannel) mf.Items.Add(clearchat);
            if (cvm.PeerType == PeerType.Chat && cvm.ChatSettings.State == UserStateInChat.In) mf.Items.Add(kickself);
            if (cvm.PeerType == PeerType.Chat && cvm.ChatSettings.State == UserStateInChat.Left) mf.Items.Add(returnto);
            return mf;
        }

        #region Control's events

        private void CreateChatButtonClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ChatCreationView), null);
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SearchView), null);
        }

        private async void ConversationsListItemClick(object sender, ItemClickEventArgs e)
        {
            ConversationViewModel cvm = e.ClickedItem as ConversationViewModel;
            if (!ViewManagement.StandaloneConversationsContains(cvm))
            {
                SessionBase.SelectedConversation = cvm;
            }
            else
            {
                await ViewManagement.SwitchToOpenedStandaloneConversationAsync(cvm);
                ChangeSelectionToCurrentConversation();
            }
        }

        private void ConversationsListItemLostFocus(object sender, RoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() is ListViewItem) return;
            ChangeSelectionToCurrentConversation();
        }

        private void convstypes_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb != null && rb.GroupName == "convstypes")
            {
                ConvTypeFlyout.Hide();
                if (ConvListTypeText != null) ConvListTypeText.Text = rb.Content?.ToString();
                ConversationsFilter filter = ConversationsFilter.All;
                switch (rb.Name)
                {
                    case nameof(ctAll): filter = ConversationsFilter.All; break;
                    case nameof(ctImp): filter = ConversationsFilter.Important; break;
                    case nameof(ctUns): filter = ConversationsFilter.Unanswered; break;
                    case nameof(ctUnr): filter = ConversationsFilter.Unread; break;
                }
                SessionBase.GetConversationsByFilter(filter);
            }
        }

        private void ShowStarredMessagesModal(object sender, RoutedEventArgs e)
        {
            ImportantMessages imm = new ImportantMessages();
            imm.Closed += (a, b) =>
            {
                if (b != null && b is MessageViewModel msg)
                {
                    VKSession.Current.SessionBase.SwitchToConversation(msg.PeerId, msg.Id);
                }
            };
            imm.Show();
        }

        private async void MiniAudioPlayerControl_Click(object sender, RoutedEventArgs e)
        {
            await ViewManagement.OpenAudioPlayer();
        }

        private void MiniAudioPlayerControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            AudioPlayerViewModel.CloseMainInstance();
        }

        #endregion

        #region Menu

        private void ShowMenu(object sender, RoutedEventArgs e)
        {
            List<Entity<VKSession>> sessions = new List<Entity<VKSession>>();
            List<VKSession> userSessions = VKSession.GetSessionsForVKUser(VKSession.Current.Id);

            foreach (VKSession s in userSessions)
            {
                if (!VKSession.Compare(s, VKSession.Current))
                {
                    Entity<VKSession> entity = new Entity<VKSession>(s.SessionId, s.DisplayName, String.Empty, s.Avatar);
                    entity.Object = s;
                    sessions.Add(entity);
                }
            }
            SessionsList.ItemsSource = sessions;
            SessionFlyoutSeparator.Visibility = sessions.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            SessionsFlyout.ShowAt(sender as FrameworkElement);
        }

        private async void SessionsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            SessionsFlyout.Hide();
            VKSession s = (e.ClickedItem as Entity<VKSession>).Object;
            s.StartSession();
            await ViewManagement.OpenSession(s);
        }

        private void ShowBlacklistModal(object sender, RoutedEventArgs e)
        {
            SessionsFlyout.Hide();
            UserBlacklist ubmodal = new UserBlacklist();
            ubmodal.Show();
        }

        private void OpenCommunityPicker(object sender, RoutedEventArgs e)
        {
            SessionsFlyout.Hide();
            SessionsModal modal = new SessionsModal();
            modal.Show();
        }

        private async void OpenSettingsWindow(object sender, RoutedEventArgs e)
        {
            SessionsFlyout.Hide();
            await ViewManagement.OpenSettingsWindow(VKSession.CurrentUser.API);
        }

        private async void TryLogout(object sender, RoutedEventArgs e)
        {
            SessionsFlyout.Hide();

            Alert alert = new Alert
            {
                Header = Locale.Get("logout"),
                Text = Locale.Get("logout_dialog"),
                PrimaryButtonText = Locale.Get("yes"),
                SecondaryButtonText = Locale.Get("no")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                VKSession.LogoutAsync();
            }
        }

        #endregion
    }
}