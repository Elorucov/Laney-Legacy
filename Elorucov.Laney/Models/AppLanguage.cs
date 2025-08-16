using System.Collections.Generic;

namespace Elorucov.Laney.Models {
    public class AppLanguage {
        public string DisplayName { get; set; }
        public string LanguageCode { get; set; }

        public static List<AppLanguage> SupportedLanguages = new List<AppLanguage> {
            new AppLanguage { LanguageCode = "en-US", DisplayName = "English" },
            new AppLanguage { LanguageCode = "ru", DisplayName = "Русский" },
            new AppLanguage { LanguageCode = "uk", DisplayName = "Українська" }
        };
    }
}
