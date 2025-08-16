using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.ListHelpers;
using Elorucov.Laney.Services.Logger;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Upload;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

// Ported from L2 for new Share target page.

namespace Elorucov.Laney.ViewModel.Controls {
    public enum OutboundAttachmentType { Attachment, ForwardedMessages, Place }
    public enum OutboundAttachmentUploadState { Unknown, InProgress, Success, Failed }
    public enum OutboundAttachmentUploadFileType { PhotoForWall, PhotoForMessage, VideoForWall, VideoForMessage, DocumentForWall, DocumentForMessage, AudioMessage, Graffiti, Audio }

    public class OutboundAttachmentViewModel : BaseViewModel {
        private DispatcherTimer _uploadStatusTimer;

        private OutboundAttachmentType _type;
        private char _icon;
        private string _displayName;
        private AttachmentBase _attachment;
        private BitmapImage _previewImage;
        private string _extraInfo;
        private double _uploadProgress;
        private OutboundAttachmentUploadState _uploadState;
        private ThreadSafeObservableCollection<LMessage> _forwardedMessages;
        private Tuple<double, double> _place;

        public OutboundAttachmentType Type { get { return _type; } private set { _type = value; OnPropertyChanged(); } }
        public char Icon { get { return _icon; } private set { _icon = value; OnPropertyChanged(); } }
        public string DisplayName { get { return _displayName; } private set { _displayName = value; OnPropertyChanged(); } }
        public AttachmentBase Attachment { get { return _attachment; } private set { _attachment = value; OnPropertyChanged(); } }
        public BitmapImage PreviewImage { get { return _previewImage; } private set { _previewImage = value; OnPropertyChanged(); } }
        public string ExtraInfo { get { return _extraInfo; } private set { _extraInfo = value; OnPropertyChanged(); } }
        public double UploadProgress { get { return _uploadProgress; } private set { _uploadProgress = value; OnPropertyChanged(); } }
        public OutboundAttachmentUploadState UploadState { get { return _uploadState; } private set { _uploadState = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<LMessage> ForwardedMessages { get { return _forwardedMessages; } private set { _forwardedMessages = value; OnPropertyChanged(); } }
        public Tuple<double, double> Place { get { return _place; } private set { _place = value; OnPropertyChanged(); } }

        public long FromPeerId { get; private set; } // for forwarded messages.
        public Exception ErrorInfo { get; private set; }

        IFileUploader uploader;
        StorageFile UploadableFile;
        OutboundAttachmentUploadFileType uploadFileType;
        long peerId;

        public OutboundAttachmentViewModel(AttachmentBase attachment) {
            UploadState = OutboundAttachmentUploadState.Success;
            Log.Info($"Init outbound attachment {attachment.ObjectType}");
            switch (attachment.GetType().Name) {
                case nameof(Photo): SetUp(attachment as Photo); break;
                case nameof(Video): SetUp(attachment as Video); break;
                case nameof(Document): SetUp(attachment as Document); break;
                case nameof(Poll): SetUp(attachment as Poll); break;
                case nameof(AudioMessage): SetUp(attachment as AudioMessage); break;
                case nameof(Audio): SetUp(attachment as Audio); break;
                case nameof(AudioPlaylist): SetUp(attachment as AudioPlaylist); break;
                case nameof(Podcast): SetUp(attachment as Podcast); break;
                case nameof(Graffiti): SetUp(attachment as Graffiti); break;
                case nameof(Story): SetUp(attachment as Story); break;
                case nameof(WallPost): SetUp(attachment as WallPost); break;
                case nameof(WallReply): SetUp(attachment as WallReply); break;
                case nameof(UGCSticker): SetUp(attachment as UGCSticker); break;
                default:
                    Attachment = attachment;
                    Log.Warn($"OutboundAttachmentViewModel: unknown attachment. id: {attachment.ToString()}");
                    break;
            }
        }

        public OutboundAttachmentViewModel(IEnumerable<LMessage> forwardedMessages, long fromPeerId) {
            Log.Info($"Init outbound forwarded messages. Count: {forwardedMessages.Count()}");
            FromPeerId = fromPeerId;
            Type = OutboundAttachmentType.ForwardedMessages;
            UploadState = OutboundAttachmentUploadState.Success;
            ForwardedMessages = new ThreadSafeObservableCollection<LMessage>(forwardedMessages);
            UpdateUIForFwdMessages();
            ForwardedMessages.CollectionChanged += (a, b) => UpdateUIForFwdMessages();
        }

        public OutboundAttachmentViewModel(double latitude, double longitude) {
            Log.Info($"Init outbound geo attachment.");
            Type = OutboundAttachmentType.Place;
            UploadState = OutboundAttachmentUploadState.Success;
            Place = new Tuple<double, double>(latitude, longitude);
            Icon = '';
            DisplayName = $"{Math.Round(latitude, 4)}\n{Math.Round(longitude, 4)}";
        }

        private OutboundAttachmentViewModel() { }

        #region Setup

        private void SetUp(Photo p) {
            Icon = '';
            PreviewImage = new BitmapImage { UriSource = p.PreviewImageUri };
            Attachment = p;
        }

        private void SetUp(Video v) {
            Icon = '';
            PreviewImage = new BitmapImage { UriSource = v.PreviewImageUri };
            ExtraInfo = v.DurationTime.ToNormalString();
            Attachment = v;
        }

        private void SetUp(Document d) {
            Icon = '';
            if (d.Preview != null) {
                PreviewImage = new BitmapImage { UriSource = d.PreviewImageUri };
                ExtraInfo = d.Extension.ToUpper();
            } else {
                DisplayName = d.Title;
            }
            Attachment = d;
        }

        private void SetUp(Poll p) {
            Icon = '';
            DisplayName = p.Question;
            Attachment = p;
        }

        private void SetUp(AudioMessage a) {
            Icon = '';
            DisplayName = a.DurationTime.ToNormalString();
            Attachment = a;
        }

        private void SetUp(Audio a) {
            Icon = '';
            DisplayName = a.Title;
            Attachment = a;
        }

        private void SetUp(Podcast p) {
            Icon = '';
            DisplayName = p.Title;
            Attachment = p;
        }

        private void SetUp(AudioPlaylist ap) {
            Icon = '';
            DisplayName = ap.Title;
            Attachment = ap;
        }

        private void SetUp(Graffiti g) {
            Icon = '';
            DisplayName = Locale.Get("atch_graffiti");
            Attachment = g;
        }

        private void SetUp(Story s) {
            Icon = '';
            DisplayName = Locale.Get("atch_story");
            Attachment = s;
        }

        private void SetUp(WallPost wp) {
            Icon = '';
            DisplayName = string.IsNullOrEmpty(wp.Text) ? Locale.Get("atch_wall") : wp.Text;
            Attachment = wp;
        }

        private void SetUp(WallReply wr) {
            Icon = '';
            DisplayName = string.IsNullOrEmpty(wr.Text) ? Locale.Get("atch_wall_reply") : wr.Text;
            Attachment = wr;
        }

        private void SetUp(UGCSticker ugcs) {
            Icon = '';
            DisplayName = Locale.Get("atch_sticker");
            if (ugcs.Images?.Count > 0) PreviewImage = new BitmapImage { UriSource = ugcs.Images.FirstOrDefault().Uri };
            Attachment = ugcs;
        }

        #endregion

        #region Uploading file

        public static OutboundAttachmentViewModel CreateFromFile(long peerId, StorageFile file, OutboundAttachmentUploadFileType uploadFileType, bool dontUploadImmediately = false) {
            OutboundAttachmentViewModel oavm = new OutboundAttachmentViewModel();
            new System.Action(async () => { await oavm.SetFileAsync(peerId, file, uploadFileType); })();
            if (!dontUploadImmediately) _ = oavm.DoUploadFileInternal(uploadFileType);
            return oavm;
        }

        public static async Task<List<OutboundAttachmentViewModel>> CreateFromDataPackageView(long peerId, DataPackageView dpview, OutboundAttachmentUploadFileType type = OutboundAttachmentUploadFileType.DocumentForMessage, int limit = 10, bool dontUploadImmediately = false) {
            List<OutboundAttachmentViewModel> attachments = new List<OutboundAttachmentViewModel>();
            if (dpview.Contains(StandardDataFormats.Bitmap)) {
                Log.Info($"OutboundAttachmentViewModel: CreateFromDataPackageView bitmap, type: {type.ToString()}");
                IRandomAccessStreamReference imageReceived = await dpview.GetBitmapAsync();
                if (imageReceived != null) {
                    StorageFile image = await DataPackageParser.SaveBitmapFromClipboardAsync(imageReceived);
                    OutboundAttachmentViewModel oavm = CreateFromFile(peerId, image, peerId == 0 ? OutboundAttachmentUploadFileType.PhotoForWall : OutboundAttachmentUploadFileType.PhotoForMessage, dontUploadImmediately);
                    attachments.Add(oavm);
                } else {

                }
            }
            if (dpview.Contains(StandardDataFormats.StorageItems)) {
                Log.Info($"OutboundAttachmentViewModel: CreateFromDataPackageView files, type: {type.ToString()}");
                var files = await dpview.GetStorageItemsAsync();
                for (int i = 0; i < Math.Min(limit, files.Count); i++) {
                    attachments.Add(CreateFromFile(peerId, files[i] as StorageFile, type, dontUploadImmediately));
                }
            }
            return attachments;
        }

        private async Task SetFileAsync(long peerId, StorageFile file, OutboundAttachmentUploadFileType type) {
            UploadableFile = file;
            uploadFileType = type;
            this.peerId = peerId;
            Icon = '';
            DisplayName = file.Name;
            try {
                var preview = await file.GetScaledImageAsThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.PicturesView, 128);
                if (preview != null && preview.Type == Windows.Storage.FileProperties.ThumbnailType.Image) {
                    BitmapImage img = new BitmapImage();
                    await img.SetSourceAsync(preview);
                    PreviewImage = img;
                }
            } catch (Exception ex) {
                Log.Error($"OutboundAttachmentViewModel.SetFile 0x{ex.HResult.ToString("x8")}");
            }
        }

        public async Task<bool> DoUpload() {
            return await DoUploadFileInternal(uploadFileType);
        }

        private async Task<bool> DoUploadFileInternal(OutboundAttachmentUploadFileType uploadFileType) {
            var file = UploadableFile;
            UploadState = OutboundAttachmentUploadState.InProgress;
            Icon = '';
            DisplayName = file.Name;
            var preview = await file.GetScaledImageAsThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.PicturesView, 128);
            if (preview != null && preview.Type == Windows.Storage.FileProperties.ThumbnailType.Image) {
                BitmapImage img = new BitmapImage();
                await img.SetSourceAsync(preview);
                PreviewImage = img;
            }

            try {
                long gid = peerId < 0 ? peerId * -1 : 0;

                // Activity status
                if (!AppParameters.DontSendActivity) {
                    switch (uploadFileType) {
                        case OutboundAttachmentUploadFileType.PhotoForMessage:
                        case OutboundAttachmentUploadFileType.VideoForMessage:
                        case OutboundAttachmentUploadFileType.DocumentForMessage:
                        case OutboundAttachmentUploadFileType.AudioMessage:
                            _uploadStatusTimer = new DispatcherTimer {
                                Interval = TimeSpan.FromSeconds(5)
                            };
                            _uploadStatusTimer.Tick += UploadStatusTimerTick;
                            break;
                    }
                }

                // Upload process
                switch (uploadFileType) {
                    case OutboundAttachmentUploadFileType.PhotoForWall:
                    case OutboundAttachmentUploadFileType.PhotoForMessage:
                        var srp = uploadFileType == OutboundAttachmentUploadFileType.PhotoForWall ? await Photos.GetWallUploadServer(gid) : await Photos.GetMessagesUploadServer();
                        if (srp is PhotoUploadServer pus) {
                            uploader = APIHelper.GetUploadMethod("photo", pus.Uri, file);
                            uploader.UploadFailed += Uploader_UploadFailed;
                            uploader.ProgressChanged += Uploader_ProgressChanged;
                            var pr = await uploader.UploadAsync();
                            if (pr == null) throw new ArgumentNullException("Upload error, no response!");
                            UploadProgress = 100;
                            PhotoUploadResult pur = JsonConvert.DeserializeObject<PhotoUploadResult>(pr);
                            if (string.IsNullOrEmpty(pur.Photo)) throw new Exception($"{pur.Error}: {pur.ErrorDescription}");
                            var presult = uploadFileType == OutboundAttachmentUploadFileType.PhotoForWall ? await Photos.SaveWallPhoto(pur.Photo, pur.Server, pur.Hash, peerId) : await Photos.SaveMessagesPhoto(pur.Photo, pur.Server, pur.Hash);
                            if (presult is List<PhotoSaveResult> lpsr) {
                                PhotoSaveResult psr = lpsr.FirstOrDefault();
                                SetUp(new Photo { Id = psr.Id, OwnerId = psr.OwnerId, AccessKey = psr.AccessKey, Sizes = psr.Sizes });
                                _uploadStatusTimer?.Stop();
                            } else {
                                throw new ArgumentException("photos.save response is incorrect!");
                            }
                        } else {
                            string method = uploadFileType == OutboundAttachmentUploadFileType.PhotoForWall ? "photos.getWallUploadServer" : "photos.getMessagesUploadServer";
                            throw new Exception($"{method} response is incorrect!");
                        }
                        break;
                    case OutboundAttachmentUploadFileType.DocumentForWall:
                    case OutboundAttachmentUploadFileType.DocumentForMessage:
                    case OutboundAttachmentUploadFileType.AudioMessage:
                        var srd = uploadFileType == OutboundAttachmentUploadFileType.DocumentForWall ?
                            await Docs.GetWallUploadServer(gid) :
                            await Docs.GetMessagesUploadServer(uploadFileType == OutboundAttachmentUploadFileType.AudioMessage ? "audio_message" : "doc");
                        if (srd is VkUploadServer dus) {
                            uploader = APIHelper.GetUploadMethod("file", dus.Uri, file);
                            uploader.UploadFailed += Uploader_UploadFailed;
                            uploader.ProgressChanged += Uploader_ProgressChanged;
                            var dr = await uploader.UploadAsync();
                            if (dr == null) throw new ArgumentNullException("Upload error, no response!");
                            UploadProgress = 100;
                            DocumentUploadResult dur = JsonConvert.DeserializeObject<DocumentUploadResult>(dr);
                            if (string.IsNullOrEmpty(dur.File)) throw new Exception($"{dur.Error}: {dur.ErrorDescription}");
                            var dresult = await Docs.Save(dur.File, file.Name);
                            if (dresult is Attachment atch) {
                                switch (atch.Type) {
                                    case AttachmentType.AudioMessage:
                                        SetUp(atch.AudioMessage); break;
                                    case AttachmentType.Graffiti:
                                        SetUp(atch.Graffiti); break;
                                    case AttachmentType.Document:
                                        SetUp(atch.Document); break;
                                }
                                _uploadStatusTimer?.Stop();
                            } else {
                                throw new ArgumentException("docs.save response is incorrect!");
                            }
                        } else {
                            throw new Exception("docs.getMessagesUploadServer response is incorrect!");
                        }

                        break;
                    case OutboundAttachmentUploadFileType.VideoForWall:
                    case OutboundAttachmentUploadFileType.VideoForMessage:
                        var srv = await Videos.Save(file.DisplayName, "Uploaded via Laney", uploadFileType == OutboundAttachmentUploadFileType.VideoForMessage, groupId: gid);
                        if (srv is VideoUploadServer vus) {
                            uploader = APIHelper.GetUploadMethod("video_file", vus.Uri, file);
                            uploader.UploadFailed += Uploader_UploadFailed;
                            uploader.ProgressChanged += Uploader_ProgressChanged;
                            var vr = await uploader.UploadAsync();
                            if (vr == null) throw new ArgumentNullException("Upload error, no response!");
                            UploadProgress = 100;
                            _uploadStatusTimer?.Stop();
                            VideoUploadResult vur = JsonConvert.DeserializeObject<VideoUploadResult>(vr);
                            Attachment = new Video { Id = vur.VideoId, OwnerId = vur.OwnerId, AccessKey = vus.AccessKey };
                            ExtraInfo = "--:--";
                        } else {
                            throw new ArgumentException("video.save response is incorrect!");
                        }
                        break;
                }
                UploadState = OutboundAttachmentUploadState.Success;
                return true;
            } catch (Exception ex) {
                Log.Error($"OutboundAttachmentViewModel.DoUploadFileInternal: file upload error! Type: {uploadFileType.ToString()}; HResult: 0x{ex.HResult.ToString("x8")}\n{ex.Message}");
                UploadState = OutboundAttachmentUploadState.Failed;
                ErrorInfo = ex;
                UploadProgress = 0;
                _uploadStatusTimer?.Stop();
                return false;
            }
        }

        private void UploadStatusTimerTick(object sender, object e) {
            new System.Action(async () => {
                switch (uploadFileType) {
                    case OutboundAttachmentUploadFileType.PhotoForMessage:
                        await Messages.SetActivity(peerId, "photo");
                        break;
                    case OutboundAttachmentUploadFileType.VideoForMessage:
                        await Messages.SetActivity(peerId, "video");
                        break;
                    case OutboundAttachmentUploadFileType.DocumentForMessage:
                        await Messages.SetActivity(peerId, "file");
                        break;
                    case OutboundAttachmentUploadFileType.AudioMessage:
                        await Messages.SetActivity(peerId, "audiomessage");
                        break;
                }
            })();
        }

        private void Uploader_UploadFailed(Exception ex) {
            Log.Error($"OutboundAttachmentViewModel.DoUploadFileInternal: exception thrown in uploader! HResult: 0x{ex.HResult.ToString("x8")}\n{ex.Message}");
            _uploadStatusTimer?.Stop();
            new System.Action(async () => {
                await CoreApplication.GetCurrentView().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    UploadState = OutboundAttachmentUploadState.Failed;
                    UploadProgress = 0;
                });
            })();
        }

        private void Uploader_ProgressChanged(double totalBytes, double bytesSent, double percent, string debugInfo) {
            new System.Action(async () => {
                await CoreApplication.GetCurrentView().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    UploadProgress = percent;
                });
            })();
        }

        public void CancelUpload() {
            if (uploader == null) return;
            uploader.CancelUpload();
        }

        #endregion

        #region Forwarded messages

        private void UpdateUIForFwdMessages() {
            int c = ForwardedMessages.Count;
            DisplayName = String.Format(Locale.GetDeclensionForFormat(c, "messages"), c);
            Icon = '';
        }

        #endregion

        public override string ToString() {
            switch (Type) {
                case OutboundAttachmentType.Attachment: return Attachment.ToString();
                // case OutboundAttachmentType.ForwardedMessages: return String.Join(",", ForwardedMessages.Select(m => m.Id));
                default: return null;
            }
        }
    }
}