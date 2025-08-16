using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.SettingsPages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Blacklist : Page {
        public Blacklist() {
            this.InitializeComponent();
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

        ObservableCollection<MentionItem> Blacklisted = new ObservableCollection<MentionItem>();
        int Count = 0;

        private void Load(object sender, RoutedEventArgs e) {
            list.ItemsSource = Blacklisted;
            new System.Action(async () => { await LoadBlacklistedUsers(); })();
        }

        private async Task LoadBlacklistedUsers() {
            if (!progress.IsIndeterminate) {
                progress.IsIndeterminate = true;

                object resp = await Account.GetBanned();
                if (resp is VKList<long> l) {
                    foreach (long id in l.Items) {
                        if (id.IsUser()) {
                            User user = l.Profiles.Where(u => u.Id == id).FirstOrDefault();
                            Blacklisted.Add(new MentionItem(id, user.Domain, user.FullName, user.Photo));
                        } else if (id.IsGroup()) {
                            Group group = l.Groups.Where(g => g.Id == id * -1).FirstOrDefault();
                            Blacklisted.Add(new MentionItem(id, group.Domain, group.Name, group.Photo));
                        }
                    }
                    Count = l.Count;
                    UpdateCountText();
                } else {
                    if (Blacklisted.Count == 0) loadStatus.Text = Locale.Get("global_error");
                    Functions.ShowHandledErrorDialog(resp);
                }
                progress.IsIndeterminate = false;
            }
        }

        private void UpdateCountText() {
            loadStatus.Text = Count > 0 ? "" : Locale.Get("sb_nousers");
            loadStatus.Visibility = Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RemoveFromBlacklist(object sender, RoutedEventArgs e) {
            HyperlinkButton btn = sender as HyperlinkButton;
            MentionItem u = btn.DataContext as MentionItem;
            new System.Action(async () => {
                object resp = await Account.Unban(u.Id);
                if (resp is bool) {
                    Blacklisted.Remove(u);
                    Count--;
                    UpdateCountText();
                } else {
                    Functions.ShowHandledErrorDialog(resp);
                }
            })();
        }
    }
}