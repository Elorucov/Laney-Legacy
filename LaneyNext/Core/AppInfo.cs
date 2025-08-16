using Elorucov.Laney.Helpers;
using Elorucov.Laney.Helpers.Attributes;
using System;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.System.Profile;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Core
{
    public enum AppReleaseState
    {
        Internal = 82, Beta = 3, Release = 75
    }

    public class AppInfo
    {
        public static AppReleaseState ReleaseState { get { return AppReleaseState.Internal; } }
        public static PackageVersion Version { get { return Package.Current.Id.Version; } }
        public static string UserAgent { get { return $"LaneyMessenger (2; {Version.Major}.{Version.Minor}.{Version.Build}; {OSHelper.GetVersion()}; {AnalyticsInfo.VersionInfo.DeviceFamily}; {Package.Current.Id.Architecture})"; } }
        public static DateTime BuildDateTime { get { return GetBuildDateTime(); } }
        public static DateTime ExpirationDate { get { return BuildDateTime.Date.AddDays(90); } }
        public static bool IsExpired { get { return DateTime.Now.Date > ExpirationDate && ReleaseState == AppReleaseState.Internal; } }

        private static DateTime GetBuildDateTime()
        {
            int unixtime = 0;
            Assembly assembly = typeof(App).GetTypeInfo().Assembly;
            var cattrs = assembly.CustomAttributes;
            foreach (var attr in cattrs)
            {
                if (attr.AttributeType == typeof(BuildTimeAttribute))
                {
                    unixtime = (int)attr.ConstructorArguments[0].Value;
                }
            }

            return DateTimeOffset.FromUnixTimeSeconds(unixtime).DateTime;
        }

        public static async void ShowExpiredInfoAsync()
        {
            MessageDialog dlg = new MessageDialog($"If you are a tester, contact the developer for the latest test build.", "This build has expired");
            dlg.Options = MessageDialogOptions.AcceptUserInputAfterDelay;
            await dlg.ShowAsync();
            Application.Current.Exit();
        }
    }
}