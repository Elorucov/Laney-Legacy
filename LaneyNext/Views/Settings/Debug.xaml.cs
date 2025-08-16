using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Helpers.UI;
using System;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Settings
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Debug : SettingsPageBase
    {
        internal static readonly byte[] Features = new byte[] { 0x37, 0x53, 0x72, 0x65 };

        public Debug()
        {
            this.InitializeComponent();
            CategoryId = Constants.SettingsDebugId;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var sessions = await VKSession.GetSessionsAsync();
            var users = sessions.Where(s => s.Type == SessionType.VKUser).ToList();
            Sessions.ItemsSource = users;
            if (users.Count > 0) Sessions.SelectedIndex = 0;

            // Settings
            if (OSHelper.IsAPIContractPresent(7))
            {
                p00.IsOn = Core.Settings.FailFastOnErrors;
                p00.Toggled += (a, b) =>
                {
                    Core.Settings.FailFastOnErrors = p00.IsOn;
                    App.Current.DebugSettings.FailFastOnErrors = p00.IsOn;
                };
            }
            else
            {
                ffp.Visibility = Visibility.Collapsed;
            }

            p01.IsOn = Core.Settings.DebugShowMessageIdCtx;
            p01.Toggled += (a, b) => Core.Settings.DebugShowMessageIdCtx = p01.IsOn;

            p02.IsOn = Core.Settings.AlternativeUploadMethod;
            p02.Toggled += (a, b) => Core.Settings.AlternativeUploadMethod = p02.IsOn;

            p03.IsOn = Core.Settings.DebugShowMessagesListScrollInfo;
            p03.Toggled += (a, b) => Core.Settings.DebugShowMessagesListScrollInfo = p03.IsOn;

            p04.IsOn = Core.Settings.DebugDisplayRAMUsage;
            p04.Toggled += (a, b) => Core.Settings.DebugDisplayRAMUsage = p04.IsOn;

            p05.IsOn = Core.Settings.KeepLogsAfterLogout;
            p05.Toggled += (a, b) => Core.Settings.KeepLogsAfterLogout = p05.IsOn;

            p06.IsOn = Core.Settings.ShowAllPrivacySettings;
            p06.Toggled += (a, b) => Core.Settings.ShowAllPrivacySettings = p06.IsOn;

            p07.IsOn = Core.Settings.UseYandexMaps;
            p07.Toggled += (a, b) => Core.Settings.UseYandexMaps = p07.IsOn;

            p08.IsOn = Core.Settings.MessageBubbleTemplateLoadMethod;
            p08.Toggled += (a, b) => Core.Settings.MessageBubbleTemplateLoadMethod = p08.IsOn;

            p09.IsOn = Core.Settings.StoryViewerSlowDownAnimation;
            p09.Toggled += (a, b) => Core.Settings.StoryViewerSlowDownAnimation = p09.IsOn;

            p10.IsOn = Core.Settings.StoryViewerNoLightThemeForFlyouts;
            p10.Toggled += (a, b) => Core.Settings.StoryViewerNoLightThemeForFlyouts = p10.IsOn;

            p11.IsOn = Core.Settings.StoryViewerClickableStickerBorder;
            p11.Toggled += (a, b) => Core.Settings.StoryViewerClickableStickerBorder = p11.IsOn;

            p12.IsOn = Core.Settings.LottieViewDebug;
            p12.Toggled += (a, b) => Core.Settings.LottieViewDebug = p12.IsOn;

            p13.IsOn = Core.Settings.DontSendActivity;
            p13.Toggled += (a, b) => Core.Settings.DontSendActivity = p13.IsOn;
        }

        private VKSession CurrentSession { get { return Sessions.Items.Count > 0 ? Sessions.SelectedItem as VKSession : null; } }

        private void cb0_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in ApplicationData.Current.LocalSettings.Values)
            {
                ApplicationData.Current.LocalSettings.Values[p.Key] = null;
            }
        }

        private async void cb1_Click(object sender, RoutedEventArgs e)
        {
            var result = await Windows.Security.Credentials.UI.UserConsentVerifier.RequestVerificationAsync("Verify yeah.");
            new Snackbar
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = result.ToString(),
                BeforeIcon = VKIconName.Icon16Fire,
                BeforeIconBackground = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 128, 128, 128))
            }.ShowOnCurrentWindow();
        }

        private async void cb2_Click(object sender, RoutedEventArgs e)
        {
            (sender as Control).IsEnabled = false;

            var appdata = ApplicationData.Current.LocalFolder;
            StorageFolder logfolder = await appdata.GetFolderAsync("logs");

            FolderPicker fsp = new FolderPicker();
            fsp.FileTypeFilter.Add(".log");
            fsp.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            var r = await fsp.PickSingleFolderAsync();
            if (r != null)
            {
                Log.StopAll();
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("logexport", r);

                string logarchname = $"L2_{AppInfo.Version.Build}_logs.zip";
                string path = $"{appdata.Path}\\{logarchname}";
                await Task.Run(() =>
                {
                    ZipFile.CreateFromDirectory(logfolder.Path, path, CompressionLevel.Optimal, false);
                });

                var logarch = await StorageFile.GetFileFromPathAsync(path);
                await logarch.MoveAsync(r);
                await logfolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                Log.ReinitAllLogs();
                await new MessageDialog($"{r.Path}\\{logarchname}", "Logs successfully saved!").ShowAsync();
            }

            (sender as Control).IsEnabled = true;
        }

        private void cb3_Click(object sender, RoutedEventArgs e)
        {
            object g = String.Empty;
            int i = (int)g;
        }

        private async void cb4_Click(object sender, RoutedEventArgs e)
        {
            InputScope iscope = new InputScope();
            iscope.Names.Add(new InputScopeName(InputScopeNameValue.Digits));

            TextBox tb = new TextBox
            {
                Margin = new Thickness(0, 8, 0, 0),
                Style = (Style)Application.Current.Resources["VKTextBox"],
                InputScope = iscope,
                MaxLength = 10,
                PlaceholderText = "User ID (> 0) or group ID (< 0)"
            };

            Alert alert = new Alert
            {
                Header = "Show user or group card",
                Content = tb,
                PrimaryButtonText = "Show",
                SecondaryButtonText = Locale.Get("close")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                int id = 0;
                if (Int32.TryParse(tb.Text, out id) && id != 0)
                {
                    CoreApplicationView cav = await ViewManagement.GetViewBySession(CurrentSession);
                    if (cav == null) return;
                    ViewManagement.SwitchToView(cav, () => Router.ShowCard(id));
                }
            }
        }

        private async void cb5_Click(object sender, RoutedEventArgs e)
        {
            InputScope iscope = new InputScope();
            iscope.Names.Add(new InputScopeName(InputScopeNameValue.Digits));

            TextBox tb = new TextBox
            {
                Margin = new Thickness(0, 8, 0, 0),
                Style = (Style)Application.Current.Resources["VKTextBox"],
                InputScope = iscope,
                MaxLength = 10,
                PlaceholderText = "Peer ID (ex. 172894294, -1, or 2000000004)"
            };

            Alert alert = new Alert
            {
                Header = "Show conversation",
                Content = tb,
                PrimaryButtonText = "Show",
                SecondaryButtonText = Locale.Get("close")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                int id = 0;
                if (Int32.TryParse(tb.Text, out id) && id != 0)
                {
                    CoreApplicationView cav = await ViewManagement.GetViewBySession(CurrentSession);
                    if (cav == null) return;
                    ViewManagement.SwitchToView(cav, () => VKSession.Current.SessionBase.SwitchToConversation(id));
                }
            }
        }

        private async void cb6_Click(object sender, RoutedEventArgs e)
        {
            CoreApplicationView cav = await ViewManagement.GetViewBySession(CurrentSession);
            if (cav == null) return;
            ViewManagement.SwitchToView(cav, () => OpenParseLinkModal());
        }

        private async void OpenParseLinkModal()
        {
            TextBox tb = new TextBox
            {
                Margin = new Thickness(0, 8, 0, 0),
                Style = (Style)Application.Current.Resources["VKTextBox"],
                PlaceholderText = "Link",
            };

            Alert alert = new Alert
            {
                Header = "Parse link",
                Content = tb,
                PrimaryButtonText = "Parse",
                SecondaryButtonText = Locale.Get("close")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                if (Uri.IsWellFormedUriString(tb.Text, UriKind.RelativeOrAbsolute))
                {
                    var r = await Router.LaunchLinkAsync(tb.Text);
                    await new Alert
                    {
                        Header = "Link parsing result",
                        Text = $"{r.Item1}\n{r.Item2}",
                        PrimaryButtonText = Locale.Get("close"),
                    }.ShowAsync();
                }
            }
        }

        private void cb7_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DebugViews.APIConsoleView), (Sessions.SelectedItem as VKSession).API);
        }

        private void cb8_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DebugViews.SettingsEditor));
        }

        private void cb9_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DebugViews.TextParserTest));
        }

        private async void cb10_Click(object sender, RoutedEventArgs e)
        {
            var result = await LocalPassword.ShowPasswordDialogAsync();
            new Snackbar
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = result.ToString(),
                BeforeIcon = VKIconName.Icon16Fire,
                BeforeIconBackground = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 128, 128, 128))
            }.ShowOnCurrentWindow();
        }
    }
}