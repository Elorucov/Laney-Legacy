using System;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Elorucov.Laney.Core
{
    public class Settings
    {
        private static ApplicationDataContainer adc = ApplicationData.Current.LocalSettings;

        public delegate void SettingChangedDelegate(string name, object value);
        public static event SettingChangedDelegate SettingChanged;

        public static T Get<T>(string key, T defaultValue = default)
        {
            try
            {
                object v = adc.Values[key];
                return v != null ? (T)adc.Values[key] : defaultValue;
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }

        public static void Set(string key, object value)
        {
            adc.Values[key] = value;
            SettingChanged?.Invoke(key, value);
        }

        private static string Caller([CallerMemberName] string memberName = "")
        {
            return memberName;
        }

        #region App settings

        // General
        public static bool SendMessageViaCtrlEnter { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool DontParseLinks { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool DisableMentions { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool SuggestStickers { get { return Get<bool>(Caller(), true); } set { Set(Caller(), value); } }
        public static bool AnimatedStickers { get { return Get<bool>(Caller(), true); } set { Set(Caller(), value); } }

        // Appearance
        public static int Theme { get { return Get<int>(Caller(), 1); } set { Set(Caller(), value); } } // 0 — auto; 1 — sys; 2 — light; 3 — dark
        public static double MessageFontSize { get { return Get<double>(Caller(), 16); } set { Set(Caller(), value); } }
        public static int ChatBackgroundType { get { return Get<int>(Caller(), 0); } set { Set(Caller(), value); } } // 0 — none, 1 — color, 2 — image
        public static string ChatBackground { get { return Get<string>(Caller(), Constants.DefaultChatBackgroundColor); } set { Set(Caller(), value); } } // for color — #RRGGBB; for image — ms-appx:///.../Image.png
        public static bool ChatBackgroundImageStretch { get { return Get<bool>(Caller(), true); } set { Set(Caller(), value); } } // true — uniform-to-fill; false — repeat/tile

        // Privacy

        public static bool UseWindowsHello { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }

        // Audio player
        public static bool AudioPlayerIsLoopingEnabled { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }

        // Debug
        public static bool AlternativeUploadMethod { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool DebugShowMessageIdCtx { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool DebugShowMessagesListScrollInfo { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool DebugDisplayRAMUsage { get { return Get<bool>(Caller(), true); } set { Set(Caller(), value); } }
        public static bool FailFastOnErrors { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool ShowAllPrivacySettings { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool KeepLogsAfterLogout { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool UseYandexMaps { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool MessageBubbleTemplateLoadMethod { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool StoryViewerSlowDownAnimation { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool StoryViewerNoLightThemeForFlyouts { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool StoryViewerClickableStickerBorder { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool LottieViewDebug { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }
        public static bool DontSendActivity { get { return Get<bool>(Caller()); } set { Set(Caller(), value); } }

        #endregion
    }
}