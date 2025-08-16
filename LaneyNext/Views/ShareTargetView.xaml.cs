using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.ViewModels.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using VK.VKUI;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using Windows.ApplicationModel.Activation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShareTargetView : Page
    {
        public ShareTargetView()
        {
            this.InitializeComponent();
        }

        private ShareTargetViewModel ViewModel { get { return DataContext as ShareTargetViewModel; } }
        ShareTargetActivatedEventArgs Arguments;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter != null && e.Parameter is ShareTargetActivatedEventArgs stargs) Arguments = stargs;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(TitleBar);

            // Local password
            var result = await LocalPassword.ShowPasswordDialogAsync();
            if (result == PasswordDialogResult.Cancelled) Arguments.ShareOperation.ReportCompleted();

            // Check if sessions available
            ScreenSpinner<List<VKSession>> ss = new ScreenSpinner<List<VKSession>>();
            var sessions = await ss.ShowAsync(VKSession.GetSessionsAsync());
            if (sessions.Count > 0)
            {
                Root.Visibility = Visibility.Visible;
                DataContext = new ShareTargetViewModel(Arguments.ShareOperation, sessions);
            }
            else
            {
                // TODO: Normal dialog.
                await new MessageDialog("Please, authorize first!", "Not authorized").ShowAsync();
                Arguments.ShareOperation.ReportCompleted();
            }
        }

        private void RemoveOutboundAttachment(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSending) return;
            OutboundAttachmentViewModel oavm = (sender as FrameworkElement).DataContext as OutboundAttachmentViewModel;
            if (oavm.UploadState == OutboundAttachmentUploadState.InProgress) oavm.CancelUpload();
            ViewModel.Attachments.Remove(oavm);
        }

        private void AddConvToSelectedPeers(object sender, ItemClickEventArgs e)
        {
            if (ViewModel.IsSending) return;
            ConversationViewModel conv = e.ClickedItem as ConversationViewModel;
            if (ViewModel.SelectedPeers.Count >= 10) return;
            var q = ViewModel.SelectedPeers.Where(p => p.Id == conv.Id);
            if (q.Count() == 0)
            {
                Entity entity = new Entity(conv.Id, conv.Title, String.Empty, conv.Avatar);
                entity.ExtraButtonIcon = VKUILibrary.GetIconTemplate(VKIconName.Icon24Cancel);
                entity.ExtraButtonCommand = new Helpers.RelayCommand(c =>
                {
                    if (!ViewModel.IsSending) ViewModel.SelectedPeers.Remove(entity);
                });
                ViewModel.SelectedPeers.Add(entity);
            }
            else
            {
                ViewModel.SelectedPeers.Remove(q.FirstOrDefault());
            }
        }

        private void SearchBoxKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (ViewModel.IsSending) return;
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                ViewModel.SearchConversations();
            }
        }
    }
}