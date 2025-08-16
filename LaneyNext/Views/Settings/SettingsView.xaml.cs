using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using VK.VKUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Settings
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SettingsView : Page
    {
        ObservableCollection<SettingsMenuItem> SubPages = new ObservableCollection<SettingsMenuItem> {
            new SettingsMenuItem(Constants.SettingsGeneralId, Locale.Get("settings_general"), VKIconName.Icon28SettingsOutline, typeof(General)),
            new SettingsMenuItem(Constants.SettingsInterfaceId, Locale.Get("settings_interface"), VKIconName.Icon28PaletteOutline, typeof(Interface)),
            new SettingsMenuItem(Constants.SettingsNotificationsId, Locale.Get("settings_notifications"), VKIconName.Icon28Notifications, typeof(Notifications)),
            new SettingsMenuItem(Constants.SettingsPrivacyId, Locale.Get("settings_privacy"), VKIconName.Icon28PrivacyOutline, typeof(Privacy)),
            new SettingsMenuItem(Constants.SettingsAboutId, Locale.Get("settings_about"), VKIconName.Icon28InfoOutline, typeof(About)),
            new SettingsMenuItem(Constants.SettingsDebugId, "Debug", VKIconName.Icon28BugOutline, typeof(Debug)),
        };

        bool IsWide { get { return ActualWidth >= 840; } }

        public SettingsView()
        {
            this.InitializeComponent();
            MenuItems.ItemsSource = SubPages;

            SubPageFrame.Navigate(typeof(Page));
        }

        #region Navigation logic

        private void SubPageFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is SettingsPageBase ssp)
            {
                // Checking if menu contains an item with the same CategoryId.
                SettingsMenuItem smi = SubPages.Where(s => ssp.CategoryId == s.CategoryId).FirstOrDefault();
                if (smi != null)
                {
                    // If contains, select this item.
                    MenuItems.SelectedItem = smi;
                    SubPageTitleBar.Content = smi.Title;
                }

                // Change page name in titlebar.
                if (!String.IsNullOrEmpty(ssp.Title)) SubPageTitleBar.Content = ssp.Title;

                // Set titlebar buttons
                SubPageTitleBar.RightButtons.Clear();
                ssp.RightButtons.ForEach(b => SubPageTitleBar.RightButtons.Add(b));

                CheckBackButtonVisibility();
                ShowHideSubPage(true);
            }
            else if (e.Content is Page p)
            { // For first page in SubPageFrame history.
                // We just hide SubPageContent to show menu if window width is narrow.
                ShowHideSubPage(false);
            }
            else
            {
                throw new ArgumentException("Only SettingsSubPage is supported.");
            }
        }

        private void CheckBackButtonVisibility()
        {
            if (!IsWide)
            {
                BackButton.Visibility = Visibility.Visible;
            }
            else
            {
                BackButton.Visibility = SubPageFrame.BackStackDepth > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            SubPageFrame.GoBack();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            SettingsMenuItem smi = e.ClickedItem as SettingsMenuItem;
            if (MenuItems.SelectedItem == smi) return; // Don't navigate to current displayed page
            SubPageFrame.Navigate(smi.Page);
        }

        #endregion

        #region Menu / page visibility, switcher

        public void ShowHideSubPage(bool isShow)
        {
            if (IsWide) return;

            MainPageTitleBar.Visibility = isShow ? Visibility.Collapsed : Visibility.Visible;
            MainPageContent.Visibility = isShow ? Visibility.Collapsed : Visibility.Visible;
            SubPageTitleBar.Visibility = !isShow ? Visibility.Collapsed : Visibility.Visible;
            SubPageContent.Visibility = !isShow ? Visibility.Collapsed : Visibility.Visible;

            if (!isShow) MenuItems.SelectedItem = null;
        }

        private void UpdateLayout(object sender, SizeChangedEventArgs e)
        {
            double w = ActualWidth;
            if (IsWide)
            {
                MainPageContent.Width = 280;
                SubPageContent.Width = 560 - SubPageContent.Margin.Left - SubPageContent.Margin.Right;
                Grid.SetColumn(MainPageContent, 1);
                Grid.SetColumn(MainPageTitleBar, 1);
                Grid.SetColumnSpan(MainPageContent, 1);
                Grid.SetColumnSpan(MainPageTitleBar, 1);
                Grid.SetColumn(SubPageContent, 2);
                Grid.SetColumn(SubPageTitleBar, 2);
                Grid.SetColumnSpan(SubPageContent, 1);
                Grid.SetColumnSpan(SubPageTitleBar, 1);

                MainPageTitleBar.Visibility = Visibility.Visible;
                MainPageContent.Visibility = Visibility.Visible;
                SubPageTitleBar.Visibility = Visibility.Visible;
                SubPageContent.Visibility = Visibility.Visible;

                if (SubPageFrame.BackStackDepth == 0) SubPageFrame.Navigate(SubPages.First().Page);
            }
            else
            {
                MainPageContent.Width = w;
                SubPageContent.Width = w;
                Grid.SetColumn(MainPageContent, 0);
                Grid.SetColumn(MainPageTitleBar, 0);
                Grid.SetColumnSpan(MainPageContent, 4);
                Grid.SetColumnSpan(MainPageTitleBar, 4);
                Grid.SetColumn(SubPageContent, 0);
                Grid.SetColumn(SubPageTitleBar, 0);
                Grid.SetColumnSpan(SubPageContent, 4);
                Grid.SetColumnSpan(SubPageTitleBar, 4);
                ShowHideSubPage(SubPageFrame.BackStackDepth > 0);
            }
            CheckBackButtonVisibility();
        }

        #endregion
    }
}