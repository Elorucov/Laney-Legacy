using Elorucov.Laney.Services.Logger;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Elorucov.Laney.Services.UI {
    public class EmojiFontManager {
        private const string _remoteSource = "https://elorucov.github.io/laney/v1/emoji_fonts/apple.ttf";
        public const string LocalSource = "ms-appdata:///Local/emoji_fonts/apple.ttf";

        public static bool IsAvailable { get; private set; }
        private static bool isRunned = false;

        public static async Task<bool> CheckAsync() {
            if (isRunned) return IsAvailable;
            isRunned = true;

            bool localFileAvailable = await CheckIsLocalFileAvailable();
            if (localFileAvailable) {
                IsAvailable = true;
                return true;
            } else {
                await Task.Factory.StartNew(async () => await TryGetFromRemote());
                return false;
            }
        }

        private static async Task<bool> CheckIsLocalFileAvailable() {
            try {
                StorageFile fontFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(LocalSource));
                return true;
            } catch (FileNotFoundException) {
                Log.Warn($"EmojiFontManager: file not found.");
                return false;
            } catch (Exception ex) {
                Log.Error(ex, $"EmojiFontManager: cannot check is local file available!");
                return false;
            }
        }

        static bool logError = true;

        private static async Task TryGetFromRemote() {
            try {
                HttpClient hc = new HttpClient();
                var response = await hc.GetAsync(new Uri(_remoteSource));
                response.EnsureSuccessStatusCode();

                StorageFolder fontsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("emoji_fonts", CreationCollisionOption.OpenIfExists);
                StorageFile fontFile = await fontsFolder.CreateFileAsync("apple.ttf", CreationCollisionOption.OpenIfExists);

                using (IRandomAccessStream fileStream = await fontFile.OpenAsync(FileAccessMode.ReadWrite)) {
                    await response.Content.WriteToStreamAsync(fileStream);
                }

                IsAvailable = true;
                if (logError) Log.Info($"EmojiFontManager.TryGetFromRemote: success!.");
            } catch (Exception ex) {
                if (logError) Log.Error($"EmojiFontManager.TryGetFromRemote: exception 0x{ex.HResult.ToString("x8")}.");
                logError = false;

                await Task.Delay(1000);
                await TryGetFromRemote();
            }
        }
    }
}