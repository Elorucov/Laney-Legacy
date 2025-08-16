using Elorucov.Laney.Helpers;
using Elorucov.Laney.Helpers.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.System;
using Windows.System.Profile;

namespace Elorucov.Laney.Core
{
    public class Insider
    {
        class InsiderUpdate
        {
            [JsonProperty("major")]
            public int Major { get; set; }

            [JsonProperty("minor")]
            public int Minor { get; set; }

            [JsonProperty("build")]
            public int Build { get; set; }

            [JsonProperty("link")]
            public string Link { get; set; }

            [JsonProperty("changelog")]
            public List<string> Changelog { get; set; }
        }

        class InsiderCheckResponse
        {

            [JsonProperty("error_code")]
            public int ErrorCode { get; set; }

            [JsonProperty("error_message")]
            public string ErrorMessage { get; set; }

            [JsonProperty("update")]
            public InsiderUpdate Update { get; set; }
        }

        static HttpClient hclient;

        public static async void CheckAsync()
        {
            try
            {
                string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
                ulong osv = ulong.Parse(sv);
                ulong osmj = (osv & 0xFFFF000000000000L) >> 48;
                ulong osmn = (osv & 0x0000FFFF00000000L) >> 32;
                ulong osbl = (osv & 0x00000000FFFF0000L) >> 16;

                int vkid = VKSession.Current.Id;
                string sig = Encryptor.ToSHA1($"İ{osbl}{osmn}{AnalyticsInfo.VersionInfo.DeviceFamily}{osmj}{AppInfo.Version.Build}{vkid}{AppInfo.Version.Minor}{Package.Current.Id.Architecture}{AppInfo.Version.Major}Ə");

                if (hclient == null)
                {
                    hclient = new HttpClient();
                    hclient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                    hclient.DefaultRequestHeaders.Add("User-Agent", AppInfo.UserAgent);
                    hclient.DefaultRequestHeaders.Add("Laney-Signature", sig);
                }

                using (HttpRequestMessage hmsg = new HttpRequestMessage(HttpMethod.Post, new Uri("https://laney.elor.top/v2/insider.php")))
                {
                    hmsg.Content = new FormUrlEncodedContent(new Dictionary<string, string> { { "vk_user_id", vkid.ToString() } });
                    using (var resp = await hclient.SendAsync(hmsg))
                    {
                        string response = await resp.Content.ReadAsStringAsync();
                        InsiderCheckResponse icr = JsonConvert.DeserializeObject<InsiderCheckResponse>(response);
                        ContinueCheck(icr);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.General.Error("Failed!", ex);
                if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                {
                    CheckAsync();
                }
                else
                {
                    if (AppInfo.IsExpired) AppInfo.ShowExpiredInfoAsync();
                }
            }
        }

        private static async void ContinueCheck(InsiderCheckResponse icr)
        {
            if (icr.ErrorCode == 6)
            { // latest build
                Log.General.Info("This build is latest available");
                if (AppInfo.IsExpired) AppInfo.ShowExpiredInfoAsync();
                return;
            }
            if (icr.ErrorCode == 0)
            {
                ShowNewUpdateDialog(icr.Update);
                return;
            }

            Log.General.Warn("Server returns error", new ValueSet { { "code", icr.ErrorCode }, { "message", icr.ErrorMessage } });
            string title, description;
            Action afterDialogAction;
            switch (icr.ErrorCode)
            {
                case 3:
                    title = "Ошибка доступа";
                    description = "Данный аккаунт не участвует в тестировании внутренних версий.";
                    afterDialogAction = Logout;
                    break;
                default:
                    title = "Сервер обновлений вернул ошибку";
                    description = $"Код ошибки: {icr.ErrorCode}\nСooбщение: {icr.ErrorMessage}\n\nОбновление будет проверено позже автоматически.";
                    afterDialogAction = WaitAndCheckAgain;
                    break;
            }

            if (AppInfo.IsExpired) AppInfo.ShowExpiredInfoAsync();

            await new Alert
            {
                Header = title,
                Text = description,
                PrimaryButtonText = Locale.Get("close")
            }.ShowAsync();
            afterDialogAction?.Invoke();
        }

        private static async void ShowNewUpdateDialog(InsiderUpdate update)
        {
            string title = "Доступна новая сборка!";
            string description = $"Версия: {update.Major}.{update.Minor}.{update.Build}. Что нового:\n";
            foreach (string chlog in update.Changelog)
            {
                description += $"— {chlog};\n";
            }
            description += "Если у вас установлено приложение \"Установщик приложения\" (Desktop app installer), оно запустится после скачивания сборки и попросит установить её.";

            Alert alert = new Alert
            {
                Header = title,
                Text = description,
                PrimaryButtonText = Locale.Get("Обновить"),
                SecondaryButtonText = Locale.Get("Позже")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                DownloadUpdateAsync(new Uri(update.Link));
            }
            else
            {
                Log.General.Warn("User doesn't want to update app :(", new ValueSet { { "new_build", update.Build } });
                if (AppInfo.IsExpired) AppInfo.ShowExpiredInfoAsync();
            }
        }

        private static async void DownloadUpdateAsync(Uri source)
        {
            Log.General.Info("Starting download update...");

            StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Path.GetFileName(source.LocalPath), CreationCollisionOption.ReplaceExisting);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation operation = downloader.CreateDownload(source, file);

            Progress<DownloadOperation> downloadCallback = new Progress<DownloadOperation>(OnDownloadProgressChanged);
            await operation.StartAsync().AsTask(CancellationToken.None, downloadCallback);
        }

        private static async void OnDownloadProgressChanged(DownloadOperation operation)
        {
            var progress = operation.Progress;
            double percentage = 100 / (double)progress.TotalBytesToReceive * (double)progress.BytesReceived;
            Log.General.Verbose($"Update download progress: {operation.Progress.Status} {Math.Round(percentage, 1)}%");
            if (progress.BytesReceived == progress.TotalBytesToReceive)
            {
                bool result = await Launcher.LaunchFileAsync(operation.ResultFile);
                Log.General.Info($"Downloaded update launch result: {result}");
                return;
            }
            switch (operation.Progress.Status)
            {
                case BackgroundTransferStatus.Canceled:
                case BackgroundTransferStatus.Error:
                    Log.General.Warn("Some problems with downloading update", new ValueSet { { "status", operation.Progress.Status.ToString() }, { "progress", percentage } });
                    await Task.Delay(5000);  // Wait 5 sec.
                    DownloadUpdateAsync(operation.RequestedUri);
                    break;
            }
        }

        private static void Logout()
        {
            if (AppInfo.ReleaseState != AppReleaseState.Internal) return;
            VKSession.LogoutAsync();
        }

        private static async void WaitAndCheckAgain()
        {
            if (AppInfo.IsExpired) AppInfo.ShowExpiredInfoAsync();
            await Task.Delay(30000);  // Wait 30 sec.
            CheckAsync();
        }
    }
}