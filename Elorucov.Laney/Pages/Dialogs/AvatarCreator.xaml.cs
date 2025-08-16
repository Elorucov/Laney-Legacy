using Elorucov.Laney.Models;
using Elorucov.Laney.Models.AvatarCreator;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.ViewModel;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Pages.Dialogs {

    public sealed partial class AvatarCreator : Modal {

        public AvatarCreator(bool showSetAsChatPhotoButton = false) {
            InitializeComponent();
            if (showSetAsChatPhotoButton) {
                setBtn.Visibility = Visibility.Visible;
            }
            Setup();
        }

        private void Setup() {
            CoreApplication.GetCurrentView().CoreWindow.ResizeCompleted += (a, b) => {
                // try to fix split view bug after resize.
                if (splitView.DisplayMode == SplitViewDisplayMode.Inline) splitView.IsPaneOpen = true;
            };
            SystemNavigationManager.GetForCurrentView().BackRequested += OnSystemBackButtonPressed;
        }

        AvatarCreatorViewModel ViewModel => DataContext as AvatarCreatorViewModel;

        private void OnSystemBackButtonPressed(object sender, BackRequestedEventArgs e) {
            if (splitView.DisplayMode == SplitViewDisplayMode.Overlay && splitView.IsPaneOpen) {
                e.Handled = true;
                splitView.IsPaneOpen = false;
            }
        }

        private void OnRootSizeChanged(object sender, SizeChangedEventArgs e) {
            double width = e.NewSize.Width;
            if (width >= 600) {
                splitView.DisplayMode = SplitViewDisplayMode.Inline;
                splitView.IsPaneOpen = true;
                splitView.OpenPaneLength = rootGrid.ActualWidth - 320;
                Grid.SetColumnSpan(randomBtn, 2);
                chooseBtn.Visibility = Visibility.Collapsed;
                paneCloseBtn.Visibility = Visibility.Collapsed;
                paneSeparator.Visibility = Visibility.Visible;
            } else {
                splitView.DisplayMode = SplitViewDisplayMode.Overlay;
                splitView.IsPaneOpen = false;
                splitView.OpenPaneLength = 320;
                Grid.SetColumnSpan(randomBtn, 1);
                chooseBtn.Visibility = Visibility.Visible;
                paneCloseBtn.Visibility = Visibility.Visible;
                paneSeparator.Visibility = Visibility.Collapsed;
            }
        }

        private void OpenRightPanel(object sender, RoutedEventArgs e) {
            splitView.IsPaneOpen = true;
        }

        private void CloseRightPanel(object sender, RoutedEventArgs e) {
            splitView.IsPaneOpen = false;
        }

        private void ListViewItemClicked(object sender, ItemClickEventArgs e) {
            if (splitView.DisplayMode == SplitViewDisplayMode.Overlay) splitView.IsPaneOpen = false;
        }

        //

        private void OnLoaded(object sender, RoutedEventArgs e) {
            DataContext = new AvatarCreatorViewModel();
            new System.Action(async () => { await ViewModel.SetupAsync(); })();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(AvatarCreatorViewModel.GradientDirection):
                    CheckGradientDirection();
                    break;
                case nameof(AvatarCreatorViewModel.GroupedEmojis):
                    if (splitView.DisplayMode == SplitViewDisplayMode.Overlay) splitView.IsPaneOpen = true;
                    break;
            }
        }

        private void StickersPivotItemLoaded(object sender, RoutedEventArgs e) {
            (sender as FrameworkElement).Loaded -= StickersPivotItemLoaded;
            new System.Action(async () => { await ViewModel.LoadStickersAsync(); })();
        }

        private void KaomojiPivotItemLoaded(object sender, RoutedEventArgs e) {
            (sender as FrameworkElement).Loaded -= KaomojiPivotItemLoaded;
            ViewModel.InitKaomoji();
        }

        private void CheckGradientDirection() {
            switch (ViewModel.GradientDirection) {
                case GradientDirection.TopLeftToBottomRight:
                    directionLTRBtn.IsChecked = true;
                    directionTTBBtn.IsChecked = false;
                    directionRTLBtn.IsChecked = false;
                    gradientBrush.StartPoint = new Point(0, 0);
                    gradientBrush.EndPoint = new Point(1, 1);
                    break;
                case GradientDirection.TopToBottom:
                    directionLTRBtn.IsChecked = false;
                    directionTTBBtn.IsChecked = true;
                    directionRTLBtn.IsChecked = false;
                    gradientBrush.StartPoint = new Point(0, 0);
                    gradientBrush.EndPoint = new Point(0, 1);
                    break;
                case GradientDirection.TopRightToBottomLeft:
                    directionLTRBtn.IsChecked = false;
                    directionTTBBtn.IsChecked = false;
                    directionRTLBtn.IsChecked = true;
                    gradientBrush.StartPoint = new Point(1, 0);
                    gradientBrush.EndPoint = new Point(0, 1);
                    break;
            }
        }

        //

        private void ChangeGradientDirectionToLTR(object sender, RoutedEventArgs e) {
            ViewModel.GradientDirection = GradientDirection.TopLeftToBottomRight;
        }

        private void ChangeGradientDirectionToTTB(object sender, RoutedEventArgs e) {
            ViewModel.GradientDirection = GradientDirection.TopToBottom;
        }

        private void ChangeGradientDirectionToRTL(object sender, RoutedEventArgs e) {
            ViewModel.GradientDirection = GradientDirection.TopRightToBottomLeft;
        }

        private void ScrollGPLLeft(object sender, RoutedEventArgs e) {
            double x = gradientsPresetScrollViewer.HorizontalOffset;
            gradientsPresetScrollViewer.ChangeView(x - 72, null, null);
        }

        private void ScrollGPLRight(object sender, RoutedEventArgs e) {
            double x = gradientsPresetScrollViewer.HorizontalOffset;
            gradientsPresetScrollViewer.ChangeView(x + 72, null, null);
        }

        private void SetGradient(object sender, RoutedEventArgs e) {
            GradientPreset preset = (sender as HyperlinkButton).Tag as GradientPreset;
            ViewModel.ApplyGradientPreset(preset);
        }

        #region AvatarCreatorItem rendering

        private void RenderKaomoji(object sender, RoutedEventArgs e) {
            Kaomoji kaomoji = (sender as FrameworkElement).Tag as Kaomoji;
            if (kaomoji != null) new System.Action(async () => { await RenderKaomoji(kaomoji); })();
        }

        private async Task RenderKaomoji(Kaomoji kaomoji) {
            workCanvas.Children.RemoveAt(workCanvas.Children.Count - 1);

            Border element = await kaomoji.RenderAsync(RenderMode.InCanvas, false) as Border;

            TextBlock ktb = element.Child as TextBlock;
            ktb.FontSize = ViewModel.KaomojiSize;
            ktb.LineHeight = ViewModel.KaomojiSize;
            ktb.Foreground = new SolidColorBrush(ViewModel.KaomojiColor);

            workCanvas.Children.Add(element);
            stickerTools.Visibility = Visibility.Collapsed;
            kaomojiTools.Visibility = Visibility.Visible;
            if (splitView.DisplayMode == SplitViewDisplayMode.Overlay) splitView.IsPaneOpen = false;
        }

        private void DrawACIInGVI(FrameworkElement sender, DataContextChangedEventArgs args) {
            Border container = sender as Border;
            if (args.NewValue != null && args.NewValue is IAvatarCreatorItem aci) {
                new System.Action(async () => {
                    container.Child = await aci.RenderAsync(RenderMode.InGridViewItem, true);
                })();
            }
        }

        private void DrawACIInCanvas(object sender, SelectionChangedEventArgs e) {
            GridView gridView = sender as GridView;
            if (gridView.SelectedItem != null && gridView.SelectedItem is IAvatarCreatorItem aci) {
                kaomojiTools.Visibility = Visibility.Collapsed;
                stickerTools.Visibility = aci is VKSticker ? Visibility.Visible : Visibility.Collapsed;

                workCanvas.Children.RemoveAt(workCanvas.Children.Count - 1);
                new System.Action(async () => {
                    var element = await aci.RenderAsync(RenderMode.InCanvas, stickerBorder.IsChecked.Value);
                    workCanvas.Children.Add(element);
                })();
            }
        }

        #endregion

        #region Kaomoji-specific

        private void ResizeKaomoji(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e) {
            Double size = e.NewValue;
            if (workCanvas.Children.Last() is Border b) {
                TextBlock kaomoji = b.Child as TextBlock;
                kaomoji.FontSize = size;
                kaomoji.LineHeight = size;
            }
        }

        private void ChangeKaomojiColor(Microsoft.UI.Xaml.Controls.ColorPicker sender, Microsoft.UI.Xaml.Controls.ColorChangedEventArgs args) {
            if (workCanvas.Children.Last() is Border b) {
                TextBlock kaomoji = b.Child as TextBlock;
                kaomoji.Foreground = new SolidColorBrush(args.NewColor);
            }
        }

        #endregion

        private void GenerateRandomAvatar(object sender, RoutedEventArgs e) {
            switch (panePivot.SelectedIndex) {
                case 0:
                    ViewModel.GenerateRandomEmojiAvatar();
                    break;
                case 1:
                    ViewModel.GenerateRandomStickerAvatar();
                    break;
                case 2:
                    new System.Action(async () => {
                        await RenderKaomoji(ViewModel.GetRandomKaomoji());
                    })();
                    break;
            }
        }

        StorageFile lastSavedFile = null;
        private void SaveAvatar(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                try {
                    lastSavedFile = await Functions.TakeUIElementScreenShot(workCanvas, "laney_avatar");
                    await new ContentDialog {
                        Title = Locale.Get("file_saved"),
                        Content = lastSavedFile.Path,
                        PrimaryButtonText = "OK"
                    }.ShowAsync();
                } catch (Exception ex) {
                    Functions.ShowHandledErrorDialog(ex);
                }
            })();
        }

        private void SaveAndClose(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                if (lastSavedFile != null) {
                    Hide(lastSavedFile);
                } else {
                    try {
                        var folder = ApplicationData.Current.TemporaryFolder;
                        lastSavedFile = await Functions.TakeUIElementScreenShot(workCanvas, "laney_avatar", folder);
                        Hide(lastSavedFile);
                    } catch (Exception ex) {
                        Functions.ShowHandledErrorDialog(ex);
                    }
                }
            })();
        }

        private void stickerBorder_Checked(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                var selected = ViewModel.SelectedSticker;
                ViewModel.SelectedSticker = null;
                await Task.Delay(1); // required
                ViewModel.SelectedSticker = selected;
            })();
        }
    }
}