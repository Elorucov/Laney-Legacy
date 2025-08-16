using Elorucov.Laney.Controls.WinUI;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.SettingsPages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Privacy : Page {
        public Privacy() {
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

        private void Load(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await GetPrivacySettingsAsync(); })();
        }

        PrivacyResponse UserPrivacy;

        private async Task GetPrivacySettingsAsync() {
            ErrorInfo.IsOpen = false;
            if (!progress.IsIndeterminate) {
                progress.IsIndeterminate = true;

                string privacyKeys = "mail_send,chat_invite_user,calls,company_messages,page_access,closed_profile";
                object resp = await Account.GetPrivacySettings(AppParameters.ShowAllPrivacySettings ? null : privacyKeys);
                if (resp is PrivacyResponse p) {
                    UserPrivacy = p;
                    SetupPrivacySettings();
                } else {
                    var err = Functions.GetNormalErrorInfo(resp);
                    ErrorInfo.Title = err.Item1;
                    ErrorInfo.Message = err.Item2;
                    ErrorInfo.IsOpen = true;
                }
                progress.IsIndeterminate = false;
            }
        }

        private void SetupPrivacySettings() {
            var group = UserPrivacy.Settings.GroupBy(p => p.Section);
            bool fixMargin = true;
            foreach (var section in group) {
                PrivacySection sectionExt = UserPrivacy.Sections.Where(ps => ps.Name == section.Key).FirstOrDefault();
                string sectionName = sectionExt != null ? sectionExt.Title : section.Key;
                string sectionDesc = sectionExt != null ? sectionExt.Description : null;

                SettingsGroup sg = new SettingsGroup { Header = sectionName };
                if (fixMargin) sg.Margin = new Thickness(0, -40, 0, 0);

                foreach (PrivacySetting setting in section) {
                    SettingsCard s = BuildSetting(setting);
                    sg.Items.Add(s);
                }
                if (!string.IsNullOrEmpty(sectionExt?.Description)) {
                    sg.Items.Add(new TextBlock {
                        Text = sectionExt.Description,
                        Margin = new Thickness(1, 8, 0, 0),
                        Style = Resources["DescriptionTextStyle"] as Style
                    });
                }
                RootStackPanel.Children.Add(sg);
                fixMargin = false;
            }
            ;
        }

        private SettingsCard BuildSetting(PrivacySetting setting) {
            SettingsCard s = new SettingsCard { Header = !string.IsNullOrEmpty(setting.Title) ? setting.Title : setting.Key };

            switch (setting.Type) {
                case "binary":
                    s.Content = BuildToggleSwitch(setting.Key, setting.Value.IsEnabled);
                    break;
                case "category":
                    s.Content = BuildComboBox(setting.Key, setting.SupportedCategories, setting.Value.Category);
                    break;
                case "list":
                    s.Content = BuildComboBox(setting.Key, setting.SupportedCategories, setting.Value.Category, true);
                    break;
                default:
                    s.Description = Locale.Get("unknown_attachment") + $" (type: {setting.Type})";
                    break;
            }

            return s;
        }

        private ToggleSwitch BuildToggleSwitch(string key, bool isEnabled) {
            ToggleSwitch ts = new ToggleSwitch {
                IsOn = isEnabled
            };
            ts.Toggled += async (a, b) => {
                if (!ts.IsEnabled) return;
                ts.IsEnabled = false;
                bool result = await SetPrivacy(key, ts.IsOn.ToString().ToLower(), true);
                if (!result) ts.IsOn = !ts.IsOn;
                ts.IsEnabled = true;
            };
            return ts;
        }

        private object BuildComboBox(string key, List<string> supportedCategories, string currentCategory, bool isList = false) {
            if (currentCategory == null) currentCategory = "some";
            ObservableCollection<PrivacyCategory> categories = new ObservableCollection<PrivacyCategory>();
            PrivacyCategory currCategory = null;

            foreach (string category in supportedCategories) {
                var ctg = UserPrivacy.SupportedCategories.Where(c => c.Value == category).FirstOrDefault();
                if (ctg != null) {
                    if (currentCategory != "some" && category == "some") continue;
                    categories.Add(ctg);
                    if (currentCategory == category) currCategory = ctg;
                }
            }
            ;

            int index = categories.IndexOf(currCategory);

            ComboBox cb = new ComboBox {
                ItemsSource = categories,
                SelectedIndex = index
            };
            cb.SelectionChanged += async (a, b) => {
                if (cb.SelectedItem == null || !cb.IsEnabled) return;
                cb.IsEnabled = false;
                PrivacyCategory selected = cb.SelectedItem as PrivacyCategory;
                if (selected == currCategory) return;

                bool result = await SetPrivacy(key, selected.Value);
                if (result) {
                    if (currCategory.Value == "some") categories.Remove(currCategory);
                    currCategory = selected;
                } else {
                    cb.SelectedItem = currCategory;
                }
                cb.IsEnabled = true;
            };
            return cb;
        }

        private async Task<bool> SetPrivacy(string key, string value, bool isBool = false) {
            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Account.SetPrivacy(key, value, isBool ? null : value));
            if (resp is PrivacySettingValue p) {
                return true;
            } else {
                Functions.ShowHandledErrorTip(resp);
                return false;
            }
        }
    }
}