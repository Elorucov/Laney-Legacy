using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.SettingsPages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class General : Page {
        public General() {
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

        bool canFireToggledEvent = false;

        private void LoadSettings(object sender, RoutedEventArgs e) {
            stickersSuggestions.IsOn = AppParameters.StickersKeywordsEnabled;
            AnimatedStickers.IsOn = AppParameters.AnimatedStickers;
            animStickersStg.IsEnabled = Theme.IsAnimationsEnabled;
            LanguageSetting();
            // APIDomainInfo();
            new System.Action(async () => {
                await CheckLogs();
                await CheckPasscode();
            })();

            if (AppParameters.MessageSendEnterButtonMode) {
                msm1.IsChecked = true;
            } else {
                msm2.IsChecked = true;
            }

            dontParseLinks.IsOn = AppParameters.MessageSendDontParseLinks;
            dontParseLinks.Toggled += (a, b) => AppParameters.MessageSendDontParseLinks = dontParseLinks.IsOn;

            disableMentions.IsOn = AppParameters.MessageSendDisableMentions;
            disableMentions.Toggled += (a, b) => AppParameters.MessageSendDisableMentions = disableMentions.IsOn;

            p12.IsOn = AppParameters.LongPollFix;
            p12.Toggled += (a, b) => AppParameters.LongPollFix = p12.IsOn;

            p13.IsOn = AppParameters.IsTextSelectionEnabled;
            p13.Toggled += (a, b) => {
                AppParameters.IsTextSelectionEnabled = p13.IsOn;
                Theme.ChangeIsTextSelectionEnabled();
            };

            canFireToggledEvent = true;
        }

        private void EnterBtnSendMethodChanged(object sender, RoutedEventArgs e) {
            RadioButton b = sender as RadioButton;
            int i = int.Parse(b.Tag.ToString());
            AppParameters.MessageSendEnterButtonMode = i == 1 ? true : false;
        }

        private void LanguageSetting() {
            List<AppLanguage> langs = new List<AppLanguage>();
            langs.Insert(0, new AppLanguage { LanguageCode = null, DisplayName = "Default" });
            langs.AddRange(AppLanguage.SupportedLanguages);

            CurrentLang.ItemsSource = langs;

            // Check current lang
            string overridel = ApplicationLanguages.PrimaryLanguageOverride;

            if (string.IsNullOrEmpty(overridel)) {
                CurrentLang.SelectedIndex = 0;
            } else {
                AppLanguage lng = langs.FirstOrDefault(z => z.LanguageCode == overridel);
                CurrentLang.SelectedItem = lng;
            }

            CurrentLang.SelectionChanged += (a, b) => {
                AppLanguage lng = CurrentLang.SelectedItem as AppLanguage;
                if (lng.LanguageCode == null) {
                    ApplicationLanguages.PrimaryLanguageOverride = null;
                } else {
                    ApplicationLanguages.PrimaryLanguageOverride = lng.LanguageCode;
                }


                Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView().Reset();
                lang.Description = Locale.Get("restart_required");
            };
        }

        //private void APIDomainInfo() {
        //    proxy.Description = string.IsNullOrEmpty(AppParameters.VkApiDomain) ? Locale.Get("sg_apiserver_notusing") : AppParameters.VkApiDomain;
        //}

        private async Task CheckLogs() {
            string info = "";

            try {
                StorageFolder logfolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("logs");
                ulong size = 0;

                foreach (var f in await logfolder.GetFilesAsync()) {
                    size += (await f.GetBasicPropertiesAsync()).Size;
                    loginfo.Description = $"{Locale.Get("sg_logs_sizeinfo")}: {Functions.GetFileSize(size)}.";
                }

                logbtn.IsEnabled = true;
            } catch (Exception ex) {
                if (ex.HResult == -2147024894) {
                    info = Locale.Get("sg_logs_empty");
                } else {
                    info = $"{Locale.Get("global_error")} 0x{ex.HResult.ToString("x8")}";
                }
            }
            prog.IsActive = false;
        }

        private void ExportLogs(object sender, RoutedEventArgs e) {
            logbtn.IsEnabled = false;
            prog.IsActive = true;
            loginfo.Description = $"{Locale.Get("wait")}...";

            Log.UnInit();
            new System.Action(async () => {
                bool result = await Functions.SaveLogsAsync();

                await Log.InitAsync();
                await CheckLogs();
            })();
        }

        //private void Button_Click(object sender, RoutedEventArgs e) {
        //    ContentDialog dlg = new ContentDialog();
        //    dlg.Content = new ProxySettings();
        //    dlg.SecondaryButtonText = Locale.Get("close");

        //    await dlg.ShowAsync();
        //}

        private async Task CheckPasscode() {
            if (string.IsNullOrEmpty(AppParameters.Passcode)) {
                passetbtn.Content = Locale.Get("sg_passcode_set");
                passrmbtn.Visibility = Visibility.Collapsed;
            } else {
                passetbtn.Content = Locale.Get("sg_passcode_change");
                passrmbtn.Visibility = Visibility.Visible;
                await CheckWinHelloAvailable();
            }
        }

        private async Task CheckWinHelloAvailable() {
            var result = await UserConsentVerifier.CheckAvailabilityAsync();
            if (result == UserConsentVerifierAvailability.Available) {
                ToggleSwitch ts = passwinhello.Content as ToggleSwitch;
                passwinhello.Visibility = Visibility.Visible;
                ts.IsOn = AppParameters.WindowsHelloInsteadPasscode;
                ts.IsEnabled = true;
            }
        }

        private void SetOrChangePasscode(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await ShowPasscodeChangerModal(true); })();
        }

        private void RemovePasscode(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await ShowPasscodeChangerModal(false); })();
        }

        private async Task ShowPasscodeChangerModal(bool forChange) {
            // forChange is ignored if passcode is not set.
            bool hasPass = !string.IsNullOrEmpty(AppParameters.Passcode);

            string title = Locale.Get("sg_pass_modal_creation");
            string pbtn = Locale.Get("sg_pass_modal_btn_set");
            StackPanel sp = new StackPanel();

            PasswordBox opb = new PasswordBox() {
                Header = Locale.Get("sg_pass_old"),
                Margin = new Thickness(0, 12, 0, 0)
            };
            PasswordBox npb = new PasswordBox() {
                Header = Locale.Get("sg_pass_new"),
                Margin = new Thickness(0, 12, 0, 0)
            };
            PasswordBox rpb = new PasswordBox() {
                Header = Locale.Get("sg_pass_again"),
                Margin = new Thickness(0, 12, 0, 0)
            };

            if (!hasPass) {
                TextBlock info = new TextBlock { Text = Locale.Get("sg_passcode_info"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
                sp.Children.Add(info);
            }

            if (hasPass) {
                title = Locale.Get(forChange ? "sg_pass_modal_change" : "sg_pass_modal_remove");
                if (!forChange) pbtn = Locale.Get("sg_pass_modal_btn_delete");
                sp.Children.Add(opb);
            }

            if (!hasPass || (hasPass && forChange)) {
                sp.Children.Add(npb);
                sp.Children.Add(rpb);
            }

            TextBlock status = new TextBlock { Foreground = Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as SolidColorBrush };
            sp.Children.Add(status);

            ContentDialog dlg = new ContentDialog() {
                Title = title,
                PrimaryButtonText = pbtn,
                SecondaryButtonText = Locale.Get("cancel"),
                DefaultButton = ContentDialogButton.Primary,
            };
            dlg.Content = sp;
            dlg.PrimaryButtonClick += (a, b) => { PasscodeSetup(hasPass, forChange, dlg, opb, npb, rpb, status, b); };

            var result = await dlg.ShowAsync();
            await CheckPasscode();
        }

        private void PasscodeSetup(bool hasPass, bool forChange, ContentDialog dlg, PasswordBox opb, PasswordBox npb, PasswordBox rpb, TextBlock status, ContentDialogButtonClickEventArgs b) {
            b.Cancel = true;
            status.Text = string.Empty;

            if (!hasPass) {
                if (npb.Password == rpb.Password) {
                    if (npb.Password.Length >= 4) {
                        AppParameters.Passcode = npb.Password;
                        dlg.Hide();
                    } else {
                        status.Text = Locale.Get("sg_pass_modal_short");
                    }
                } else {
                    status.Text = Locale.Get("sg_pass_modal_mismatch");
                }
            } else {
                if (AppParameters.Passcode == opb.Password) {
                    if (forChange) {
                        if (npb.Password == rpb.Password) {
                            if (npb.Password.Length >= 4) {
                                AppParameters.Passcode = npb.Password;
                                dlg.Hide();
                            } else {
                                status.Text = Locale.Get("sg_pass_modal_short");
                            }
                        } else {
                            status.Text = Locale.Get("sg_pass_modal_mismatch");
                        }
                    } else {
                        AppParameters.Passcode = null;
                        AppParameters.WindowsHelloInsteadPasscode = false;
                        passwinhello.Visibility = Visibility.Collapsed;
                        dlg.Hide();
                    }
                } else {
                    status.Text = Locale.Get("sg_pass_modal_wrong");
                }
            }
        }

        private void EnableDisableWinHello(object sender, RoutedEventArgs e) {
            ToggleSwitch ts = passwinhello.Content as ToggleSwitch;
            if (!ts.IsEnabled) return;

            new System.Action(async () => {
                ts.IsEnabled = false;
                var result = await UserConsentVerifier.RequestVerificationAsync(" ");
                if (result == UserConsentVerificationResult.Verified) {
                    AppParameters.WindowsHelloInsteadPasscode = !AppParameters.WindowsHelloInsteadPasscode;
                }
                ts.IsOn = AppParameters.WindowsHelloInsteadPasscode;
                ts.IsEnabled = true;
            })();
        }

        private void EnableDisableStickersSuggestions(object sender, RoutedEventArgs e) {
            if (!canFireToggledEvent) return;
            AppParameters.StickersKeywordsEnabled = stickersSuggestions.IsOn;
            if (AppParameters.StickersKeywordsEnabled) {
                new System.Action(async () => { await StickersKeywords.InitAsync(); })();
            }
        }

        private void EnableDisableAnimatedStickers(object sender, RoutedEventArgs e) {
            if (!canFireToggledEvent) return;
            AppParameters.AnimatedStickers = AnimatedStickers.IsOn;
        }
    }
}