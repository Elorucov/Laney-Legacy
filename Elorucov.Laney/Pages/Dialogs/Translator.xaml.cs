using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.LongPoll;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class Translator : Modal {
        long peerId = 0;
        int cmid = 0;
        string text;
        List<Tuple<string, string>> Pairs = new List<Tuple<string, string>>();

        public Translator(LMessage message) {
            this.InitializeComponent();
            peerId = message.PeerId;
            cmid = message.ConversationMessageId;
            text = message.Text;

            LongPoll.TranslationReceived += LongPoll_TranslationReceived;
            Loaded += Translator_Loaded;
            Unloaded += Translator_Unloaded;

            new System.Action(async () => { await SetupSkeletonsAsync(); })();
        }

        private void Translator_Loaded(object sender, RoutedEventArgs e) {
            foreach (string pair in AppSession.MessagesTranslationLanguagePairs) {
                string[] p = pair.Split('-');
                Pairs.Add(new Tuple<string, string>(p[0], p[1]));
            }

            OriginalText.Text = VKTextParser.GetOnlyParsedText(text);

            int cyrillicLettersCount = 0;
            Regex cyrillic = new Regex(@"[\p{IsCyrillic}\p{P}\p{N}\s]*");
            MatchCollection matches = cyrillic.Matches(text);
            foreach (Match match in matches) {
                cyrillicLettersCount += match.Length;
            }

            // Если кириллических букв больше половины, будем переводить по умолчанию на англ.
            string tlang = cyrillicLettersCount != 0 && text.Length / cyrillicLettersCount > 2 ? "en" : "ru";
            new System.Action(async () => { await DoTranslate(tlang); })();
        }

        private void Translator_Unloaded(object sender, RoutedEventArgs e) {
            LongPoll.TranslationReceived -= LongPoll_TranslationReceived;
            Loaded -= Translator_Loaded;
            Unloaded -= Translator_Unloaded;
        }

        private async Task SetupSkeletonsAsync() {
            SolidColorBrush brush = (SolidColorBrush)App.Current.Resources["SystemControlBackgroundBaseMediumBrush"];

            await SkeletonAnimation.Start(LoaderSkeleton,
                new List<Shape> { TextSkeleton1, TextSkeleton2, TextSkeleton3, TextSkeleton4 },
                brush.Color);
        }

        private async Task DoTranslate(string lang = null) {
            TranslatedText.Text = string.Empty;
            LoaderSkeleton.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(lang)) {
                lang = $"{fromLangBtn.Tag}-{toLangBtn.Tag}";
            }

            object resp = await Messages.Translate(peerId, cmid, lang);
            if (resp is int i) {
                // ¯\_(ツ)_/¯
            } else if (resp is VKError err) {
                string title, message = "";

                if (err.error_code == 972) {
                    title = Locale.Get("translation_error_972_title");
                    message = Locale.Get("translation_error_972");
                } else if (err.error_code == 973) {
                    await ShowLangPickerDialog();
                    return;
                } else {
                    title = Locale.Get("translation_error_general_title");
                    message = Locale.Get("translation_error_general");
                    message += $"\n{Locale.Get("api_error")} ({err.error_code}): {err.error_msg}";
                }

                ContentDialog dlg = new ContentDialog {
                    Title = title,
                    Content = message,
                    PrimaryButtonText = Locale.Get("close"),
                    DefaultButton = ContentDialogButton.Primary
                };
                await dlg.ShowAsync();
                if (err.error_code != 973) Hide();
            } else {
                Functions.ShowHandledErrorDialog(resp);
                Hide();
            }
        }

        private async Task ShowLangPickerDialog() {
            ComboBox slangcb = new ComboBox {
                Header = Locale.Get("translation_original_lang"),
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            ComboBox tlangcb = new ComboBox {
                Header = Locale.Get("translation_translate_lang"),
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            List<string> slangs = new List<string>();
            foreach (var pair in Pairs) {
                if (slangs.Contains(pair.Item1)) continue;
                ComboBoxItem cbi = new ComboBoxItem {
                    Tag = pair.Item1,
                    Content = Locale.Get($"lang_{pair.Item1}")
                };
                slangcb.Items.Add(cbi);
                slangs.Add(pair.Item1);
            }

            slangcb.SelectionChanged += (a, b) => {
                ComboBoxItem scbi = slangcb.SelectedItem as ComboBoxItem;
                string slang = (string)scbi.Tag;

                tlangcb.Items.Clear();
                foreach (var pair in Pairs) {
                    if (pair.Item2 != slang && pair.Item1 == slang) {
                        ComboBoxItem cbi = new ComboBoxItem {
                            Tag = pair.Item2,
                            Content = Locale.Get($"lang_{pair.Item2}")
                        };
                        tlangcb.Items.Add(cbi);
                    }
                }
                tlangcb.SelectedIndex = 0;
            };

            slangcb.SelectedIndex = 0;

            //

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = Locale.Get("translation_error_wronglang"), TextWrapping = TextWrapping.Wrap });
            sp.Children.Add(slangcb);
            sp.Children.Add(tlangcb);

            ContentDialog dlg = new ContentDialog {
                Title = Locale.Get("translation_error_wronglang_title"),
                Content = sp,
                PrimaryButtonText = Locale.Get("translate"),
                SecondaryButtonText = Locale.Get("cancel"),
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                ComboBoxItem scbi = slangcb.SelectedItem as ComboBoxItem;
                string slang = (string)scbi.Tag;

                ComboBoxItem tcbi = tlangcb.SelectedItem as ComboBoxItem;
                string tlang = (string)tcbi.Tag;

                await DoTranslate($"{slang}-{tlang}");
            } else {
                Hide();
            }
        }

        private void LongPoll_TranslationReceived(object sender, LPTranslation e) {
            if (e.PeerId == peerId && e.ConversationMessageId == cmid && e.Error == 0) {
                LoaderSkeleton.Visibility = Visibility.Collapsed;
                TranslatedText.Text = e.Translation.Replace("<br>", "\n").Replace("&quot;", "\"").Replace("&amp;", "&")
                                       .Replace("&lt;", "<").Replace("&gt;", ">");

                string[] p = e.Language.Split('-');
                SetOriginalLang(p[0]);
                SetTranslationLang(p[1]);
            }
        }

        private void SetOriginalLang(string lang) {
            fromLangBtn.Tag = lang;
            fromLangBtn.Content = Locale.Get($"lang_{lang}");
        }

        private void SetTranslationLang(string lang) {
            toLangBtn.Tag = lang;
            toLangBtn.Content = Locale.Get($"lang_{lang}");
        }

        private void fromLangBtnClicked(object sender, RoutedEventArgs e) {
            FrameworkElement button = sender as FrameworkElement;
            string lang = (string)button.Tag;

            List<string> langs = new List<string>();
            foreach (var pair in Pairs) {
                if (!langs.Contains(pair.Item1)) langs.Add(pair.Item1);
            }

            MenuFlyout mf = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
            foreach (string flang in langs) {
                ToggleMenuFlyoutItem mfi = new ToggleMenuFlyoutItem {
                    Tag = flang,
                    Text = Locale.Get($"lang_{flang}"),
                    IsChecked = flang == lang
                };
                mfi.Click += async (a, b) => {
                    if (flang != lang) {
                        SetOriginalLang(flang);

                        foreach (var pair in Pairs) {
                            if (pair.Item1 == flang) {
                                SetTranslationLang(pair.Item2);
                                break;
                            }
                        }

                        await DoTranslate();
                    }
                };
                mf.Items.Add(mfi);
            }

            mf.ShowAt(button);
        }

        private void toLangBtnClicked(object sender, RoutedEventArgs e) {
            FrameworkElement button = sender as FrameworkElement;
            string tlang = (string)button.Tag;
            string flang = (string)fromLangBtn.Tag;

            List<string> langs = new List<string>();
            foreach (var pair in Pairs) {
                if (!langs.Contains(pair.Item2) && pair.Item1 == flang) langs.Add(pair.Item2);
            }

            MenuFlyout mf = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
            foreach (string lang in langs) {
                ToggleMenuFlyoutItem mfi = new ToggleMenuFlyoutItem {
                    Tag = lang,
                    Text = Locale.Get($"lang_{lang}"),
                    IsChecked = lang == tlang
                };
                mfi.Click += (a, b) => {
                    if (lang != tlang) {
                        SetTranslationLang(lang);
                        new System.Action(async () => { await DoTranslate(); })();
                    }
                };
                mf.Items.Add(mfi);
            }

            mf.ShowAt(button);
        }

        private void CopyTranslation(object sender, RoutedEventArgs e) {
            DataPackage dp = new DataPackage();
            dp.RequestedOperation = DataPackageOperation.Copy;
            dp.SetText(TranslatedText.Text);
            Clipboard.SetContent(dp);
            Tips.Show(Locale.Get("copied_to_clipboard"));
        }
    }
}