using Elorucov.Laney.Services.Common;
using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.PreviewDebug.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class HiddenSettings : Page {
        public HiddenSettings() {
            this.InitializeComponent();
            LoadParameters();
        }

        private void LoadParameters() {
            p01.IsOn = AppParameters.DisplayRamUsage;
            p01.Toggled += (a, b) => AppParameters.DisplayRamUsage = (a as ToggleSwitch).IsOn;

            p02.IsOn = AppParameters.ShowGalleryViewDebugInfo;
            p02.Toggled += (a, b) => AppParameters.ShowGalleryViewDebugInfo = (a as ToggleSwitch).IsOn;

            p03.IsOn = AppParameters.LongPollDebugInfoInStatus;
            p03.Toggled += (a, b) => AppParameters.LongPollDebugInfoInStatus = (a as ToggleSwitch).IsOn;

            p04.IsOn = AppParameters.ShowOnlineApplicationId;
            p04.Toggled += (a, b) => AppParameters.ShowOnlineApplicationId = (a as ToggleSwitch).IsOn;

            p05.IsOn = AppParameters.AdvancedMessageInfo;
            p05.Toggled += (a, b) => AppParameters.AdvancedMessageInfo = (a as ToggleSwitch).IsOn;

            p06.IsOn = AppParameters.EnableSpellCheckForMessageForm;
            p06.Toggled += (a, b) => AppParameters.EnableSpellCheckForMessageForm = (a as ToggleSwitch).IsOn;

            p07.IsOn = AppParameters.OpenVKLinksInLaney;
            p07.Toggled += (a, b) => AppParameters.OpenVKLinksInLaney = (a as ToggleSwitch).IsOn;

            p08.IsOn = AppParameters.ShowDebugItemsInMenu;
            p08.Toggled += (a, b) => AppParameters.ShowDebugItemsInMenu = (a as ToggleSwitch).IsOn;

            p09.IsOn = AppParameters.KeepDeletedMessagesInUI;
            p09.Toggled += (a, b) => AppParameters.KeepDeletedMessagesInUI = (a as ToggleSwitch).IsOn;

            p10.IsOn = AppParameters.ShowStoryViewerDebugInfo;
            p10.Toggled += (a, b) => AppParameters.ShowStoryViewerDebugInfo = (a as ToggleSwitch).IsOn;

            p11.IsOn = AppParameters.FeedDebug;
            p11.Toggled += (a, b) => AppParameters.FeedDebug = (a as ToggleSwitch).IsOn;

            p12.IsOn = AppParameters.LogPushNotifications;
            p12.Toggled += (a, b) => AppParameters.LogPushNotifications = (a as ToggleSwitch).IsOn;

            p13.IsOn = AppParameters.VKMFutureFeatures;
            p13.Toggled += (a, b) => AppParameters.VKMFutureFeatures = (a as ToggleSwitch).IsOn;

            p14.IsOn = AppParameters.StoryClickableStickerBorder;
            p14.Toggled += (a, b) => AppParameters.StoryClickableStickerBorder = (a as ToggleSwitch).IsOn;

            p21.IsOn = AppParameters.CTEForceSolidColor;
            p21.Toggled += (a, b) => AppParameters.CTEForceSolidColor = (a as ToggleSwitch).IsOn;

            p22.IsOn = AppParameters.DontSendMarkAsRead;
            p22.Toggled += (a, b) => AppParameters.DontSendMarkAsRead = (a as ToggleSwitch).IsOn;

            p23.IsOn = AppParameters.DontSendActivity;
            p23.Toggled += (a, b) => AppParameters.DontSendActivity = (a as ToggleSwitch).IsOn;

            p27.IsOn = AppParameters.MessageRenderingPhase;
            p27.Toggled += (a, b) => AppParameters.MessageRenderingPhase = (a as ToggleSwitch).IsOn;

            p28.IsOn = AppParameters.UseLegacyMREBImplForModernWindows;
            p28.Toggled += (a, b) => AppParameters.UseLegacyMREBImplForModernWindows = (a as ToggleSwitch).IsOn;

            p29.IsOn = AppParameters.ShowAllPrivacySettings;
            p29.Toggled += (a, b) => AppParameters.ShowAllPrivacySettings = (a as ToggleSwitch).IsOn;

            p30.IsOn = AppParameters.ForceAudioPlayerModal;
            p30.Toggled += (a, b) => AppParameters.ForceAudioPlayerModal = (a as ToggleSwitch).IsOn;

            p31.IsOn = AppParameters.StickersKeywordsCacheWordsForSticker;
            p31.Toggled += (a, b) => AppParameters.StickersKeywordsCacheWordsForSticker = (a as ToggleSwitch).IsOn;

            p32.IsOn = AppParameters.ContactPanelOnWin11Enabled;
            p32.Toggled += (a, b) => AppParameters.ContactPanelOnWin11Enabled = (a as ToggleSwitch).IsOn;

            p33.IsOn = AppParameters.ThreadSafety;
            p33.Toggled += (a, b) => AppParameters.ThreadSafety = (a as ToggleSwitch).IsOn;

            p34.IsOn = AppParameters.ForceAcrylicBackgroundOnWin11;
            p34.Toggled += (a, b) => AppParameters.ForceAcrylicBackgroundOnWin11 = (a as ToggleSwitch).IsOn;

            p35.IsOn = AppParameters.BackButtonForNavDebug;
            p35.Toggled += (a, b) => AppParameters.BackButtonForNavDebug = (a as ToggleSwitch).IsOn;

            p36.IsOn = AppParameters.DisableAutoLogoff;
            p36.Toggled += (a, b) => AppParameters.DisableAutoLogoff = (a as ToggleSwitch).IsOn;

            SetupNumberTextBox(tb01, AppParameters.ConversationsLoadCount, (v) => AppParameters.ConversationsLoadCount = v);
            SetupNumberTextBox(tb02, AppParameters.MessagesLoadCount, (v) => AppParameters.MessagesLoadCount = v);
            SetupNumberTextBox(tb03, AppParameters.FileUploaderProvider, (v) => AppParameters.FileUploaderProvider = v);
        }

        private void SetupNumberTextBox(TextBox tb, int value, Action<int> valueChanged) {
            tb.Text = value.ToString();
            tb.TextChanging += (a, b) => {
                if (int.TryParse(tb.Text, out value)) {
                    valueChanged?.Invoke(value);
                    tb.BorderBrush = new SolidColorBrush(Colors.Green);
                } else {
                    tb.BorderBrush = new SolidColorBrush(Colors.Red);
                }
            };
        }
    }
}