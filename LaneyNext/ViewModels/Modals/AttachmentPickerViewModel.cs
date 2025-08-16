using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class AttachmentPickerViewModel : BaseViewModel
    {
        private ObservableCollection<PhotoAlbum> _photoAlbums;
        private PhotoAlbum _selectedPhotoAlbum;
        private ObservableCollection<VideoAlbum> _videoAlbums;
        private VideoAlbum _selectedVideoAlbum;
        private int _documentTypeIndex = -1;

        private ObservableCollection<Photo> _photos = new ObservableCollection<Photo>();
        private ObservableCollection<Video> _videos = new ObservableCollection<Video>();
        private ObservableCollection<Document> _documents = new ObservableCollection<Document>();
        private ObservableCollection<AttachmentBase> _selectedAttachments = new ObservableCollection<AttachmentBase>();
        private int _selectedAttachmentsCount;

        private bool _isPhotosLoading;
        private bool _isVideosLoading;
        private bool _isDocsLoading;

        private PlaceholderViewModel _photosPlaceholder;
        private PlaceholderViewModel _videosPlaceholder;
        private PlaceholderViewModel _docsPlaceholder;

        public ObservableCollection<PhotoAlbum> PhotoAlbums { get { return _photoAlbums; } private set { _photoAlbums = value; OnPropertyChanged(); } }
        public PhotoAlbum SelectedPhotoAlbum { get { return _selectedPhotoAlbum; } set { _selectedPhotoAlbum = value; OnPropertyChanged(); } }
        public ObservableCollection<VideoAlbum> VideoAlbums { get { return _videoAlbums; } private set { _videoAlbums = value; OnPropertyChanged(); } }
        public VideoAlbum SelectedVideoAlbum { get { return _selectedVideoAlbum; } set { _selectedVideoAlbum = value; OnPropertyChanged(); } }
        public int DocumentTypeIndex { get { return _documentTypeIndex; } set { _documentTypeIndex = value; OnPropertyChanged(); } }

        public ObservableCollection<Photo> Photos { get { return _photos; } private set { _photos = value; OnPropertyChanged(); } }
        public ObservableCollection<Video> Videos { get { return _videos; } private set { _videos = value; OnPropertyChanged(); } }
        public ObservableCollection<Document> Documents { get { return _documents; } private set { _documents = value; OnPropertyChanged(); } }
        public ObservableCollection<AttachmentBase> SelectedAttachments { get { return _selectedAttachments; } set { _selectedAttachments = value; OnPropertyChanged(); } }
        public int SelectedAttachmentsCount { get { return _selectedAttachmentsCount; } private set { _selectedAttachmentsCount = value; OnPropertyChanged(); } }

        public bool IsPhotosLoading { get { return _isPhotosLoading; } private set { _isPhotosLoading = value; OnPropertyChanged(); } }
        public bool IsVideosLoading { get { return _isVideosLoading; } private set { _isVideosLoading = value; OnPropertyChanged(); } }
        public bool IsDocsLoading { get { return _isDocsLoading; } private set { _isDocsLoading = value; OnPropertyChanged(); } }

        public PlaceholderViewModel PhotosPlaceholder { get { return _photosPlaceholder; } set { _photosPlaceholder = value; OnPropertyChanged(); } }
        public PlaceholderViewModel VideosPlaceholder { get { return _videosPlaceholder; } set { _videosPlaceholder = value; OnPropertyChanged(); } }
        public PlaceholderViewModel DocsPlaceholder { get { return _docsPlaceholder; } set { _docsPlaceholder = value; OnPropertyChanged(); } }

        private bool noMorePhotos = false;
        private bool noMoreVideos = false;
        private bool noMoreDocs = false;

        public AttachmentPickerViewModel()
        {
            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(SelectedPhotoAlbum): Photos.Clear(); noMorePhotos = false; LoadPhotos(); break;
                    case nameof(SelectedVideoAlbum): Videos.Clear(); noMoreVideos = false; LoadVideos(); break;
                    case nameof(DocumentTypeIndex): Documents.Clear(); noMoreDocs = false; LoadDocuments(); break;
                }
            };

            SelectedAttachments.CollectionChanged += UpdateCounter;
        }

        #region Photos

        public async void LoadPhotoAlbums()
        {
            if (IsPhotosLoading) return;
            PhotosPlaceholder = null;
            IsPhotosLoading = true;

            try
            {
                VKList<PhotoAlbum> albums = await VKSession.Current.API.Photos.GetAlbumsAsync(VKSession.Current.SessionId, null, 0, 100, true, true);
                PhotoAlbums = new ObservableCollection<PhotoAlbum>(albums.Items);
                PhotoAlbums.Insert(0, new PhotoAlbum { Id = 0, Title = Locale.Get("all") });
                SelectedPhotoAlbum = PhotoAlbums.First();
            }
            catch (Exception ex)
            {
                PhotosPlaceholder = PlaceholderViewModel.GetForException(ex, () => LoadPhotoAlbums());
            }

            IsPhotosLoading = false;
        }

        public async void LoadPhotos()
        {
            if (IsPhotosLoading || noMorePhotos) return;
            PhotosPlaceholder = null;
            IsPhotosLoading = true;

            try
            {
                VKList<Photo> photos = SelectedPhotoAlbum.Id != 0 ?
                    await VKSession.Current.API.Photos.GetAsync(VKSession.Current.SessionId, SelectedPhotoAlbum.Id, null, true, false, Photos.Count, 100) :
                    await VKSession.Current.API.Photos.GetAllAsync(VKSession.Current.SessionId, false, Photos.Count, 100);
                noMorePhotos = photos.Items.Count == 0;
                photos.Items.ForEach(p => Photos.Add(p));
            }
            catch (Exception ex)
            {
                if (Photos.Count == 0)
                {
                    PhotosPlaceholder = PlaceholderViewModel.GetForException(ex, () => LoadPhotos());
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex)) LoadPhotos();
                }
            }

            IsPhotosLoading = false;
        }

        #endregion

        #region Video

        public async void LoadVideoAlbums()
        {
            if (IsVideosLoading) return;
            VideosPlaceholder = null;
            IsVideosLoading = true;

            try
            {
                VKList<VideoAlbum> albums = await VKSession.Current.API.Video.GetAlbumsAsync(VKSession.Current.SessionId, 0, 100, false, true);
                VideoAlbums = new ObservableCollection<VideoAlbum>(albums.Items);
                VideoAlbums.Insert(0, new VideoAlbum { Id = 0, Title = Locale.Get("all") });
                SelectedVideoAlbum = VideoAlbums.First();
            }
            catch (Exception ex)
            {
                VideosPlaceholder = PlaceholderViewModel.GetForException(ex, () => LoadVideoAlbums());
            }

            IsVideosLoading = false;
        }

        public async void LoadVideos()
        {
            if (IsVideosLoading || noMoreVideos) return;
            VideosPlaceholder = null;
            IsVideosLoading = true;

            try
            {
                VKList<Video> videos = await VKSession.Current.API.Video.GetAsync(VKSession.Current.SessionId, null, SelectedVideoAlbum.Id, Videos.Count, 100);
                noMoreVideos = videos.Items.Count == 0;
                videos.Items.ForEach(v => Videos.Add(v));
            }
            catch (Exception ex)
            {
                if (Videos.Count == 0)
                {
                    VideosPlaceholder = PlaceholderViewModel.GetForException(ex, () => LoadVideos());
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex)) LoadVideos();
                }
            }

            IsVideosLoading = false;
        }

        #endregion

        #region Documents

        public async void LoadDocuments()
        {
            if (IsDocsLoading || noMoreDocs) return;
            DocsPlaceholder = null;
            IsDocsLoading = true;

            try
            {
                VKList<Document> docs = await VKSession.Current.API.Docs.GetAsync(VKSession.Current.SessionId, DocumentTypeIndex, Documents.Count, 100);
                noMoreDocs = docs.Items.Count == 0;
                docs.Items.ForEach(d => Documents.Add(d));
            }
            catch (Exception ex)
            {
                if (Documents.Count == 0)
                {
                    DocsPlaceholder = PlaceholderViewModel.GetForException(ex, () => LoadDocuments());
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex)) LoadDocuments();
                }
            }

            IsDocsLoading = false;
        }

        #endregion

        private void UpdateCounter(object sender, NotifyCollectionChangedEventArgs e)
        {
            SelectedAttachmentsCount = SelectedAttachments.Count;
        }
    }
}
