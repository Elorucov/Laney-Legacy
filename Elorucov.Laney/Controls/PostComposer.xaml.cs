using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.ViewModel.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class PostComposer : UserControl {
        PostComposerViewModel ViewModel => DataContext as PostComposerViewModel;

        public PostComposer() {
            this.InitializeComponent();
            DataContextChanged += PostComposer_DataContextChanged;

            // To avoid crash on older version of OS
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) splitButton.CornerRadius = new CornerRadius(0, 4, 4, 0);

            // TextBox keydown event handler
            KeyEventHandler keh = new KeyEventHandler(ButtonEvents);
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5)) {
                PostText.AddHandler(TextBox.PreviewKeyDownEvent, keh, true);
            } else {
                PostText.AddHandler(TextBox.KeyDownEvent, keh, true);
            }

            AllowDrop = true;
            DragOver += ShowDropArea;
            DragLeave += HideDropArea;
            Drop += HideDropArea;
            DropDoc.DragOver += DocDragOver;
            DropImg.DragOver += ImgDragOver;
            DropVid.DragOver += VidDragOver;
        }

        private void PostComposer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            DataContextChanged -= PostComposer_DataContextChanged;
            if (AppParameters.FeedDebug) {
                new System.Action(async () => {
                    do {
                        await Task.Delay(500);
                    } while (ViewModel == null);

                    FindName(nameof(debug));
                    UpdateDebugInfo();
                    ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                })();
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            UpdateDebugInfo();
        }

        private void UpdateDebugInfo() {
            var vm = ViewModel;
            var tl = !string.IsNullOrEmpty(vm.Text) ? vm.Text.Length : 0;
            var geo = vm.Geopoint != null ? $"{vm.Geopoint.Position.Latitude}x{vm.Geopoint.Position.Longitude}" : "N/A";
            var time = vm.PublishTime.HasValue ? vm.PublishTime.Value.ToString("yyyy.MM.dd H:mm:ss") : "now";
            debug.Text = $"WallID: {vm.WallOwnerId}; TL: {tl}; AC: {vm.Attachments.Count}; GEO: {geo}\n"
                + $"Availability. PAM: {vm.HasPreviewableAttachments}; P: {vm.IsPrivacyAvailable}; PP: {vm.CanPublishPostponed}; CPFUIG: {vm.CanPublishFromUserInGroup}; NC: {vm.CanDisableComments}; Signer: {vm.CanPostWithSigner}; NN: {vm.CanDisableNotifications}\n"
                + $"States.       PAM: {vm.PrimaryAttachmentsMode}; P: {vm.Privacy}; PP: {time}; PAG: {vm.PostAsGroup}; NC: {vm.DisableComments}; Signer: {vm.ShowSigner}; NN: {vm.DisableNotifications}";
            if (vm.CanPublishOnlyForDons) debug.Text += $"\nDon setting: {vm.DonSetting}; available for all param: {vm.PublishToNonDonsAfterDays}";
        }

        private void OnDateTimePickerFlyoutOpened(object sender, object e) {
            PPDatePicker.MinDate = DateTimeOffset.Now;

            if (ViewModel.PublishTime == null) {
                if (PPDatePicker.SelectedDates.Count > 0) PPDatePicker.SelectedDates.Clear(); // to avoid crash if open flyout second time without first time choice.
                PPDatePicker.SelectedDates.Add(DateTimeOffset.Now.Date);
                PPTimePicker.SelectedTime = DateTime.Now.AddHours(1).AddSeconds(-DateTime.Now.Second).TimeOfDay;
                ViewModel.PublishTime = null; // нельзя установить время публикации сразу после открытия datetimepicker-а.
            }
        }

        private void OnDateChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args) {
            var dateoff = sender.SelectedDates.FirstOrDefault();
            if (dateoff == null) return;

            var date = dateoff.Date;
            if (PPTimePicker.SelectedTime.HasValue) date = date.Add(PPTimePicker.SelectedTime.Value);

            ViewModel.PublishTime = date;
        }

        private void OnTimeChanged(object sender, TimePickerValueChangedEventArgs e) {
            if (!PPTimePicker.SelectedTime.HasValue) return;
            var dateoff = PPDatePicker.SelectedDates.FirstOrDefault();
            if (dateoff == null) return;

            var date = dateoff.Date;
            date = date.Add(PPTimePicker.SelectedTime.Value);

            ViewModel.PublishTime = date;
        }

        private void RemoveOutboundAttachment(object sender, RoutedEventArgs e) {
            OutboundAttachmentViewModel oavm = (sender as FrameworkElement).DataContext as OutboundAttachmentViewModel;
            if (oavm.UploadState == OutboundAttachmentUploadState.InProgress) oavm.CancelUpload();
            ViewModel.Attachments.Remove(oavm);
        }

        private void ChangePrimaryAttachmentsMode(object sender, RoutedEventArgs e) {
            RadioButton rb = sender as RadioButton;
            if (int.TryParse(rb.Tag.ToString(), out int mode)) {
                ViewModel.PrimaryAttachmentsMode = mode;
            }
        }

        private void PostParamsFlyout_Opened(object sender, object e) {
            pamRb1.IsChecked = ViewModel.PrimaryAttachmentsMode != 1;
            pamRb2.IsChecked = ViewModel.PrimaryAttachmentsMode == 1;
        }

        #region Drag'n'Drop

        private void ShowDropArea(object sender, DragEventArgs e) {
            try {
                if (ViewModel == null || DropArea.Visibility == Visibility.Visible) return;
                e.AcceptedOperation = DataPackageOperation.None;
                e.DragUIOverride.IsContentVisible = true;

                if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                    DropDoc.Visibility = Visibility.Collapsed;
                    DropImg.Visibility = Visibility.Collapsed;
                    DropVid.Visibility = Visibility.Collapsed;
                    // выше три строчки нужны, т. к. парсинг файлов идёт не мгновенно,
                    // и можно случайно бросить файлы не туда, куда надо.

                    new System.Action(async () => {
                        var items = await e.DataView.GetStorageItemsAsync();
                        if (items.Count > 10) return;
                        DropArea.Visibility = Visibility.Visible;
                        int imagesCount = 0;
                        int videosCount = 0;
                        foreach (IStorageItem sitem in items) {
                            if (sitem is StorageFile file) {
                                if (DataPackageParser.IsImage(file)) {
                                    imagesCount++;
                                    continue;
                                }
                                if (DataPackageParser.IsVideo(file)) {
                                    videosCount++;
                                    continue;
                                }
                            }
                        }

                        // чтобы не произошло изменения высоты composer-а.
                        ComposerBase.Opacity = 0;
                        ComposerBase.IsHitTestVisible = false;

                        DropDoc.Visibility = Visibility.Visible;
                        if (Math.Min(imagesCount, videosCount) == 0 && Math.Max(imagesCount, videosCount) == items.Count) {
                            bool isImage = imagesCount == items.Count;
                            bool isVideo = videosCount == items.Count;
                            if (isImage) DropDocText.Text = Locale.Get("cv_drop_photo_doc");
                            if (isVideo) DropDocText.Text = Locale.Get("cv_drop_video_doc");
                            DropImg.Visibility = isImage ? Visibility.Visible : Visibility.Collapsed;
                            DropVid.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
                            DropArea.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                        } else {
                            DropDocText.Text = Locale.Get("cv_drop_doc_post");
                            DropArea.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                        }
                    })();
                }
            } catch (Exception ex) {
                Functions.ShowHandledErrorTip(ex);
            }
        }

        private void HideDropArea(object sender, DragEventArgs e) {
            Debug.WriteLine("HideDropArea");
            DropDoc.Visibility = Visibility.Collapsed;
            DropImg.Visibility = Visibility.Collapsed;
            DropVid.Visibility = Visibility.Collapsed;
            DropArea.Visibility = Visibility.Collapsed;

            ComposerBase.Opacity = 1;
            ComposerBase.IsHitTestVisible = true;
        }

        private void DocDragOver(object sender, DragEventArgs e) {
            Debug.WriteLine("DocDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void ImgDragOver(object sender, DragEventArgs e) {
            Debug.WriteLine("ImgDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void VidDragOver(object sender, DragEventArgs e) {
            Debug.WriteLine("ImgDragOver");
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void DropToDoc(object sender, DragEventArgs e) {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed; ComposerBase.Opacity = 1;
            ComposerBase.IsHitTestVisible = true;

            new System.Action(async () => {
                var items = await e.DataView.GetStorageItemsAsync();
                var files = items.Cast<StorageFile>();
                await ViewModel.AttachFilesAndUpload(files, OutboundAttachmentUploadFileType.DocumentForWall);
            })();
        }

        private void DropToImg(object sender, DragEventArgs e) {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed;
            ComposerBase.Opacity = 1;
            ComposerBase.IsHitTestVisible = true;

            new System.Action(async () => {
                var items = await e.DataView.GetStorageItemsAsync();
                var files = items.Cast<StorageFile>();
                await ViewModel.AttachFilesAndUpload(files, OutboundAttachmentUploadFileType.PhotoForWall);
            })();
        }

        private void DropToVid(object sender, DragEventArgs e) {
            e.Handled = true;
            DropArea.Visibility = Visibility.Collapsed;
            ComposerBase.Opacity = 1;
            ComposerBase.IsHitTestVisible = true;

            new System.Action(async () => {
                var items = await e.DataView.GetStorageItemsAsync();
                var files = items.Cast<StorageFile>();
                await ViewModel.AttachFilesAndUpload(files, OutboundAttachmentUploadFileType.VideoForWall);
            })();
        }

        #endregion

        private void ButtonEvents(object sender, KeyRoutedEventArgs e) {
            new System.Action(async () => {
                bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                bool shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

                // Paste file from clipboard
                if ((ctrl && e.Key == VirtualKey.V) || (shift && e.Key == VirtualKey.Insert)) {
                    try {
                        var dataPackageView = Clipboard.GetContent();
                        if (dataPackageView.Contains(StandardDataFormats.Bitmap)) {
                            e.Handled = true;
                            IRandomAccessStreamReference imageReceived = await dataPackageView.GetBitmapAsync();
                            if (imageReceived != null) {
                                var ft = new List<string>(dataPackageView.AvailableFormats);
                                var fileTypes = new List<string>(dataPackageView.Properties.FileTypes);
                                StorageFile file = null;

                                if (ft.Contains("SystemInputDataTransferContent")) { // гифка из системной панели emoji
                                    file = await DataPackageParser.SaveBitmapAsDocFromClipboardAsync(imageReceived, "gif");
                                    await ViewModel.AttachFilesAndUpload(new List<StorageFile> { file }, OutboundAttachmentUploadFileType.DocumentForWall);
                                } else {
                                    file = await DataPackageParser.SaveBitmapFromClipboardAsync(imageReceived);
                                    await ViewModel.AttachFilesAndUpload(new List<StorageFile> { file }, OutboundAttachmentUploadFileType.PhotoForWall);
                                }
                            }
                        } else if (dataPackageView.Contains(StandardDataFormats.StorageItems)) {
                            e.Handled = true;
                            var files = await dataPackageView.GetStorageItemsAsync();
                            await DataPackageParser.UploadFilesFromClipboardAsync(ViewModel, files);
                        }
                    } catch (Exception ex) {
                        Log.Error($"Error while getting and sending content from clipboard! 0x{ex.HResult.ToString("x8")}");
                        Functions.ShowHandledErrorDialog(ex);
                    }
                }
            })();
        }
    }
}