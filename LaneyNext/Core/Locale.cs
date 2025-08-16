using ELOR.VKAPILib.Objects;
using System;
using System.Globalization;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.System.UserProfile;

namespace Elorucov.Laney.Core
{
    public class Locale
    {
        static ResourceLoader loader = new ResourceLoader();

        public static string Get(string key)
        {
            string l = loader.GetString(key);
            return String.IsNullOrEmpty(l) ? $"%{key}%" : l;
        }

        public static string GetOrEmpty(string key)
        {
            return loader.GetString(key);
        }

        public static string Get(string key, Sex sex)
        {
            string s = sex == Sex.Female ? "_f" : "_m";
            if (sex == Sex.Train) s = "_c";
            return Get(key + s);
        }

        public static string GetOrEmpty(string key, Sex sex)
        {
            string s = sex == Sex.Female ? "_f" : "_m";
            if (sex == Sex.Train) s = "_c";
            return GetOrEmpty(key + s);
        }

        public static CultureInfo GetCurrentCultureInfo()
        {
            return new CultureInfo(GlobalizationPreferences.Languages[0].ToString());
        }

        private static string GetDeclensionSuffix(decimal num)
        {
            int number = (int)num % 100;
            if (number >= 11 && number <= 19)
            {
                return "_plu";
            }

            var i = number % 10;
            switch (i)
            {
                case 1:
                    return "_nom";
                case 2:
                case 3:
                case 4:
                    return "_gen";
                default:
                    return "_plu";
            }
        }

        public static string GetDeclension(decimal num, string str)
        {
            return Get($"{str}{GetDeclensionSuffix(num)}");
        }

        //

        private static ResourceMap ResourceMap = ResourceManager.Current.MainResourceMap;

        public static string GetForFormat(string key)
        {
            return ResourceMap?.GetValue("Resources/" + key, new ResourceContext())?.ValueAsString;
        }

        public static string GetForFormat(string key, ResourceContext context)
        {
            return ResourceMap?.GetValue("Resources/" + key, context)?.ValueAsString;
        }

        public static string GetDeclensionForFormat(decimal num, string key)
        {
            return GetForFormat($"{key}{GetDeclensionSuffix(num)}");
        }
    }
}
