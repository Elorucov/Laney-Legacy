using Windows.Storage;

namespace Elorucov.Laney.Services.Common {
    public class AppParameters {
        public static int ApplicationID { get => 6614620; }        // App ID from https://vk.com/editapp?id={ApplicationID}&section=options
        public static string ApplicationSecret { get => "***"; }   // Secure key from https://vk.com/editapp?id={ApplicationID}&section=options
        public static int Scope { get => 471126; }

        //

        #region Parameters constants
        const string id = "id";
        const string at = "at";
        const string et = "et";
        const string wat = "wat";
        const string watexp = "watexp";
        const string vkmwebid = "vkm_web_id";
        const string cid = "cid";
        const string cs = "cs";
        const string name = "name";
        const string avatar = "avatar";
        const string atf = "atf";
        const string vkapidomain = "vkapidomain";
        const string galleryViewDebug = "dbg_gvdi";
        const string msgEnterBtn = "msg_enter";
        const string msgDontParseLinks = "msg_nolinks";
        const string msgDisableMentions = "msg_nomentions";
        const string msgSilent = "msg_silent";
        const string advLog = "advlog";
        const string pushLog = "pushlog";
        const string alternativeUpload = "file_uploader_provider";
        const string uisTheme = "uis_theme";
        const string uisMsgFontSize = "uis_msg_font_size";
        const string showAppId = "show_app_id";
        const string msgAdvInfo = "msg_adv_info";
        const string openvklink = "open_vk_link_in_laney";
        const string debugItemsCtxMenu = "debug_items_ctxmenu";
        const string keepDeletedMessagesInUI = "keep_deleted_messages_in_ui";
        const string convLoadCount = "conv_load_count";
        const string msgLoadCount = "msg_load_count";
        const string notifications = "notifications";
        const string toastSendMsgWithReply = "toast_send_msg_with_reply";
        const string passcode = "passcode";
        const string winhello = "winhello";
        const string stickerskeywords = "stickers_keywords";
        const string stkwcache = "stickers_keywords_cache";
        const string hintMsgFlyout = "hint_msg_send_flyout";
        const string hintChatDescFlyout = "hint_chat_description_flyout";
        const string testlog1 = "testlog_toast_background";
        const string testMsgFormSpellCheckEnabled = "test_msg_form_spell_check_enabled";
        const string testStoryClickableStickerBorder = "test_story_clickable_sticker_border";
        const string storyViewerDebug = "dbg_storyviewer";
        const string animatedStickers = "animated_stickers";
        const string showChatJoinDate = "show_chat_join_date";
        const string dbgRam = "dbg_ram";
        const string cteStyle = "cte_style";
        const string chatBackground = "chat_background";
        const string cteAccent = "cte_accent";
        const string cteIgnoreChatTheme = "cte_ignore_chat_theme";
        const string cteForceSolidColor = "cte_force_solid_color";
        const string nechitalka = "nechitalka";
        const string nepisalka = "nepisalka";
        const string webTokenSupport = "web_token_support";
        const string specEventTooltips = "special_event_tooltips";
        const string msgRenderingPhase = "msg_rendering_phase";
        const string privacyAll = "privacy_all";
        const string forceAudioPlayerModal = "force_audio_player_modal";
        const string contactPanelWin11 = "my_people_win11";
        const string ths = "ths";
        const string ctlsrc = "ctlsrc";
        const string altEmojiFont = "alt_emoji_font";
        const string lpDebugStatus = "lp_debug_status";
        const string statSent = "stat_sent";
        const string convMultiWindow = "conv_multi_window";
        const string disableAutoLogoff = "disable_auto_logoff";
        const string contactsPanelInteracted = "contacts_panel_interacted";
        const string foldersPlacement = "folders_placement";
        const string vkmff = "vkmff";
        const string chatsListLines = "chats_list_lines";
        const string audioLoop = "audio_loop";
        const string lastMsgPreview = "last_msg_preview";
        const string thpos = "thpos";
        const string lpFix = "lp_fix";
        const string quickReactions = "quick_reactions";
        const string frid = "frid";
        const string textSelection = "text_selection";
        const string messagesStats = "messages_statistics";
        const string bb = "bb";
        const string fdbg = "fdbg";
        const string nomica = "nomica";
        const string messageRichEditBoxImpl = "mreb_impl";
        #endregion

        private static ApplicationDataContainer adc = ApplicationData.Current.LocalSettings;
        public static void Reset() {
            var values = adc.Values;
            foreach (var item in values) {
                adc.Values.Remove(item);
            }
        }

        public static long UserID {
            get { return adc.Values[id] != null && adc.Values[id] is long ? (long)adc.Values[id] : 0; }
            set { adc.Values[id] = value; }
        }

        public static string AccessToken {
            get { return adc.Values[at] != null ? adc.Values[at].ToString() : null; }
            set { adc.Values[at] = value; }
        }

        public static string WebToken {
            get { return adc.Values[wat] != null ? adc.Values[wat].ToString() : null; }
            set { adc.Values[wat] = value; }
        }

        public static string ExchangeToken {
            get { return adc.Values[et] != null ? adc.Values[et].ToString() : null; }
            set { adc.Values[et] = value; }
        }

        public static long WebTokenExpires {
            get { return adc.Values[watexp] != null && adc.Values[watexp] is long ? (long)adc.Values[watexp] : 0; }
            set { adc.Values[watexp] = value; }
        }

        public static bool UseWebVKMID {
            get { return adc.Values[vkmwebid] != null ? (bool)adc.Values[vkmwebid] : false; }
            set { adc.Values[vkmwebid] = value; }
        }

        public static string UserName {
            get { return adc.Values[name] != null ? adc.Values[name].ToString() : null; }
            set { adc.Values[name] = value; }
        }

        public static string UserAvatar {
            get { return adc.Values[avatar] != null ? adc.Values[avatar].ToString() : null; }
            set { adc.Values[avatar] = value; }
        }

        public static bool DebugMenuUsed {
            get { return adc.Values[atf] != null ? (bool)adc.Values[atf] : false; }
            set { adc.Values[atf] = value; }
        }

        //

        public static string VkApiDomain {
            get { return adc.Values[vkapidomain] != null ? adc.Values[vkapidomain].ToString() : null; }
            set { adc.Values[vkapidomain] = value; }
        }

        // При нажатии на Enter, если false — новая строка.
        public static bool MessageSendEnterButtonMode {
            get { return adc.Values[msgEnterBtn] != null ? (bool)adc.Values[msgEnterBtn] : true; }
            set { adc.Values[msgEnterBtn] = value; }
        }

        // Don't parse links
        public static bool MessageSendDontParseLinks {
            get { return adc.Values[msgDontParseLinks] != null ? (bool)adc.Values[msgDontParseLinks] : false; }
            set { adc.Values[msgDontParseLinks] = value; }
        }

        // Disable mentions
        public static bool MessageSendDisableMentions {
            get { return adc.Values[msgDisableMentions] != null ? (bool)adc.Values[msgDisableMentions] : false; }
            set { adc.Values[msgDisableMentions] = value; }
        }

        // Silent
        public static bool MessageSendSilent {
            get { return adc.Values[msgSilent] != null ? (bool)adc.Values[msgSilent] : false; }
            set { adc.Values[msgSilent] = value; }
        }

        // Long poll fix
        public static bool LongPollFix {
            get { return adc.Values[lpFix] != null ? (bool)adc.Values[lpFix] : false; }
            set { adc.Values[lpFix] = value; }
        }

        // Выделение текста сообщения

        public static bool IsTextSelectionEnabled {
            get { return adc.Values[textSelection] != null ? (bool)adc.Values[textSelection] : false; }
            set { adc.Values[textSelection] = value; }
        }

        // Passcode
        public static string Passcode {
            get { return adc.Values[passcode] != null ? adc.Values[passcode].ToString() : null; }
            set { adc.Values[passcode] = value; }
        }

        // Use Windows Hello
        public static bool WindowsHelloInsteadPasscode {
            get { return adc.Values[winhello] != null ? (bool)adc.Values[winhello] : false; }
            set { adc.Values[winhello] = value; }
        }

        // Stickers keywords
        public static bool StickersKeywordsEnabled {
            get { return adc.Values[stickerskeywords] != null ? (bool)adc.Values[stickerskeywords] : true; }
            set { adc.Values[stickerskeywords] = value; }
        }

        public static bool StickersKeywordsCacheWordsForSticker {
            get { return adc.Values[stkwcache] != null ? (bool)adc.Values[stkwcache] : false; }
            set { adc.Values[stkwcache] = value; }
        }

        // Animated stickers
        public static bool AnimatedStickers {
            get { return adc.Values[animatedStickers] != null ? (bool)adc.Values[animatedStickers] : true; }
            set { adc.Values[animatedStickers] = value; }
        }

        // UI theme
        public static int Theme {
            get { return adc.Values[uisTheme] != null && adc.Values[uisTheme] is int ? (int)adc.Values[uisTheme] : 0; }
            set { adc.Values[uisTheme] = value; }
        }

        // UI accent
        //public static Color Accent {
        //    get { return adc.Values[uisAccent] != null && adc.Values[uisAccent] is string ? UI.Theme.ParseColor((string)adc.Values[uisAccent]) : Color.FromArgb(0, 0, 0, 0); }
        //    set { adc.Values[uisAccent] = $"{value.R},{value.G},{value.B}"; }
        //}

        // CTE: predefined style id
        public static string CTEStyle {
            get { return adc.Values[cteStyle] != null && adc.Values[cteStyle] is string s ? s : null; }
            set { adc.Values[cteStyle] = value; }
        }

        // CTE: custom background
        public static string ChatBackground {
            get { return adc.Values[chatBackground] != null && adc.Values[chatBackground] is string s ? s : null; }
            set { adc.Values[chatBackground] = value; }
        }

        // CTE: custom predefined accent & gradients (appearance id)
        public static string CTEAccent {
            get { return adc.Values[cteAccent] != null && adc.Values[cteAccent] is string s ? s : null; }
            set { adc.Values[cteAccent] = value; }
        }

        // CTE: ignore chat theme (force use own local style)
        public static bool CTEIgnoreChatTheme {
            get { return adc.Values[cteIgnoreChatTheme] != null ? (bool)adc.Values[cteIgnoreChatTheme] : false; }
            set { adc.Values[cteIgnoreChatTheme] = value; }
        }

        // Lines in chats list item (true — 3, false — 2)
        public static bool ChatsListLines {
            get { return adc.Values[chatsListLines] != null ? (bool)adc.Values[chatsListLines] : false; }
            set { adc.Values[chatsListLines] = value; }
        }

        // Last message preview
        public static bool LastMessagePreview {
            get { return adc.Values[lastMsgPreview] != null ? (bool)adc.Values[lastMsgPreview] : false; }
            set { adc.Values[lastMsgPreview] = value; }
        }

        // Images previews (thumbs) position. (true — thumbs and message text, false — message text and thumbs)
        public static bool ThumbsPosition {
            get { return adc.Values[thpos] != null ? (bool)adc.Values[thpos] : false; }
            set { adc.Values[thpos] = value; }
        }

        // UI message font size
        public static double MessageFontSize {
            get { return adc.Values[uisMsgFontSize] != null && adc.Values[uisMsgFontSize] is double ? (double)adc.Values[uisMsgFontSize] : 16; }
            set { adc.Values[uisMsgFontSize] = value; }
        }

        // Quick reactions
        public static string QuickReactions {
            get { return adc.Values[quickReactions] != null ? (string)adc.Values[quickReactions] : string.Empty; }
            set { adc.Values[quickReactions] = value; }
        }

        // Quick reactions
        public static int FastReactionId {
            get { return adc.Values[frid] != null && adc.Values[frid] is int ? (int)adc.Values[frid] : 0; }
            set { adc.Values[frid] = value; }
        }

        // Folders placement (true — vertical, false — horizontal)
        public static bool FoldersPlacement {
            get { return adc.Values[foldersPlacement] != null ? (bool)adc.Values[foldersPlacement] : false; }
            set { adc.Values[foldersPlacement] = value; }
        }

        // Alternative emoji font
        public static bool AlternativeEmojiFont {
            get { return adc.Values[altEmojiFont] != null ? (bool)adc.Values[altEmojiFont] : false; }
            set { adc.Values[altEmojiFont] = value; }
        }

        // Notifications
        public static int Notifications {
            get { return adc.Values[notifications] != null && adc.Values[notifications] is int ? (int)adc.Values[notifications] : 1; }
            set { adc.Values[notifications] = value; }
        }

        public static bool SendMessageWithReplyFromToast {
            get { return adc.Values[toastSendMsgWithReply] != null ? (bool)adc.Values[toastSendMsgWithReply] : true; }
            set { adc.Values[toastSendMsgWithReply] = value; }
        }

        // Experimental
        public static bool AdvancedLogging {
            get { return adc.Values[advLog] != null ? (bool)adc.Values[advLog] : false; }
            set { adc.Values[advLog] = value; }
        }

        public static bool LogPushNotifications {
            get { return adc.Values[pushLog] != null ? (bool)adc.Values[pushLog] : false; }
            set { adc.Values[pushLog] = value; }
        }

        public static bool ShowGalleryViewDebugInfo {
            get { return adc.Values[galleryViewDebug] != null ? (bool)adc.Values[galleryViewDebug] : false; }
            set { adc.Values[galleryViewDebug] = value; }
        }

        public static int FileUploaderProvider {
            get { return adc.Values[alternativeUpload] != null ? (int)adc.Values[alternativeUpload] : 0; }
            set { adc.Values[alternativeUpload] = value; }
        }

        public static bool ShowOnlineApplicationId {
            get { return adc.Values[showAppId] != null ? (bool)adc.Values[showAppId] : false; }
            set { adc.Values[showAppId] = value; }
        }

        public static bool AdvancedMessageInfo {
            get { return adc.Values[msgAdvInfo] != null ? (bool)adc.Values[msgAdvInfo] : false; }
            set { adc.Values[msgAdvInfo] = value; }
        }

        public static bool OpenVKLinksInLaney {
            get { return adc.Values[openvklink] != null ? (bool)adc.Values[openvklink] : false; }
            set { adc.Values[openvklink] = value; }
        }

        public static bool ShowDebugItemsInMenu {
            get { return adc.Values[debugItemsCtxMenu] != null ? (bool)adc.Values[debugItemsCtxMenu] : false; }
            set { adc.Values[debugItemsCtxMenu] = value; }
        }

        public static bool KeepDeletedMessagesInUI {
            get { return adc.Values[keepDeletedMessagesInUI] != null ? (bool)adc.Values[keepDeletedMessagesInUI] : false; }
            set { adc.Values[keepDeletedMessagesInUI] = value; }
        }

        public static int ConversationsLoadCount {
            get { return adc.Values[convLoadCount] != null && adc.Values[convLoadCount] is int ? (int)adc.Values[convLoadCount] : 40; }
            set { adc.Values[convLoadCount] = value; }
        }

        public static int MessagesLoadCount {
            get { return adc.Values[msgLoadCount] != null && adc.Values[msgLoadCount] is int ? (int)adc.Values[msgLoadCount] : 40; }
            set { adc.Values[msgLoadCount] = value; }
        }

        public static string TestLogBackgroundToast {
            get { return adc.Values[testlog1] != null ? adc.Values[testlog1].ToString() : null; }
            set { adc.Values[testlog1] = value; }
        }

        public static bool EnableSpellCheckForMessageForm {
            get { return adc.Values[testMsgFormSpellCheckEnabled] != null ? (bool)adc.Values[testMsgFormSpellCheckEnabled] : true; }
            set { adc.Values[testMsgFormSpellCheckEnabled] = value; }
        }

        public static bool StoryClickableStickerBorder {
            get { return adc.Values[testStoryClickableStickerBorder] != null ? (bool)adc.Values[testStoryClickableStickerBorder] : false; }
            set { adc.Values[testStoryClickableStickerBorder] = value; }
        }

        public static bool ShowStoryViewerDebugInfo {
            get { return adc.Values[storyViewerDebug] != null ? (bool)adc.Values[storyViewerDebug] : false; }
            set { adc.Values[storyViewerDebug] = value; }
        }

        public static bool ShowChatJoinDate {
            get { return adc.Values[showChatJoinDate] != null ? (bool)adc.Values[showChatJoinDate] : false; }
            set { adc.Values[showChatJoinDate] = value; }
        }

        public static bool DisplayRamUsage {
            get { return adc.Values[dbgRam] != null ? (bool)adc.Values[dbgRam] : false; }
            set { adc.Values[dbgRam] = value; }
        }

        public static bool CTEForceSolidColor {
            get { return adc.Values[cteForceSolidColor] != null ? (bool)adc.Values[cteForceSolidColor] : false; }
            set { adc.Values[cteForceSolidColor] = value; }
        }

        public static bool DontSendMarkAsRead {
            get { return adc.Values[nechitalka] != null ? (bool)adc.Values[nechitalka] : false; }
            set { adc.Values[nechitalka] = value; }
        }

        public static bool DontSendActivity {
            get { return adc.Values[nepisalka] != null ? (bool)adc.Values[nepisalka] : false; }
            set { adc.Values[nepisalka] = value; }
        }

        public static bool WebTokenSupport {
            get { return true; }
            set { adc.Values[webTokenSupport] = value; }
        }

        public static bool MessageRenderingPhase {
            get { return adc.Values[msgRenderingPhase] != null ? (bool)adc.Values[msgRenderingPhase] : false; }
            set { adc.Values[msgRenderingPhase] = value; }
        }

        public static bool ShowAllPrivacySettings {
            get { return adc.Values[privacyAll] != null ? (bool)adc.Values[privacyAll] : false; }
            set { adc.Values[privacyAll] = value; }
        }

        public static bool ForceAudioPlayerModal {
            get { return adc.Values[forceAudioPlayerModal] != null ? (bool)adc.Values[forceAudioPlayerModal] : false; }
            set { adc.Values[forceAudioPlayerModal] = value; }
        }

        public static bool ContactPanelOnWin11Enabled {
            get { return adc.Values[contactPanelWin11] != null ? (bool)adc.Values[contactPanelWin11] : false; }
            set { adc.Values[contactPanelWin11] = value; }
        }

        public static bool ThreadSafety {
            get { return adc.Values[ths] != null ? (bool)adc.Values[ths] : true; }
            set { adc.Values[ths] = value; }
        }

        public static string ChatThemesListSource {
            get { return adc.Values[ctlsrc] != null && adc.Values[ctlsrc] is string s ? s : "https://elorucov.github.io/laney/v1/chat_themes.json"; }
            set { adc.Values[ctlsrc] = value; }
        }

        public static bool LongPollDebugInfoInStatus {
            get { return adc.Values[lpDebugStatus] != null ? (bool)adc.Values[lpDebugStatus] : false; }
            set { adc.Values[lpDebugStatus] = value; }
        }

        public static bool ConvMultiWindow {
            get { return adc.Values[convMultiWindow] != null ? (bool)adc.Values[convMultiWindow] : false; }
            set { adc.Values[convMultiWindow] = value; }
        }

        public static bool DisableAutoLogoff {
            get { return adc.Values[disableAutoLogoff] != null ? (bool)adc.Values[disableAutoLogoff] : false; }
            set { adc.Values[disableAutoLogoff] = value; }
        }

        public static bool VKMFutureFeatures {
            get { return adc.Values[vkmff] != null ? (bool)adc.Values[vkmff] : false; }
            set { adc.Values[vkmff] = value; }
        }

        public static bool MessagesStatsFeature {
            get { return adc.Values[messagesStats] != null ? (bool)adc.Values[messagesStats] : true; }
            set { adc.Values[messagesStats] = value; }
        }

        public static bool BackButtonForNavDebug {
            get { return adc.Values[bb] != null ? (bool)adc.Values[bb] : false; }
            set { adc.Values[bb] = value; }
        }

        public static bool FeedDebug {
            get { return adc.Values[fdbg] != null ? (bool)adc.Values[fdbg] : false; }
            set { adc.Values[fdbg] = value; }
        }

        public static bool ForceAcrylicBackgroundOnWin11 {
            get { return adc.Values[nomica] != null ? (bool)adc.Values[nomica] : false; }
            set { adc.Values[nomica] = value; }
        }

        public static bool UseLegacyMREBImplForModernWindows {
            get { return adc.Values[messageRichEditBoxImpl] != null ? (bool)adc.Values[messageRichEditBoxImpl] : false; }
            set { adc.Values[messageRichEditBoxImpl] = value; }
        }

        // Hints

        public static bool HintMessageSendFlyout {
            get { return adc.Values[hintMsgFlyout] != null ? (bool)adc.Values[hintMsgFlyout] : false; }
            set { adc.Values[hintMsgFlyout] = value; }
        }

        public static bool HintChatDescriptionFlyout {
            get { return adc.Values[hintChatDescFlyout] != null ? (bool)adc.Values[hintChatDescFlyout] : false; }
            set { adc.Values[hintChatDescFlyout] = value; }
        }

        public static string SpecialEventTooltips {
            get { return adc.Values[specEventTooltips] != null ? adc.Values[specEventTooltips].ToString() : null; }
            set { adc.Values[specEventTooltips] = value; }
        }

        // Internal

        public static bool StatSent {
            get { return adc.Values[statSent] != null ? (bool)adc.Values[statSent] : false; }
            set { adc.Values[statSent] = value; }
        }

        public static bool ContactsPanelInteracted {
            get { return adc.Values[contactsPanelInteracted] != null ? (bool)adc.Values[contactsPanelInteracted] : false; }
            set { adc.Values[contactsPanelInteracted] = value; }
        }

        public static int VKMApplicationID {
            get { return adc.Values[cid] != null && adc.Values[cid] is int i ? i : 51453752; }
            set { adc.Values[cid] = value; }
        }

        public static string VKMSecret {
            get { return adc.Values[cs] != null && adc.Values[cs] is string s ? s : "4UyuCUsdK8pVCNoeQuGi"; }
            set { adc.Values[cs] = value; }
        }

        // Misc

        public static bool AudioPlayerRepeat {
            get { return adc.Values[audioLoop] != null ? (bool)adc.Values[audioLoop] : false; }
            set { adc.Values[audioLoop] = value; }
        }
    }
}