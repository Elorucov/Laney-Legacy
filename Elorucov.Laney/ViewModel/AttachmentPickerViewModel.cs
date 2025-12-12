using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Elorucov.Laney.ViewModel {
    public class AttachmentPickerViewModel : BaseViewModel {
        private ObservableCollection<AlbumLite> _photoAlbums;
        private AlbumLite _selectedPhotoAlbum;
        private ObservableCollection<AlbumLite> _videoAlbums;
        private AlbumLite _selectedVideoAlbum;
        private ObservableCollection<AudioPlaylist> _audioPlaylists;
        private AudioPlaylist _selectedAudioPlaylist;
        private int _documentTypeIndex = -1;

        private ObservableCollection<Photo> _photos = new ObservableCollection<Photo>();
        private ObservableCollection<Video> _videos = new ObservableCollection<Video>();
        private ObservableCollection<Document> _documents = new ObservableCollection<Document>();
        private ObservableCollection<Audio> _audios = new ObservableCollection<Audio>();
        private ObservableCollection<AttachmentBase> _selectedAttachments = new ObservableCollection<AttachmentBase>();
        private int _selectedAttachmentsCount;

        private bool _isPhotosLoading;
        private bool _isVideosLoading;
        private bool _isAudiosLoading;
        private bool _isDocsLoading;

        private PlaceholderViewModel _photosPlaceholder;
        private PlaceholderViewModel _videosPlaceholder;
        private PlaceholderViewModel _audiosPlaceholder;
        private PlaceholderViewModel _docsPlaceholder;

        public ObservableCollection<AlbumLite> PhotoAlbums { get { return _photoAlbums; } private set { _photoAlbums = value; OnPropertyChanged(); } }
        public AlbumLite SelectedPhotoAlbum { get { return _selectedPhotoAlbum; } set { _selectedPhotoAlbum = value; OnPropertyChanged(); } }
        public ObservableCollection<AlbumLite> VideoAlbums { get { return _videoAlbums; } private set { _videoAlbums = value; OnPropertyChanged(); } }
        public AlbumLite SelectedVideoAlbum { get { return _selectedVideoAlbum; } set { _selectedVideoAlbum = value; OnPropertyChanged(); } }
        public ObservableCollection<AudioPlaylist> AudioPlaylists { get { return _audioPlaylists; } private set { _audioPlaylists = value; OnPropertyChanged(); } }
        public AudioPlaylist SelectedAudioPlaylist { get { return _selectedAudioPlaylist; } set { _selectedAudioPlaylist = value; OnPropertyChanged(); } }
        public int DocumentTypeIndex { get { return _documentTypeIndex; } set { _documentTypeIndex = value; OnPropertyChanged(); } }

        public ObservableCollection<Photo> Photos { get { return _photos; } private set { _photos = value; OnPropertyChanged(); } }
        public ObservableCollection<Video> Videos { get { return _videos; } private set { _videos = value; OnPropertyChanged(); } }
        public ObservableCollection<Document> Documents { get { return _documents; } private set { _documents = value; OnPropertyChanged(); } }
        public ObservableCollection<Audio> Audios { get { return _audios; } private set { _audios = value; OnPropertyChanged(); } }
        public ObservableCollection<AttachmentBase> SelectedAttachments { get { return _selectedAttachments; } set { _selectedAttachments = value; OnPropertyChanged(); } }
        public int SelectedAttachmentsCount { get { return _selectedAttachmentsCount; } private set { _selectedAttachmentsCount = value; OnPropertyChanged(); } }

        public bool IsPhotosLoading { get { return _isPhotosLoading; } private set { _isPhotosLoading = value; OnPropertyChanged(); } }
        public bool IsVideosLoading { get { return _isVideosLoading; } private set { _isVideosLoading = value; OnPropertyChanged(); } }
        public bool IsAudiosLoading { get { return _isAudiosLoading; } private set { _isAudiosLoading = value; OnPropertyChanged(); } }
        public bool IsDocsLoading { get { return _isDocsLoading; } private set { _isDocsLoading = value; OnPropertyChanged(); } }

        public PlaceholderViewModel PhotosPlaceholder { get { return _photosPlaceholder; } set { _photosPlaceholder = value; OnPropertyChanged(); } }
        public PlaceholderViewModel VideosPlaceholder { get { return _videosPlaceholder; } set { _videosPlaceholder = value; OnPropertyChanged(); } }
        public PlaceholderViewModel AudiosPlaceholder { get { return _audiosPlaceholder; } set { _audiosPlaceholder = value; OnPropertyChanged(); } }
        public PlaceholderViewModel DocsPlaceholder { get { return _docsPlaceholder; } set { _docsPlaceholder = value; OnPropertyChanged(); } }

        private bool noMorePhotos = false;
        private bool noMoreVideos = false;
        private bool noMoreDocs = false;
        private bool noMoreAudios = false;
        private bool allAudiosPreloaded = true;

        public AttachmentPickerViewModel() {
            PropertyChanged += async (a, b) => {
                switch (b.PropertyName) {
                    case nameof(SelectedPhotoAlbum): Photos.Clear(); noMorePhotos = false; await LoadPhotosAsync(); break;
                    case nameof(SelectedVideoAlbum): Videos.Clear(); noMoreVideos = false; await LoadVideosAsync(); break;
                    case nameof(SelectedAudioPlaylist):
                        if (allAudiosPreloaded) {
                            allAudiosPreloaded = false;
                        } else {
                            Audios.Clear(); await LoadAudiosAsync();
                        }
                        break;
                    case nameof(DocumentTypeIndex): Documents.Clear(); noMoreDocs = false; await LoadDocumentsAsync(); break;
                }
            };

            SelectedAttachments.CollectionChanged += UpdateCounter;
        }

        #region Photos

        public async Task LoadPhotoAlbumsAsync() {
            if (IsPhotosLoading) return;
            PhotosPlaceholder = null;
            IsPhotosLoading = true;

            object resp = await Execute.GetPhotoAlbums(AppParameters.UserID);
            if (resp is List<AlbumLite> albums) {
                if (albums.Count > 0) albums[0].Title = Locale.Get("all");
                PhotoAlbums = new ObservableCollection<AlbumLite>(albums);
                if (PhotoAlbums.Count > 0) SelectedPhotoAlbum = PhotoAlbums.First();
            } else {
                PhotosPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await LoadPhotoAlbumsAsync());
            }

            IsPhotosLoading = false;
        }

        public async Task LoadPhotosAsync() {
            if (IsPhotosLoading || noMorePhotos) return;
            PhotosPlaceholder = null;
            IsPhotosLoading = true;

            object resp;
            long albumId = 0;
            if (SelectedPhotoAlbum != null) albumId = SelectedPhotoAlbum.Id;
            switch (albumId) {
                case 0:
                    resp = await VkAPI.Methods.Photos.GetAll(AppParameters.UserID, true, Photos.Count);
                    break;
                case -9000:
                    resp = await VkAPI.Methods.Photos.GetUserPhotos(AppParameters.UserID, true, Photos.Count);
                    break;
                default:
                    resp = await VkAPI.Methods.Photos.Get(AppParameters.UserID, SelectedPhotoAlbum.Id, true, true, Photos.Count);
                    break;
            }

            if (resp is VKList<Photo> respPhotos) {
                noMorePhotos = respPhotos.Items.Count == 0;
                respPhotos.Items.ForEach((p) => Photos.Add(p));
            } else {
                if (Photos.Count == 0) {
                    PhotosPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await LoadPhotosAsync());
                } else {
                    Functions.ShowHandledErrorDialog(resp, async () => await LoadPhotosAsync());
                }
            }

            IsPhotosLoading = false;
        }

        #endregion

        #region Video

        public async Task LoadVideoAlbumsAsync() {
            if (IsVideosLoading) return;
            VideosPlaceholder = null;
            IsVideosLoading = true;

            object resp = await Execute.GetVideoAlbums(AppParameters.UserID);
            if (resp is List<AlbumLite> albums) {
                VideoAlbums = new ObservableCollection<AlbumLite>(albums);
                if (VideoAlbums.Count > 0) SelectedVideoAlbum = VideoAlbums.First();
            } else {
                VideosPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await LoadVideoAlbumsAsync());
            }

            IsVideosLoading = false;
        }

        public async Task LoadVideosAsync() {
            if (IsVideosLoading || noMoreVideos) return;
            VideosPlaceholder = null;
            IsVideosLoading = true;

            long albumId = 0;
            if (SelectedVideoAlbum != null) albumId = SelectedVideoAlbum.Id;
            object resp = await VkAPI.Methods.Videos.Get(AppParameters.UserID, Videos.Count, 50, albumId);
            if (resp is VKList<Video> videos) {
                noMoreVideos = videos.Items.Count == 0;
                videos.Items.ForEach(v => Videos.Add(v));
            } else {
                if (Videos.Count == 0) {
                    VideosPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await LoadVideosAsync());
                } else {
                    Functions.ShowHandledErrorDialog(resp, async () => await LoadVideosAsync());
                }
            }

            IsVideosLoading = false;
        }

        #endregion

        #region Documents

        public async Task LoadDocumentsAsync() {
            if (IsDocsLoading || noMoreDocs) return;
            DocsPlaceholder = null;
            IsDocsLoading = true;

            object resp = await VkAPI.Methods.Docs.Get(50, Documents.Count, DocumentTypeIndex, AppParameters.UserID);
            if (resp is VKList<Document> docs) {
                docs.Items.ForEach(d => Documents.Add(d));
            } else {
                if (Documents.Count == 0) {
                    DocsPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await LoadDocumentsAsync());
                } else {
                    Functions.ShowHandledErrorDialog(resp, async () => await LoadDocumentsAsync());
                }
            }

            IsDocsLoading = false;
        }

        #endregion

        #region Audios

        public async Task LoadAudioPlaylistsAsync() {
            if (IsAudiosLoading) return;
            AudiosPlaceholder = null;
            IsAudiosLoading = true;

            object resp = await Execute.GetAudiosAndPlaylists();
            if (resp is AudiosAndPlaylistsResponse apr) {
                AudioPlaylists = new ObservableCollection<AudioPlaylist>(apr.Playlists.Items);
                AudioPlaylists.Insert(0, new AudioPlaylist { Id = 0, Title = Locale.Get("all"), Count = apr.Audios.Count });
                SelectedAudioPlaylist = AudioPlaylists.FirstOrDefault();
                Audios = new ObservableCollection<Audio>(apr.Audios.Items);
            } else {
                AudiosPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await LoadAudioPlaylistsAsync());
            }

            IsAudiosLoading = false;
        }

        public async Task LoadAudiosAsync() {
            long albumId = 0;
            if (SelectedAudioPlaylist != null) albumId = SelectedAudioPlaylist.Id;

            if (IsAudiosLoading || noMoreAudios) return;
            AudiosPlaceholder = null;
            IsAudiosLoading = true;

            object resp = await VkAPI.Methods.Audios.Get(albumId, Audios.Count);
            if (resp is VKList<Audio> audios) {
                noMoreAudios = audios.Items.Count == 0;
                foreach (Audio a in audios.Items) {
                    Audios.Add(a);
                }
            } else {
                if (Audios.Count == 0) {
                    AudiosPlaceholder = PlaceholderViewModel.GetForHandledError(resp, async () => await LoadAudiosAsync());
                } else {
                    Functions.ShowHandledErrorDialog(resp, async () => await LoadAudiosAsync());
                }
            }

            IsAudiosLoading = false;
        }

        #endregion

        private void UpdateCounter(object sender, NotifyCollectionChangedEventArgs e) {
            SelectedAttachmentsCount = SelectedAttachments.Count;
        }
    }
}