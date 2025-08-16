using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Services.Network {
    public static class LNetExtensions {
        static Dictionary<string, byte[]> _cachedImages = new Dictionary<string, byte[]>();
        const int cachesLimit = 500;

        // dontSendCookies required for private graffities.
        public static async Task SetUriSourceAsync(this BitmapImage image, Uri uri, bool dontSendCookies = false) {
            if (uri == null) return;

            string key = uri.AbsoluteUri;
            if (_cachedImages.ContainsKey(key)) {
                var bytes = _cachedImages[key];
                SetBytesToImage(bytes, image, uri, true);
                return;
            }
            try {
                HttpResponseMessage hmsg = await LNet.GetAsync(uri, dontSendCookies: dontSendCookies);
                hmsg.EnsureSuccessStatusCode();

                var bytes = await hmsg.Content.ReadAsByteArrayAsync();
                if (_cachedImages.Count >= cachesLimit) _cachedImages.Remove(_cachedImages.FirstOrDefault().Key);
                _cachedImages[key] = bytes;
                SetBytesToImage(bytes, image, uri, false);
            } catch (Exception ex) {
                Log.Error($"BitmapImage.SetUriSourceAsync error 0x{ex.HResult.ToString("x8")}: {ex.Message}\nUrl: {uri}");
            }
        }

        private static void SetBytesToImage(byte[] bytes, BitmapImage image, Uri uri, bool isCached) {
            try {
                using (MemoryStream stream = new MemoryStream()) {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Seek(0, SeekOrigin.Begin);
                    image.SetSource(stream.AsRandomAccessStream());
                }
            } catch (TaskCanceledException) {
                Log.Error($"SetBytesToImage: task cancelled! isCached: {isCached}, Size: {bytes.Length}, Url: {uri.ToString()}");
            } catch (Exception ex) {
                Log.Error($"SetBytesToImage error 0x{ex.HResult.ToString("x8")}: {ex.Message}\nisCached: {isCached}, Size: {bytes.Length}, Url: {uri}");
            }
        }

        public static byte[] TryGetCachedImage(Uri uri) {
            string key = uri.AbsoluteUri;
            if (_cachedImages.ContainsKey(key)) return _cachedImages[key];
            return null;
        }

        public static async Task SetUriSourceAsync(this Avatar avatar, Uri uri) {
            if (APIHelper.PlaceholderAvatars.Contains(uri)) return;
            BitmapImage img = new BitmapImage { DecodePixelType = DecodePixelType.Logical };
            await img.SetUriSourceAsync(uri);
            avatar.ImageSource = img;
        }

        public static void ClearImagesCache() {
            _cachedImages.Clear();
        }

        public static Tuple<int, uint> GetImagesCacheSize() {
            int count = 0;
            uint size = 0;
            foreach (var image in _cachedImages) {
                if (image.Value != null) {
                    count++;
                    size += (uint)image.Value.Length;
                }
            }
            return new Tuple<int, uint>(count, size);
        }

        public static async Task<MediaSource> CreateMediaSourceFromLNetUriAsync(Uri uri, string mimeType) {
            try {
                HttpResponseMessage hmsg = await LNet.GetAsync(uri);
                hmsg.EnsureSuccessStatusCode();

                using (var stream = await hmsg.Content.ReadAsStreamAsync()) {
                    return MediaSource.CreateFromStream(stream.AsRandomAccessStream(), mimeType);
                }
            } catch (Exception ex) {
                Log.Error($"CreateFromLNetUriAsync error 0x{ex.HResult.ToString("x8")}: {ex.Message}\nUrl: {uri}");
                return null;
            }
        }

        public static async Task<StorageFile> DownloadFileToTempFolderAsync(Uri uri) {
            try {
                StorageFile file = (await ApplicationData.Current.TemporaryFolder.TryGetItemAsync(uri.Segments.Last())) as StorageFile;
                if (file != null) {
                    return file;
                }

                HttpResponseMessage hmsg = await LNet.GetAsync(uri);
                using (var stream = await hmsg.Content.ReadAsStreamAsync()) {
                    file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(uri.Segments.Last(), CreationCollisionOption.GenerateUniqueName);
                    using (var fileStream = await file.OpenStreamForWriteAsync()) {
                        await stream.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                    }
                    return file;
                }
            } catch (Exception ex) {
                Functions.ShowHandledErrorTip(ex, "Error while saving a temporary file!");
                return null;
            }
        }
    }
}