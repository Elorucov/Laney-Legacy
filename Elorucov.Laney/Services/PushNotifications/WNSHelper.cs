using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.VkAPI.Methods;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Notifications;

namespace Elorucov.Laney.Services.PushNotifications {
    public class VKNotificationHelper {
        static PushNotificationChannel channel = null;

        public static async Task<string> GetChannelUri() {
            try {
                if (AppParameters.Notifications == 0) return null;

                if (channel == null) {
                    channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                }

                if (AppParameters.Notifications == 2) {
                    string b64 = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(channel.Uri));
                    return b64;
                } else {
                    return channel.Uri;
                }
            } catch {
                return null;
            }
        }

        public static string GetDeviceId() {
            EasClientDeviceInformation eas = new EasClientDeviceInformation();
            return eas.Id.ToString();
        }

        public static async Task<bool> RegisterBackgrundTaskAsync() {
            try {
                const string taskName = "PNS";

                // If background task is already registered, do nothing
                if (BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(taskName)))
                    return true;

                // Otherwise request access
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                if (status.ToString().Contains("Denied") || status == BackgroundAccessStatus.Unspecified) return false;

                // Create the background task
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder() {
                    Name = taskName
                };

                // Assign the toast action trigger
                builder.SetTrigger(new PushNotificationTrigger());
                builder.CancelOnConditionLoss = false;
                builder.IsNetworkRequested = true;

                // And register the task
                BackgroundTaskRegistration trr = builder.Register();
                Log.Info($"PNS BackgroundTaskRegistration: {trr.TaskId}");
                return true;
            } catch { return false; }
        }

        public static void UnregisterBackgroundTask() {
            try {
                foreach (var task in BackgroundTaskRegistration.AllTasks) {
                    if (task.Value.Name == "PNS") {
                        var pns = (BackgroundTaskRegistration)task.Value;
                    }
                }

                if (channel != null) {
                    channel.Close();
                    channel = null;
                }
            } catch (Exception ex) {
                Log.Error($"VKNotificationHelper.UnregisterBackgroundTask: error 0x{ex.HResult.ToString("x8")}!");
            }
        }

        public static async Task DisconnectAsync() {
            object resp = await Account.UnregisterDevice(GetDeviceId());
        }

        public static void SetBadge(uint number) {
            var badge = new BadgeNumericContent {
                Number = number
            };
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(new BadgeNotification(badge.GetXml()));
        }
    }
}