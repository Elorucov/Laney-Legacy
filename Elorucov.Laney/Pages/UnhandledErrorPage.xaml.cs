using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.LongPoll;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class UnhandledErrorPage : Page {
        private int _type;
        private Exception _exception;
        static bool isAlreadyOpen;

        public static async Task ShowErrorPageAsync(int v, Exception e) {
            if (isAlreadyOpen) return;
            isAlreadyOpen = true;
            await ViewManagement.CloseAllAnotherWindowsAsync();

            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                SystemNavigationManager.GetForCurrentView().BackRequested += (a, b) => { b.Handled = true; App.Current.Exit(); };
                foreach (Popup p in VisualTreeHelper.GetOpenPopups(Window.Current)) {
                    p.IsOpen = false;
                }
                LongPoll.Stop();
                Window.Current.Content = new UnhandledErrorPage(v, e);
            });
        }

        public UnhandledErrorPage(int type, Exception exception) {
            this.InitializeComponent();
            _type = type;
            _exception = exception;

            Loaded += (a, b) => {
                if (Theme.IsMicaAvailable) LayoutRoot.Background = null;
                LongPoll.Stop();
                Log.UnInit();
                AudioPlayerViewModel.CloseMainInstance();
                AudioPlayerViewModel.CloseVoiceMessageInstance();
                AppSession.Clear();
                ErrType.Text = GetErrorType(type);
            };
        }

        private string GetErrorType(int type) {
            switch (type) {
                case 0: return "CoreApplication";
                case 1: return "Common/XAML";
                case 2: return "Unobserved task";
                default: return "Unknown";
            }
        }

        private void ShowErrorInfo(object sender, RoutedEventArgs e) {
            string exc = $"HResult: (0x{_exception.HResult.ToString("x8")})\n\nMessage:\n{_exception.Message}\n\nSource: {_exception.Source}\n\nStacktrace:\n{_exception.StackTrace}";

            if (_exception.InnerException != null) {
                var ie = _exception.InnerException;
                exc += $"\n\n===== INNER EXCEPTION =====\nHResult: (0x{ie.HResult.ToString("x8")})\n\nMessage:\n{ie.Message}\n\nSource: {ie.Source}\n\nStacktrace:\n{ie.StackTrace}";
            }

            new System.Action(async () => {
                await new ContentDialog() {
                    Title = $"Error info",
                    Content = new ScrollViewer {
                        Content = new TextBlock {
                            Text = exc,
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 12,
                            FontFamily = new FontFamily("Lucida Console"),
                            MinHeight = 384,
                            IsTextSelectionEnabled = true
                        },
                    },
                    IsPrimaryButtonEnabled = false,
                    SecondaryButtonText = "Close",
                }.ShowAsync();
            })();
        }

        private void LinkClicked(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args) {
            new System.Action(async () => { await Launcher.LaunchUriAsync(new Uri("https://vk.me/elorlaney")); })();
        }

        private void SaveLogs(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                logsBtn.IsEnabled = false;
                bool result = await Functions.SaveLogsAsync();
                logsBtn.IsEnabled = true;
            })();
        }
    }
}
