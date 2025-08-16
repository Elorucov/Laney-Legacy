using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.PushNotifications;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Methods;
using System;
using System.Collections.Generic;
using VK.VKUI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.SettingsPages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Notifications : Page {
        public Notifications() {
            this.InitializeComponent();
            var host = Main.GetCurrent();
            BackButton.Visibility = host.IsWideMode ? Visibility.Collapsed : Visibility.Visible;
            host.SizeChanged += Host_SizeChanged;
            Unloaded += (a, b) => host.SizeChanged -= Host_SizeChanged;
        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e) {
            BackButton.Visibility = Main.GetCurrent().IsWideMode ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            Main.GetCurrent().GoBack();
        }

        bool isLoaded = false;
        private void LoadSettings(object sender, RoutedEventArgs e) {
            notifs.IsOn = AppParameters.Notifications == 1;
            SwitchNotificationType(AppParameters.Notifications);
            notifs.Toggled += async (a, b) => {
                int type = (a as ToggleSwitch).IsOn ? 1 : 0;
                AppParameters.Notifications = type;
                SwitchNotificationType(type);

                object req = await Execute.RegisterDevice();
                if (req is RegisterDeviceResult result) {
                    Tips.Show(Locale.Get("settings_saved"), $"U: {result.Unregistered}; R: {result.Registered}");
                } else {
                    Functions.ShowHandledErrorTip(req);
                }
            };

            replyMsg.IsOn = AppParameters.SendMessageWithReplyFromToast;
            replyMsg.Toggled += (a, b) => AppParameters.SendMessageWithReplyFromToast = (a as ToggleSwitch).IsOn;

            InitPushSettngs();
            isLoaded = true;
        }

        private void SwitchNotificationType(int type) {
            Reply.Visibility = type == 1 ? Visibility.Visible : Visibility.Collapsed;
            if (AppSession.PushSettings != null) NotifSettings.Visibility = type == 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InitPushSettngs() {
            if (AppSession.PushSettings == null) return;
            SetupSetting(AppSession.PushSettings.Message, nsMsg, nsMsgSound, nsMsgContent);
            SetupSetting(AppSession.PushSettings.Chat, nsChat, nsChatSound, nsChatContent);
        }

        private void SetupSetting(List<string> settings, ToggleSwitch main, ToggleSwitch sound, ToggleSwitch content) {
            if (settings.Contains("off")) main.IsOn = false;
            if (settings.Contains("on")) main.IsOn = true;
            sound.IsOn = !settings.Contains("no_sound");
            content.IsOn = !settings.Contains("no_text");
        }

        private void ChangeMsgPushSettings(object sender, RoutedEventArgs e) {
            if (!isLoaded) return;
            List<string> values = new List<string>();
            if (nsMsg.IsOn) {
                values.Add("on");
                if (!nsMsgSound.IsOn) values.Add("no_sound");
                if (!nsMsgContent.IsOn) values.Add("no_text");
            } else {
                values.Add("off");
            }
            new System.Action(async () => {
                string did = VKNotificationHelper.GetDeviceId();
                ScreenSpinner<object> ssp = new ScreenSpinner<object>();
                object response = await ssp.ShowAsync(Account.SetPushSettings(did, "msg", String.Join(",", values)));
                Functions.ShowHandledErrorDialog(response);
                if (response is bool b && b) AppSession.PushSettings.Message = values;
            })();
        }

        private void ChangeChatPushSettings(object sender, RoutedEventArgs e) {
            if (!isLoaded) return;
            List<string> values = new List<string>();
            if (nsChat.IsOn) {
                values.Add("on");
                if (!nsChatSound.IsOn) values.Add("no_sound");
                if (!nsChatContent.IsOn) values.Add("no_text");
            } else {
                values.Add("off");
            }

            new System.Action(async () => {
                string did = VKNotificationHelper.GetDeviceId();
                ScreenSpinner<object> ssp = new ScreenSpinner<object>();
                object response = await ssp.ShowAsync(Account.SetPushSettings(did, "chat", String.Join(",", values)));
                Functions.ShowHandledErrorDialog(response);
                if (response is bool b && b) AppSession.PushSettings.Chat = values;
            })();
        }
    }
}