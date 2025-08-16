using Elorucov.Laney.Models;
using Elorucov.Laney.Models.AvatarCreator;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.Web.Http;

namespace Elorucov.Laney.ViewModel {
    public class AvatarCreatorViewModel : BaseViewModel {
        private bool _isReady = false;
        private bool _isIndeterminate = true;
        private double _loadingProgress = 0;

        private ObservableCollection<AvatarCreatorItemCollection> _groupedEmojis;
        private Emoji _selectedEmoji;
        private string _emojiSearchQuery;
        private List<EmojiSkinTone> _emojiSkinTones;
        private EmojiSkinTone _emojiCurrentSkinTone;

        bool _isStickerPacksLoading = false;
        private ObservableCollection<AvatarCreatorItemCollection> _stickerPacks = new ObservableCollection<AvatarCreatorItemCollection>();
        private VKSticker _selectedSticker;

        private ObservableCollection<AvatarCreatorItemCollection> _kaomojiList;
        private double _kaomojiSize = 48;
        private Color _kaomojiColor;

        private Color _gradientStartColor;
        private Color _gradientEndColor;
        private GradientDirection _gradientDirection;


        public bool IsReady { get { return _isReady; } private set { _isReady = value; OnPropertyChanged(); } }
        public bool IsIndeterminate { get { return _isIndeterminate; } private set { _isIndeterminate = value; OnPropertyChanged(); } }
        public double LoadingProgress { get { return _loadingProgress; } private set { _loadingProgress = value; OnPropertyChanged(); } }


        public ObservableCollection<AvatarCreatorItemCollection> GroupedEmojis { get { return _groupedEmojis; } set { _groupedEmojis = value; OnPropertyChanged(); } }
        public Emoji SelectedEmoji { get { return _selectedEmoji; } set { _selectedEmoji = value; OnPropertyChanged(); } }
        public string EmojiSearchQuery { get { return _emojiSearchQuery; } set { _emojiSearchQuery = value; OnPropertyChanged(); } }
        public List<EmojiSkinTone> EmojiSkinTones { get { return _emojiSkinTones; } set { _emojiSkinTones = value; OnPropertyChanged(); } }
        public EmojiSkinTone EmojiCurrentSkinTone { get { return _emojiCurrentSkinTone; } set { _emojiCurrentSkinTone = value; OnPropertyChanged(); } }

        public bool IsStickerPacksLoading { get { return _isStickerPacksLoading; } private set { _isStickerPacksLoading = value; OnPropertyChanged(); } }
        public ObservableCollection<AvatarCreatorItemCollection> StickerPacks { get { return _stickerPacks; } private set { _stickerPacks = value; OnPropertyChanged(); } }
        public VKSticker SelectedSticker { get { return _selectedSticker; } set { _selectedSticker = value; OnPropertyChanged(); } }

        public ObservableCollection<AvatarCreatorItemCollection> KaomojiList { get { return _kaomojiList; } private set { _kaomojiList = value; OnPropertyChanged(); } }
        public Double KaomojiSize { get { return _kaomojiSize; } set { _kaomojiSize = value; OnPropertyChanged(); } }
        public Color KaomojiColor { get { return _kaomojiColor; } set { _kaomojiColor = value; OnPropertyChanged(); } }

        public Color GradientStartColor { get { return _gradientStartColor; } set { _gradientStartColor = value; OnPropertyChanged(); } }
        public Color GradientEndColor { get { return _gradientEndColor; } set { _gradientEndColor = value; OnPropertyChanged(); } }
        public GradientDirection GradientDirection { get { return _gradientDirection; } set { _gradientDirection = value; OnPropertyChanged(); } }
        public List<GradientPreset> GradientPresets { get { return GradientPreset.Presets; } }

        public async Task SetupAsync() {
            GradientStartColor = Colors.Yellow;
            GradientEndColor = Colors.Blue;
            KaomojiColor = Colors.White;

            EmojiSearchAction = new DelayedAction(() => SeachEmojiAndShow(EmojiSearchQuery), TimeSpan.FromSeconds(1));
            await LoadEmojiListAsync();

            PropertyChanged += AvatarCreatorViewModel_PropertyChanged;
        }

        private void AvatarCreatorViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(EmojiSearchQuery):
                    EmojiSearchAction.PrepareToExecute();
                    break;
                case nameof(EmojiCurrentSkinTone):
                    UpdateGroupedEmoji();
                    break;
            }
        }

        public void ApplyGradientPreset(GradientPreset preset) {
            GradientStartColor = preset.StartColor;
            GradientEndColor = preset.EndColor;
            GradientDirection = preset.Direction;
        }

        #region All about emoji

        static EmojiWebResponse EmojiData;
        DelayedAction EmojiSearchAction;
        static List<Emoji> EmojiWithSkinVariations = new List<Emoji>();

        private async Task LoadEmojiListAsync() {
            try {
                if (EmojiData == null) EmojiData = await Emoji.GetEmojisAsync();
                EmojiWithSkinVariations = EmojiData.Emoji.Where(e => e.SkinVariations != null).ToList();

                Progress<HttpProgress> progress = new Progress<HttpProgress>();
                progress.ProgressChanged += async (a, b) => {
                    if (!b.TotalBytesToReceive.HasValue) return;

                    var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
                    if (dispatcher.HasThreadAccess) {
                        await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                            IsIndeterminate = false;
                            LoadingProgress = 100 / (double)b.TotalBytesToReceive.Value * b.BytesReceived;
                        });
                    }
                };

                // Loading spritesheet
                bool isLoaded = await Emoji.LoadSpriteSheetAsync(progress);
                if (isLoaded) {
                    // Skin tones
                    List<EmojiSkinTone> skinTones = new List<EmojiSkinTone> {
                        new EmojiSkinTone(string.Empty, "default", Color.FromArgb(255, 255, 200, 61))
                    };
                    EmojiCurrentSkinTone = skinTones.First();

                    IEnumerable<Emoji> components = EmojiData.Emoji.Where(emoji => emoji.Category == "component");
                    foreach (Emoji component in components) {
                        string name = string.Empty;
                        Color color = Color.FromArgb(0, 0, 0, 0);

                        switch (component.Unified) {
                            case "1F3FB":
                                name = "light";
                                color = Color.FromArgb(255, 249, 224, 192);
                                break;
                            case "1F3FC":
                                name = "medium-light";
                                color = Color.FromArgb(255, 225, 183, 147);
                                break;
                            case "1F3FD":
                                name = "medium";
                                color = Color.FromArgb(255, 197, 149, 105);
                                break;
                            case "1F3FE":
                                name = "medium-dark";
                                color = Color.FromArgb(255, 157, 104, 65);
                                break;
                            case "1F3FF":
                                name = "dark";
                                color = Color.FromArgb(255, 86, 66, 55);
                                break;
                        }

                        var skinToneName = EmojiData.SkinTones.Where(st => st.Key == name).FirstOrDefault();
                        if (!string.IsNullOrEmpty(skinToneName.Message)) name = skinToneName.Message;
                        skinTones.Add(new EmojiSkinTone(component.Unified, name, color));
                    }
                    EmojiSkinTones = skinTones;
                    IsReady = true;
                    ShowGroupedEmojiList();
                    GenerateRandomEmojiAvatar();
                }
            } catch (Exception ex) {
                Functions.ShowHandledErrorDialog(ex);
                Toolkit.UWP.Controls.ModalsManager.CloseLastOpenedModal();
            }
        }

        private void ShowGroupedEmojiList() {
            var grouped = EmojiData.Emoji.OrderBy(emoji => emoji.SortOrder)
                //.Where(emoji => emoji.Category != "component")
                .GroupBy(emoji => emoji.Category, (key, items) => {
                    var localized = EmojiData.Groups.Where(g => g.Key == key).FirstOrDefault();
                    if (localized != null) key = localized.Message;
                    return new AvatarCreatorItemCollection(key, items);
                });
            GroupedEmojis = new ObservableCollection<AvatarCreatorItemCollection>(grouped);
        }

        private void UpdateGroupedEmoji() {
            if (GroupedEmojis == null) return;
            foreach (var group in GroupedEmojis) {
                for (int i = 0; i < group.Items.Count; i++) {
                    Emoji emoji = group.Items[i] as Emoji;
                    var emojiWS = EmojiWithSkinVariations.Where(e => emoji.SortOrder == e.SortOrder).FirstOrDefault();
                    if (emojiWS == null) continue;
                    if (!string.IsNullOrEmpty(EmojiCurrentSkinTone.Hex)) {
                        string shex = EmojiCurrentSkinTone.Hex;
                        if (emojiWS.SkinVariations.ContainsKey(shex)) {
                            group.Items[i] = emojiWS.SkinVariations[EmojiCurrentSkinTone.Hex];
                        } else {
                            foreach (var sve in emojiWS.SkinVariations) {
                                if (sve.Key == $"{shex}-{shex}") {
                                    group.Items[i] = sve.Value;
                                }
                            }
                        }
                    } else {
                        group.Items[i] = emojiWS;
                    }
                }
            }
        }

        private void SeachEmojiAndShow(string query) {
            if (string.IsNullOrEmpty(query)) {
                ShowGroupedEmojiList();
                if (!string.IsNullOrEmpty(EmojiCurrentSkinTone.Hex)) UpdateGroupedEmoji();
                return;
            }

            query = query.ToLower();

            List<Emoji> foundEmoji = new List<Emoji>();
            foreach (Emoji emoji in EmojiData.Emoji) {
                if (!string.IsNullOrEmpty(emoji.Name) && emoji.Name.ToLower().Contains(query)) {
                    foundEmoji.Add(TryGetEmojiWithSkin(emoji, EmojiCurrentSkinTone.Hex));
                    continue;
                } else if (emoji.ShortNames != null) {
                    var found = emoji.ShortNames.Where(s => s.Contains(query)).FirstOrDefault();
                    if (found != null) {
                        foundEmoji.Add(TryGetEmojiWithSkin(emoji, EmojiCurrentSkinTone.Hex));
                        continue;
                    }
                }
            }

            GroupedEmojis = new ObservableCollection<AvatarCreatorItemCollection>() {
                new AvatarCreatorItemCollection(Locale.Get("found"), foundEmoji)
            };
        }

        private Emoji TryGetEmojiWithSkin(Emoji emoji, string skinHex) {
            if (!string.IsNullOrEmpty(skinHex) && emoji.SkinVariations != null && emoji.SkinVariations.ContainsKey(skinHex))
                return emoji.SkinVariations[skinHex];
            return emoji;
        }

        #endregion

        #region All about stickers

        public async Task LoadStickersAsync() {
            if (IsStickerPacksLoading) return;
            IsStickerPacksLoading = true;
            object respgs = await Execute.GetRecentStickersAndGraffities();
            if (respgs is StickersFlyoutRecentItems rec) {
                var favoriteStickers = rec.FavoriteStickers.Select(s => new VKSticker(s));
                AvatarCreatorItemCollection favorites = new AvatarCreatorItemCollection(Locale.Get("favorites"), favoriteStickers);
                StickerPacks.Add(favorites);

                var recentStickers = rec.RecentStickers.Select(s => new VKSticker(s));
                AvatarCreatorItemCollection recent = new AvatarCreatorItemCollection(Locale.Get("recent"), recentStickers);
                StickerPacks.Add(recent);

                bool needRefresh = AppSession.CachedStickerPacks == null || AppSession.CachedStickerPacksTimestamp == null ||
                    DateTime.Now - AppSession.CachedStickerPacksTimestamp.Value > TimeSpan.FromMinutes(10);

                if (needRefresh) {
                    Log.Info("AvatarCreator: sticker packs are not cached or cache expired, loading from api...");
                    object resps = await Store.GetProducts("stickers", "active", true);
                    if (resps is VKList<StoreProduct> sitems) {
                        AddStickerPacks(sitems.Items);
                        AppSession.CachedStickerPacks = sitems.Items;
                        AppSession.CachedStickerPacksTimestamp = DateTime.Now;
                    } else {
                        Functions.ShowHandledErrorTip(resps);
                    }
                } else {
                    Log.Info("AvatarCreator: loading sticker packs from cache...");
                    AddStickerPacks(AppSession.CachedStickerPacks);
                }
                IsStickerPacksLoading = false;
            } else {
                IsStickerPacksLoading = false;
                Functions.ShowHandledErrorDialog(respgs, async () => await LoadStickersAsync());
            }
        }

        private void AddStickerPacks(List<StoreProduct> stickerPacks) {
            // stickerPacks.Sort(sp => sp.Id);
            foreach (var pack in stickerPacks) {
                if (!AppSession.ActiveStickerPacks.Contains(pack.Id))
                    AppSession.ActiveStickerPacks.Add(pack.Id);
                var stickers = pack.Stickers.Select(s => new VKSticker(s));
                StickerPacks.Add(new AvatarCreatorItemCollection(pack.Title, stickers, pack.Previews.Last().Uri));
            }
        }

        #endregion

        #region All about kaomoji

        public void InitKaomoji() {
            if (KaomojiList == null) KaomojiList = new ObservableCollection<AvatarCreatorItemCollection>(Kaomoji.PrebuildKaomojiList);
        }

        #endregion

        public void GenerateRandomEmojiAvatar() {
            int seed = (int)(DateTime.Now - new DateTime(2022, 05, 25)).TotalSeconds;

            int gradientPresetIndex = new Random(seed).Next(0, GradientPresets.Count - 1);
            ApplyGradientPreset(GradientPresets[gradientPresetIndex]);

            int emojiIndex = new Random(seed).Next(0, EmojiData.Emoji.Count - 1);
            SelectedEmoji = TryGetEmojiWithSkin(EmojiData.Emoji[emojiIndex], EmojiCurrentSkinTone.Hex);
        }

        public void GenerateRandomStickerAvatar() {
            int seed = (int)(DateTime.Now - new DateTime(2022, 05, 25)).TotalSeconds;

            int gradientPresetIndex = new Random(seed).Next(0, GradientPresets.Count - 1);
            ApplyGradientPreset(GradientPresets[gradientPresetIndex]);

            int spIndex = new Random(seed).Next(0, StickerPacks.Count - 1);
            AvatarCreatorItemCollection pack = StickerPacks[spIndex];

            int stickerIndex = new Random(seed).Next(0, pack.Items.Count - 1);
            SelectedSticker = (VKSticker)pack.Items[stickerIndex];
        }

        public Kaomoji GetRandomKaomoji() {
            int seed = (int)(DateTime.Now - new DateTime(2022, 05, 25)).TotalSeconds;

            int gradientPresetIndex = new Random(seed).Next(0, GradientPresets.Count - 1);
            ApplyGradientPreset(GradientPresets[gradientPresetIndex]);

            int kgIndex = new Random(seed).Next(0, KaomojiList.Count - 1);
            AvatarCreatorItemCollection group = KaomojiList[kgIndex];

            int kIndex = new Random(seed).Next(0, group.Items.Count - 1);
            return (Kaomoji)group.Items[kIndex];
        }
    }
}