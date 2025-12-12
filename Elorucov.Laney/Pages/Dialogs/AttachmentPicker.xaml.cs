using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Pages.Dialogs {
    public enum AttachmentPickerResultType {
        Attachments, PhotoFiles, VideoFiles, Files
    }

    public class AttachmentPickerResult {
        public AttachmentPickerResultType Type { get; private set; }
        public IEnumerable<StorageFile> Files { get; private set; }
        public IEnumerable<AttachmentBase> Attachments { get; private set; }

        public AttachmentPickerResult(StorageFile file, AttachmentPickerResultType type = AttachmentPickerResultType.Files) {
            Type = type;
            Files = new List<StorageFile> { file };
        }

        public AttachmentPickerResult(IEnumerable<StorageFile> files, AttachmentPickerResultType type = AttachmentPickerResultType.Files) {
            Type = type;
            Files = files;
        }

        public AttachmentPickerResult(AttachmentBase attachment) {
            Type = AttachmentPickerResultType.Attachments;
            Attachments = new List<AttachmentBase> { attachment };
        }

        public AttachmentPickerResult(IEnumerable<AttachmentBase> attachments) {
            Type = AttachmentPickerResultType.Attachments;
            Attachments = attachments;
        }
    }

    public sealed partial class AttachmentPicker : Modal {
        private AttachmentPickerViewModel ViewModel { get { return DataContext as AttachmentPickerViewModel; } }
        private int limit = 10;

        public AttachmentPicker(int limit, int tab = 0) {
            this.InitializeComponent();
            this.limit = limit;
            DataContext = new AttachmentPickerViewModel();

            MainPivot.SelectionChanged += async (a, b) => await LoadPivotItemAsync();
            if (tab >= 0 && tab < MainPivot.Items.Count) MainPivot.SelectedIndex = tab;
        }

        private async Task LoadPivotItemAsync() {
            switch (MainPivot.SelectedIndex) {
                case 0: if (ViewModel.PhotoAlbums == null) await ViewModel.LoadPhotoAlbumsAsync(); break;
                case 1: if (ViewModel.VideoAlbums == null) await ViewModel.LoadVideoAlbumsAsync(); break;
                case 2: if (ViewModel.DocumentTypeIndex < 0) ViewModel.DocumentTypeIndex = 0; break;
                case 3: if (ViewModel.AudioPlaylists == null) await ViewModel.LoadAudioPlaylistsAsync(); break;
            }
        }

        private void RegisterIncrementalLoadingEvent(object sender, RoutedEventArgs e) {
            if (sender is ListViewBase lvb) {
                ScrollViewer sv = lvb.GetScrollViewerFromListView();
                if (sv != null) sv.ViewChanged += async (a, b) => {
                    if (sv.VerticalOffset >= sv.ScrollableHeight - 128) {
                        switch (Int32.Parse(lvb.Tag.ToString())) {
                            case 1: await ViewModel.LoadPhotosAsync(); break;
                            case 2: await ViewModel.LoadVideosAsync(); break;
                            case 3: await ViewModel.LoadDocumentsAsync(); break;
                            case 4: await ViewModel.LoadAudiosAsync(); break;
                        }
                    }
                };
            }
        }

        private void ItemChecked(object sender, SelectionChangedEventArgs e) {
            var photos = PhotosGrid.SelectedItems;
            var videos = VideosList.SelectedItems;
            var docs = DocsList.SelectedItems;
            var audios = AudiosList.SelectedItems;

            int count = photos.Count + videos.Count + docs.Count + audios.Count;
            Debug.WriteLine($"Count: {count}; Added count: {e.AddedItems.Count}; Removed count: {e.RemovedItems.Count}");
            if (e.AddedItems.Count == 1) Debug.WriteLine($"Added last: {e.AddedItems.Last()}");
            if (e.RemovedItems.Count == 1) Debug.WriteLine($"Removed last: {e.RemovedItems.Last()}");
            AttachButton.IsEnabled = count > 0;
            CounterBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (count > limit) {
                (sender as ListViewBase).SelectedItems.Remove(e.AddedItems.Last());
            } else {
                if (e.AddedItems.Count > 0) ViewModel.SelectedAttachments.Add(e.AddedItems.Last() as AttachmentBase);
                foreach (var i in e.RemovedItems) {
                    if (i is AttachmentBase a) {
                        ViewModel.SelectedAttachments.Remove(a);
                    }
                }
            }
        }

        private void AttachAndClose(object sender, RoutedEventArgs e) {
            Hide(new AttachmentPickerResult(ViewModel.SelectedAttachments.ToList()));
        }

        #region Buttons in title

        private void ShowCameraCaptureWindowForPhoto(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                var captureUI = new CameraCaptureUI();
                captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;

                var photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
                if (photo != null) Hide(new AttachmentPickerResult(photo, AttachmentPickerResultType.PhotoFiles));
            })();
        }

        private void OpenFilePickerForPhoto(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                FileOpenPicker fop = new FileOpenPicker();
                foreach (string format in DataPackageParser.ImageFormats) {
                    fop.FileTypeFilter.Add(format);
                }
                fop.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                fop.ViewMode = PickerViewMode.Thumbnail;
                var files = await fop.PickMultipleFilesAsync();

                if (files != null && files.Count() > 0) Hide(new AttachmentPickerResult(files.Take(limit), AttachmentPickerResultType.PhotoFiles));
            })();
        }

        private void ShowCameraCaptureWindowForVideo(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                var captureUI = new CameraCaptureUI();
                captureUI.VideoSettings.AllowTrimming = true;
                captureUI.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;

                var video = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Video);
                if (video != null) Hide(new AttachmentPickerResult(video, AttachmentPickerResultType.VideoFiles));
            })();
        }

        private void OpenFilePickerForVideo(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                FileOpenPicker fop = new FileOpenPicker();
                foreach (string format in DataPackageParser.VideoFormats) {
                    fop.FileTypeFilter.Add(format);
                }
                fop.SuggestedStartLocation = PickerLocationId.VideosLibrary;
                fop.ViewMode = PickerViewMode.Thumbnail;
                var files = await fop.PickMultipleFilesAsync();

                if (files != null && files.Count() > 0) Hide(new AttachmentPickerResult(files.Take(limit), AttachmentPickerResultType.VideoFiles));
            })();
        }

        private void OpenFilePickerForDocs(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                FileOpenPicker fop = new FileOpenPicker();
                fop.FileTypeFilter.Add("*");
                fop.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                fop.ViewMode = PickerViewMode.List;
                var files = await fop.PickMultipleFilesAsync();

                if (files != null && files.Count() > 0) Hide(new AttachmentPickerResult(files.Take(limit), AttachmentPickerResultType.Files));
            })();
        }

        #endregion

        private void FixPhotosGridMargin(object sender, object e) {
            if (PhotosGrid.ItemsPanelRoot != null) {
                PhotosGrid.ItemsPanelRoot.Margin = new Thickness(12, 2, 8, 0);
            }
            PhotosGrid.LayoutUpdated -= FixPhotosGridMargin;
        }

        private void OpenPhotoViewer(UIElement sender, Windows.UI.Xaml.Input.ContextRequestedEventArgs args) {
            GalleryItem gp = new GalleryItem((sender as FrameworkElement).DataContext as Photo);
            PhotoViewer.Show(new Tuple<List<GalleryItem>, GalleryItem>(new List<GalleryItem> { gp }, gp), true);
        }

        private void UpdateAudiosCount(FrameworkElement sender, DataContextChangedEventArgs args) {
            TextBlock tb = sender as TextBlock;
            AudioPlaylist ap = args.NewValue as AudioPlaylist;
            if (ap == null) {
                tb.Text = string.Empty;
                return;
            }

            var count = ap.Count;
            tb.Text = $"{count} {Locale.GetDeclension(count, $"atch_audio")}";
        }

        private void AudioUC_IsPlayButtonClicked(object sender, Audio a) {
            new System.Action(async () => {
                if (a.ContentRestricted > 0 || String.IsNullOrEmpty(a.Url)) {
                    object r = await Audios.GetRestrictionPopup(a.Id);
                    if (r != null && r is AudioRestrictionInfo info) {
                        await new ContentDialog {
                            Title = info.Title,
                            Content = info.Text,
                            PrimaryButtonText = Locale.Get("close")
                        }.ShowAsync();
                    } else {
                        Functions.ShowHandledErrorDialog(r);
                    }
                    return;
                }
                AudioPlayerViewModel.PlaySong(ViewModel.Audios.ToList(), a, ViewModel.SelectedAudioPlaylist.Title);
            })();
        }
    }
}