using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class StickersFlyout : UserControl {
        public StickersFlyout(Flyout flyout, long peerId, long productId = 0, bool isChatSticker = false) {
            this.InitializeComponent();
            Flyout = flyout;
            this.productId = productId;
            this.peerId = peerId;
            this.isChatSticker = isChatSticker;

            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}; productId: {productId}; isChatSticker: {isChatSticker}");

            Loaded += (a, b) => {
                StickerSelected += (c) => Flyout.Hide();
                GraffitiSelected += (c) => Flyout.Hide();
            };
        }

        private Flyout Flyout;
        private long productId = 0;
        private long peerId;
        private bool isChatSticker;


        ObservableCollection<StickerFlyoutItem> MenuItems = null;

        public delegate void StickerSelectedDelegate(ISticker sticker);
        public event StickerSelectedDelegate StickerSelected;

        public delegate void GraffitiSelectedDelegate(Document graffiti);
        public event GraffitiSelectedDelegate GraffitiSelected;

        private void InitStickers(object sender, RoutedEventArgs e) {
            bool canOpenSysEmojiPanel = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7);

            if (MenuItems == null) {
                MenuItems = new ObservableCollection<StickerFlyoutItem>();

                new System.Action(async () => {
                    object respgs = await Execute.GetRecentStickersAndGraffities();
                    if (respgs is StickersFlyoutRecentItems rec) {
                        if (canOpenSysEmojiPanel) {
                            StickerFlyoutItem emojimenu = new StickerFlyoutItem();
                            emojimenu.Type = StickerFlyoutItemType.EmojiPicker;
                            emojimenu.Title = Locale.Get("emoji") + " (Win + .)";
                            emojimenu.Glyph = '';
                            MenuItems.Add(emojimenu);
                        }

                        StickerFlyoutItem gfmenu = new StickerFlyoutItem();
                        gfmenu.Type = StickerFlyoutItemType.GraffitiMenu;
                        gfmenu.Title = Locale.Get("graffiti");
                        gfmenu.Glyph = '';
                        gfmenu.Graffities = new ObservableCollection<Document>(rec.Graffities);
                        MenuItems.Add(gfmenu);

                        StickerFlyoutItem favorite = new StickerFlyoutItem();
                        favorite.Type = StickerFlyoutItemType.StickerPackView;
                        favorite.Title = Locale.Get("favorites");
                        favorite.Glyph = '';
                        favorite.Stickers = new ObservableCollection<Sticker>(rec.FavoriteStickers);
                        MenuItems.Add(favorite);

                        StickerFlyoutItem recent = new StickerFlyoutItem();
                        recent.Type = StickerFlyoutItemType.StickerPackView;
                        recent.Title = Locale.Get("recent");
                        recent.Glyph = '';
                        recent.Stickers = new ObservableCollection<Sticker>(rec.RecentStickers);
                        MenuItems.Add(recent);

                        Items.ItemsSource = MenuItems;
                        Items.SelectedIndex = canOpenSysEmojiPanel ? 3 : 2;

                        if (peerId.IsChat()) {
                            if (AppSession.UGCPacks != null && AppSession.UGCPacks.ContainsKey(peerId)) {
                                Log.Info($"StickersFlyout: loading chat stickers from cache... ({peerId})");
                                await AddUGCPacks(AppSession.UGCPacks[peerId]);
                            } else {
                                Log.Info($"StickersFlyout: getting chat stickers from api... ({peerId})");

                                object respugc = await Execute.GetUGCPacks(peerId);
                                if (respugc is UGCStickerPacksResponse ugcp && ugcp.Items.Count > 0) {
                                    await AddUGCPacks(ugcp.Items);
                                    if (AppSession.UGCPacks == null) AppSession.UGCPacks = new Dictionary<long, List<UGCStickerPack>>();
                                    AppSession.UGCPacks.Add(peerId, ugcp.Items);
                                } else {
                                    Functions.ShowHandledErrorTip(respugc);
                                }
                            }
                        }

                        bool needRefresh = AppSession.CachedStickerPacks == null || AppSession.CachedStickerPacksTimestamp == null ||
                            DateTime.Now - AppSession.CachedStickerPacksTimestamp.Value > TimeSpan.FromMinutes(10);

                        if (needRefresh) {
                            Log.Info("StickersFlyout: sticker packs are not cached or cache expired, loading from api...");
                            object resps = await Store.GetProducts("stickers", "active", true);
                            if (resps is VKList<StoreProduct> sitems) {
                                await AddStickerPacks(sitems.Items);
                                AppSession.CachedStickerPacks = sitems.Items;
                                AppSession.CachedStickerPacksTimestamp = DateTime.Now;
                            } else {
                                Functions.ShowHandledErrorTip(resps);
                            }
                        } else {
                            Log.Info("StickersFlyout: loading sticker packs from cache...");
                            await AddStickerPacks(AppSession.CachedStickerPacks);
                        }
                        loader.Visibility = Visibility.Collapsed;
                    } else {
                        Functions.ShowHandledErrorTip(respgs);
                    }
                })();
            }
        }

        private async Task AddUGCPacks(List<UGCStickerPack> items) {
            StickerFlyoutItem scrollToPack = null;
            foreach (var pack in items) {
                StickerFlyoutItem ugc = new StickerFlyoutItem();
                ugc.Type = StickerFlyoutItemType.UGCStickerPackView;
                ugc.Title = items.Count > 1 ? $"{Locale.Get("stickers_picker_ugc")} ({pack.Id})" : Locale.Get("stickers_picker_ugc");
                ugc.Hint = Locale.Get("stickers_picker_ugc_hint");
                ugc.Path = (string)App.Current.Resources["ChatStickerIcon"];
                ugc.ChatStickers = new ObservableCollection<UGCSticker>(pack.Stickers);
                MenuItems.Add(ugc);
                if (isChatSticker && productId == pack.Id) scrollToPack = ugc;
            }

            if (scrollToPack != null) {
                await Task.Yield();
                Items.SelectedItem = scrollToPack;
                Items.ScrollIntoView(scrollToPack);
            }
        }

        private async Task AddStickerPacks(List<StoreProduct> items) {
            StickerFlyoutItem scrollToPack = null;
            foreach (var a in items) {
                if (!AppSession.ActiveStickerPacks.Contains(a.Id))
                    AppSession.ActiveStickerPacks.Add(a.Id);

                StickerFlyoutItem i = new StickerFlyoutItem();
                i.Type = StickerFlyoutItemType.StickerPackView;
                i.Title = a.Title;

                StickerImage si = a.Previews.Count >= 2 ? a.Previews[a.Previews.Count - 2] : a.Previews[0];
                i.PreviewImage = si.Uri;
                i.Stickers = new ObservableCollection<Sticker>(a.Stickers);
                MenuItems.Add(i);
                if (!isChatSticker && productId == a.Id) scrollToPack = i;
            }

            if (scrollToPack != null) {
                await Task.Yield();
                Items.SelectedItem = scrollToPack;
                Items.ScrollIntoView(scrollToPack);
            }
        }

        private void SelectSticker(object sender, ItemClickEventArgs e) {
            if (e.ClickedItem is Sticker s) {
                if (s.IsAllowed) StickerSelected?.Invoke(s);
            } else if (e.ClickedItem is UGCSticker ugc) {
                if (!ugc.IsDeleted && !ugc.IsClaimed) StickerSelected?.Invoke(ugc);
            }
        }

        private void SelectGraffiti(object sender, ItemClickEventArgs e) {
            GraffitiSelected?.Invoke(e.ClickedItem as Document);
        }

        private void GraffitiFile(object sender, RoutedEventArgs e) {
            Tips.Show(Locale.Get("underconstruction"));
        }

        private void GraffitiDraw(object sender, RoutedEventArgs e) {
            Tips.Show(Locale.Get("underconstruction"));
        }

        private void Items_ItemClick(object sender, ItemClickEventArgs e) {
            StickerFlyoutItem item = e.ClickedItem as StickerFlyoutItem;
            if (item.Type == StickerFlyoutItemType.EmojiPicker) OpenEmojiPicker();
        }

        private void Items_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            StickerFlyoutItem item = Items.SelectedItem as StickerFlyoutItem;
            if (item.Type == StickerFlyoutItemType.EmojiPicker) OpenEmojiPicker();
        }

        private void OpenEmojiPicker() {
            StickerSelected?.Invoke(null);
        }

        private void Image_ContextRequested(UIElement sender, Windows.UI.Xaml.Input.ContextRequestedEventArgs args) {
            FrameworkElement el = sender as FrameworkElement;
            if (el.DataContext is Sticker sticker) {
                new System.Action(async () => { await Functions.ShowStickerKeywordsFlyoutAsync(el, args, sticker.StickerId); })();
            }
        }

        TeachingTip tt = null;
        private void ShowPackHint(object sender, RoutedEventArgs e) {
            HyperlinkButton btn = sender as HyperlinkButton;
            StickerFlyoutItem pack = btn.DataContext as StickerFlyoutItem;

            if (tt == null) {
                tt = new TeachingTip {
                    Title = pack.Title,
                    Subtitle = pack.Hint,
                    PreferredPlacement = TeachingTipPlacementMode.Top,
                    Target = btn
                };
                Tips.AddToAppRoot(tt);
            }
            tt.IsOpen = true;
            Tips.FixUI(tt);
        }
    }
}