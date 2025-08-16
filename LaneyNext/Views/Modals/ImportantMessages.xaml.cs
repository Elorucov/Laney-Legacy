using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using VK.VKUI.Controls;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ImportantMessages : Modal
    {
        public ImportantMessages()
        {
            this.InitializeComponent();
            DataContext = new ImportantMessagesViewModel();
            Loaded += (a, b) =>
            {
                ScrollViewer ListScrollViewer = MessagesList.GetScrollViewer();
                ListScrollViewer.RegisterIncrementalLoadingEvent(() => (DataContext as ImportantMessagesViewModel).Load());
            };
        }

        private void RenderMessage(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            MessageViewModel msg = args.NewValue as MessageViewModel;

            if (sender is Border b && msg != null)
            {
                var v = new MessageView(msg, null, true, b, ActualWidth);
            }
        }

        #region Context menu

        private void ShowContextMenuHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Touch && e.HoldingState == Windows.UI.Input.HoldingState.Started)
                if (e.OriginalSource is FrameworkElement) ShowContextMenu((FrameworkElement)e.OriginalSource);
        }

        private void ShowContextMenuRight(object sender, RightTappedRoutedEventArgs e)
        {
            if (UIViewSettings.GetForCurrentView().UserInteractionMode != UserInteractionMode.Touch)
                if (e.OriginalSource is FrameworkElement) ShowContextMenu((FrameworkElement)e.OriginalSource);
        }

        private void ShowContextMenu(FrameworkElement fe)
        {
            MessageViewModel msg = null;
            if (fe is ListViewItem lvi)
            {
                msg = (lvi.ContentTemplateRoot as FrameworkElement).DataContext as MessageViewModel;
            }
            else
            {
                msg = fe.DataContext as MessageViewModel;
            }
            if (msg == null) return;

            VK.VKUI.Popups.MenuFlyout mf = new VK.VKUI.Popups.MenuFlyout();

            if (Core.Settings.DebugShowMessageIdCtx)
            {
                CellButton dbg = new CellButton { Icon = VKIconName.Icon28BugOutline, Text = $"MID: {msg.Id}, CID: {msg.ConversationMessageId}" };
                mf.Items.Add(dbg);
                mf.Items.Add(new MenuFlyoutSeparator());
            }

            CellButton gotomsg = new CellButton { Icon = VKIconName.Icon28MessageOutline, Text = Locale.Get("go_to_message") };
            CellButton star = new CellButton { Icon = VKIconName.Icon28FavoriteOutline, Text = Locale.Get("msg_ctx_star") };
            CellButton unstar = new CellButton { Icon = VKIconName.Icon28Favorite, Text = Locale.Get("msg_ctx_unstar") };

            gotomsg.Click += (a, b) => Hide(msg);
            star.Click += (a, b) => Mark(msg, true);
            unstar.Click += (a, b) => Mark(msg, false);

            mf.Items.Add(gotomsg);
            mf.Items.Add(msg.IsImportant ? unstar : star);

            mf.ShowAt(fe);
        }

        #endregion

        private async void Mark(MessageViewModel msg, bool important)
        {
            try
            {
                var resp = await VKSession.Current.API.Messages.MarkAsImportantAsync(new List<int> { msg.Id }, important);
                if (resp.First() == msg.Id) msg.IsImportant = important;
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) Mark(msg, important);
            }
        }

        private void MessagesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Hide(e.ClickedItem as MessageViewModel);
        }
    }
}
