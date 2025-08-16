using Elorucov.VkAPI.Objects;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Elorucov.Laney.Services.PushNotifications {
    public class PushChannelUpdater {
        public static async Task<bool> TryRegisterBackgroundTaskAsync() {
            try {
                const string taskName = "PushChannelUpdaterBackgroundTask";

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

                // Set triggers
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.ServicingComplete, false));
                builder.SetTrigger(new TimeTrigger(30, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

                // And register the task
                BackgroundTaskRegistration registration = builder.Register();
                return true;
            } catch { return false; }
        }

        public static async Task UpdateAsync(IBackgroundTaskInstance instance) {
            object td = instance.TriggerDetails;

            string data = $"Result: ";

            object resp = await Execute.Execute.RegisterDevice();
            if (resp is bool b) {
                data += 0;
            } else if (resp is VKError err) {
                data += err.error_code;
            } else if (resp is VKErrorResponse errr) {
                data += errr.error.error_code;
            } else if (resp is Exception ex) {
                data += ex.HResult;
            }
            data += $"; {DateTime.Now}; ";
            data += td != null ? td.GetType().Name : "N/A";

            System.IO.File.WriteAllText($"{ApplicationData.Current.LocalFolder.Path}\\pcuinfo.txt", data);
        }
    }
}