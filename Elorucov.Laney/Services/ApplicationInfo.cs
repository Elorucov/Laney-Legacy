using Elorucov.Laney.Services.Common;
using System;
using System.Reflection;
using Windows.ApplicationModel;

namespace Elorucov.Laney.Services {
    public enum ApplicationReleaseState {
        Private = 43, Beta = 98, Release = 75
    }

    public class ApplicationInfo {
        public static ApplicationReleaseState ReleaseState { get { return ApplicationReleaseState.Release; } }
        public static string UserAgent { get { return $"LaneyMessenger {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build} (OS build: {Functions.GetOSBuild()})"; } }
        const double ActiveDays = 90;
        public static DateTime BuildDate { get { return GetBuildTime(); } }
        public static ushort Build { get { return Package.Current.Id.Version.Build; } }

        public static DateTime GetExpirationDate() {
            return BuildDate.AddDays(ActiveDays);
        }

        private static DateTime GetBuildTime() {
            Assembly assembly = typeof(App).GetTypeInfo().Assembly;
            var version = assembly.GetName().Version;
            var vdate = new DateTime(2000, 1, 1).Add(new TimeSpan(
            TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
            TimeSpan.TicksPerSecond * 2 * version.Revision)); // seconds since midnight, (multiply by 2 to get original)
            return vdate.ToUniversalTime().ToLocalTime(); // fix timezone.
        }

        public static string GetVersion(bool less = false) {
            Package app = Package.Current;

            string st = "";
            switch (ReleaseState) {
                case ApplicationReleaseState.Private: st = " ALPHA"; break;
                case ApplicationReleaseState.Beta: st = " BETA"; break;
                case ApplicationReleaseState.Release: st = ""; break;
            }
            if (less) return $"{app.Id.Version.Major}.{app.Id.Version.Minor}.{app.Id.Version.Build}";
            return $"{app.Id.Version.Major}.{app.Id.Version.Minor}.{app.Id.Version.Build}{st}".ToUpperInvariant();
        }
    }
}
