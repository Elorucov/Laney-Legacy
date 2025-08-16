using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.SettingsPages;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using System.Collections.Generic;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsCategoriesView : Page {
        public SettingsCategoriesView() {
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            InitializeComponent();

            List<SettingPageEntry> primary = new List<SettingPageEntry> {
                new SettingPageEntry(typeof(General), Locale.Get("settings_general/Text"), ""),
                new SettingPageEntry(typeof(Interface), Locale.Get("settings_appearance/Text"), ""),
                new SettingPageEntry(typeof(Reactions), Locale.Get("settings_reactions/Text"), ""),
                new SettingPageEntry(typeof(Notifications), Locale.Get("settings_notifications/Text"), ""),
                new SettingPageEntry(typeof(Privacy), Locale.Get("settings_privacy/Text"), ""),
                new SettingPageEntry(typeof(Blacklist), Locale.Get("settings_blacklist/Text"), ""),
                new SettingPageEntry(typeof(About), Locale.Get("settings_about_app/Text"), ""),
                new SettingPageEntry(typeof(Experimental), Locale.Get("settings_experimental/Text"), "")
            };
            PrimaryPages.ItemsSource = primary;
            // PrimaryPages.SelectedItem = primary.First();
            PrimaryPages.Focus(FocusState.Programmatic);
        }

        private void GoBack(object sender, RoutedEventArgs e) {
            Frame.GoBack(App.DefaultBackNavTransition);
        }

        private void NavigateToPage(SettingPageEntry entry) {
            Main.GetCurrent().ToggleContentLayerVisibility(false);
            Main.GetCurrent().NavigateToPage(entry.Page, null, true);
        }

        private void PrimaryPages_ItemClick(object sender, ItemClickEventArgs e) {
            SettingPageEntry spe = e.ClickedItem as SettingPageEntry;
            if (spe == null) return;
            NavigateToPage(spe);
        }
    }
}
