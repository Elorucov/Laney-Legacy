using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.ViewModels.Controls;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    public sealed partial class AttachmentPicker : Modal
    {
        private AttachmentPickerViewModel ViewModel { get { return DataContext as AttachmentPickerViewModel; } }

        public AttachmentPicker(int index)
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("ms-appx:///Styles/StylesWithoutKey.xaml") });
            this.InitializeComponent();
            DataContext = new AttachmentPickerViewModel();
            ThePivot.SelectionChanged += (a, b) => LoadPivotItem();
            ThePivot.SelectedIndex = index;
        }

        private void LoadPivotItem()
        {
            switch (ThePivot.SelectedIndex)
            {
                case 0: if (ViewModel.PhotoAlbums == null) ViewModel.LoadPhotoAlbums(); break;
                case 1: if (ViewModel.VideoAlbums == null) ViewModel.LoadVideoAlbums(); break;
                case 2: if (ViewModel.DocumentTypeIndex < 0) ViewModel.DocumentTypeIndex = 0; break;
            }
        }

        private void RegisterIncrementalLoadingEvent(object sender, RoutedEventArgs e)
        {
            if (sender is ListViewBase lvb)
            {
                lvb.GetScrollViewer().RegisterIncrementalLoadingEvent(() =>
                {
                    switch (Int32.Parse(lvb.Tag.ToString()))
                    {
                        case 1: ViewModel.LoadPhotos(); break;
                        case 2: ViewModel.LoadVideos(); break;
                        case 3: ViewModel.LoadDocuments(); break;
                    }
                });
            }
        }

        private void ItemChecked(object sender, SelectionChangedEventArgs e)
        {
            var photos = PhotosGrid.SelectedItems;
            var videos = VideosGrid.SelectedItems;
            var docs = DocsList.SelectedItems;

            int count = photos.Count + videos.Count + docs.Count;
            Debug.WriteLine($"Count: {count}; Added count: {e.AddedItems.Count}; Removed count: {e.RemovedItems.Count}");
            if (e.AddedItems.Count == 1) Debug.WriteLine($"Added last: {e.AddedItems.Last()}");
            if (e.RemovedItems.Count == 1) Debug.WriteLine($"Removed last: {e.RemovedItems.Last()}");

            int max = 10 - ViewModels.ConversationViewModel.CurrentFocused.MessageInput.AttachmentsCount;
            if (count > max)
            {
                (sender as ListViewBase).SelectedItems.Remove(e.AddedItems.Last());
            }
            else
            {
                if (e.AddedItems.Count > 0) ViewModel.SelectedAttachments.Add(e.AddedItems.Last() as AttachmentBase);
                foreach (var i in e.RemovedItems)
                {
                    if (i is AttachmentBase a)
                    {
                        ViewModel.SelectedAttachments.Remove(a);
                    }
                }
            }
        }

        private void AttachAndClose(object sender, RoutedEventArgs e)
        {
            foreach (var a in ViewModel.SelectedAttachments)
            {
                ViewModels.ConversationViewModel.CurrentFocused.MessageInput.Attach(a);
            }
            Hide();
        }

        #region Buttons in title

        private async void ShowCameraCaptureWindowForPhoto(object sender, RoutedEventArgs e)
        {
            var captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;

            var photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (photo != null) Attach(new List<StorageFile> { photo }, OutboundAttachmentUploadFileType.Photo);
        }

        private async void OpenFilePickerForPhoto(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fop = new FileOpenPicker();
            fop.FileTypeFilter.Add(".jpg");
            fop.FileTypeFilter.Add(".jpeg");
            fop.FileTypeFilter.Add(".png");
            fop.FileTypeFilter.Add(".bmp");
            fop.FileTypeFilter.Add(".gif");
            fop.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fop.ViewMode = PickerViewMode.Thumbnail;
            IReadOnlyList<StorageFile> files = await fop.PickMultipleFilesAsync();
            if (files != null) Attach(files, OutboundAttachmentUploadFileType.Photo);
        }

        private async void ShowCameraCaptureWindowForVideo(object sender, RoutedEventArgs e)
        {
            var captureUI = new CameraCaptureUI();
            captureUI.VideoSettings.AllowTrimming = true;
            captureUI.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;

            var video = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Video);
            if (video != null) Attach(new List<StorageFile> { video }, OutboundAttachmentUploadFileType.Video);
        }

        private async void OpenFilePickerForVideo(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fop = new FileOpenPicker();
            fop.FileTypeFilter.Add(".avi");
            fop.FileTypeFilter.Add(".mp4");
            fop.FileTypeFilter.Add(".3gp");
            fop.FileTypeFilter.Add(".mpeg");
            fop.FileTypeFilter.Add(".mpg");
            fop.FileTypeFilter.Add(".mov");
            fop.FileTypeFilter.Add(".wmv");
            fop.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            fop.ViewMode = PickerViewMode.Thumbnail;
            IReadOnlyList<StorageFile> files = await fop.PickMultipleFilesAsync();
            if (files != null) Attach(files, OutboundAttachmentUploadFileType.Video);
        }

        private async void OpenFilePickerForDocs(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fop = new FileOpenPicker();
            fop.FileTypeFilter.Add("*");
            fop.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            fop.ViewMode = PickerViewMode.List;
            IReadOnlyList<StorageFile> files = await fop.PickMultipleFilesAsync();
            if (files != null) Attach(files, OutboundAttachmentUploadFileType.Doc);
        }

        #endregion

        private void Attach(IEnumerable<StorageFile> files, OutboundAttachmentUploadFileType type)
        {
            Hide();
            ConversationViewModel current = ConversationViewModel.CurrentFocused;
            foreach (StorageFile file in files)
            {
                OutboundAttachmentViewModel oavm = OutboundAttachmentViewModel.CreateFromFile(current.Id, file, type);
                current.MessageInput.Attach(oavm);
            }
        }
    }
}
