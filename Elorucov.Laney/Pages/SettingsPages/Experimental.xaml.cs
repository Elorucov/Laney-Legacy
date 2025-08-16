using Elorucov.Laney.Services.Common;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.SettingsPages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Experimental : Page {
        public Experimental() {
            this.InitializeComponent();
            warn.Title = Locale.Get("restart_required");

            var host = Main.GetCurrent();
            BackButton.Visibility = host.IsWideMode ? Visibility.Collapsed : Visibility.Visible;
            host.SizeChanged += Host_SizeChanged;
            Unloaded += (a, b) => host.SizeChanged -= Host_SizeChanged;
        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e) {
            BackButton.Visibility = Main.GetCurrent().IsWideMode ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            Main.GetCurrent().GoBack();
        }

        private void LoadSettings(object sender, RoutedEventArgs e) {
            if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) {
                sci01.Visibility = Visibility.Collapsed;
            }

            if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 14)) {
                sci03.Visibility = Visibility.Collapsed;
            }

            pc01.IsOn = AppParameters.DisplayRamUsage;
            pc01.Toggled += (a, b) => AppParameters.DisplayRamUsage = (a as ToggleSwitch).IsOn;

            pc02.IsOn = AppParameters.ShowOnlineApplicationId;
            pc02.Toggled += (a, b) => AppParameters.ShowOnlineApplicationId = (a as ToggleSwitch).IsOn;

            pi01.IsOn = AppParameters.UseLegacyMREBImplForModernWindows;
            pi01.Toggled += (a, b) => AppParameters.UseLegacyMREBImplForModernWindows = (a as ToggleSwitch).IsOn;

            pi03.IsOn = AppParameters.ForceAcrylicBackgroundOnWin11;
            pi03.Toggled += (a, b) => AppParameters.ForceAcrylicBackgroundOnWin11 = (a as ToggleSwitch).IsOn;

            cn01.SelectedIndex = AppParameters.FileUploaderProvider;
            cn01.SelectionChanged += (a, b) => AppParameters.FileUploaderProvider = (a as ComboBox).SelectedIndex;
        }
    }
}
