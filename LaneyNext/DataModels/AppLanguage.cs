using System.Collections.Generic;

namespace Elorucov.Laney.DataModels
{
    public class AppLanguage
    {
        public string DisplayName { get; set; }
        public string Code { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }

        public static List<AppLanguage> SupportedLanguages = new List<AppLanguage> {
            new AppLanguage { Code = "en-US", DisplayName = "English" },
            new AppLanguage { Code = "ru", DisplayName = "Русский" },
            new AppLanguage { Code = "uk", DisplayName = "Українська" }
        };
    }
}