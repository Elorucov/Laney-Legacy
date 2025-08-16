using Elorucov.Laney.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Elorucov.Laney.Helpers
{
    public class DataPackageViewHelpers
    {
        private static readonly List<string> ImageFormats = new List<string> {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".heic"
        };
        private static readonly List<string> VideoFormats = new List<string> {
            ".mp4", ".avi", ".mpg", ".3gp", ".mpeg", ".mov", ".wmv", ".mkv"
        };

        public static async Task<bool> HasOnlyImageFiles(DataPackageView dpview)
        {
            if (!dpview.Contains(StandardDataFormats.StorageItems)) return false;
            foreach (StorageFile file in await dpview.GetStorageItemsAsync())
            {
                if (!ImageFormats.Contains(file.FileType)) return false;
            }
            return true;
        }

        public static async Task<bool> HasOnlyVideoFiles(DataPackageView dpview)
        {
            if (!dpview.Contains(StandardDataFormats.StorageItems)) return false;
            foreach (StorageFile file in await dpview.GetStorageItemsAsync())
            {
                if (!VideoFormats.Contains(file.FileType)) return false;
            }
            return true;
        }

        public static async Task<StorageFile> SaveBitmapFromDataPackagViewAsync(DataPackageView dpview)
        {
            if (!dpview.Contains(StandardDataFormats.Bitmap)) throw new ArgumentException("DataPackageView contains no bitmap!");
            IRandomAccessStreamReference imageReceived = null;
            imageReceived = await dpview.GetBitmapAsync();
            if (imageReceived == null) throw new ArgumentException("Failed to get bitmap!");
            StorageFile f = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync($"lnydppimg_{Guid.NewGuid()}.jpg", CreationCollisionOption.GenerateUniqueName);
            Log.General.Info($"Created file for bitmap: {f.Path}");
            using (var ostr = await f.OpenStreamForWriteAsync())
            {
                var b = (await imageReceived.OpenReadAsync()).AsStream();
                b.CopyTo(ostr);
                b.Flush();
                b.Dispose();
            }
            return f;
        }
    }
}
