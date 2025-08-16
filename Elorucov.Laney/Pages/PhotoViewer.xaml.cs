using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class PhotoViewer : OverlayModal {
        Tuple<List<GalleryItem>, GalleryItem> Photos = null;
        bool disableSavingToVK;
        public PhotoViewer(Tuple<List<GalleryItem>, GalleryItem> photos = null, bool disableSavingToVK = false) {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            Photos = photos;
            this.disableSavingToVK = disableSavingToVK;

            Loaded += async (a, b) => {
                await SetUpUIAsync();
                await ShowPhotos(Photos);
            };
        }

        private async Task SetUpUIAsync() {
            SystemNavigationManager.GetForCurrentView().BackRequested += PhotoViewer_BackRequested;

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                await TitleAndStatusBar.ChangeColor(Windows.UI.Color.FromArgb(255, 255, 255, 255));
                CoreApplicationViewTitleBar tb = CoreApplication.GetCurrentView().TitleBar;
                CounterContainer.Margin = new Thickness(0, tb.Height, 0, 0);
                tb.LayoutMetricsChanged += (a, b) => {
                    CounterContainer.Margin = new Thickness(0, a.Height, 0, 0);
                };
            } else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                ChangeBounds(ApplicationView.GetForCurrentView().VisibleBounds, StatusBar.GetForCurrentView().OccludedRect);
                ApplicationView.GetForCurrentView().VisibleBoundsChanged += (a, b) => {
                    ChangeBounds(a.VisibleBounds, StatusBar.GetForCurrentView().OccludedRect);
                };
            }
            if (Theme.IsAnimationsEnabled) ShowAnimation();
        }

        private void PhotoViewer_BackRequested(object sender, BackRequestedEventArgs e) {
            Close();
        }

        private void Close(object data = null) {
            SystemNavigationManager.GetForCurrentView().BackRequested -= PhotoViewer_BackRequested;
            new System.Action(async () => {
                if (Theme.IsAnimationsEnabled) {
                    HideAnimation();
                    await System.Threading.Tasks.Task.Delay(250);
                }
                Hide(data);
            })();
        }

        private void ShowAnimation() {
            ElementCompositionPreview.SetIsTranslationEnabled(LayoutRoot, true);
            Visual visual = ElementCompositionPreview.GetElementVisual(LayoutRoot);
            Compositor compositor = visual.Compositor;
            visual.Offset = new System.Numerics.Vector3(0, (float)Window.Current.Bounds.Height, 0);

            Vector3KeyFrameAnimation vfa = compositor.CreateVector3KeyFrameAnimation();
            vfa.InsertKeyFrame(1f, new System.Numerics.Vector3(0, 0, 0));
            vfa.Duration = TimeSpan.FromMilliseconds(250);
            vfa.Direction = Windows.UI.Composition.AnimationDirection.Normal;
            vfa.IterationCount = 1;

            visual.StartAnimation("Offset", vfa);
        }

        private void HideAnimation() {
            ElementCompositionPreview.SetIsTranslationEnabled(LayoutRoot, true);
            Visual visual = ElementCompositionPreview.GetElementVisual(LayoutRoot);
            Compositor compositor = visual.Compositor;
            visual.Offset = new System.Numerics.Vector3(0, (float)Window.Current.Bounds.Height, 0);

            Vector3KeyFrameAnimation vfa = compositor.CreateVector3KeyFrameAnimation();
            vfa.InsertKeyFrame(1f, new System.Numerics.Vector3(0, 0, 0));
            vfa.Duration = TimeSpan.FromMilliseconds(250);
            vfa.Direction = Windows.UI.Composition.AnimationDirection.Reverse;
            vfa.IterationCount = 1;

            visual.StartAnimation("Offset", vfa);
        }

        private void ChangeBounds(Rect vbrect, Rect sbrect) {
            var o = DisplayInformation.GetForCurrentView().CurrentOrientation;
            if (o == DisplayOrientations.Portrait) {
                LayoutRoot.Margin = new Thickness(0, 0, 0, Window.Current.Bounds.Height - vbrect.Bottom);
                CounterContainer.Margin = new Thickness(0, sbrect.Height, 0, 0);
            } else {
                LayoutRoot.Margin = new Thickness(0);
                CounterContainer.Margin = new Thickness(0);
            }
        }

        private async Task ShowPhotos(Tuple<List<GalleryItem>, GalleryItem> photosinfo) {
            if (Photos != null && photosinfo.Item1.Count > 0 && photosinfo.Item2 != null) {
                int index = photosinfo.Item1.IndexOf(photosinfo.Item2);
                Log.Info($"{GetType().Name} > Showing photos ({photosinfo.Item1.Count}), index: {index}");
                imgs.SelectionChanged += Imgs_SelectionChanged;
                imgs.ItemsSource = photosinfo.Item1;
                imgs.SelectedIndex = index;
                imgs.Tapped += (a, b) => {
                    Overlay.Visibility = Overlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                };
                await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {
                    imgs.Focus(FocusState.Programmatic);
                });
            }
        }

        private void Imgs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            new System.Action(async () => {
                string desc = "";
                if (imgs.SelectedIndex >= 0) {
                    Counter.Text = $"{imgs.SelectedIndex + 1} / {Photos.Item1.Count}";

                    GalleryItem p = Photos.Item1.ElementAt(imgs.SelectedIndex);
                    desc = p.Description;
                    await GetOwnerAndDateTimeInfo(p);
                }
                Desc.Text = desc;
                Desc.Visibility = string.IsNullOrEmpty(desc) ? Visibility.Collapsed : Visibility.Visible;
            })();
        }

        private async Task GetOwnerAndDateTimeInfo(GalleryItem p) {
            PhotoPublishedTime.Text = APIHelper.GetNormalizedTime(p.Date, true);
            if (p.OwnerId.IsUser()) {
                User u = AppSession.GetCachedUser(p.OwnerId);
                if (u == null) {
                    object resp = await Users.Get(p.OwnerId);
                    if (resp is User) u = resp as User;
                }
                if (u != null) {
                    OwnerName.Text = u.FullName;
                    BitmapImage img = new BitmapImage();
                    await img.SetUriSourceAsync(u.Photo);

                    OwnerPhoto.ImageSource = img;
                    OwnerName.Visibility = Visibility.Visible;
                    OwnerPhoto.Visibility = Visibility.Visible;
                }
            } else if (p.OwnerId.IsGroup()) {
                Group u = AppSession.GetCachedGroup(p.OwnerId * -1);
                if (u == null) {
                    object resp = await Groups.GetById(p.OwnerId * -1);
                    if (resp is VKList<object> groups) u = groups.Groups[0];
                }
                if (u != null) {
                    OwnerName.Text = u.Name;
                    BitmapImage img = new BitmapImage();
                    await img.SetUriSourceAsync(u.Photo);

                    OwnerPhoto.ImageSource = img;
                    OwnerName.Visibility = Visibility.Visible;
                    OwnerPhoto.Visibility = Visibility.Visible;
                }
            }
        }

        #region Static members

        public static void Show(Tuple<List<GalleryItem>, GalleryItem> pi, bool disableSavingToVK = false) {
            if (pi.Item2.Source == null) {
                Log.Warn($"PhotoViewer (static) > selected photo contains no image!");
                return;
            }

            Log.Info($"PhotoViewer (static) > Opening photo viewer...");
            PhotoViewer pv = new PhotoViewer(pi, disableSavingToVK);
            pv.Closed += async (a, b) => {
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                    await Theme.UpdateTitleBarColors(App.UISettings);
                }
                if (b is List<AttachmentBase> list) {
                    ModalsManager.CloseLastOpenedModal();
                    Main.GetCurrent()?.StartForwardingAttachments(list);
                }
            };
            pv.Show();
        }

        #endregion

        private void Download(object sender, RoutedEventArgs e) {
            try {
                GalleryItem item = imgs.SelectedItem as GalleryItem;
                if (item.Source == null) return;
                string fileName = string.Empty;
                bool isDoc = false;

                if (item.OriginalObject is Photo p) {
                    fileName = $"lny{p.OwnerId}_{p.Id}_{item.Source.Segments.Last()}";
                } else if (item.OriginalObject is Document d) {
                    fileName = d.Title;
                    isDoc = true;
                } else {
                    return;
                }

                if (item.Source == null) return;

                string path = string.Empty;
                new System.Action(async () => {
                    var resp = await LNet.GetAsync(item.Source);
                    resp.EnsureSuccessStatusCode();

                    byte[] barray = await resp.Content.ReadAsByteArrayAsync();

                    try {
                        if (isDoc) {
                            StorageFile docf = await DownloadsFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                            await FileIO.WriteBytesAsync(docf, barray);
                            path = docf.Path;
                        } else {
                            StorageFolder lf = await KnownFolders.PicturesLibrary.CreateFolderAsync("Laney", CreationCollisionOption.OpenIfExists);
                            StorageFile imgf = await lf.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                            await FileIO.WriteBytesAsync(imgf, barray);
                            path = imgf.Path;
                        }
                    } catch (Exception ex) { // strange, but someone experienced crash when getting PicturesLibrary, ignoring parent try-catch, lol.
                        Functions.ShowHandledErrorDialog(ex);
                    }
                })();

                Tips.Show($"{Locale.Get(!isDoc ? "photoviewer_saved" : "downloaded")}", path);
            } catch (Exception ex) {
                Functions.ShowHandledErrorDialog(ex);
            }
        }

        private void SaveToVk(object sender, RoutedEventArgs e) {
            GalleryItem item = imgs.SelectedItem as GalleryItem;

            new System.Action(async () => {
                if (item.OriginalObject is Photo p) {
                    object resp = await VkAPI.Methods.Photos.Copy(p.OwnerId, p.Id, p.AccessKey);
                    if (resp is int i) {
                        Tips.Show(Locale.Get("photoviewer_savedtoalbum"));
                    } else {
                        Functions.ShowHandledErrorTip(resp);
                    }
                } else if (item.OriginalObject is Document d) {
                    object resp = await Docs.Add(d.OwnerId, d.Id, d.AccessKey);
                    if (resp is string id) {
                        Tips.Show(Locale.Get("saved_to_docs"));
                    } else {
                        Functions.ShowHandledErrorTip(resp);
                    }
                }
            })();
        }

        private void SharePhoto(object sender, RoutedEventArgs e) {
            GalleryItem item = imgs.SelectedItem as GalleryItem;
            Close(new List<AttachmentBase> { (AttachmentBase)item.OriginalObject });
        }

        private void CopyToClipboard(object sender, RoutedEventArgs e) {
            GalleryItem item = imgs.SelectedItem as GalleryItem;
            if (item.Source == null) return;

            new System.Action(async () => {
                byte[] bytes = LNetExtensions.TryGetCachedImage(item.Source);
                if (bytes != null) {
                    try {
                        using (MemoryStream stream = new MemoryStream()) {
                            stream.Write(bytes, 0, bytes.Length);
                            stream.Seek(0, SeekOrigin.Begin);

                            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                            var ims = new InMemoryRandomAccessStream();
                            var encoder = await BitmapEncoder.CreateForTranscodingAsync(ims, decoder);
                            await encoder.FlushAsync();

                            DataPackage dp = new DataPackage();
                            dp.RequestedOperation = DataPackageOperation.Copy;
                            dp.SetBitmap(RandomAccessStreamReference.CreateFromStream(ims));
                            Clipboard.SetContent(dp);
                            Tips.Show(Locale.Get("copied_to_clipboard"));
                        }
                    } catch (Exception ex) {
                        Functions.ShowHandledErrorDialog(ex);
                    }
                } else {
                    // Photo is not downloaded and cached yet
                    Tips.Show(Locale.Get("global_error"));
                }
            })();
        }

        private void Exit(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ShowContextMenu(object sender, RoutedEventArgs e) {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void CheckContextMenu(object sender, object e) {
            if (ViewManagement.GetWindowType() != WindowType.Main) shareBtn.Visibility = Visibility.Collapsed;

            GalleryItem item = imgs.SelectedItem as GalleryItem;
            bool isDoc = item.OriginalObject is Document;

            downloadBtn.Text = Locale.Get(isDoc ? "download" : "photoviewer_savebtn");
            saveVkBtn.Text = Locale.Get(isDoc ? "save_to_docs" : "photoviewer_savetoalbumbtn");
            copyBtn.Text = Locale.Get("copy_clipboard");

            if (item.Source == null) {
                downloadBtn.Visibility = Visibility.Collapsed;
                copyBtn.Visibility = Visibility.Collapsed;
            }

            if (disableSavingToVK) {
                saveVkBtn.Visibility = Visibility.Collapsed;
            } else {
                saveVkBtn.Visibility = isDoc && item.OwnerId == AppParameters.UserID ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}