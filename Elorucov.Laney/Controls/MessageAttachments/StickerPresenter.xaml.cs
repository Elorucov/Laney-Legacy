using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using VK.VKUI.Controls;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class StickerPresenter : UserControl {
        long id = 0;
        public StickerPresenter() {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(StickerProperty, ChangedCallback);
            Unloaded += (a, b) => {
                if (id != 0) UnregisterPropertyChangedCallback(StickerProperty, id);
            };
        }

        public static readonly DependencyProperty StickerProperty = DependencyProperty.Register(
                   nameof(Sticker), typeof(ISticker), typeof(StickerPresenter), new PropertyMetadata(default));

        public ISticker Sticker {
            get { return (ISticker)GetValue(StickerProperty); }
            set { SetValue(StickerProperty, value); }
        }

        public static readonly DependencyProperty IsDarkThemeForcedProperty = DependencyProperty.Register(
           nameof(IsDarkThemeForced), typeof(bool), typeof(StickerPresenter), new PropertyMetadata(default));

        public bool IsDarkThemeForced {
            get { return (bool)GetValue(IsDarkThemeForcedProperty); }
            set { SetValue(IsDarkThemeForcedProperty, value); }
        }

        private void ChangedCallback(DependencyObject sender, DependencyProperty dp) {
            new System.Action(async () => { await DisplayStickerAsync((ISticker)GetValue(dp)); })();
        }

        private async Task DisplayStickerAsync(ISticker sticker) {
            Uri uri = APIHelper.GetStickerUri(sticker, 168, IsDarkThemeForced);
            if (uri != null) await img.SetUriSourceAsync(uri);

            if (sticker is Sticker s) {
                string keywords = StickersKeywords.GetKeywordsForSticker(s.StickerId);
                if (!string.IsNullOrEmpty(keywords)) keywords = $"\n{keywords}";
                ToolTipService.SetToolTip(this, $"ID: {s.StickerId}{keywords}");
                TryPlayAnimatedSticker(s);
            } else if (sticker is UGCSticker ugc) {
                if (ugc.IsDeleted) {
                    ShowRestrictionInfo(VKIconName.Icon28DeleteOutline, Locale.Get("ugc_sticker_deleted"));
                } else if (ugc.IsClaimed || ugc.Status == UGCStickerStatus.Banned) {
                    ShowRestrictionInfo(VKIconName.Icon28CancelOutline, Locale.Get("ugc_sticker_banned"));
                } else if (!string.IsNullOrEmpty(ugc.ActiveRestriction)) {
                    switch (ugc.ActiveRestriction) {
                        case "age_18":
                            ShowRestrictionInfo(VKIconName.Icon28ErrorOutline, Locale.Get("ugc_sticker_age_restricted"));
                            break;
                        default:
                            ShowRestrictionInfo(VKIconName.Icon28ErrorOutline, ugc.ActiveRestriction);
                            break;
                    }
                }
            }
        }

        bool animStickerInitialized = false;

        private void TryPlayAnimatedSticker(Sticker sticker) {
            new System.Action(async () => {
                try {
                    if (Theme.IsAnimationsEnabled && AppParameters.AnimatedStickers && !string.IsNullOrEmpty(sticker.AnimationUrl)) {
                        string url = sticker.AnimationUrl;
                        if (Theme.IsDarkTheme() || IsDarkThemeForced) url = sticker.AnimationUrl.Replace(".json", "b.json");
                        StorageFile animatedStickerFile = await LNetExtensions.DownloadFileToTempFolderAsync(new Uri(url));

                        if (!animStickerInitialized && animatedStickerFile != null) {
                            animStickerInitialized = true;
#if ARM32
                        FindName(nameof(AnimatedStickerLegacy));
                        AnimatedStickerLegacy.FirstFrameRendered += (a, b) => StaticSticker.Visibility = Visibility.Collapsed;
                        AnimatedStickerLegacy.Source = new Uri($"ms-appdata://temp/{animatedStickerFile.Name}");
                    }
                    AnimatedStickerLegacy.Play();
#else
                            FindName(nameof(AnimatedSticker));
                            AnimatedSticker.FirstFrameRendered += (a, b) => StaticSticker.Visibility = Visibility.Collapsed;
                            AnimatedSticker.Source = new Uri($"ms-appdata://temp/{animatedStickerFile.Name}");
                        }
                        AnimatedSticker.Play();
#endif
                    }
                } catch (Exception ex) {
                    var info = Functions.GetNormalErrorInfo(ex);
                    ShowRestrictionInfo(VKIconName.Icon28ErrorOutline, $"{info.Item1}\n{info.Item2}");
                }
            })();
        }

        private void TryShowStickerPack(object sender, RoutedEventArgs e) {
            if (Sticker is Sticker sticker) {
                new System.Action(async () => {
                    if (AppSession.ActiveStickerPacks.Contains(sticker.ProductId)) {
                        var ctrl = CoreApplication.GetCurrentView().CoreWindow.GetKeyState(Windows.System.VirtualKey.Control);
                        if (ctrl == Windows.UI.Core.CoreVirtualKeyStates.Down) {
                            await VKLinks.ShowStickerPackInfoAsync(sticker.ProductId);
                        } else {
                            MessageForm.TryShowStickersFlyout(sticker.ProductId);
                        }
                        if (true) return;
                    } else {
                        await VKLinks.ShowStickerPackInfoAsync(sticker.ProductId);
                    }
                })();
            } else if (Sticker is UGCSticker ugc) {
                if (string.IsNullOrEmpty(ugc.ActiveRestriction)) {
                    MessageForm.TryShowStickersFlyoutUGC(ugc.OwnerId, ugc.PackId);
                } else {
                    new System.Action(async () => {
                        string title = "";
                        string text = "";

                        switch (ugc.ActiveRestriction) {
                            case "age_18":
                                title = Locale.Get("ugc_sticker_age_restriction_modal_title");
                                text = Locale.Get("ugc_sticker_age_restriction_modal_text");
                                break;
                            default:
                                title = Locale.Get("global_error");
                                text = $"active_restriction: {ugc.ActiveRestriction}";
                                break;
                        }

                        ContentDialog dlg = new ContentDialog {
                            Title = title,
                            Content = text,
                            PrimaryButtonText = Locale.Get("modal_ok"),
                            DefaultButton = ContentDialogButton.Primary
                        };
                        await dlg.ShowAsync();
                    })();
                }
            }
        }

        private void ShowRestrictionInfo(VKIconName icon, string text) {
            FindName(nameof(UGCRestrictionInfo));
            InfoIcon.Id = icon;
            InfoText.Text = text;
        }
    }
}