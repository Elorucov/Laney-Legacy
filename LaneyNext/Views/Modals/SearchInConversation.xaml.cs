using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System;
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
    public sealed partial class SearchInConversation : Modal
    {

        SearchInConversationViewModel ViewModel { get { return DataContext as SearchInConversationViewModel; } set { DataContext = value; } }

        public SearchInConversation(int peerId, string name)
        {
            this.InitializeComponent();
            TitleText.Text = $"{Locale.Get("conv_ctx_search")} — {name}";
            DataContext = new SearchInConversationViewModel(peerId);
            Loaded += (a, b) =>
            {
                DatePicker.MinYear = new DateTimeOffset(new DateTime(2006, 10, 13));
                DatePicker.MaxYear = DateTimeOffset.Now;

                ScrollViewer ListScrollViewer = MessagesList.GetScrollViewer();
                ListScrollViewer.RegisterIncrementalLoadingEvent(() => ViewModel.DoSearch());
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

        private void SearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ViewModel.DoSearch();
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

            gotomsg.Click += (a, b) => Hide(msg);

            mf.Items.Add(gotomsg);

            mf.ShowAt(fe);
        }

        #endregion

        private void MessagesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Hide(e.ClickedItem as MessageViewModel);
        }

        private void OnDatePicked(DatePickerFlyout sender, DatePickedEventArgs args)
        {
            ViewModel.Date = args.NewDate.Date;
        }
    }
}