using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Upload;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.PreviewDebug.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class FileUploader : Page {
        public FileUploader() {
            this.InitializeComponent();
        }

        Uri uri = null;
        int t = 0;
        IFileUploader vkfu;

        PhotoUploadResult pur;
        VideoUploadResult vur;
        DocumentUploadResult dur;
        string audioMsgUploadResult;

        private void PhotoServer(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                object r = await Photos.GetMessagesUploadServer();
                if (r is PhotoUploadServer) {
                    var pus = r as PhotoUploadServer;
                    res.Text = $"Success!\n\nUrl: {pus.Url}\nAlbum id: {pus.AlbumId}\nUser id: {pus.UserId}\n\nPress \"Upload\" button and select file.";
                    UploadUrl.Text = pus.Uri.AbsoluteUri;
                    t = 1;
                } else if (r is VKErrorResponse) {
                    var ex = (r as VKErrorResponse).error;
                    res.Text = $"API error!\n{ex.error_code}: {ex.error_msg}";
                } else if (r is Exception) {
                    var ex = r as Exception;
                    res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                }
            })();
        }

        private void VideoServer(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                object r = await Videos.Save("Laney debug video", "Laney debug video", true);
                if (r is VideoUploadServer) {
                    var vus = r as VideoUploadServer;
                    res.Text = $"Success!\n\nUrl: {vus.Url}\nTitle: {vus.Title}\nDesc: {vus.Description}\nAccess key: {vus.AccessKey}\n\nPress \"Upload\" button and select file.";
                    UploadUrl.Text = vus.Uri.AbsoluteUri;
                    t = 3;
                } else if (r is VKErrorResponse) {
                    var ex = (r as VKErrorResponse).error;
                    res.Text = $"API error!\n{ex.error_code}: {ex.error_msg}";
                } else if (r is Exception) {
                    var ex = r as Exception;
                    res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                }
            })();
        }

        private void DocServer(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                object r = await Docs.GetMessagesUploadServer("doc", AppParameters.UserID.ToString());
                if (r is VkUploadServer) {
                    var dus = r as VkUploadServer;
                    res.Text = $"Success!\n\nUrl: {dus.Url}\n\nPress \"Upload\" button and select file.";
                    UploadUrl.Text = dus.Uri.AbsoluteUri;
                    t = 2;
                } else if (r is VKErrorResponse) {
                    var ex = (r as VKErrorResponse).error;
                    res.Text = $"API error!\n{ex.error_code}: {ex.error_msg}";
                } else if (r is Exception) {
                    var ex = r as Exception;
                    res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                }
            })();
        }

        private void DocServerAudio(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                object r = await Docs.GetMessagesUploadServer("audio_message", AppParameters.UserID.ToString());
                if (r is VkUploadServer) {
                    var dus = r as VkUploadServer;
                    res.Text = $"Success!\n\nUrl: {dus.Url}\n\nPress \"Upload\" button and select file.";
                    UploadUrl.Text = dus.Uri.AbsoluteUri;
                    t = 2;
                } else if (r is VKErrorResponse) {
                    var ex = (r as VKErrorResponse).error;
                    res.Text = $"API error!\n{ex.error_code}: {ex.error_msg}";
                } else if (r is Exception) {
                    var ex = r as Exception;
                    res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                }
            })();
        }

        private void AudioMsgServer(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                object r = await Messages.GetAudioMessageUploadServer();
                if (r is VkUploadServer) {
                    var dus = r as VkUploadServer;
                    res.Text = $"Success!\n\nUrl: {dus.Url}\n\nPress \"Upload\" button and select file.";
                    UploadUrl.Text = dus.Uri.AbsoluteUri;
                    t = 4;
                } else if (r is VKErrorResponse) {
                    var ex = (r as VKErrorResponse).error;
                    res.Text = $"API error!\n{ex.error_code}: {ex.error_msg}";
                } else if (r is Exception) {
                    var ex = r as Exception;
                    res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                }
            })();
        }

        private void Upload(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                try {
                    uri = new Uri(UploadUrl.Text);
                    if (uri != null && t != 0) {
                        switch (t) {
                            case 1: await UploadPhotoAsync(); break;
                            case 2: await UploadDocAsync(); break;
                            case 3: await UploadVideoAsync(); break;
                            case 4: await UploadAudioMsgAsync(); break;
                        }
                    }
                } catch (Exception ex) {
                    res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                }
            })();
        }

        private async Task UploadPhotoAsync() {
            FileOpenPicker fop = new FileOpenPicker();
            fop.FileTypeFilter.Add(".jpg");
            fop.FileTypeFilter.Add(".jpeg");
            fop.FileTypeFilter.Add(".png");
            fop.FileTypeFilter.Add(".bmp");
            fop.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fop.ViewMode = PickerViewMode.Thumbnail;

            StorageFile file = await fop.PickSingleFileAsync();
            if (file != null) {
                if (um.IsOn == false) {
                    vkfu = new VKFileUploader("photo", uri, file);
                } else if (um.IsOn == true) {
                    vkfu = new VKFileUploaderViaHttpClient("photo", uri, file);
                }
                vkfu.UploadFailed += (e) => res.Text = $"Upload failed!\n0x{e.HResult.ToString("x8")}: {e.Message}";
                vkfu.ProgressChanged += UploadProgressChanged;
                string resp = await vkfu.UploadAsync();
                if (resp != null) {
                    UploadError err = JsonConvert.DeserializeObject<UploadError>(resp);
                    pur = JsonConvert.DeserializeObject<PhotoUploadResult>(resp);
                    res.Text = $"Success!\n\nServer: {pur.Server}\nPhoto:\n{pur.Photo}\n\nHash: {pur.Hash}\n\nAnd now press \"Save\" button.";
                }
                vkfu = null;
            }
        }

        private async Task UploadVideoAsync() {
            FileOpenPicker fop = new FileOpenPicker();
            fop.FileTypeFilter.Add(".avi");
            fop.FileTypeFilter.Add(".mp4");
            fop.FileTypeFilter.Add(".3gp");
            fop.FileTypeFilter.Add(".mpeg");
            fop.FileTypeFilter.Add(".mov");
            fop.FileTypeFilter.Add(".mp3");
            fop.FileTypeFilter.Add(".wmv");
            fop.FileTypeFilter.Add(".flv");
            fop.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            fop.ViewMode = PickerViewMode.Thumbnail;

            StorageFile file = await fop.PickSingleFileAsync();
            if (file != null) {
                if (um.IsOn == false) {
                    vkfu = new VKFileUploader("video_file", uri, file);
                } else if (um.IsOn == true) {
                    vkfu = new VKFileUploaderViaHttpClient("video_file", uri, file);
                }
                vkfu.UploadFailed += (e) => res.Text = $"Upload failed!\n0x{e.HResult.ToString("x8")}: {e.Message}";
                vkfu.ProgressChanged += UploadProgressChanged;
                string resp = await vkfu.UploadAsync();
                if (resp != null) {
                    UploadError err = JsonConvert.DeserializeObject<UploadError>(resp);
                    vur = JsonConvert.DeserializeObject<VideoUploadResult>(resp);
                    res.Text = $"Success!\n\nOwner id: {vur.OwnerId}\nVideo id:\n{vur.VideoId}\nSize: {vur.Size}\nHash: {vur.VideoHash}\n\nNo need to click on the \"Save\" button.";
                }
                vkfu = null;
            }
        }

        private async Task UploadDocAsync() {
            FileOpenPicker fop = new FileOpenPicker();
            fop.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            fop.ViewMode = PickerViewMode.List;
            fop.FileTypeFilter.Add("*");

            StorageFile file = await fop.PickSingleFileAsync();
            if (file != null) {
                if (um.IsOn == false) {
                    vkfu = new VKFileUploader("file", uri, file);
                } else if (um.IsOn == true) {
                    vkfu = new VKFileUploaderViaHttpClient("file", uri, file);
                }
                vkfu.UploadFailed += (e) => res.Text = $"Upload failed!\n0x{e.HResult.ToString("x8")}: {e.Message}";
                vkfu.ProgressChanged += UploadProgressChanged;
                string resp = await vkfu.UploadAsync();
                if (resp != null) {
                    dur = JsonConvert.DeserializeObject<DocumentUploadResult>(resp);
                    res.Text = $"Success!\n\nFile: {dur.File}\n\nAnd now press \"Save\" button.";
                }
                vkfu = null;
            }
        }

        private async Task UploadAudioMsgAsync() {
            FileOpenPicker fop = new FileOpenPicker();
            fop.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            fop.ViewMode = PickerViewMode.List;
            fop.FileTypeFilter.Add(".wav");
            fop.FileTypeFilter.Add(".mp3");
            fop.FileTypeFilter.Add(".aac");

            StorageFile file = await fop.PickSingleFileAsync();
            if (file != null) {
                if (um.IsOn == false) {
                    vkfu = new VKFileUploader("file", uri, file);
                } else if (um.IsOn == true) {
                    vkfu = new VKFileUploaderViaHttpClient("file", uri, file);
                }
                vkfu.UploadFailed += (e) => res.Text = $"Upload failed!\n0x{e.HResult.ToString("x8")}: {e.Message}";
                vkfu.ProgressChanged += UploadProgressChanged;
                string resp = await vkfu.UploadAsync();
                if (resp != null) {
                    //dur = JsonConvert.DeserializeObject<DocumentUploadResult>(resp);
                    //res.Text = $"Success!\n\nFile: {dur.File}\n\nAnd now press \"Save\" button.";
                    res.Text = resp;
                    audioMsgUploadResult = resp;
                }
                vkfu = null;
            }
        }

        private void UploadProgressChanged(double totalBytes, double bytesSent, double percent, string debugInfo) {
            new System.Action(async () => {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    res.Text = debugInfo;
                });
            })();
        }

        private void Cancel(object sender, RoutedEventArgs e) {
            if (vkfu != null) vkfu.CancelUpload();
        }

        private void Save(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                if (pur != null) {
                    object r = await Photos.SaveMessagesPhoto(pur.Photo, pur.Server.ToString(), pur.Hash);
                    if (r is List<PhotoSaveResult>) {
                        var rs = (r as List<PhotoSaveResult>)[0];
                        res.Text = $"Success!\n\nId: {rs.Id}\nAlbum id: {rs.AlbumId}\nPhoto id: {rs.PhotoId}\nOwner id: {rs.OwnerId}\nAccessKey: {rs.AccessKey}\n\nAttach: photo{rs.OwnerId}_{rs.Id}_{rs.AccessKey}";
                    } else if (r is VKErrorResponse) {
                        var ex = (r as VKErrorResponse).error;
                        res.Text = $"API error!\n{ex.error_code}: {ex.error_msg}";
                    } else if (r is Exception) {
                        var ex = r as Exception;
                        res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                    }
                    pur = null;
                }
                if (dur != null) {
                    object r = await Docs.Save(dur.File);
                    if (r is Attachment rs) {
                        string fileinfo = "";

                        switch (rs.Type) {
                            case AttachmentType.Document: fileinfo = $"Attach: doc{rs.Document.OwnerId}_{rs.Document.Id}"; break;
                            case AttachmentType.Graffiti: fileinfo = $"Attach: doc{rs.Graffiti.OwnerId}_{rs.Graffiti.Id}"; break;
                            case AttachmentType.AudioMessage: fileinfo = $"Attach: doc{rs.AudioMessage.OwnerId}_{rs.AudioMessage.Id}"; break;
                        }

                        res.Text = $"Success! Type: {rs.Type}\n\n{fileinfo}";
                    } else if (r is VKErrorResponse) {
                        var ex = (r as VKErrorResponse).error;
                        res.Text = $"API error!\n{ex.error_code}: {ex.error_msg}";
                    } else if (r is Exception) {
                        var ex = r as Exception;
                        res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                    }
                    dur = null;
                }
                if (!string.IsNullOrEmpty(audioMsgUploadResult)) {
                    object r = await Messages.SaveAudioMessage(audioMsgUploadResult);
                    if (r is AudioMessage am) {
                        res.Text = $"Audio message saved successfully!\n\n{am.ToString()}";
                    } else if (r is VKErrorResponse) {
                        var ex = (r as VKErrorResponse).error;
                        res.Text = $"API error!\n{ex.error_code}: {ex.error_msg}";
                    } else if (r is Exception) {
                        var ex = r as Exception;
                        res.Text = $"Exception!\n0x{ex.HResult.ToString("x8")}: {ex.Message}";
                    }
                    dur = null;
                }
            })();
        }
    }
}
