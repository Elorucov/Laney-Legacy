using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VK.VKUI.Controls;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class AudioPlayerView : Page {
        const double AudioPlayerFaceHeight = 160;
        const double AudioPlayerFullHeight = 358;

        private bool IsInModal = false;
        public static ApplicationView CurrentView { get; private set; }

        AudioPlayerViewModel ViewModel { get { return DataContext as AudioPlayerViewModel; } set { DataContext = value; } }
        bool PlaylistVisible = false;

        public AudioPlayerView() {
            this.InitializeComponent();
            SetUp();
        }

        public AudioPlayerView(bool isInModal) {
            IsInModal = isInModal;
            this.InitializeComponent();
            SetUp();
        }

        private void SetUp() {
            ViewModel = AudioPlayerViewModel.MainInstance;
            ViewModel.PropertyChanged += AudioPlayerPropertyChanged;
            CoverPlaceholderIcon.Id = GetIconForCurrentType(ViewModel.CurrentSong.Type);

            AudioPlayerViewModel.InstancesChanged += AudioPlayerInstancesChanged;
        }

        private void SetUpPage(object sender, RoutedEventArgs e) {
            Log.Info($"{GetType().Name} > SetUpPage. Is in modal: {IsInModal}");
            if (!IsInModal) {
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                    if (Theme.IsMicaAvailable) {
                        BackdropMaterial.SetApplyToRootOrPageBackground(this, true);
                        Background = null;
                    }

                    Window.Current.SizeChanged += Current_SizeChanged;
                    Window.Current.Closed += (a, b) => AudioPlayerViewModel.InstancesChanged -= AudioPlayerInstancesChanged;

                    TitleAndStatusBar.ExtendView(true, true);
                    CoreApplicationViewTitleBar tb = CoreApplication.GetCurrentView().TitleBar;
                    tb.LayoutMetricsChanged += (a, b) => {
                        Log.Info($"{GetType().Name} > TitleBarLayoutMetricsChanged.");
                        ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, AudioPlayerFaceHeight));
                    };
                    new System.Action(async () => { await UpdateTitleBarColors(App.UISettings); })();
                    App.UISettings.ColorValuesChanged += async (a, b) => {
                        Log.Info($"{GetType().Name} > UISettings.ColorValuesChanged.");
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                            await UpdateTitleBarColors(a);
                        });
                    };
                    ApplicationView.GetForCurrentView().Consolidated += (a, b) => { CurrentView = null; };
                    CurrentView = ApplicationView.GetForCurrentView();
                }
            } else {
                MediaSlider.DragStopHandlerElement = this;
            }
            CoreApplication.GetCurrentView().CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
        }

        public async Task UpdateTitleBarColors(UISettings uis) {
            Color fc = uis.GetColorValue(UIColorType.Foreground);
            await TitleAndStatusBar.ChangeColor(fc);

            Color bc = uis.GetColorValue(UIColorType.Background);
            bc.A = 0;
            TitleAndStatusBar.ChangeBackgroundColor(bc);

            Log.Info($"{GetType().Name} > UpdateTitleBarColors: Background: {bc.R},{bc.G},{bc.B}; Foreground: {fc.R},{fc.G},{fc.B}");
        }

        private void AudioPlayerInstancesChanged(object sender, EventArgs e) {
            new System.Action(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    if (AudioPlayerViewModel.MainInstance != null) {
                        ViewModel = AudioPlayerViewModel.MainInstance;
                        ViewModel.PropertyChanged += AudioPlayerPropertyChanged;
                    } else {
                        CurrentView = null;
                        if (!CoreApplication.GetCurrentView().IsMain) Window.Current.Close();
                    }
                });
            })();
        }

        private void AudioPlayerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            new System.Action(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    if (ViewModel == null) return;
                    // Sometimes MediaSlider.Position and CoverPlaceholderIcon.ContentTemplate 
                    // fails to bind AudioPlayerViewModel.Position and AudioPlayerViewModel.CurrentSong.CoverPlaceholderIcon properties without any reason.
                    // So I had to write this crutch...
                    if (e.PropertyName == nameof(AudioPlayerViewModel.Position)) {
                        MediaSlider.Position = ViewModel.Position;
                    }
                    if (e.PropertyName == nameof(AudioPlayerViewModel.CurrentSong)) {
                        CoverPlaceholderIcon.Id = GetIconForCurrentType(ViewModel.CurrentSong.Type);
                    }
                });
            })();
        }

        private VKIconName GetIconForCurrentType(AudioType type) {
            switch (type) {
                case AudioType.Podcast: return VKIconName.Icon28PodcastOutline;
                case AudioType.VoiceMessage: return VKIconName.Icon28VoiceOutline;
                default: return VKIconName.Icon28SongOutline;
            }
        }

        double oldheight = 0;
        double hdiff = 0;
        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e) {
            if (IsInModal) {
                Window.Current.SizeChanged -= Current_SizeChanged;
                return;
            }
            if (e.Size.Height != oldheight) {
                hdiff = e.Size.Height - oldheight;
                oldheight = e.Size.Height;
                double plh = e.Size.Height - FaceLayer.ActualHeight - PlaylistInfo.ActualHeight + 9;
                Debug.WriteLine($"hdiff: {hdiff}, old: {oldheight}, new: {e.Size.Height}, plh: {plh}");

                if (plh >= 0) AudiosList.Height = plh;
            }
        }

        private void CoreWindow_ResizeCompleted(CoreWindow sender, object args) {
            FixWindowHeight();
        }

        private void FixWindowHeight() {
            double w = Window.Current.Bounds.Width;
            double h = Window.Current.Bounds.Height;
            if (h < AudioPlayerFaceHeight) {
                PlaylistVisible = false;
            } else if (h > AudioPlayerFaceHeight && h < AudioPlayerFullHeight) {
                PlaylistVisible = hdiff > 0;
            } else if (h > AudioPlayerFullHeight) {
                PlaylistVisible = true;
            }
            ResizeWindowHeightAfterToggle();
        }

        private void ResizeWindowHeightAfterToggle() {
            double h = PlaylistVisible ? AudioPlayerFullHeight : AudioPlayerFaceHeight;
            ApplicationView.GetForCurrentView().TryResizeView(new Size(Window.Current.Bounds.Width, h));
        }

        private void MediaSlider_PositionChanged(object sender, TimeSpan e) {
            ViewModel?.SetPosition(e);
        }

        private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args) {
            switch (args.KeyCode) {
                case 32: // Space
                    if (ViewModel.IsPlaying) {
                        ViewModel.Pause();
                    } else {
                        ViewModel.Play();
                    }
                    break;
            }
        }
    }
}