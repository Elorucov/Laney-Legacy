using Windows.Globalization;

namespace LaneyWidgets {
    internal static class AppLang {
        internal static string GetCurrentLang() {
            var lang = ApplicationLanguages.PrimaryLanguageOverride;
            if (String.IsNullOrEmpty(lang)) lang = ApplicationLanguages.Languages.FirstOrDefault();
            if (lang.Contains("-")) lang = lang.Split('-')[0];
            return lang;
        }

        internal static string Get(string key) {
            string lang = GetCurrentLang();
            if (!Keys.ContainsKey(lang)) return $"%{lang}.{key}%";

            var keys = Keys[lang];
            if (!keys.ContainsKey(key)) return $"%{key}%";

            return keys[key];
        }

        internal static Dictionary<string, Dictionary<string, string>> Keys = new() {
            { "en", new () {
                    { "loading", "Loading..." },
                    { "auth_required", "Open Laney and log in to VK account to show content here" },
                    { "online", "online" },
                    { "online_mobile", "online from mobile" },
                    { "online_via", "online via" }
                } 
            },
            { "ru", new () {
                    { "loading", "Загрузка..." },
                    { "auth_required", "Чтобы увидеть содержимое, откройте Laney и авторизуйтесь" },
                    { "online", "в сети" },
                    { "online_mobile", "в сети с телефона" },
                    { "online_via", "в сети с" }
                }
            },
            { "uk", new () {
                    { "loading", "Завантаження..." },
                    { "auth_required", "!Чтобы увидеть содержимое, откройте Laney и авторизуйтесь" },
                    { "online", "у мережі" },
                    { "online_mobile", "у мережі з телефону" },
                    { "online_via", "у мережі з" }
                }
            }
        };
    }
}
