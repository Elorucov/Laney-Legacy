using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.VkAPI.Objects;
using System;

namespace Elorucov.Laney.Services {
    public class VKClientsHelper {
        public static string GetAppIconByLPResponse(int id) {
            switch (id) {
                case 0: return "";
                case 1: return "m";
                case 2: return "i";
                case 3: return "i";
                case 4: return "a";
                case 5: return "w";
                case 6: return "w";
                default: return "d";
            }
        }

        public static int GetLPAppIdByAppId(long id) {
            switch (id) {
                case 2274003: return 4;
                case 3140623:
                case 3087106: return 2;
                case 3682744: return 3;
                case 3502557:
                case 3502561: return 5;
                case 3697615: return 6;
                case 5027722: return 7;
                case 2685278: return 1;
                case 5955265: return 1;
                default: return 7;
            }
        }

        public static string GetOnlineStatus(UserEx user) {
            return GetOnlineInfoString(user.OnlineInfo, user.Sex, user.OnlineInfo.AppName);
        }

        public static string GetOnlineInfoString(UserOnlineInfo o, Sex sex, string appNameOverride = null) {
            string result = string.Empty;
            if (o == null) return result;
            string s = sex == Sex.Female ? "_f" : "_m";
            if (o.Visible) {
                if (o.isOnline) {
                    if (o.AppId > 0) {
                        string appName = string.IsNullOrEmpty(appNameOverride) ? o.AppId.ToString() : appNameOverride;
                        result = String.Format(Locale.GetForFormat("online_via"), appName);
                    } else {
                        result = Locale.Get("online");
                    }
                } else {
                    result = o.LastSeenUnix > 0 ?
                        String.Format(Locale.GetForFormat($"offline_last_seen{s}"), o.LastSeen.ToTimeAndDate()) :
                        Locale.Get("offline");
                }
            } else {
                result = Locale.Get($"offline{s}_{o.Status.ToEnumMemberAttrValue()}");
            }
            return result;
        }

        public static string GetOnlineInfoString(OnlineQueueEvent oqe, Sex sex) {
            string result = string.Empty;
            if (oqe == null) return result;
            string s = sex == Sex.Female ? "_f" : "_m";
            if (oqe.Online) {
                result = GetAppNameByLPResponse(oqe.Platform);
            } else {
                result = oqe.LastSeenUnix > 0 ?
                    String.Format(Locale.GetForFormat($"offline_last_seen{s}"), oqe.LastSeen.ToTimeAndDate()) :
                    Locale.Get("offline");
            }
            return result;
        }

        public static string GetAppNameByLPResponse(int id) {
            switch (id) {
                case 0: return "";
                case 1: return Locale.Get("online_mobile");
                case 2: return String.Format(Locale.GetForFormat("online_via"), "iOS");
                case 3: return String.Format(Locale.GetForFormat("online_via"), "iPad");
                case 4: return String.Format(Locale.GetForFormat("online_via"), "Android");
                case 5: return String.Format(Locale.GetForFormat("online_via"), "Windows Phone");
                case 6: return String.Format(Locale.GetForFormat("online_via"), "Windows");
                default: return Locale.Get("online");
            }
        }
    }
}