using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Elorucov.Laney.Pages {

    public sealed partial class ImView : Page {
        Action<FrameworkElement> MainMenuAction;

        public ImView() {
            this.InitializeComponent();
            try {
                NavigationCacheMode = NavigationCacheMode.Required;
                SetupUI();
            } catch (Exception ex) {
                // От одного конкретного чела происходит краш на этом моменте, лол.
                Functions.ShowHandledErrorDialog(ex);
            }
        }

        private void SetupUI() {
            new System.Action(async () => {
                await Task.Delay(1);
                if (AppParameters.FoldersPlacement) {
                    FindName(nameof(FoldersTabsSide));
                    FindName(nameof(BackgroundMica));
                    FindName(nameof(FolderName));
                    FindName(nameof(folderNameSkeleton));
                    FindName(nameof(TitleBarSeparator));
                    Main.GetCurrent().MenuButton = MenuButtonVertical;
                } else {
                    FindName(nameof(MenuButtonHorizontal));
                    FindName(nameof(FoldersTabsTop));
                    FindName(nameof(foldersTabsSkeleton));
                    Main.GetCurrent().MenuButton = MenuButtonHorizontal;
                }

                DataContext = AppSession.ImViewModel;
                ChangeUIForCompactMode(Main.GetCurrent().IsLeftPaneCompact);
                Main.GetCurrent().LeftPaneCompactModeChanged += ImView_LeftPaneCompactModeChanged;

                await Task.Delay(150);
                Main.GetCurrent().MenuButton.Focus(FocusState.Programmatic);
            })();
        }

        private void ImView_LeftPaneCompactModeChanged(object sender, bool e) {
            ChangeUIForCompactMode(e);
        }

        private void ChangeUIForCompactMode(bool isCompact) {
            if (isCompact) {
                FindName(nameof(CompactLayout));
                CompactLayout.Visibility = Visibility.Visible;
                MainLayout.Visibility = Visibility.Collapsed;
            } else {
                if (CompactLayout != null) CompactLayout.Visibility = Visibility.Collapsed;
                MainLayout.Visibility = Visibility.Visible;
                if (AppParameters.FoldersPlacement) {
                    Main.GetCurrent().MenuButton = MenuButtonVertical;
                } else {
                    Main.GetCurrent().MenuButton = MenuButtonHorizontal;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            try {
                MainMenuAction = e.Parameter as Action<FrameworkElement>;
                Theme.FoldersViewChanged += Theme_FoldersViewChanged;
                Theme.ChatsListItemTemplateChanged += Theme_ChatsListItemTemplateChanged;
                CoreApplication.GetCurrentView().CoreWindow.KeyDown += CoreWindow_KeyDown;
            } catch (Exception ex) {
                Log.Error($"ImView.OnNavigatedTo error! 0x{ex.HResult.ToString("x8")}");
                Functions.ShowHandledErrorDialog(ex);
            }
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            base.OnNavigatedFrom(e);
            // Theme.FoldersViewChanged -= Theme_FoldersViewChanged;
            CoreApplication.GetCurrentView().CoreWindow.KeyDown -= CoreWindow_KeyDown;
        }

        private void OpenMenu(object sender, RoutedEventArgs e) {
            MainMenuAction?.Invoke(sender as FrameworkElement);
        }

        private void SelectConversation(object sender, ItemClickEventArgs e) {
            Main.GetCurrent().ShowConversationPage(e.ClickedItem as ConversationViewModel, -1, false);
        }

        private void GoToSearchPage(object sender, RoutedEventArgs e) {
            Frame.Navigate(typeof(SearchView), null, App.DefaultNavTransition);
        }

        private void GoToChatCreationPage(object sender, RoutedEventArgs e) {
            Main.GetCurrent().SwitchToLeftFrame(true);
            Frame.Navigate(typeof(ChatCreationView), null, App.DefaultNavTransition);
        }

        private void ShowConversationContextMenu(UIElement sender, ContextRequestedEventArgs args) {
            FrameworkElement el = sender as FrameworkElement;
            ConversationViewModel conv = el.DataContext as ConversationViewModel;
            bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

            if (conv == null) return;
            args.Handled = true;
            if (ctrl) {
                new Dialogs.Dev.MessageJSONView(conv).Show();
                return;
            }

            bool unread = conv.IsMarkedUnread || conv.UnreadMessagesCount > 0;

            MenuFlyout mf = new MenuFlyout();

            if (AppParameters.AdvancedMessageInfo) {
                MenuFlyoutItem dbg = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = $"Peer id: {conv.ConversationId}" };
                dbg.Click += (a, b) => {
                    new Dialogs.Dev.MessageJSONView(conv).Show();
                };
                mf.Items.Add(dbg);

                MenuFlyoutItem cmid = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = $"Check last message" };
                cmid.Click += (a, b) => {
                    int c = conv.LastMessage == null ? 0 : conv.LastMessage.ConversationMessageId;
                    int d = conv.ReceivedMessages.Count > 0 ? conv.ReceivedMessages.LastOrDefault().ConversationMessageId : 0;
                    int e = conv.ReceivedMessages.Count;

                    Tips.Show("Info:", $"Received messages: {e}\nLast message cmid (property): {c}\nLast message cmid (from collection): {d}");
                };
                mf.Items.Add(cmid);

                mf.Items.Add(new MenuFlyoutSeparator());
            }

            MenuFlyoutItem unr = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = unread ? "" : "" }, Text = Locale.Get(unread ? "convctx_mark_as_read" : "convctx_mark_as_unread") };
            MenuFlyoutItem pin = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("convctx_pin") };
            MenuFlyoutItem unpin = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("convctx_unpin") };
            MenuFlyoutSubItem folders = new MenuFlyoutSubItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("add_to_folder") };
            MenuFlyoutItem archive = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("convctx_archive") };
            MenuFlyoutItem unarchive = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("convctx_unarchive") };
            MenuFlyoutItem del = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("convctx_delete"), Style = (Style)App.Current.Resources["DestructiveMenuFlyoutItem"] };
            MenuFlyoutItem delForAll = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("convctx_delete_for_all"), Style = (Style)App.Current.Resources["DestructiveMenuFlyoutItem"] };

            MenuFlyoutItem standalone = new MenuFlyoutItem { Text = "Open in separated window" };

            unr.Click += async (a, b) => {
                object r = unread
                    ? await Messages.MarkAsRead(conv.ConversationId, conv.LastMessage.ConversationMessageId, true)
                    : await Messages.MarkAsUnreadConversation(conv.ConversationId);
                Functions.ShowHandledErrorTip(r);
            };
            archive.Click += async (a, b) => {
                object r = await Messages.ArchiveConversation(conv.ConversationId);
                if (r is int i && i == 1) {
                    Tips.Show(Locale.Get("conv_archived"));
                } else {
                    Functions.ShowHandledErrorTip(r);
                }
            };
            unarchive.Click += async (a, b) => {
                object r = await Messages.UnarchiveConversation(conv.ConversationId);
                if (r is int i && i == 1) {
                    Tips.Show(Locale.Get("conv_unarchived"));
                } else {
                    Functions.ShowHandledErrorTip(r);
                }
            };
            pin.Click += async (a, b) => {
                object r = await Messages.PinConversation(conv.ConversationId);
                Functions.ShowHandledErrorTip(r);
            };
            unpin.Click += async (a, b) => {
                object r = await Messages.UnpinConversation(conv.ConversationId);
                Functions.ShowHandledErrorTip(r);
            };
            del.Click += async (a, b) => {
                await APIHelper.ClearChatHistoryAsync(conv.ConversationId);
            };

            delForAll.Click += async (a, b) => await APIHelper.DeleteChatForAllAsync((int)conv.ConversationId - 2000000000, conv.Title);

            standalone.Click += async (a, b) => {
                ConversationViewModel fc = (from c in AppSession.CachedConversations where c.ConversationId == conv.ConversationId select c).ToList().FirstOrDefault();
                if (fc == null) {
                    fc = new ConversationViewModel(conv.ConversationId);
                    AppSession.CachedConversations.Add(fc);
                }
                await ViewManagement.OpenNewWindow(typeof(ConversationView), conv.Title, fc);
            };

            if (conv.ConversationId != AppParameters.UserID) mf.Items.Add(unr);
            if (AppSession.ImViewModel.Folders.Count > 2) {
                foreach (var f in AppSession.ImViewModel.Folders) {
                    if (f.Id <= 0 || f.Type != "default") continue;
                    ToggleMenuFlyoutItem tmfi = new ToggleMenuFlyoutItem {
                        Icon = new FixedFontIcon { Glyph = "" },
                        Text = f.Name,
                        IsChecked = conv.FolderIds.Contains(f.Id)
                    };
                    tmfi.Click += async (a, b) => {
                        if (conv.FolderIds.Contains(f.Id)) {
                            await APIHelper.RemoveConvFromFolderAsync(conv.ConversationId, f.Id);
                        } else {
                            await APIHelper.AddConvToFolderAsync(conv.ConversationId, f.Id);
                        }
                    };
                    folders.Items.Add(tmfi);
                }

                mf.Items.Add(folders);
            }

            if (conv.SortId.MajorId != 0 && conv.SortId.MajorId % 16 == 0) {
                mf.Items.Add(unpin);
            } else { // Стоит сделать проверку на кол-во закреплённых бесед
                mf.Items.Add(pin);
            }
            mf.Items.Add(conv.IsArchived ? unarchive : archive);

            mf.Items.Add(del);
            if (conv.ConversationId.IsChat() && conv.ChatSettings?.Permissions != null) mf.Items.Add(delForAll);
            if (AppParameters.ConvMultiWindow) mf.Items.Add(standalone);

            Point pos;
            bool posReached = args != null && args.TryGetPosition(el, out pos);

            if (mf.Items.Count > 0) {
                if (posReached) {
                    mf.ShowAt(el, pos);
                } else {
                    mf.ShowAt(el);
                }
            }
        }

        // Экстравагантным образом удаляем сепаратор закреплённой беседы в папках.
        private void RemoveSeparatorFromConvItemUI(object sender, RoutedEventArgs e) {
            if (AppSession.ImViewModel?.CurrentFolder?.Id > 0) {
                FrameworkElement indicator = sender as FrameworkElement;
                Panel parent = indicator?.Parent as Panel;
                parent?.Children.Remove(indicator);
            }
        }

        private void ShowFolderContextMenu(UIElement sender, ContextRequestedEventArgs args) {
            FrameworkElement el = sender as FrameworkElement;
            ConversationsFolder folder = el.DataContext as ConversationsFolder;

            if (folder == null) return;
            args.Handled = true;

            MenuFlyout mf = new MenuFlyout();

            MenuFlyoutItem fgs = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("edit_folders") };
            MenuFlyoutItem fls = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("edit_folder") };
            MenuFlyoutItem del = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("delete"), Style = (Style)App.Current.Resources["DestructiveMenuFlyoutItem"] };

            fgs.Click += (a, b) => new FoldersSettings().Show();
            fls.Click += (a, b) => new FoldersSettings(folder.Id, folder.Name).Show();
            del.Click += async (a, b) => {
                if (await APIHelper.TryDeleteFolderAsync(folder)) {
                    AppSession.ImViewModel.Folders.Remove(folder);
                }
            };

            if (folder.Id <= 0) mf.Items.Add(fgs);
            if (folder.Id > 0 && folder.Type == "default") mf.Items.Add(fls);
            if (folder.Id > 0) mf.Items.Add(del);

            Point pos;
            bool posReached = args != null && args.TryGetPosition(el, out pos);

            if (mf.Items.Count > 0) {
                if (posReached) {
                    mf.ShowAt(el, pos);
                } else {
                    mf.ShowAt(el);
                }
            }
        }

        private void RemoveLRButtons(object sender, RoutedEventArgs e) {
            FlipView fv = sender as FlipView;
            Panel parent = null;

            try {
                Button prev = fv.FindControlByName<Button>("PreviousButtonHorizontal");
                if (prev != null) {
                    parent = prev.Parent as Panel;
                    parent.Children.Remove(prev);
                }

                Button next = fv.FindControlByName<Button>("NextButtonHorizontal");
                if (next != null) {
                    parent.Children.Remove(next);
                }
            } catch (Exception ex) {
                Log.Error($"{GetType().Name}: Cannot remove L&R buttons from FlipView! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
        }

        private void Theme_FoldersViewChanged(object sender, bool e) {
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Frame.Navigate(typeof(ImView), MainMenuAction, new SuppressNavigationTransitionInfo());
        }

        private void Theme_ChatsListItemTemplateChanged(object sender, bool e) {
            foreach (var listView in convsListViews) {
                listView.ItemTemplate = (DataTemplate)Resources[AppParameters.ChatsListLines ? "ConversationItemTemplate3Line" : "ConversationItemTemplate"];
            }
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args) {
            int num = 0;
            int key = (int)args.VirtualKey;
            if (key >= 49 && key <= 57) num = key - 48;

            bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            if (ctrl && args.VirtualKey == VirtualKey.F5) {
                new System.Action(async () => { await AppSession.ImViewModel.RefreshAsync(); })();
            }

            var focused = FocusManager.GetFocusedElement();
            bool isFocusedOnTextBox = focused != null && (focused is TextBox || focused is RichEditBox);

            if (ctrl && !shift && num != 0 && !isFocusedOnTextBox) {
                var folder = AppSession.ImViewModel.CurrentFolder;
                if (folder != null && folder.Conversations?.Count > num) {
                    Main.GetCurrent().ShowConversationPage(folder.Conversations[num - 1]);
                }
            }

            if (ctrl && shift && num != 0 && AppSession.ImViewModel.Folders.Count > num && !isFocusedOnTextBox) {
                AppSession.ImViewModel.CurrentFolder = AppSession.ImViewModel.Folders[num - 1];
            }

            if (ctrl && shift && args.VirtualKey == VirtualKey.F && !isFocusedOnTextBox) {
                Frame.Navigate(typeof(SearchView), null, App.DefaultNavTransition);
            }

            if (ctrl && args.VirtualKey == VirtualKey.N && !isFocusedOnTextBox) {
                Main.GetCurrent().SwitchToLeftFrame(true);
                Frame.Navigate(typeof(ChatCreationView), null, App.DefaultNavTransition);
            }

            if (!ctrl && args.VirtualKey == VirtualKey.F9) {
                MainMenuAction?.Invoke(AppParameters.FoldersPlacement ? MenuButtonVertical : MenuButtonHorizontal);
            }
        }

        private List<ListView> convsListViews = new List<ListView>();
        private void OnListViewLoaded(object sender, RoutedEventArgs e) {
            ListView listView = sender as ListView;
            listView.Loaded -= OnListViewLoaded;

            try {
                // Set ItemTemplate
                if (listView.Name != nameof(CompactChatsList)) {
                    listView.ItemTemplate = (DataTemplate)Resources[AppParameters.ChatsListLines ? "ConversationItemTemplate3Line" : "ConversationItemTemplate"];
                    convsListViews.Add(listView);
                }

                // Get scroll viewer.
                Border border = VisualTreeHelper.GetChild(listView, 0) as Border;
                ScrollViewer scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;

                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            } catch (Exception ex) {
                Functions.ShowHandledErrorDialog(ex);
            }
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv == null) return;

                ConversationsFolder ownerFolder = sv.DataContext as ConversationsFolder;

                if (ownerFolder == null) return;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 72) {
                    new System.Action(async () => await ownerFolder.LoadConversations())();
                }
            }
        }

        private void InitConvsSkeleton(object sender, RoutedEventArgs e) {
            StackPanel panel = sender as StackPanel;

            SolidColorBrush brush = (SolidColorBrush)App.Current.Resources["SystemControlBackgroundBaseMediumBrush"];

            DataTemplate convTemp1 = App.Current.Resources["convTemp1"] as DataTemplate;
            DataTemplate convTemp2 = App.Current.Resources["convTemp2"] as DataTemplate;
            DataTemplate convTemp3 = App.Current.Resources["convTemp3"] as DataTemplate;
            new System.Action(async () => {
                await SkeletonAnimation.FillPanelAndStartAnimation(panel, new List<DataTemplate> { convTemp1, convTemp2, convTemp3 }, brush.Color, 14);
            })();
        }

        private void InitFoldersTabsSkeleton(object sender, RoutedEventArgs e) {
            StackPanel panel = sender as StackPanel;

            SolidColorBrush brush = (SolidColorBrush)App.Current.Resources["SystemControlBackgroundBaseMediumBrush"];

            DataTemplate horizontalFolderTemp = App.Current.Resources["horizontalFolderTemp"] as DataTemplate;
            new System.Action(async () => { await SkeletonAnimation.FillPanelAndStartAnimation(panel, horizontalFolderTemp, brush.Color, 3, LayoutRoot); })();
        }

        private void InitSideFoldersSkeleton(object sender, RoutedEventArgs e) {
            StackPanel panel = sender as StackPanel;

            SolidColorBrush brush = (SolidColorBrush)App.Current.Resources["SystemControlBackgroundBaseMediumBrush"];

            DataTemplate verticalFolderTemp = App.Current.Resources["verticalFolderTemp"] as DataTemplate;
            new System.Action(async () => { await SkeletonAnimation.FillPanelAndStartAnimation(panel, verticalFolderTemp, brush.Color, 6, LayoutRoot); })();
        }

        private void InitFolderNameSkeleton(object sender, RoutedEventArgs e) {
            SolidColorBrush brush = (SolidColorBrush)App.Current.Resources["SystemControlBackgroundBaseMediumBrush"];
            new System.Action(async () => { await SkeletonAnimation.Start(LayoutRoot, new List<Shape> { folderNameSkeleton }, brush.Color); })();
        }

        #region Message preview tooltip

        private void CLVISecondLineLoaded(object sender, RoutedEventArgs e) {
            var tt = new ToolTip();
            tt.Opened += ToolTip_Opened;
            tt.Closed += ToolTip_Closed;
            ToolTipService.SetToolTip(sender as FrameworkElement, tt);
        }

        private void ToolTip_Opened(object sender, RoutedEventArgs e) {
            var tt = sender as ToolTip;
            var conv = tt.DataContext as ConversationViewModel;
            if (AppParameters.LastMessagePreview && tt != null && conv != null && conv.LastMessage != null) {
                var msg = conv.LastMessage;
                if (msg.Action == null && !msg.IsExpired) {
                    var msgUi = MessageUIHelper.Build(msg, null);
                    bool hasAva = conv.Type == PeerType.Chat && msg.SenderId != AppParameters.UserID;
                    double leftMarginWithoutAva = msg.SenderId == AppParameters.UserID ? -12 : -4;
                    msgUi.Margin = msg.Action == null ? new Thickness(hasAva ? -52 : leftMarginWithoutAva, 0, -12, 0) : new Thickness();
                    tt.Background = msg.HasSticker() || msg.ContainsOnlyEmojis() ? (Brush)App.Current.Resources["AcrylicBackgroundFillColorDefaultBrush"] : new SolidColorBrush(Colors.Transparent);
                    tt.MaxWidth = double.PositiveInfinity;
                    tt.BorderThickness = new Thickness();
                    tt.Padding = new Thickness();
                    if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) tt.CornerRadius = new CornerRadius(16);
                    tt.Content = msgUi;
                } else {
                    tt.IsOpen = false;
                }
            } else {
                tt.IsOpen = false;
            }
        }

        private void ToolTip_Closed(object sender, RoutedEventArgs e) {
            if (sender is ToolTip tt) tt.Content = new object();
        }

        #endregion

        private void FolderSelectorButton_Click(object sender, RoutedEventArgs e) {
            FrameworkElement el = sender as FrameworkElement;
            FoldersFlyout.ShowAt(el);
        }
    }
}