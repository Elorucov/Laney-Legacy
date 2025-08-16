using Elorucov.Laney.Services.Common;
using Elorucov.Laney.ViewModel.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Elorucov.Laney.Services {
    public class DataPackageParser {
        public static readonly List<string> ImageFormats = new List<string> {
            ".jpg", ".jpeg", ".png", ".bmp", ".heic", ".gif"
        };
        public static readonly List<string> VideoFormats = new List<string> {
            ".mp4", ".avi", ".mpg", ".mkv", ".mov", ".3gp", ".webm", ".hevc"
        };

        public static bool IsImage(StorageFile file) {
            bool contains = ImageFormats.Contains(file.FileType.ToLower());
            Logger.Log.Info($"DataPackageParser.IsImage: filetype={file.FileType}, contains={contains}");
            return contains;
        }

        public static bool IsVideo(StorageFile file) {
            bool contains = VideoFormats.Contains(file.FileType.ToLower());
            Logger.Log.Info($"DataPackageParser.IsVideo: filetype={file.FileType}, contains={contains}");
            return contains;
        }

        public static async Task<StorageFile> SaveBitmapFromClipboardAsync(IRandomAccessStreamReference rasr) {
            try {
                StorageFile f = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync($"lnydppimg_{Guid.NewGuid()}.jpg", CreationCollisionOption.GenerateUniqueName);
                Logger.Log.Info($"DataPackageParser > SaveBitmapFromClipboardAsync: Created file for bitmap: {f.Path}");

                using (var imageStream = await rasr.OpenReadAsync()) {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);
                    SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync();

                    using (var fileStream = await f.OpenAsync(FileAccessMode.ReadWrite)) {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fileStream);
                        encoder.SetSoftwareBitmap(bitmap);
                        await encoder.FlushAsync();
                    }
                }
                return f;
            } catch (Exception ex) {
                Logger.Log.Error($"DataPackageParser > SaveBitmapFromClipboardAsync: Error 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                Functions.ShowHandledErrorDialog(ex);
            }
            return null;
        }

        public static async Task<StorageFile> SaveBitmapAsDocFromClipboardAsync(IRandomAccessStreamReference rasr, string extension) {
            try {
                StorageFile f = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync($"lnydppimg_{Guid.NewGuid()}.{extension}", CreationCollisionOption.GenerateUniqueName);
                Logger.Log.Info($"DataPackageParser > SaveBitmapAsDocFromClipboardAsync: Created file for bitmap: {f.Path}");

                using (var ostr = await f.OpenStreamForWriteAsync()) {
                    using (var imageStream = (await rasr.OpenReadAsync()).AsStream()) {
                        await imageStream.CopyToAsync(ostr);
                        await imageStream.FlushAsync();
                    }
                }
                return f;
            } catch (Exception ex) {
                Logger.Log.Error($"DataPackageParser > SaveBitmapFromClipboardAsync: Error 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                Functions.ShowHandledErrorDialog(ex);
            }
            return null;
        }

        public static async Task UploadFilesFromClipboardAsync(MessageFormViewModel mfvm, IReadOnlyList<IStorageItem> data) {
            if (data.Count > 0) {
                int imagesCount = 0;
                int videosCount = 0;
                foreach (StorageFile file in data) {
                    if (IsImage(file)) {
                        imagesCount++;
                        continue;
                    }
                    if (IsVideo(file)) {
                        videosCount++;
                        continue;
                    }
                }

                if (Math.Min(imagesCount, videosCount) == 0 && Math.Max(imagesCount, videosCount) == data.Count) {
                    bool isImage = imagesCount == data.Count;
                    bool isVideo = videosCount == data.Count;
                    if (isImage) {
                        await mfvm.UploadPhotoMulti(data.Cast<StorageFile>());
                    } else if (isVideo) {
                        await mfvm.UploadVideoMulti(data.Cast<StorageFile>());
                    }
                } else {
                    await mfvm.UploadDocMulti(data.Cast<StorageFile>());
                }
            }
        }

        public static async Task UploadFilesFromClipboardAsync(PostComposerViewModel pcvm, IReadOnlyList<IStorageItem> data) {
            if (data.Count > 0) {
                int imagesCount = 0;
                int videosCount = 0;
                foreach (StorageFile file in data) {
                    if (IsImage(file)) {
                        imagesCount++;
                        continue;
                    }
                    if (IsVideo(file)) {
                        videosCount++;
                        continue;
                    }
                }

                if (Math.Min(imagesCount, videosCount) == 0 && Math.Max(imagesCount, videosCount) == data.Count) {
                    bool isImage = imagesCount == data.Count;
                    bool isVideo = videosCount == data.Count;
                    if (isImage) {
                        await pcvm.AttachFilesAndUpload(data.Cast<StorageFile>(), OutboundAttachmentUploadFileType.PhotoForWall);
                    } else if (isVideo) {
                        await pcvm.AttachFilesAndUpload(data.Cast<StorageFile>(), OutboundAttachmentUploadFileType.VideoForWall);
                    }
                } else {
                    await pcvm.AttachFilesAndUpload(data.Cast<StorageFile>(), OutboundAttachmentUploadFileType.DocumentForWall);
                }
            }
        }

        public static async Task<MessageFormViewModel> ParseAsync(DataPackageView data, MessageFormViewModel mfvm) {
            if (data.Contains(StandardDataFormats.WebLink)) {
                try {
                    var a = await data.GetWebLinkAsync();
                    mfvm.MessageText = $"{data.Properties.Title}\n{data.Properties.Description}\n\n{a}";
                } catch { }
            }
            if (data.Contains(StandardDataFormats.Text)) {
                try {
                    var a = await data.GetTextAsync();
                    mfvm.MessageText = $"{data.Properties.Title}\n{data.Properties.Description}\n\n{a}";
                } catch { }
            }
            if (data.Contains(StandardDataFormats.StorageItems)) {
                try {
                    var a = await data.GetStorageItemsAsync();
                    if (a.Count > 0) {
                        mfvm.MessageText = $"{data.Properties.Title}\n{data.Properties.Description}";
                        await UploadFilesFromClipboardAsync(mfvm, a);
                    }
                } catch { }
            }
            if (data.Contains(StandardDataFormats.Bitmap)) {
                try {
                    var a = await data.GetBitmapAsync();
                    mfvm.MessageText = $"{data.Properties.Title}\n{data.Properties.Description}";
                    StorageFile f = await SaveBitmapFromClipboardAsync(a);
                    await mfvm.UploadPhoto(f);
                } catch { }
            }
            return mfvm;
        }
    }
}
