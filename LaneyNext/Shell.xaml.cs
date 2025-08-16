using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers.UI;
using Elorucov.Laney.ViewModels;
using System;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace Elorucov.Laney
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Shell : Page
    {
        public Shell()
        {
            this.InitializeComponent();
            // Titlebar on desktop
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
            {
                SetUpWindow();
                Window.Current.SetTitleBar(TitleBarArea);
                SetUpTitleBar(CoreApplication.GetCurrentView().TitleBar);
                ConfigureShadows();

                var version = AppInfo.Version;
                if (Core.Settings.DebugDisplayRAMUsage)
                {
                    DispatcherTimer tmr = new DispatcherTimer();
                    tmr.Interval = TimeSpan.FromSeconds(0.5);
                    tmr.Tick += (a, b) => DebugRamUsage.Text = $"B{version.Build} / {Math.Round((double)MemoryManager.AppMemoryUsage / 1024 / 1024, 1)}M";
                    tmr.Start();
                }
                else if (AppInfo.ReleaseState == AppReleaseState.Internal)
                {
                    DebugRamUsage.Text = $"{version.Major}.{version.Minor}.{version.Build}";
                }
            }

            // Session
            ApplicationView.GetForCurrentView().Title = VKSession.Current.DisplayName;
            VKSession.Current.PropertyChanged += (a, b) => ApplicationView.GetForCurrentView().Title = VKSession.Current.DisplayName;

            if (VKSession.Current.SessionBase == null) VKSession.Current.SessionBase = new SessionBaseViewModel();
            DataContext = VKSession.Current;
            VKSession.SessionBound += async (a, b) =>
            {
                if (b == CoreApplication.GetCurrentView())
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (DataContext != null || VKSession.Compare(DataContext as VKSession, VKSession.Current)) return;
                        DataContext = VKSession.Current;
                        ApplicationView.GetForCurrentView().Title = VKSession.Current.DisplayName;
                        Log.General.Info("Session bound to view:", new ValueSet { { "session_id", VKSession.Current.SessionId } });
                    });
                }
            };

            LeftFrame.Navigate(typeof(Views.SessionBaseView), this);
            RightFrame.Content = new Views.ConversationView() { Tag = this };
        }

        #region UI

        // Titlebar
        public double TitleBarHeight { get { return TitleBarArea.Height; } }
        public event EventHandler<double> TitleBarHeightChanged;

        private void ConfigureShadows()
        {
            ShadowHelper.DrawShadow(ConversationBackgroundWithoutImage, RightFrameShadow, new System.Numerics.Vector3(0, 0, 0), 24, 0.25f);
        }

        private void SetUpWindow()
        {
            CoreApplication.GetCurrentView().Properties[Constants.ViewTypeKey] = ViewType.Session;
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(360, 500));
            var ctb = CoreApplication.GetCurrentView().TitleBar;
            SetUpTitleBar(ctb);
            ctb.IsVisibleChanged += (a, b) => SetUpTitleBar(a);
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += (a, b) =>
            {
                SetUpTitleBar(a);
            };
        }

        private void SetUpTitleBar(CoreApplicationViewTitleBar ctb)
        {
            TitleBarArea.Height = ctb.Height;
            TitleBarArea.Margin = new Thickness(0, 0, ctb.SystemOverlayRightInset - 46, 0);
            UIViewSettings uivs = UIViewSettings.GetForCurrentView();
            ApplicationView av = ApplicationView.GetForCurrentView();

            bool isFullScreen = uivs.UserInteractionMode == UserInteractionMode.Touch ||
                (uivs.UserInteractionMode == UserInteractionMode.Mouse && av.IsFullScreenMode);

            Log.General.Info(String.Empty, new ValueSet { { "is_full_screen", isFullScreen }, { "is_visible", ctb.IsVisible }, { "height", ctb.Height } });

            if (isFullScreen)
            {
                TitleBarHeightChanged?.Invoke(this, 0);
                TitleBarFullScreenBackgroundElement.Visibility = Visibility.Visible;
                TitleBarTransform.TranslateY = ctb.IsVisible ? 0 : -ctb.Height;
            }
            else
            {
                TitleBarHeightChanged?.Invoke(this, ctb.Height);
                TitleBarFullScreenBackgroundElement.Visibility = Visibility.Collapsed;
                TitleBarTransform.TranslateY = 0;
            }
        }

        // Panels

        public bool IsOnRightFrame { get; private set; }
        public bool IsWide { get { return ActualWidth >= 680; } }
        public double RightFrameWidth { get { return ActualWidth >= 680 ? RightFrameRoot.ActualWidth : ActualWidth; } }

        private void ShellSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth >= 680)
            {
                double lwc = ActualWidth / 3.25;
                double lw = lwc > 320 ? lwc : 320;
                LeftFrame.IsHitTestVisible = true;
                Grid.SetColumn(RightFrameRoot, 1);
                LeftContentTransform.TranslateX = 0;
                RightContentTransform.TranslateX = 0;
                LeftColumnDefinition.Width = new GridLength(lw, GridUnitType.Pixel);
                RightColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
                LeftFrame.Visibility = Visibility.Visible;
                RightFrameRoot.Visibility = Visibility.Visible;
            }
            else
            {
                Grid.SetColumn(RightFrameRoot, 0);
                LeftFrame.IsHitTestVisible = true;
                RightFrameRoot.IsHitTestVisible = true;
                IsTabStop = true;
                if (IsOnRightFrame)
                {
                    LeftContentTransform.TranslateX = -(e.NewSize.Width / 2);
                    RightContentTransform.TranslateX = 0;
                    LeftFrame.Visibility = Visibility.Collapsed;
                    RightFrameRoot.Visibility = Visibility.Visible;
                }
                else
                {
                    LeftContentTransform.TranslateX = 0;
                    RightContentTransform.TranslateX = e.NewSize.Width;
                    LeftFrame.Visibility = Visibility.Visible;
                    RightFrameRoot.Visibility = Visibility.Collapsed;
                }
                LeftColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
                RightColumnDefinition.Width = new GridLength(0, GridUnitType.Pixel);
            }
        }

        public void SwitchFrame(bool right)
        {
            if (IsOnRightFrame == right) return;
            IsOnRightFrame = right;
            LeftFrame.Visibility = Visibility.Visible;
            RightFrameRoot.Visibility = Visibility.Visible;
            if (ActualWidth < 680)
            {
                Focus(FocusState.Programmatic);
                LeftFrame.IsHitTestVisible = false;
                RightFrameRoot.IsHitTestVisible = false;
                IsTabStop = false;
                if (IsOnRightFrame)
                {
                    LeftForwardAnimation.From = 0;
                    LeftForwardAnimation.To = -(ActualWidth / 2);
                    RightForwardAnimation.From = ActualWidth;
                    RightForwardAnimation.To = 0;
                    ForwardAnimationStoryboard.Begin();
                }
                else
                {
                    LeftFrame.IsHitTestVisible = true;
                    LeftBackAnimation.From = -(ActualWidth / 2);
                    LeftBackAnimation.To = 0;
                    RightBackAnimation.From = 0;
                    RightBackAnimation.To = ActualWidth;
                    BackAnimationStoryboard.Begin();
                }
            }
        }

        private void ForwardAnimationCompleted(object sender, object e)
        {
            LeftFrame.IsHitTestVisible = true;
            RightFrameRoot.IsHitTestVisible = true;
            IsTabStop = true;
            LeftFrame.Visibility = Visibility.Collapsed;
            RightFrameRoot.Visibility = Visibility.Visible;
        }

        private void BackAnimationCompleted(object sender, object e)
        {
            LeftFrame.IsHitTestVisible = true;
            RightFrameRoot.IsHitTestVisible = true;
            IsTabStop = true;
            LeftFrame.Visibility = Visibility.Visible;
            RightFrameRoot.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}