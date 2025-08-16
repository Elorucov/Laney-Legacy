using Elorucov.Laney.Models.AvatarCreator;
using Elorucov.Laney.Services.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace Elorucov.Laney.Models {
    public class LocalizedWords {
        [JsonProperty("key")]
        public string Key { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }
    }

    public class EmojiWebResponse {
        [JsonProperty("emoji")]
        public List<Emoji> Emoji { get; set; }

        [JsonProperty("groups")]
        public List<LocalizedWords> Groups { get; set; }

        [JsonProperty("skinTones")]
        public List<LocalizedWords> SkinTones { get; set; }
    }

    public class EmojiSkinTone {
        public string Hex { get; private set; }
        public string Name { get; private set; }
        public Color Color { get; private set; }

        public EmojiSkinTone(string hex, string name, Color color) {
            Hex = hex;
            Name = name;
            Color = color;
        }
    }

    public class Emoji : IAvatarCreatorItem {

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("unified")]
        public string Unified { get; private set; }

        [JsonProperty("non_qualified")]
        public string NonQualified { get; private set; }

        [JsonProperty("sheet_x")]
        public uint SheetX { get; private set; }

        [JsonProperty("sheet_y")]
        public uint SheetY { get; private set; }

        [JsonProperty("short_names")]
        public List<string> ShortNames { get; set; }

        [JsonProperty("texts")]
        public List<string> Texts { get; private set; }

        [JsonProperty("sort_order")]
        public int SortOrder { get; private set; }

        [JsonProperty("skin_variations")]
        public Dictionary<string, Emoji> SkinVariations { get; private set; }

        #region Static

        public static readonly string EmojiDataEndpoint = $"https://elorucov.github.io/laney/v1/emoji";
        const string EmojiImagesFolder = "images";
        const string EmojisSpriteSheet = "spritesheet.png";
        static HttpClient hc = new HttpClient();

        public static async Task<EmojiWebResponse> GetEmojisAsync() {
            string lang = Locale.Get("lang");
            var response = await hc.GetAsync(new Uri($"{EmojiDataEndpoint}/emoji_{lang}.json"));
            string emojisJson = await response.Content.ReadAsStringAsync();
            EmojiWebResponse ewr = JsonConvert.DeserializeObject<EmojiWebResponse>(emojisJson);
            foreach (Emoji emoji in ewr.Emoji) {
                if (emoji.SkinVariations == null) continue;
                foreach (var variation in emoji.SkinVariations) {
                    variation.Value.Name = emoji.Name;
                    variation.Value.ShortNames = emoji.ShortNames;
                    variation.Value.Texts = emoji.Texts;
                    variation.Value.Category = emoji.Category;
                    variation.Value.SortOrder = emoji.SortOrder;
                }
            }
            return ewr;
        }

        public static SoftwareBitmap SpriteSheet { get; private set; }
        const uint SheetSize = 64; // emoji size in sprite sheet

        public static async Task<bool> LoadSpriteSheetAsync(Progress<HttpProgress> progress) {
            if (SpriteSheet != null) return true;
            HttpRequestMessage hrm = new HttpRequestMessage();
            hrm.Method = HttpMethod.Get;
            hrm.RequestUri = new Uri($"{EmojiDataEndpoint}/{EmojisSpriteSheet}");

            HttpResponseMessage response = await hc.SendRequestAsync(hrm, HttpCompletionOption.ResponseContentRead).AsTask(progress);
            var buffer = await response.Content.ReadAsBufferAsync();

            using (IRandomAccessStream randomAccessStream = buffer.AsStream().AsRandomAccessStream()) {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                SoftwareBitmap raw = await decoder.GetSoftwareBitmapAsync();
                SpriteSheet = SoftwareBitmap.Convert(raw, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }
            return true;
        }

        #endregion

        private static async Task<SoftwareBitmap> GetCroppedBitmapAsync(SoftwareBitmap softwareBitmap,
            uint startPointX, uint startPointY, uint width, uint height) {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream()) {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                encoder.BitmapTransform.Bounds = new BitmapBounds() {
                    X = startPointX,
                    Y = startPointY,
                    Height = height,
                    Width = width
                };
                await encoder.FlushAsync();

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                return await decoder.GetSoftwareBitmapAsync(softwareBitmap.BitmapPixelFormat, softwareBitmap.BitmapAlphaMode);
            }
        }

        private async Task SetSourceFromSpriteSheet(Image image) {
            uint posX = (SheetX * (SheetSize + 2)) + 1;
            uint posY = (SheetY * (SheetSize + 2)) + 1;

            SoftwareBitmap bitmap = await GetCroppedBitmapAsync(SpriteSheet, posX, posY, SheetSize, SheetSize);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);
            image.Source = source;
        }

        public async Task<FrameworkElement> RenderAsync(RenderMode mode, bool extraFlag) {
            Image image = new Image();
            if (mode == RenderMode.InCanvas) {
                image.Width = 160;
                image.Width = 160;
                image.Source = new BitmapImage(new Uri($"{EmojiDataEndpoint}/{EmojiImagesFolder}/{Unified.ToLower()}.png"));
                Canvas.SetLeft(image, 80);
                Canvas.SetTop(image, 80);
            } else {
                image.Width = 32;
                image.Width = 32;
                await SetSourceFromSpriteSheet(image);
                if (Name != null && ShortNames != null)
                    ToolTipService.SetToolTip(image, $"{Name}\n{String.Join(", ", ShortNames)}\n{Unified}");
            }
            return image;
        }
    }
}