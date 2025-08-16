using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Models.UI;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Elorucov.Laney.Services.UI {
    public class ChatThemeService {
        private static bool IsGradientSupported {
            get {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5) && !AppParameters.CTEForceSolidColor;
            }
        }

        #region Storage

        public const string ChatThemesCacheFolder = "ChatThemesCache";
        private const string ctfilename = "chat_styles.json";
        private static ChatStylesConfig _config = null;

        public static List<Appearance> Appearances { get { return _config?.Appearances; } }
        public static List<Background> Backgrounds { get { return _config?.Backgrounds; } }
        public static List<ChatStyle> PrebuiltStyles { get; private set; } = new List<ChatStyle>();

        public static async Task InitThemes() {
            await Task.Factory.StartNew(async () => {
                await GetThemesFromServer();
            }).ConfigureAwait(false);
        }

        public static async Task LoadLocalThemes() {
            try {
                Stopwatch sw = Stopwatch.StartNew();
                Log.Info($"ChatThemeService.LoadLocalThemes: checking cached themes...");
                string content = await GetCachedThemes();

                if (string.IsNullOrEmpty(content)) {
                    Log.Info($"ChatThemeService.LoadLocalThemes: loading preinstalled themes");
                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ChatBackgrounds/default.json"));
                    content = File.ReadAllText(file.Path);
                }
                ParseThemes(content);

                sw.Stop();
                Log.Info($"ChatThemeService.LoadLocalThemes: backgrounds = {_config.Backgrounds.Count}, appearances = {_config.Appearances.Count}, styles = {_config.Styles.Count}, loaded in {sw.ElapsedMilliseconds} ms.");
            } catch (Exception ex) {
                Log.Error($"ChatThemeService.LoadLocalThemes: cannot load local themes! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
        }

        private static async Task GetThemesFromServer() {
            Log.Info($"ChatThemeService.GetThemesFromServer: starting...");
            Stopwatch sw = Stopwatch.StartNew();

            try {
                HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
                filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
                HttpClient hc = new HttpClient(filter);
                var response = await hc.GetAsync(new Uri(AppParameters.ChatThemesListSource));
                string content = await response.Content.ReadAsStringAsync();
                ParseThemes(content);
                await SaveThemesToCache(content);
                Log.Info($"ChatThemeService.GetThemesFromServer: backgrounds = {_config.Backgrounds.Count}, appearances = {_config.Appearances.Count}, styles = {_config.Styles.Count}, loaded from {AppParameters.ChatThemesListSource} in {sw.ElapsedMilliseconds} ms.");
            } catch (Exception ex) {
                Log.Error($"ChatThemeService.GetThemesFromServer: failed to getting themes from server! 0x{ex.HResult.ToString("x8")}: {ex.Message}.");
            }
            sw.Stop();
        }

        private static void ParseThemes(string content) {
            try {
                _config = JsonConvert.DeserializeObject<ChatStylesConfig>(content);
                PrebuiltStyles = new List<ChatStyle>();

                foreach (var style in _config.Styles) {
                    Appearance appearance = _config.Appearances.Where(a => a.Id == style.AppearanceId).FirstOrDefault();
                    Background background = _config.Backgrounds.Where(b => b.Id == style.BackgroundId).FirstOrDefault();
                    string name = _config.Names[Locale.Get("lang")].Where(l => l.Id == style.Id).Select(l => l.Value).FirstOrDefault();
                    PrebuiltStyles.Add(new ChatStyle(style.Id, string.IsNullOrEmpty(name) ? style.Id : name, appearance, background));
                }
            } catch (Exception ex) {
                Log.Error($"ChatThemeService.ParseThemes: failed to parse themes json! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
        }

        private static async Task<string> GetCachedThemes() {
            try {
                StorageFolder ctf = await ApplicationData.Current.LocalFolder.CreateFolderAsync(ChatThemesCacheFolder, CreationCollisionOption.OpenIfExists);
                return File.ReadAllText($"{ctf.Path}\\{ctfilename}");
            } catch (FileNotFoundException) {
                Log.Warn($"ChatThemeService.GetCachedThemes: cached themes file is not found.");
            } catch (Exception ex) {
                Log.Error($"ChatThemeService.GetCachedThemes: cannot open cached themes file! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
            return null;
        }

        private static async Task SaveThemesToCache(string content) {
            try {
                StorageFolder ctf = await ApplicationData.Current.LocalFolder.CreateFolderAsync(ChatThemesCacheFolder, CreationCollisionOption.OpenIfExists);
                File.WriteAllText($"{ctf.Path}\\{ctfilename}", content);
            } catch (Exception ex) {
                Log.Error($"ChatThemeService.SaveThemesToCache: cannot save cached themes file! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
        }

        #endregion

        public static ChatStyle GetCurrentChatTheme(ViewModel.ConversationViewModel cvm = null) {
            if (ViewManagement.GetWindowType() != WindowType.Main) return null;
            if (AppParameters.CTEIgnoreChatTheme) return GetThemeBasedFromUserSettings();

            if (cvm == null) cvm = Pages.ConversationView.Current?.ViewModel;
            string styleId = cvm?.Style;
            if (string.IsNullOrEmpty(styleId) || styleId == "settings_demo") styleId = AppParameters.CTEStyle;

            if (string.IsNullOrEmpty(styleId) || styleId == "user") {
                return GetThemeBasedFromUserSettings();
            } else {
                if (PrebuiltStyles == null) return null;
                ChatStyle theme = PrebuiltStyles.Where(s => s.Id == styleId).FirstOrDefault();
                return theme != null ? theme : GetThemeBasedFromUserSettings();
            }
        }

        public static ChatStyle GetThemeBasedFromUserSettings() {
            if (!string.IsNullOrEmpty(AppParameters.CTEStyle)) {
                ChatStyle preinstalled = PrebuiltStyles.Where(t => t.Id == AppParameters.CTEStyle).FirstOrDefault();
                if (preinstalled != null) return preinstalled;
            }

            Appearance appearance = null;
            Background background = null;

            string bkgnd = AppParameters.ChatBackground;
            if (!string.IsNullOrEmpty(bkgnd)) {
                if (bkgnd.StartsWith($"ms-appdata:///Local/{ChatThemesCacheFolder}/")) {
                    var bs = new BackgroundSources {
                        Type = "raster",
                        Raster = new BackgroundSource {
                            Url = bkgnd,
                        }
                    };

                    background = new Background {
                        Id = "user",
                        Light = bs,
                        Dark = bs
                    };
                } else {
                    background = Backgrounds?.Where(b => b?.Id == bkgnd).FirstOrDefault();
                }
            }

            string color = AppParameters.CTEAccent;
            if (!string.IsNullOrEmpty(color)) {
                appearance = Appearances?.Where(a => a?.Id == color).FirstOrDefault();
            }

            if (appearance != null || background != null) {
                return new ChatStyle("user", Locale.Get("custom"), appearance, background);
            }
            return null;
        }

        public static object GetLocalizedStyleName(string style) {
            string name = _config.Names[Locale.Get("lang")].Where(l => l.Id == style).Select(l => l.Value).FirstOrDefault();
            return string.IsNullOrEmpty(name) ? style : name;
        }

        #region Message ui theme & gradient

        private static Dictionary<Shape, ScrollViewer> RegisteredShapes = new Dictionary<Shape, ScrollViewer>();
        private static List<FrameworkElement> RegisteredOutgoingMessageBubbles = new List<FrameworkElement>();
        private static List<ChatBackgroundControl> RegisteredBackgroundElements = new List<ChatBackgroundControl>();
        private static List<FrameworkElement> RegisteredChatRootElements = new List<FrameworkElement>();
        private static Dictionary<Hyperlink, bool> RegisteredLinks = new Dictionary<Hyperlink, bool>();
        private static Dictionary<Hyperlink, FrameworkElement> RegisteredLinksParents = new Dictionary<Hyperlink, FrameworkElement>();

        private static double oldScrollableHeight = -1;
        private static double oldScrollHostWidth = -1;
        private static double oldScrollHostHeight = -1;

        public static void RegisterForGradientBackground(Shape shape, ScrollViewer scroll) {
            if (!RegisteredShapes.ContainsKey(shape)) {
                RegisteredShapes.Add(shape, scroll);
                shape.SizeChanged += Shape_SizeChanged;
                scroll.SizeChanged += ScrollEvent;
                scroll.ViewChanging += ScrollEvent;
                shape.Unloaded += (a, b) => {
                    shape.SizeChanged -= Shape_SizeChanged;
                    scroll.SizeChanged -= ScrollEvent;
                    RegisteredShapes.Remove(shape);
                };
            }
        }

        public static void RegisterOutgoingBubbleUI(FrameworkElement element) {
            if (!RegisteredOutgoingMessageBubbles.Contains(element)) {
                RegisteredOutgoingMessageBubbles.Add(element);
                SetupOutgoingBubbles(element);
                element.Unloaded += (a, b) => RegisteredOutgoingMessageBubbles.Remove(element);
            }
        }

        public static void RegisterBackgroundElement(ChatBackgroundControl element) {
            if (!RegisteredBackgroundElements.Contains(element)) {
                RegisteredBackgroundElements.Add(element);
                SetupBackground(element);
                element.Unloaded += (a, b) => RegisteredBackgroundElements.Remove(element);
            }
        }

        public static void RegisterChatRootElement(FrameworkElement element) {
            if (!RegisteredChatRootElements.Contains(element)) {
                RegisteredChatRootElements.Add(element);
                SetThemeStyles(element);
                element.Unloaded += (a, b) => RegisteredChatRootElements.Remove(element);
            }
        }

        public static void RegisterLink(FrameworkElement parent, Hyperlink link, bool isForOutgoing) {
            if (!RegisteredLinks.ContainsKey(link)) {
                RegisteredLinks.Add(link, isForOutgoing);
                RegisteredLinksParents.Add(link, parent);
                SetupLink(link, isForOutgoing);
                parent.Unloaded += (a, b) => {
                    RegisteredLinks.Remove(link);
                    RegisteredLinksParents.Remove(link);
                };
            }
        }

        public static void UpdateTheme() {
            foreach (var oel in RegisteredOutgoingMessageBubbles) {
                SetupOutgoingBubbles(oel);
            }

            foreach (var rs in RegisteredShapes) {
                SetupGradientBackground(rs.Key, rs.Value);
            }

            foreach (var b in RegisteredBackgroundElements) {
                SetupBackground(b);
            }

            foreach (var l in RegisteredLinks) {
                SetupLink(l.Key, l.Value);
            }

            foreach (var r in RegisteredChatRootElements) {
                SetThemeStyles(r);
            }
        }

        private static void SetupOutgoingBubbles(FrameworkElement el) {
            try {
                bool isUserSettings = el.DataContext is LMessage msg && msg.PeerId == long.MaxValue;
                var theme = !isUserSettings ? GetCurrentChatTheme() : GetThemeBasedFromUserSettings();
                bool isDark = Theme.IsDarkTheme();

                if (el.Resources.ThemeDictionaries.ContainsKey("Default")) el.Resources.ThemeDictionaries.Remove("Default");
                el.RequestedTheme = ElementTheme.Default;
                var appearance = isDark ? theme?.Appearance?.Dark : theme?.Appearance?.Light;

                if (appearance != null && appearance?.BubbleGradient?.Colors?.Count > 0) {
                    // Don't apply dark theme for standart theme in light mode
                    if (theme.Appearance.Id == "mable" && !isDark) return;

                    ResourceDictionary resources = new ResourceDictionary() {
                        Source = new Uri("ms-appx:///Themes/CTE/CTEBrushesOutgoing.xaml")
                    };
                    el.Resources.ThemeDictionaries.Add("Default", resources);
                    el.RequestedTheme = ElementTheme.Dark;
                }
            } catch (Exception ex) {
                Log.Error($"ChatThemeService.SetupOutgoingBubbles failed!!! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
            }
        }

        private static void SetupBackground(ChatBackgroundControl element) {
            var theme = GetCurrentChatTheme(element.DataContext as ViewModel.ConversationViewModel);
            if (theme != null && theme.Background != null) {
                string old = element.ChatBackground != null ? element.ChatBackground.Id : string.Empty;
                element.ChatBackground = theme.Background;
            } else {
                element.ChatBackground = null;
            }
        }

        private static void SetupLink(Hyperlink link, bool isForOutgoing) {
            bool isDark = Theme.IsDarkTheme();

            FrameworkElement el = RegisteredLinksParents[link];
            bool isUserSettings = el.DataContext is LMessage msg && msg.PeerId == long.MaxValue;
            var theme = !isUserSettings ? GetCurrentChatTheme() : GetThemeBasedFromUserSettings();
            var appearance = isDark ? theme?.Appearance?.Dark : theme?.Appearance?.Light;
            if (theme?.Id != "mable" && appearance?.HeaderTint != null) {
                link.Foreground = new SolidColorBrush(isForOutgoing ? Color.FromArgb(255, 255, 255, 255) : ParseHex(appearance.AccentColor));
            } else {
                SolidColorBrush accent = (SolidColorBrush)Application.Current.Resources["AlternativeAccentBrush"];
                link.Foreground = accent;
            }
        }

        private static void Shape_SizeChanged(object sender, SizeChangedEventArgs e) {
            Shape shape = sender as Shape;
            ScrollViewer scroll = RegisteredShapes[shape];
            SetupGradientBackground(shape, scroll);
        }

        private static void ScrollEvent(object sender, object e) {
            ScrollViewer scroll = sender as ScrollViewer;
            if (oldScrollableHeight == scroll.ScrollableHeight &&
                oldScrollHostWidth == scroll.ActualWidth &&
                oldScrollHostHeight == scroll.ActualHeight) return;

            Debug.WriteLine($"CTE Gradient: Old SH: {oldScrollableHeight}; New SH: {scroll.ScrollableHeight}");
            var shapesInThisScroll = RegisteredShapes.Where(rs => rs.Value == scroll);
            foreach (var rs in shapesInThisScroll) {
                SetupGradientBackground(rs.Key, rs.Value);
            }
            oldScrollableHeight = scroll.ScrollableHeight;
            oldScrollHostWidth = scroll.ActualWidth;
            oldScrollHostHeight = scroll.ActualHeight;
        }

        private static void SetupGradientBackground(Shape shape, ScrollViewer scroll) {
            var theme = GetCurrentChatTheme(scroll.DataContext as ViewModel.ConversationViewModel);
            bool isDark = Theme.IsDarkTheme();
            var appearance = isDark ? theme?.Appearance?.Dark : theme?.Appearance?.Light;

            if (appearance == null || appearance?.BubbleGradient?.Colors == null || appearance?.BubbleGradient?.Colors?.Count == 0) {
                ElementCompositionPreview.SetElementChildVisual(shape, null);
                return;
            }

            CompositionPropertySet scrollProp = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scroll);

            GeneralTransform gt = shape.TransformToVisual(scroll);
            Point p = gt.TransformPoint(new Point(0, 0));
            float offsetx = (float)(p.X + scroll.HorizontalOffset);
            float offsety = (float)(p.Y + scroll.VerticalOffset);

            Compositor _compositor;
            SpriteVisual _maskVisual;
            CompositionSurfaceBrush _surfBrush;
            CompositionMaskBrush _maskBrush;
            CompositionBrush _brush = null;

            _compositor = Window.Current.Compositor;
            _surfBrush = (CompositionSurfaceBrush)shape.GetAlphaMask();
            _maskBrush = _compositor.CreateMaskBrush();
            _maskBrush.Mask = _surfBrush;

            if (IsGradientSupported) {
                var gb = _compositor.CreateLinearGradientBrush();
                ParseGradientColors(appearance.BubbleGradient, _compositor, gb);
                gb.StartPoint = new Vector2(0f, 0f);
                gb.EndPoint = new Vector2(1f, 1f);
                _brush = gb;
            } else {
                var cb = _compositor.CreateColorBrush(ParseHex(appearance.AccentColor));
                _brush = cb;
            }

            _maskBrush.Source = _brush;

            _maskVisual = _compositor.CreateSpriteVisual();
            _maskVisual.Brush = _maskBrush;
            _maskVisual.Size = new Vector2((float)shape.ActualWidth, (float)shape.ActualHeight);

            ElementCompositionPreview.SetElementChildVisual(shape, _maskVisual);

            if (IsGradientSupported) {
                float xdiff = 1f / _maskVisual.Size.X;
                float xdiffw = (float)scroll.ActualWidth * xdiff;
                float xdiffo = offsetx * xdiff;

                float ydiff = 1f / _maskVisual.Size.Y;
                float ydiffh = (float)scroll.ActualHeight * ydiff;
                float ydiffo = (float)offsety * ydiff;

                ExpressionAnimation expl = _compositor.CreateExpressionAnimation();
                expl.SetReferenceParameter("Scroll", scrollProp);
                expl.SetScalarParameter("diff", xdiff);
                expl.SetScalarParameter("diffw", xdiffw);
                expl.SetScalarParameter("diffo", xdiffo);
                expl.Expression = "Min((-Scroll.Translation.X * diff) - diffo, ((-Scroll.Translation.X * diff) - diffo) + (diffw + (444 * diff)))";

                ExpressionAnimation expr = _compositor.CreateExpressionAnimation();
                expr.SetReferenceParameter("Scroll", scrollProp);
                expr.SetScalarParameter("diff", xdiff);
                expr.SetScalarParameter("diffw", xdiffw);
                expr.SetScalarParameter("diffo", xdiffo);
                expr.Expression = "((-Scroll.Translation.X * diff) - diffo) + diffw";

                ExpressionAnimation expt = _compositor.CreateExpressionAnimation();
                expt.SetReferenceParameter("Scroll", scrollProp);
                expt.SetScalarParameter("diff", ydiff);
                expt.SetScalarParameter("diffo", ydiffo);
                expt.Expression = "(-Scroll.Translation.Y * diff) - diffo";

                ExpressionAnimation expb = _compositor.CreateExpressionAnimation();
                expb.SetReferenceParameter("Scroll", scrollProp);
                expb.SetScalarParameter("diff", ydiff);
                expb.SetScalarParameter("diffh", ydiffh);
                expb.SetScalarParameter("diffo", ydiffo);
                expb.Expression = "((-Scroll.Translation.Y * diff) - diffo) + diffh";

                _brush.StartAnimation("StartPoint.X", expl);
                _brush.StartAnimation("EndPoint.X", expr);
                _brush.StartAnimation("StartPoint.Y", expt);
                _brush.StartAnimation("EndPoint.Y", expb);
            }
        }

        private static void ParseGradientColors(Gradient g, Compositor compositor, CompositionLinearGradientBrush gradient) {
            float diff = 1f / (g.Colors.Count - 1);
            for (int i = 0; i < g.Colors.Count; i++) {
                float stop = i * diff;
                gradient.ColorStops.Add(compositor.CreateColorGradientStop(stop, ParseHex(g.Colors[i])));
            }
        }

        #endregion

        public static void SetThemeStyles(FrameworkElement root) {
            var theme = GetCurrentChatTheme(root.DataContext as ViewModel.ConversationViewModel);
            var resources = root.Resources.ThemeDictionaries;
            try {
                if (resources.ContainsKey("Default")) resources.Remove("Default");
                if (resources.ContainsKey("Light")) resources.Remove("Light");
            } catch (Exception ex) {
                Log.Error($"Some f**king error when clearing theme dictionaries on ChatThemeService.SetThemeStyles! 0x{ex.HResult.ToString("x8")}");
            }
            if (theme?.Appearance != null) {
                string laccent = theme.Appearance.Light.AccentColor;
                string daccent = theme.Appearance.Dark.AccentColor;
                string laltaccent = theme.Appearance.Light.AccentColor;
                string daltaccent = theme.Appearance.Dark.AccentColor;

                string AlternativeAccentDark = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""AlternativeAccentBrush"" Color=""{daltaccent}""/>";
                string AlternativeAccentLight = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""AlternativeAccentBrush"" Color=""{laltaccent}""/>";
                string AlternativeMessageControlForegroundAccentDark = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""AlternativeMessageControlForegroundAccentBrush"" Color=""{daltaccent}""/>";
                string AlternativeMessageControlForegroundAccentLight = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""AlternativeMessageControlForegroundAccentBrush"" Color=""{laltaccent}""/>";
                string SystemControlBackgroundAccentDark = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""SystemControlBackgroundAccentBrush"" Color=""{daccent}""/>";
                string SystemControlBackgroundAccentLight = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""SystemControlBackgroundAccentBrush"" Color=""{laccent}""/>";
                string SystemControlForegroundAccentDark = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""SystemControlForegroundAccentBrush"" Color=""{daccent}""/>";
                string SystemControlForegroundAccentLight = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""SystemControlForegroundAccentBrush"" Color=""{laccent}""/>";

                string ConvViewHeaderForegroundDark = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""ConvViewHeaderForegroundBrush"" Color=""{theme.Appearance.Dark.HeaderTint}""/>";
                string ConvViewHeaderForegroundLight = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""ConvViewHeaderForegroundBrush"" Color=""{theme.Appearance.Light.HeaderTint}""/>";
                string ConvViewHeaderTextForegroundLight = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""ConvViewHeaderTextForegroundBrush"" Color=""{theme.Appearance.Light.HeaderTint}""/>";
                string WriteBarIconDark = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""WriteBarIconBrush"" Color=""{theme.Appearance.Dark.WriteBarTint}""/>";
                string WriteBarIconLight = theme.Appearance.Id == "mable" ?
                    string.Empty : $@"<SolidColorBrush x:Key=""WriteBarIconBrush"" Color=""{theme.Appearance.Light.WriteBarTint}""/>";

                string BubbleIncoming = theme.Background == null ?
                    string.Empty : $@"<SolidColorBrush x:Key=""VKImBubbleIncomingBrush"" Color=""#FFF""/>";
                string TransparentLight = theme.Background == null ?
                    string.Empty : $@"<SolidColorBrush x:Key=""TransparentBubbleBrush"" Color=""#FFF""/>";
                string TransparentDark = theme.Background == null ?
                    string.Empty : $@"<SolidColorBrush x:Key=""TransparentBubbleBrush"" Color=""#2C2D2E""/>";

                string dxaml = $@"
<ResourceDictionary x:Key=""Default""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    {AlternativeAccentDark}
    {AlternativeMessageControlForegroundAccentDark}
    {SystemControlBackgroundAccentDark}
    {SystemControlForegroundAccentDark}
    {ConvViewHeaderForegroundDark}
    {WriteBarIconDark}
    {TransparentDark}
</ResourceDictionary>
";
                string lxaml = $@"
<ResourceDictionary x:Key=""Light""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    {AlternativeAccentLight}
    {AlternativeMessageControlForegroundAccentLight}
    {SystemControlBackgroundAccentLight}
    {SystemControlForegroundAccentLight}
    {ConvViewHeaderForegroundLight}
    {ConvViewHeaderTextForegroundLight}
    {WriteBarIconLight}
    {BubbleIncoming}
    {TransparentLight}
</ResourceDictionary>
";
                ResourceDictionary dark = XamlReader.Load(dxaml) as ResourceDictionary;
                ResourceDictionary light = XamlReader.Load(lxaml) as ResourceDictionary;
                resources.Add("Default", dark);
                resources.Add("Light", light);
            } else if (theme?.Background != null) {
                string dxaml = $@"
<ResourceDictionary x:Key=""Default""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <SolidColorBrush x:Key=""VKImBubbleIncomingBrush"" Color=""{{StaticResource VKGray800Color}}""/>
    <SolidColorBrush x:Key=""TransparentBubbleBrush"" Color=""#2C2D2E""/>
</ResourceDictionary>
";

                string lxaml = $@"
<ResourceDictionary x:Key=""Light""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <SolidColorBrush x:Key=""VKImBubbleIncomingBrush"" Color=""#FFF""/>
    <SolidColorBrush x:Key=""TransparentBubbleBrush"" Color=""#FFF""/>
</ResourceDictionary>
";
                ResourceDictionary dark = XamlReader.Load(dxaml) as ResourceDictionary;
                ResourceDictionary light = XamlReader.Load(lxaml) as ResourceDictionary;
                resources.Add("Default", dark);
                resources.Add("Light", light);
            }

            root.RequestedTheme = Application.Current.RequestedTheme == ApplicationTheme.Light ?
                ElementTheme.Dark : ElementTheme.Light;
            root.RequestedTheme = ElementTheme.Default;
        }

        public static Color ParseHex(string hex) {
            string aa, rs, gs, bs;

            if (hex.Length == 7) {
                aa = string.Empty;
                rs = hex.Substring(1, 2);
                gs = hex.Substring(3, 2);
                bs = hex.Substring(5, 2);
            } else if (hex.Length == 9) {
                aa = hex.Substring(1, 2);
                rs = hex.Substring(3, 2);
                gs = hex.Substring(5, 2);
                bs = hex.Substring(7, 2);
            } else {
                throw new ArgumentException("Hex-value is wrong!");
            }

            byte a = string.IsNullOrEmpty(aa) ? (byte)255 : Byte.Parse(aa, NumberStyles.AllowHexSpecifier);
            byte r = Byte.Parse(rs, NumberStyles.AllowHexSpecifier);
            byte g = Byte.Parse(gs, NumberStyles.AllowHexSpecifier);
            byte b = Byte.Parse(bs, NumberStyles.AllowHexSpecifier);

            return Color.FromArgb(a, r, g, b);
        }
    }
}