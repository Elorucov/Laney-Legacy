using ELOR.VKAPILib;
using ELOR.VKAPILib.Objects;
using ELOR.VKAPILib.Objects.Upload;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Helpers.Uploader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.ViewModels.Controls
{
    public enum OutboundAttachmentType { Attachment, ForwardedMessages, Place }
    public enum OutboundAttachmentUploadState { Unknown, InProgress, Success, Failed }
    public enum OutboundAttachmentUploadFileType { Photo, Video, Doc, AudioMessage, Graffiti, Audio }

    public class OutboundAttachmentViewModel : BaseViewModel
    {
        private OutboundAttachmentType _type;
        private DataTemplate _icon;
        private string _displayName;
        private AttachmentBase _attachment;
        private BitmapImage _previewImage;
        private string _extraInfo;
        private double _uploadProgress;
        private OutboundAttachmentUploadState _uploadState;
        private ThreadSafeObservableCollection<MessageViewModel> _forwardedMessages;
        private Tuple<double, double> _place;

        public OutboundAttachmentType Type { get { return _type; } private set { _type = value; OnPropertyChanged(); } }
        public DataTemplate Icon { get { return _icon; } private set { _icon = value; OnPropertyChanged(); } }
        public string DisplayName { get { return _displayName; } private set { _displayName = value; OnPropertyChanged(); } }
        public AttachmentBase Attachment { get { return _attachment; } private set { _attachment = value; OnPropertyChanged(); } }
        public BitmapImage PreviewImage { get { return _previewImage; } private set { _previewImage = value; OnPropertyChanged(); } }
        public string ExtraInfo { get { return _extraInfo; } private set { _extraInfo = value; OnPropertyChanged(); } }
        public double UploadProgress { get { return _uploadProgress; } private set { _uploadProgress = value; OnPropertyChanged(); } }
        public OutboundAttachmentUploadState UploadState { get { return _uploadState; } private set { _uploadState = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<MessageViewModel> ForwardedMessages { get { return _forwardedMessages; } private set { _forwardedMessages = value; OnPropertyChanged(); } }
        public Tuple<double, double> Place { get { return _place; } private set { _place = value; OnPropertyChanged(); } }

        public int ForwardedMessagesFromGroupId { get; private set; }

        IFileUploader uploader;
        StorageFile UploadableFile;
        OutboundAttachmentUploadFileType uploadFileType;
        int peerId;

        public OutboundAttachmentViewModel(AttachmentBase attachment)
        {
            UploadState = OutboundAttachmentUploadState.Success;
            Log.General.Info("Init outbound attachment", new ValueSet { { "type", attachment.ObjectType } });
            switch (attachment.GetType().Name)
            {
                case nameof(Photo): SetUp(attachment as Photo); break;
                case nameof(Video): SetUp(attachment as Video); break;
                case nameof(Document): SetUp(attachment as Document); break;
                case nameof(Poll): SetUp(attachment as Poll); break;
                case nameof(AudioMessage): SetUp(attachment as AudioMessage); break;
                case nameof(Audio): SetUp(attachment as Audio); break;
                case nameof(Podcast): SetUp(attachment as Podcast); break;
                case nameof(Graffiti): SetUp(attachment as Graffiti); break;
                case nameof(Story): SetUp(attachment as Story); break;
                default:
                    Attachment = attachment;
                    Log.General.Warn("Unknown attachment", new ValueSet { { "id", attachment.ToString() } });
                    break;
            }
        }

        public OutboundAttachmentViewModel(IEnumerable<MessageViewModel> forwardedMessages, int groupId = 0)
        {
            Log.General.Info("Init outbound forwarded messages", new ValueSet { { "count", forwardedMessages.Count() }, { "group_id", groupId } });
            ForwardedMessagesFromGroupId = groupId;
            Type = OutboundAttachmentType.ForwardedMessages;
            UploadState = OutboundAttachmentUploadState.Success;
            ForwardedMessages = new ThreadSafeObservableCollection<MessageViewModel>(forwardedMessages);
            UpdateUIForFwdMessages();
            ForwardedMessages.CollectionChanged += (a, b) => UpdateUIForFwdMessages();
        }

        public OutboundAttachmentViewModel(double latitude, double longitude)
        {
            Log.General.Info("Init outbound geo attachment");
            Type = OutboundAttachmentType.Place;
            UploadState = OutboundAttachmentUploadState.Success;
            Place = new Tuple<double, double>(latitude, longitude);
            Icon = (DataTemplate)Application.Current.Resources["Icon24Place"];
            DisplayName = $"{Math.Round(latitude, 4)}\n{Math.Round(longitude, 4)}";
        }

        private OutboundAttachmentViewModel() { }

        #region Setup

        private void SetUp(Photo p)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24Gallery);
            PreviewImage = new BitmapImage { UriSource = p.PreviewImageUri };
            Attachment = p;
        }

        private void SetUp(Video v)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24Video);
            PreviewImage = new BitmapImage { UriSource = v.PreviewImageUri };
            ExtraInfo = v.DurationTime.ToNormalString();
            Attachment = v;
        }

        private void SetUp(Document d)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24Document);
            if (d.Preview != null)
            {
                PreviewImage = new BitmapImage { UriSource = d.PreviewImageUri };
                ExtraInfo = d.Extension.ToUpper();
            }
            else
            {
                DisplayName = d.Title;
            }
            Attachment = d;
        }

        private void SetUp(Poll p)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24Poll);
            DisplayName = p.Question;
            Attachment = p;
        }

        private void SetUp(AudioMessage a)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24Voice);
            DisplayName = a.DurationTime.ToNormalString();
            Attachment = a;
        }

        private void SetUp(Audio a)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24Music);
            DisplayName = a.Title;
            Attachment = a;
        }

        private void SetUp(Podcast p)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24Music);
            DisplayName = p.Title;
            Attachment = p;
        }

        private void SetUp(Graffiti g)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24BrushOutline);
            DisplayName = Locale.Get("msg_attachment_graffiti");
            Attachment = g;
        }

        private void SetUp(Story s)
        {
            Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24StoryOutline);
            DisplayName = Locale.Get("msg_attachment_story");
            Attachment = s;
        }

        #endregion

        #region Uploading file

        public static OutboundAttachmentViewModel CreateFromFile(int peerId, StorageFile file, OutboundAttachmentUploadFileType uploadFileType, bool dontUploadImmediately = false)
        {
            OutboundAttachmentViewModel oavm = new OutboundAttachmentViewModel();
            oavm.SetFile(peerId, file, uploadFileType);
            if (!dontUploadImmediately) oavm.DoUploadFileInternal(uploadFileType, VKSession.Current.API, VKSession.Current.GroupId);
            return oavm;
        }

        public static async Task<List<OutboundAttachmentViewModel>> CreateFromDataPackageView(int peerId, DataPackageView dpview, OutboundAttachmentUploadFileType type = OutboundAttachmentUploadFileType.Doc, int limit = 10, bool dontUploadImmediately = false)
        {
            List<OutboundAttachmentViewModel> attachments = new List<OutboundAttachmentViewModel>();
            if (dpview.Contains(StandardDataFormats.Bitmap))
            {
                Log.General.Info(String.Empty, new ValueSet { { "type", type.ToString() }, { "format", "bitmap" } });
                StorageFile image = await DataPackageViewHelpers.SaveBitmapFromDataPackagViewAsync(dpview);
                OutboundAttachmentViewModel oavm = CreateFromFile(peerId, image, OutboundAttachmentUploadFileType.Photo, dontUploadImmediately);
                attachments.Add(oavm);
            }
            if (dpview.Contains(StandardDataFormats.StorageItems))
            {
                Log.General.Info(String.Empty, new ValueSet { { "type", type.ToString() }, { "format", "files" } });
                var files = await dpview.GetStorageItemsAsync();
                for (int i = 0; i < Math.Min(limit, files.Count); i++)
                {
                    attachments.Add(CreateFromFile(peerId, files[i] as StorageFile, type, dontUploadImmediately));
                }
            }
            return attachments;
        }

        private async void SetFile(int peerId, StorageFile file, OutboundAttachmentUploadFileType type)
        {
            UploadableFile = file;
            uploadFileType = type;
            this.peerId = peerId;
            Icon = (DataTemplate)Application.Current.Resources["Icon24Upload"];
            DisplayName = file.Name;
            var preview = await file.GetScaledImageAsThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.PicturesView, 128);
            if (preview != null && preview.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
            {
                BitmapImage img = new BitmapImage();
                await img.SetSourceAsync(preview);
                PreviewImage = img;
            }
        }

        public async Task<bool> DoUpload(VKAPI API, int groupId)
        {
            return await DoUploadFileInternal(uploadFileType, API, groupId);
        }

        private async Task<bool> DoUploadFileInternal(OutboundAttachmentUploadFileType uploadFileType, VKAPI API, int groupId)
        {
            var file = UploadableFile;
            UploadState = OutboundAttachmentUploadState.InProgress;
            Icon = (DataTemplate)Application.Current.Resources["Icon24Upload"];
            DisplayName = file.Name;
            var preview = await file.GetScaledImageAsThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.PicturesView, 128);
            if (preview != null && preview.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
            {
                BitmapImage img = new BitmapImage();
                await img.SetSourceAsync(preview);
                PreviewImage = img;
            }

            try
            {
                VkUploadServer server;
                switch (uploadFileType)
                {
                    case OutboundAttachmentUploadFileType.Photo:
                        server = await API.Photos.GetMessagesUploadServerAsync(groupId);
                        PhotoUploadServer pus = server as PhotoUploadServer;
                        uploader = GetUploader("photo", pus.Uri, file);
                        uploader.UploadFailed += Uploader_UploadFailed;
                        uploader.ProgressChanged += Uploader_ProgressChanged;
                        var pr = await uploader.UploadAsync();
                        if (pr == null) throw new ArgumentNullException("Upload error, no response!");
                        UploadProgress = 100;
                        PhotoUploadResult pur = JsonConvert.DeserializeObject<PhotoUploadResult>(pr);
                        if (String.IsNullOrEmpty(pur.Photo)) throw new Exception("File is not uploaded!");
                        var presult = await API.Photos.SaveMessagesPhotoAsync(groupId, pur.Server, pur.Photo, pur.Hash);
                        if (presult.Count > 0)
                        {
                            PhotoSaveResult psr = presult[0];
                            SetUp(new Photo { Id = psr.Id, OwnerId = psr.OwnerId, AccessKey = psr.AccessKey, Sizes = psr.Sizes });
                        }
                        else
                        {
                            throw new ArgumentException("photos.save response is incorrect!");
                        }
                        break;
                    case OutboundAttachmentUploadFileType.Doc:
                    case OutboundAttachmentUploadFileType.AudioMessage:
                        server = await API.Docs.GetMessagesUploadServerAsync(groupId, 0, uploadFileType == OutboundAttachmentUploadFileType.AudioMessage);
                        uploader = GetUploader("file", server.Uri, file);
                        uploader.UploadFailed += Uploader_UploadFailed;
                        uploader.ProgressChanged += Uploader_ProgressChanged;
                        var dr = await uploader.UploadAsync();
                        if (dr == null) throw new ArgumentNullException("Upload error, no response!");
                        UploadProgress = 100;
                        DocumentUploadResult dur = JsonConvert.DeserializeObject<DocumentUploadResult>(dr);
                        if (String.IsNullOrEmpty(dur.File)) throw new Exception("File is not uploaded!");
                        var dresult = await API.Docs.SaveAsync(groupId, dur.File, file.Name);
                        switch (dresult.Type)
                        {
                            // TODO: AudioMessage and Graffiti.
                            case AttachmentType.AudioMessage:
                                SetUp(dresult.AudioMessage); break;
                            case AttachmentType.Graffiti:
                                SetUp(dresult.Graffiti); break;
                            case AttachmentType.Document:
                                SetUp(dresult.Document); break;
                        }
                        break;
                    case OutboundAttachmentUploadFileType.Video:
                        server = await API.Video.SaveAsync(groupId, file.DisplayName, "Laney", true);
                        uploader = GetUploader("video_file", server.Uri, file);
                        uploader.UploadFailed += Uploader_UploadFailed;
                        uploader.ProgressChanged += Uploader_ProgressChanged;
                        var vr = await uploader.UploadAsync();
                        if (vr == null) throw new ArgumentNullException("Upload error, no response!");
                        UploadProgress = 100;
                        VideoUploadResult vur = JsonConvert.DeserializeObject<VideoUploadResult>(vr);
                        Attachment = new Video { Id = vur.VideoId, OwnerId = vur.OwnerId, AccessKey = (server as VideoUploadServer).AccessKey };
                        ExtraInfo = "--:--";
                        break;
                }
                UploadState = OutboundAttachmentUploadState.Success;
                return true;
            }
            catch (Exception ex)
            {
                Log.General.Error($"File upload error! Type: {uploadFileType.ToString()}", ex);
                UploadState = OutboundAttachmentUploadState.Failed;
                UploadProgress = 0;
                return false;
            }
        }

        private void Uploader_UploadFailed(Exception e)
        {
            Log.General.Error($"Exception was thrown in uploader!", e);
            UploadState = OutboundAttachmentUploadState.Failed;
            UploadProgress = 0;
        }

        private void Uploader_ProgressChanged(double totalBytes, double bytesSent, double percent, string debugInfo)
        {
            UploadProgress = percent;
            switch (uploadFileType)
            {
                case OutboundAttachmentUploadFileType.Photo:
                    APIHelper.SendActivity(ELOR.VKAPILib.Methods.ActivityType.Photo, peerId);
                    break;
                case OutboundAttachmentUploadFileType.Video:
                    APIHelper.SendActivity(ELOR.VKAPILib.Methods.ActivityType.Video, peerId);
                    break;
                case OutboundAttachmentUploadFileType.Doc:
                    APIHelper.SendActivity(ELOR.VKAPILib.Methods.ActivityType.File, peerId);
                    break;
                case OutboundAttachmentUploadFileType.AudioMessage:
                    APIHelper.SendActivity(ELOR.VKAPILib.Methods.ActivityType.Audiomessage, peerId);
                    break;
            }
        }

        private IFileUploader GetUploader(string type, Uri uploadUri, StorageFile file)
        {
            if (Core.Settings.AlternativeUploadMethod)
            {
                return new VKBackgroundFileUploader(type, uploadUri, file);
            }
            else
            {
                return new VKHttpClientFileUploader(type, uploadUri, file);
            }
        }

        public void CancelUpload()
        {
            if (uploader == null) return;
            uploader.CancelUpload();
        }

        #endregion

        #region Forwarded messages

        private void UpdateUIForFwdMessages()
        {
            int c = ForwardedMessages.Count;
            DisplayName = $"{c} messages";
            Icon = (DataTemplate)Application.Current.Resources["Icon24Discussions"];
        }

        #endregion

        public override string ToString()
        {
            switch (Type)
            {
                case OutboundAttachmentType.Attachment: return Attachment.ToString();
                case OutboundAttachmentType.ForwardedMessages: return String.Join(",", ForwardedMessages.Select(m => m.Id));
                default: return null;
            }
        }
    }
}
