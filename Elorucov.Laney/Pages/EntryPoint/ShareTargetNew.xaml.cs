using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.MyPeople;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Metadata;
using Windows.System.Profile;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Ported from L2.

namespace Elorucov.Laney.Pages.EntryPoint {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShareTargetNew : Page {
        public ShareTargetNew() {
            this.InitializeComponent();
        }

        private ShareTargetViewModel ViewModel { get { return DataContext as ShareTargetViewModel; } }

        ShareTargetActivatedEventArgs Arguments;
        ShareOperation Operation { get { return Arguments?.ShareOperation; } }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            TitleAndStatusBar.ExtendView(true);
            if (Theme.IsMicaAvailable) LayoutRoot.Background = null;
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
                SelectedPeersListView.Margin = new Thickness(0, 24, 0, 0);

            if (e.Parameter != null && e.Parameter is ShareTargetActivatedEventArgs args) {
                Arguments = args;
                Operation.ReportStarted();
                Operation.ReportDataRetrieved();

                API.Initialize(AppParameters.AccessToken, Locale.Get("lang"), ApplicationInfo.UserAgent, AppParameters.ApplicationID, AppParameters.ApplicationSecret, AppParameters.VkApiDomain);
                API.ExchangeToken = AppParameters.ExchangeToken;
                API.WebTokenRefreshed = async (isSuccess, token, expiresIn) => await APIHelper.SaveRefreshedTokenAsync(isSuccess, token, expiresIn);

                CoreApplication.GetCurrentView()?.CoreWindow?.CustomProperties?.Add("type", ContactsPanel.IsContactPanelSupported && Operation.Contacts.Count > 0 ? "copa" : "host");
                if (ContactsPanel.IsContactPanelSupported && Operation.Contacts.Count > 0) {
                    new Action(async () => { await OpenConversationPage(Operation.Contacts[0]); })();
                    return;
                }
            } else {
                Operation.DismissUI();
                return;
            }

            SearchBox.PlaceholderText = Locale.Get("search_placeholder");
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                FindName(nameof(TitleBar));

                var tb = CoreApplication.GetCurrentView().TitleBar;
                TitleBar.Height = tb.Height;
                tb.LayoutMetricsChanged += (a, b) => TitleBar.Height = a.Height;
            }

            KeyEventHandler keh = new KeyEventHandler(SearchBoxKeyDown);
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5)) {
                SearchBox.AddHandler(TextBox.PreviewKeyDownEvent, keh, true);
            } else {
                SearchBox.AddHandler(TextBox.KeyDownEvent, keh, true);
            }

            // Bottom controls(a.k.a.write bar) shadows
            List<UIElement> receivers = new List<UIElement> { LayoutRoot };
            Services.UI.Shadow.TryDrawUsingThemeShadow(BottomControls, BottomControlsShadow, receivers, 6);

            new System.Action(async () => {
                await Task.Delay(100);
                DataContext = new ShareTargetViewModel(Operation);
            })();

            // Если у юзера нет локального пароля,
            // то после присвоения DataCоntext-а
            // фон в Win11 пропадает. 
            // Почему? А фиг его знает...
            App.FixMicaBackground();
        }

        private async Task OpenConversationPage(Contact contact) {
            Log.Info($"{GetType().Name} > Share to contact.");
            ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            try {
                contact = await store.GetContactAsync(contact.Id);
                int id = 0;
                bool p = Int32.TryParse(await ContactsPanel.GetRemoteIdForContactAsync(contact), out id);
                if (p) {
                    Log.Info($"{GetType().Name} > User id: {id}.");
                    var mfvm = new ViewModel.Controls.MessageFormViewModel(id);

                    ConversationView cv = new ConversationView();
                    ConversationViewModel cvm = new ConversationViewModel(id, -1);
                    cvm.MessageFormViewModel = mfvm;
                    mfvm = await DataPackageParser.ParseAsync(Operation.Data, mfvm);
                    cv.DataContext = cvm;
                    (Window.Current.Content as Frame).Content = cv;
                    Log.Verbose($"{GetType().Name} > ConversationView opened.");
                }
            } catch {
                await (new MessageDialog("Please check privacy settings.", "Access denied")).ShowAsync();
            }
        }
        private void RemoveOutboundAttachment(object sender, RoutedEventArgs e) {
            if (ViewModel.IsSending) return;
            OutboundAttachmentViewModel oavm = (sender as FrameworkElement).DataContext as OutboundAttachmentViewModel;
            if (oavm.UploadState == OutboundAttachmentUploadState.InProgress) oavm.CancelUpload();
            ViewModel.Attachments.Remove(oavm);
        }

        private void AddConvToSelectedPeers(object sender, ItemClickEventArgs e) {
            if (ViewModel.IsSending) return;
            LConversation conv = e.ClickedItem as LConversation;
            if (ViewModel.SelectedPeers.Count >= 10) return;
            var q = ViewModel.SelectedPeers.Where(p => p.Id == conv.Id);
            if (q.Count() == 0) {
                Entity entity = new Entity(conv.Id, conv.Title, String.Empty, conv.Photo);
                entity.ExtraButtonCommand = new RelayCommand(c => {
                    if (!ViewModel.IsSending) ViewModel.SelectedPeers.Remove(entity);
                });
                ViewModel.SelectedPeers.Add(entity);
                SelectedPeersListView.ScrollIntoView(entity);
            } else {
                ViewModel.SelectedPeers.Remove(q.FirstOrDefault());
            }
        }

        private void SearchBoxKeyDown(object sender, KeyRoutedEventArgs e) {
            if (ViewModel.IsSending) return;
            if (e.Key == Windows.System.VirtualKey.Enter) {
                e.Handled = true;
                new Action(async () => { await ViewModel.SearchConversationsAsync(); })();
            }
        }
    }
}