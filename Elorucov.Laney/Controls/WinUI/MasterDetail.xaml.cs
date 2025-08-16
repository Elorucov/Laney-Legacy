using System;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Elorucov.Laney.Controls.WinUI {
    public sealed partial class MasterDetail : UserControl {
        public MasterDetail() {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty RightContentProperty = DependencyProperty.Register(
            nameof(RightContent), typeof(UIElement), typeof(MasterDetail), new PropertyMetadata(default));

        public UIElement RightContent {
            get { return (UIElement)GetValue(RightContentProperty); }
            set { SetValue(RightContentProperty, value); }
        }

        public static readonly DependencyProperty LeftContentProperty = DependencyProperty.Register(
            nameof(LeftContent), typeof(UIElement), typeof(MasterDetail), new PropertyMetadata(default));

        public UIElement LeftContent {
            get { return (UIElement)GetValue(LeftContentProperty); }
            set { SetValue(LeftContentProperty, value); }
        }

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
            nameof(Footer), typeof(UIElement), typeof(MasterDetail), new PropertyMetadata(default));

        public UIElement Footer {
            get { return (UIElement)GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); CheckFooter(); }
        }

        public static readonly DependencyProperty IsRightPaneShowingProperty = DependencyProperty.Register(
            nameof(IsRightPaneShowing), typeof(bool), typeof(MasterDetail), new PropertyMetadata(default));

        public bool IsRightPaneShowing {
            get { return (bool)GetValue(IsRightPaneShowingProperty); }
            set { SetValue(IsRightPaneShowingProperty, value); SwitchPane(value); }
        }

        public static readonly DependencyProperty LeftPaneIsCompactProperty = DependencyProperty.Register(
            nameof(LeftPaneIsCompact), typeof(bool), typeof(MasterDetail), new PropertyMetadata(default));

        public bool LeftPaneIsCompact {
            get { return (bool)GetValue(LeftPaneIsCompactProperty); }
            set { SetValue(LeftPaneIsCompactProperty, value); SetUpView(true); }
        }

        public static readonly DependencyProperty IsLayerVisibleProperty = DependencyProperty.Register(
            nameof(IsLayerVisible), typeof(bool), typeof(MasterDetail), new PropertyMetadata(true));

        public bool IsLayerVisible {
            get { return (bool)GetValue(IsLayerVisibleProperty); }
            set { SetValue(IsLayerVisibleProperty, value); ToggleLayerVisibility(value); }
        }

        public bool IsWideMode { get { return ActualWidth >= 720; } }
        public double RightContentWidth { get { return ActualWidth >= 720 ? RightContentContainer.ActualWidth : ActualWidth; } }

        public event EventHandler RightPaneShowingChanged;
        public event EventHandler<bool> LeftPaneIsCompactChanged;


        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            ToggleLayerVisibility(IsLayerVisible);
            SetUpStatusBar();
            SetUpView();
            oldWidth = ActualWidth;
            CoreApplication.GetCurrentView().CoreWindow.KeyUp += CoreWindow_KeyUpDown;
            CoreApplication.GetCurrentView().CoreWindow.KeyDown += CoreWindow_KeyUpDown;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e) {
            SizeChanged -= UserControl_SizeChanged;
            Loaded -= UserControl_Loaded;
            Unloaded -= UserControl_Unloaded;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            SetUpView();
        }

        private void CheckFooter() {
            FooterContainer.Visibility = Footer == null ? Visibility.Collapsed : Visibility.Visible;
        }


        private void SetUpStatusBar() {
            UpdateLayoutDueNavBar(ApplicationView.GetForCurrentView().VisibleBounds);
            ApplicationView.GetForCurrentView().VisibleBoundsChanged += (c, d) => {
                UpdateLayoutDueNavBar(c.VisibleBounds);
            };
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                DisplayInformation di = DisplayInformation.GetForCurrentView();
                SetUpStatusBar(di.CurrentOrientation);
                di.OrientationChanged += (a, b) => {
                    SetUpStatusBar(a.CurrentOrientation);
                };
            } else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
                var tb = CoreApplication.GetCurrentView().TitleBar;
                // TopPlaceholder.Height = tb.Height;
                TopPlaceholder.Height = 32; // чтобы не прыгало
                tb.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
            }
        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) {
            TopPlaceholder.Height = sender.Height;
        }

        private void SetUpStatusBar(DisplayOrientations currentOrientation) {
            StatusBar sb = StatusBar.GetForCurrentView();
            if (currentOrientation == DisplayOrientations.Portrait || currentOrientation == DisplayOrientations.PortraitFlipped) {
                ApplicationView.GetForCurrentView().ExitFullScreenMode();
                TopPlaceholder.Height = sb.OccludedRect.Height;
            } else {
                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                TopPlaceholder.Height = 0;
            }
        }

        private void UpdateLayoutDueNavBar(Rect vb) {
            DisplayInformation di = DisplayInformation.GetForCurrentView();
            Rect ws = Window.Current.Bounds;
            if (di.CurrentOrientation == DisplayOrientations.Portrait || di.CurrentOrientation == DisplayOrientations.PortraitFlipped) {
                LayoutRoot.Margin = new Thickness(0, 0, 0, ws.Height - vb.Bottom);
            } else {
                LayoutRoot.Margin = new Thickness(0);
            }
        }

        private double oldWidth = 0;
        private const double MIN_WIDTH = 320;
        private const double COMPACT_WIDTH = 72;
        private void SetUpView(bool forceTriggerCompactEvent = false) {
            if (ActualWidth >= 720) {
                LeftCD.MaxWidth = ActualWidth / 2.5;

                Grid.SetColumnSpan(LeftContentContainer, 1);
                Grid.SetColumn(RightContentContainer, 1);
                Grid.SetRowSpan(RightContentContainer, 2);
                Grid.SetColumnSpan(FooterContainer, 1);
                Grid.SetColumnSpan(FooterDivider, 1);
                if (LeftPaneIsCompact) {
                    LeftCD.MinWidth = COMPACT_WIDTH;
                    LeftCD.Width = new GridLength(COMPACT_WIDTH);
                } else {
                    double min = Math.Max(ActualWidth / 4, MIN_WIDTH);
                    double max = Math.Max(ActualWidth / 2.25, MIN_WIDTH);
                    double pw = ActualWidth / 100 * 32;
                    LeftCD.MinWidth = Math.Max(ActualWidth / 4, MIN_WIDTH);
                    LeftCD.Width = new GridLength(Math.Clamp(pw, min, max), GridUnitType.Pixel);
                }

                LayerBackground.Opacity = 0;
                LayerBackgroundRight.Opacity = 1;
                LeftContentContainer.Visibility = Visibility.Visible;
                RightContentContainer.Visibility = Visibility.Visible;
                RightContentContainer.CornerRadius = new CornerRadius(7, 0, 0, 0);
                Splitter.Visibility = LeftPaneIsCompact ? Visibility.Collapsed : Visibility.Visible;
                if ((!forceTriggerCompactEvent && oldWidth != 0 && oldWidth < 720) || forceTriggerCompactEvent) LeftPaneIsCompactChanged?.Invoke(this, LeftPaneIsCompact);
            } else {
                LeftCD.MaxWidth = ActualWidth;
                LeftCD.MinWidth = ActualWidth;
                LeftCD.Width = new GridLength(ActualWidth, GridUnitType.Pixel);

                Grid.SetColumnSpan(LeftContentContainer, 2);
                Grid.SetColumn(RightContentContainer, 0);
                Grid.SetRowSpan(RightContentContainer, 1);
                Grid.SetColumnSpan(FooterContainer, 2);
                Grid.SetColumnSpan(FooterDivider, 2);

                LayerBackground.Opacity = 1;
                LayerBackgroundRight.Opacity = 0;
                RightContentContainer.CornerRadius = new CornerRadius(0);
                SwitchPane(IsRightPaneShowing);
                Splitter.Visibility = Visibility.Collapsed;
                if ((!forceTriggerCompactEvent && oldWidth != 0 && oldWidth >= 720) || forceTriggerCompactEvent) LeftPaneIsCompactChanged?.Invoke(this, false);
            }
            oldWidth = ActualWidth;
        }


        private void SwitchPane(bool isOpen) {
            if (ActualWidth < 720) {
                RightContentContainer.LayoutUpdated += RightContentContainer_LayoutUpdated;
                if (isOpen) {
                    LeftContentContainer.Visibility = Visibility.Collapsed;
                    RightContentContainer.Visibility = Visibility.Visible;
                    if (IsLayerVisible) LayerBackground.Visibility = Visibility.Visible;
                } else {
                    LeftContentContainer.Visibility = Visibility.Visible;
                    RightContentContainer.Visibility = Visibility.Collapsed;
                    if (IsLayerVisible) LayerBackground.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ToggleLayerVisibility(bool visible) {
            if (visible) {
                LayerBackground.Visibility = ActualWidth < 720 && IsRightPaneShowing ? Visibility.Visible : Visibility.Collapsed;
            } else {
                LayerBackground.Visibility = Visibility.Collapsed;
            }
            LayerBackgroundRight.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RightContentContainer_LayoutUpdated(object sender, object e) {
            RightContentContainer.LayoutUpdated -= RightContentContainer_LayoutUpdated;
            RightPaneShowingChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CoreWindow_KeyUpDown(CoreWindow sender, KeyEventArgs args) {
            bool ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            bool f6 = Window.Current.CoreWindow.GetKeyState(VirtualKey.F6).HasFlag(CoreVirtualKeyStates.Down);
            LeftContentContainer.Opacity = ctrl && shift && f6 ? 0 : 1;
            RightContentContainer.Opacity = ctrl && shift && f6 ? 0 : 1;
            FCP.Opacity = ctrl && shift && f6 ? 0 : 1;
        }
    }
}