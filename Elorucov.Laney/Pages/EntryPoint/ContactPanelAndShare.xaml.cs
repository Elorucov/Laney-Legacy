using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.LongPoll;
using Elorucov.Laney.Services.MyPeople;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.EntryPoint {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ContactPanelAndShare : Page {
        Windows.ApplicationModel.Contacts.Contact contact;
        ContactPanel cpanel;

        public ContactPanelAndShare() {
            this.InitializeComponent();
            CoreApplication.GetCurrentView().CoreWindow.CustomProperties.Add("type", "copa");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (e.Parameter is ContactPanelActivatedEventArgs) {
                API.Initialize(AppParameters.AccessToken, Locale.Get("lang"), ApplicationInfo.UserAgent, AppParameters.ApplicationID, AppParameters.ApplicationSecret, AppParameters.VkApiDomain);
                API.ExchangeToken = AppParameters.ExchangeToken;
                API.WebTokenRefreshed = async (isSuccess, token, expiresIn) => await APIHelper.SaveRefreshedTokenAsync(isSuccess, token, expiresIn);

                ContactPanelActivatedEventArgs args = e.Parameter as ContactPanelActivatedEventArgs;
                contact = args.Contact;
                cpanel = args.ContactPanel;
                Log.Info($"{GetType().Name} > IsHosted: {CoreApplication.GetCurrentView().IsHosted}, IsComponent: {CoreApplication.GetCurrentView().IsComponent}");
                new System.Action(async () => { await Load(); })();
            }
        }

        private async Task Load() {
            Log.Info($"{GetType().Name} > Contact RemoteId: {contact.RemoteId}.");
            Tips.Init(LayoutRoot);
            await ChatThemeService.LoadLocalThemes();

            try {
                ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                contact = await store.GetContactAsync(contact.Id);

                long id = 0;
                bool p = Int64.TryParse(await ContactsPanel.GetRemoteIdForContactAsync(contact), out id);
                Log.Info($"{GetType().Name} > GetRemoteIdForContactAsync: {id}.");
                if (p && id.IsUser()) {
                    Log.Info($"{GetType().Name} > User id: {id}.");
                    await ChatThemeService.InitThemes();

                    object st = await Execute.Startup();
                    if (st is StartupInfo inf) {
                        AppParameters.ChatThemesListSource = inf.ChatThemesListSource;
                        AppSession.MessagesTranslationLanguagePairs = inf.MessagesTranslationLanguagePairs;
                        AppSession.PushSettings = inf.PushSettings;
                        AppSession.ReactionsAssets = inf.ReactionsAssets;
                        AppSession.AvailableReactions = inf.AvailableReactions;
                        AppSession.AddUsersToCache(inf.Users);

                        AppSession.ActiveStickerPacks = inf.StickerProductIds;
                        await TryStartLongpoll(inf.LongPoll);
                    } else {
                        Functions.ShowHandledErrorDialog(st);
                        cpanel.ClosePanel();
                    }

                    object vmsresp = await Messages.GetVideoMessageShapes();
                    if (vmsresp is VideoMessageShapesResponse vms) AppSession.VideoMessageShapes = vms;

                    var cvm = new ConversationViewModel(id, -1);
                    frm.Navigate(typeof(ConversationView), cvm);
                    cvm.OnOpened(-1);

                    Log.Verbose($"{GetType().Name} > ConversationView opened.");
                } else {
                    throw new ArgumentException($"Incorrect remote ID for contact!");
                }
            } catch (Exception ex) {
                Log.Error(ex, $"{GetType().Name} > Error!");
                await ShowError($"Error (0x{ex.HResult.ToString("x8")})", $"{ex.Message}\n\nStack trace:\n{ex.StackTrace}");
            }
        }

        private async Task TryStartLongpoll(LongPollServerInfo info) {
            Log.Info($"{GetType().Name} > Init longpoll...");
            var ka = await LongPoll.InitLongPoll(info);
            if (!ka) {
                Log.Warn($"{GetType().Name} > Longpoll initialization error!");
            } else {
                Log.Info($"{GetType().Name} > LP initialized.");
            }
        }

        private async Task ShowError(string title, string subtitle = "") {
            await new ContentDialog { Title = title, Content = subtitle, PrimaryButtonText = "OK" }.ShowAsync();
        }
    }
}