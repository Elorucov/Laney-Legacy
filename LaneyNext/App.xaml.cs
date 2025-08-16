using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney
{
    /// <summary>
    /// Обеспечивает зависящее от конкретного приложения поведение, дополняющее класс Application по умолчанию.
    /// </summary>
    sealed partial class App : Application
    {
        public static bool Launched { get; private set; }

        /// <summary>
        /// Инициализирует одноэлементный объект приложения.  Это первая выполняемая строка разрабатываемого
        /// кода; поэтому она является логическим эквивалентом main() или WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            Log.General = new Log(Constants.LogGeneralProvider);
            Log.General.ReinitRequested += (a, b) => Log.General = new Log(Constants.LogGeneralProvider);

            this.Suspending += OnSuspending;
            CoreApplication.UnhandledErrorDetected += CoreApplication_UnhandledErrorDetected;
            UnhandledException += App_UnhandledException;
            DebugSettings.BindingFailed += (a, b) =>
            {
                Log.General.Warn($"Binding failed! {b.Message}");
            };
            if (OSHelper.IsAPIContractPresent(7))
            {
                App.Current.DebugSettings.FailFastOnErrors = Settings.FailFastOnErrors;
            }
            CoreApplication.EnablePrelaunch(true);
        }

        Exception CoreAppException = null;

        /// <summary>
        /// Вызывается при обычном запуске приложения пользователем.  Будут использоваться другие точки входа,
        /// например, если приложение запускается для открытия конкретного файла.
        /// </summary>
        /// <param name="e">Сведения о запросе и обработке запуска.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Log.General.Info("App launched.", new ValueSet { { "kind", e.Kind.ToString() }, { "is_expired", Core.AppInfo.IsExpired }, { "already_launched", Launched } });

            if (!Launched)
            {
                Splash s = new Splash(e);
                Window.Current.Content = s;
                Launched = true;
            }
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            base.OnShareTargetActivated(args);
            Log.General.Info("App launched.", new ValueSet { { "kind", args.Kind.ToString() }, { "is_expired", Core.AppInfo.IsExpired }, { "already_launched", Launched } });
            if (Core.AppInfo.IsExpired) App.Current.Exit();

            var so = args.ShareOperation;
            so.ReportStarted();
            so.ReportDataRetrieved();

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ThemeManager.FixTitleBarButtonsColor();

            Frame rootframe = new Frame();
            Window.Current.Content = rootframe;
            Window.Current.Activate();
            rootframe.Navigate(typeof(Views.ShareTargetView), args);
        }

        private void CoreApplication_UnhandledErrorDetected(object sender, UnhandledErrorDetectedEventArgs e)
        {
            try
            {
                e.UnhandledError.Propagate();
            }
            catch (Exception ex)
            {
                if (CoreAppException != null) return;
                CoreAppException = ex;
                Log.General.Critical($"CoreApplication Unhandled error!",
                    new ValueSet { { "hresult", $"0x{ex.HResult.ToString("x8")}" }, { "message", ex.Message.Trim() }, { "stacktrace", ex.StackTrace.Trim() } });
                HandleGlobalCrashAsync(ex, "CoreApplication.UnhandledErrorDetected");
            }
        }

        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            if (CoreAppException == null)
            {
                Exception ex = e.Exception;
                Log.General.Critical($"XAML App Unhandled Error!",
                    new ValueSet { { "hresult", $"0x{ex.HResult.ToString("x8")}" }, { "message", ex.Message.Trim() }, { "stacktrace", ex.StackTrace.Trim() } });
                HandleGlobalCrashAsync(e.Exception, "XAML.UnhandledException", e.Message);
            }
        }

        /// <summary>
        /// Вызывается при приостановке выполнения приложения.  Состояние приложения сохраняется
        /// без учета информации о том, будет ли оно завершено или возобновлено с неизменным
        /// содержимым памяти.
        /// </summary>
        /// <param name="sender">Источник запроса приостановки.</param>
        /// <param name="e">Сведения о запросе приостановки.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            Log.General.Info("Suspending...");
            deferral.Complete();
        }

        // App crash
        public static async void HandleGlobalCrashAsync(Exception ex, string source = "Unknown", string message = "")
        {
            ViewManagement.CloseAllAnotherWindows();
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                MessageDialog dlg = new MessageDialog($"Source: {source}\nHResult: 0x{ex.HResult.ToString("x8")}\n\n{ex.Message}\n\n{message}", "Unhandled error!");
                dlg.Options = MessageDialogOptions.AcceptUserInputAfterDelay;
                dlg.Commands.Add(new UICommand { Label = "Open logs folder", Id = 777 });
                dlg.Commands.Add(new UICommand { Label = "Close", Id = 666 });
                var r = await dlg.ShowAsync();
                if ((int)r.Id == 777)
                {
                    StorageFolder logsfolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists);
                    await Launcher.LaunchFolderAsync(logsfolder);
                }
                Application.Current.Exit();
            });
        }
    }
}