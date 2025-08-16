using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Views.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;

namespace Elorucov.Laney.ViewModels
{
    public class PhotoViewerViewModel : BaseViewModel
    {
        private ThreadSafeObservableCollection<PhotoViewerItem> _images;
        private PhotoViewerItem _currentImage;

        private RelayCommand _shareCommand;
        private RelayCommand _saveToCommand;
        private RelayCommand _downloadCommand;

        public ThreadSafeObservableCollection<PhotoViewerItem> Images { get { return _images; } set { _images = value; OnPropertyChanged(); } }
        public PhotoViewerItem CurrentImage { get { return _currentImage; } set { _currentImage = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaveToCommandName)); } }
        public string SaveToCommandName { get { return Locale.Get(CurrentImage.Attachment is Photo ? "save_to_album" : "save_to_docs"); } }

        public RelayCommand ShareCommand { get { return _shareCommand; } set { _shareCommand = value; OnPropertyChanged(); } }
        public RelayCommand SaveToCommand { get { return _saveToCommand; } set { _saveToCommand = value; OnPropertyChanged(); } }
        public RelayCommand DownloadCommand { get { return _downloadCommand; } set { _downloadCommand = value; OnPropertyChanged(); } }

        bool isProcessing = false;
        private VKSession CallerSession;
        public PhotoViewerViewModel(List<AttachmentBase> attachments, AttachmentBase selected, VKSession caller)
        {
            Log.General.Info("Init photoviewer", new ValueSet { { "caller_session", caller.SessionId } });
            CallerSession = caller;
            Images = new ThreadSafeObservableCollection<PhotoViewerItem>();
            foreach (AttachmentBase a in attachments)
            {
                PhotoViewerItem pvi = new PhotoViewerItem(a);
                Images.Add(pvi);
                if (a.Id == selected.Id) CurrentImage = pvi;
            }

            ShareCommand = new RelayCommand(o => Share());
            SaveToCommand = new RelayCommand(o => SaveTo());
            DownloadCommand = new RelayCommand(o => Download());
        }

        private async void Share()
        {
            var view = await ViewManagement.GetViewBySession(CallerSession);
            ViewManagement.SwitchToView(view, () =>
            {
                InternalSharing ish = new InternalSharing(CurrentImage.Attachment);
                ish.Show();
            });
        }

        private async void SaveTo()
        {
            if (isProcessing) return;
            isProcessing = true;
            try
            {
                if (CurrentImage.Attachment is Photo p)
                {
                    int pid = await CallerSession.API.Photos.CopyAsync(p.OwnerId, p.Id, p.AccessKey);
                    await (new MessageDialog(String.Empty, Locale.Get("saved_to_album"))).ShowAsync(); // TODO: Snackbar
                }
                else if (CurrentImage.Attachment is Document d)
                {
                    int did = await CallerSession.API.Docs.AddAsync(d.OwnerId, d.Id, d.AccessKey);
                    await (new MessageDialog(String.Empty, Locale.Get("saved_to_docs"))).ShowAsync(); // TODO: Snackbar
                }
            }
            catch (Exception ex)
            {
                bool r = await ExceptionHelper.ShowErrorDialogAsync(ex);
                if (r) SaveTo();
            }
            isProcessing = false;
        }

        private async void Download()
        {
            if (isProcessing) return;
            isProcessing = true;
            try
            {
                Uri url = null;
                StorageFile file = null;
                string downloaded = String.Empty;

                if (CurrentImage.Attachment is Photo p)
                {
                    downloaded = Locale.Get("photoviewer_photo_downloaded");
                    url = p.MaximalSizedPhoto.Uri;
                    file = await KnownFolders.SavedPictures.CreateFileAsync($"photo{p.OwnerId}_{p.Id}_{url.Segments.Last()}", CreationCollisionOption.GenerateUniqueName);
                }
                else if (CurrentImage.Attachment is Document d)
                {
                    downloaded = Locale.Get("downloaded");
                    url = d.Uri;
                    file = await DownloadsFolder.CreateFileAsync(d.Title, CreationCollisionOption.GenerateUniqueName);
                }

                Log.General.Info("Starting download file", new ValueSet { { "url", url.AbsoluteUri }, { "file", file.Path } });

                using (var httpClient = new HttpClient())
                {
                    using (var resp = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url)))
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            byte[] barray = await resp.Content.ReadAsByteArrayAsync();
                            await FileIO.WriteBytesAsync(file, barray);

                            // TODO: snackbar
                            await (new MessageDialog(file.Path, downloaded)).ShowAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bool r = await ExceptionHelper.ShowErrorDialogAsync(ex);
                if (r) Download();
            }
            isProcessing = false;
        }
    }
}
