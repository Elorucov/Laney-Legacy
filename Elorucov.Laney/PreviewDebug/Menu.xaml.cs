using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.PreviewDebug {
    public class DebugMenuItem {
        public string Name { get; private set; }
        public Type Page { get; private set; }
        public Action Action { get; set; }

        public DebugMenuItem(string name, Type page, Action action = null) {
            Name = name; Page = page; Action = action;
        }
    }

    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Menu : Page {
        public Menu() {
            this.InitializeComponent();
            AppParameters.DebugMenuUsed = true;
            uid.Text = $"Current session (user): {AppParameters.UserName}. ID: {AppParameters.UserID}";
            Log.Verbose($"{GetType().Name} > loading...");
            if (!API.Initialized) API.Initialize(AppParameters.AccessToken, Locale.Get("lang"), ApplicationInfo.UserAgent, AppParameters.VKMApplicationID, AppParameters.VKMSecret, AppParameters.VkApiDomain);
            API.WebToken = AppParameters.WebToken;
            API.ExchangeToken = AppParameters.ExchangeToken;
            API.WebTokenRefreshed = async (isSuccess, token, expiresIn) => await APIHelper.SaveRefreshedTokenAsync(isSuccess, token, expiresIn);

            List<DebugMenuItem> items = new List<DebugMenuItem> {
                new DebugMenuItem("Experimental settings", typeof(Pages.HiddenSettings)),
                new DebugMenuItem("VK API playground", typeof(Pages.APICall)),
                new DebugMenuItem("LongPoll tests", typeof(Pages.LongPollTest)),
                new DebugMenuItem("Message UI rendering test", typeof(Pages.MessageUITest)),
                new DebugMenuItem("File uploader", typeof(Pages.FileUploader)),
                new DebugMenuItem("XAML test", typeof(Pages.XAMLTest)),
                new DebugMenuItem("Audio recorder tests", typeof(Pages.AudioRecorderTest)),
                new DebugMenuItem("Check sessions", null, async () => await CheckSessionsAsync()),
                new DebugMenuItem("Reset hints", null,  async () => await ResetHintsAsync())
            };
            menu.ItemsSource = items;

            Loaded += (y, z) => {
                SystemNavigationManager navmgr = SystemNavigationManager.GetForCurrentView();
                navmgr.BackRequested += (a, b) => {
                    if (Frame.CanGoBack) {
                        b.Handled = true;
                        Frame.GoBack(App.DefaultBackNavTransition);
                    }
                };
                Frame.Navigated += (a, b) => {
                    navmgr.AppViewBackButtonVisibility = Frame.BackStackDepth <= 0 ? AppViewBackButtonVisibility.Collapsed : AppViewBackButtonVisibility.Visible;
                };
                TitleAndStatusBar.ExtendView(false);
            };
        }

        private async Task ResetHintsAsync() {
            AppParameters.HintMessageSendFlyout = false;
            AppParameters.HintChatDescriptionFlyout = false;
            AppParameters.SpecialEventTooltips = string.Empty;
            await new MessageDialog("Ok!").ShowAsync();
        }

        private async Task CheckSessionsAsync() {
            List<VKSession> sessions = await VKSessionManager.GetSessionsAsync();
            string info = string.Empty;

            sessions.ForEach(s => {
                info += $"{s.Id}: {s.Name}\n";
            });

            ContentDialog cd = new ContentDialog {
                Title = "Sessions",
                Content = info,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            await cd.ShowAsync();
        }

        private void menu_ItemClick(object sender, ItemClickEventArgs e) {
            if (e.ClickedItem is DebugMenuItem) {
                DebugMenuItem item = e.ClickedItem as DebugMenuItem;
                if (item.Action == null) {
                    Frame.Navigate(item.Page, null, App.DefaultNavTransition);
                } else {
                    item.Action.Invoke();
                }
            }
        }
    }
}