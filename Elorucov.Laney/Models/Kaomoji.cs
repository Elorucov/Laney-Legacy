using Elorucov.Laney.Models.AvatarCreator;
using Elorucov.Laney.Services.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Models {
    public class Kaomoji : IAvatarCreatorItem {
        public static List<AvatarCreatorItemCollection> PrebuildKaomojiList { get { return GetPrebuiltKaomojiList(); } }

        public string Text { get; private set; }

        private Kaomoji(string kaomoji) {
            Text = kaomoji;
        }

        private static List<AvatarCreatorItemCollection> GetPrebuiltKaomojiList() {
            List<string> ascii = new List<string> {
                ";)", "^_~", ";-)", ":)", "^_^", "^_____^", ":-)", ":D", "^0^", ":-D", ":P", ":-P", ";P", ":(", ":-(", "U_U",
                ":[", ">:(", ">\"<", "):", ":-O", "O.O", "OwO", "UwU", ":-()", "~_~", "^o^", ":-S", "<3", "^3^", ":-x", ":/",
                "X_X", "=/", ";(", "T_T", ";[", "+_+", "O_O", ":O", "¬_¬", ";_;", "=.=", ";]", "^_+", ";O)", ":-]", "^.^",
                "=)", ":O)", ":-3", "=D", ":|", "-.-", ">_<", ":O(", "*_*", "=[", "8-)", "^^;", ":-*", "B-)", "=_=", "-0-",
                ":S", "$_$", ":-$", ">.<", "-_-", "> <", ":]", "^^", "=]", ";D", "^_-", "=P", ":'(", "Y.Y", "=("
            };
            List<string> happy = new List<string> {
                "ヾ(≧▽≦*)o", "φ(*￣0￣)", "q(≧▽≦q)", "ψ(｀∇´)ψ", "（￣︶￣）↗　", "*^____^*", "(～￣▽￣)～", "( •̀ ω •́ )✧", "[]~(￣▽￣)~*", "φ(゜▽゜*)♪", "o(*^＠^*)o", "O(∩_∩)O",
                "(✿◡‿◡)", "`(*>﹏<*)′", "(*^▽^*)", "（*＾-＾*", "）(*^_^*)", "(❁´◡`❁)", "(≧∇≦)ﾉ", "(´▽`ʃ♡ƪ)", "(●ˇ∀ˇ●)", "○( ＾皿＾)っ", "(￣y▽￣)╭", "\\^o^/",
                "(‾◡◝)", "╰(*°▽°*)╯", "o(*^▽^*)┛", "o(*￣▽￣*)ブ", "(^_-)db(-_^)", "o(*￣▽￣*)ブ", "♪(^∇^*)", "(≧∀≦)ゞ", "o(*￣︶￣*)o", "--<-<-<@", "(oﾟvﾟ)ノ",
                "o(*≧▽≦)ツ┏━┓", "(/≧▽≦)/", "( $ _ $ )", "(☆▽☆)", "ヾ(＠⌒ー⌒＠)ノ", "ㄟ(≧◇≦)ㄏ", "o((>ω< ))o", "( *︾▽︾)", "ヾ(≧ ▽ ≦)ゝ",
                "♪(´▽｀)", "(^///^)", "(p≧w≦q)", "o(*￣▽￣*)o", "( •̀ ω •́ )y", "(o゜▽゜)o☆", "ƪ(˘⌣˘)ʃ"
            };
            List<string> greeting = new List<string> {
                "ヾ(•ω•`)o", "\\(￣︶￣*\\))", "(* ￣3)(ε￣ *)", "－O－", "(*￣3￣)╭", "( ´･･)ﾉ(._.`)", "(｡･∀･)ﾉﾞ", "o(*￣▽￣*)ブ", "(_　_)。゜zｚＺ", "(ToT)/~~~",
                "(∪.∪ )...zzz", "!(*￣(￣　*)", "(￣o￣) . z Z", "(づ￣ 3￣)づ", "（＾∀＾●）ﾉｼ", "（づ￣3￣）づ╭❤️～", "\\(@^0^@)/", "ヾ(^▽^*)))", "(～﹃～)~zZ",
                "☆⌒(*＾-゜)v", "(￣o￣) . z Z", "(*￣;(￣ *)", "||ヽ(*￣▽￣*)ノミ|Ю", "☆⌒(*＾-゜)v", "(＾Ｕ＾)ノ~ＹＯ", "o(*°▽°*)o", "ヾ(￣▽￣) Bye~Bye~",
                "( ﾟдﾟ)つ Bye", "(๑•̀ㅂ•́)و✧", "(o゜▽゜)o☆", "(ﾉ◕ヮ◕)ﾉ*:･ﾟ✧", "(∩^o^)⊃━☆", "✪ ω ✪", "＜（＾－＾）＞", "o(*￣▽￣*)o",
                "o(￣▽￣)ｄ", "(╹ڡ╹ )", "(u‿ฺu✿ฺ)", "♪(´▽｀)", "(╯▽╰ )", "ヽ(✿ﾟ▽ﾟ)ノ", "( •̀ .̫ •́ )✧", "(^^ゞ", "(＠＾０＾)", "（。＾▽＾）", "Ψ(￣∀￣)Ψ",
                "o(≧∀≦)o", "(。・∀・)ノ", "~\\(≧▽≦)/~", "b(￣▽￣)d", "o(^▽^)o", "(👉ﾟヮﾟ)👉", "👈(ﾟヮﾟ👈)", "👈(⌒▽⌒)👉",
                "(¬‿¬)", "(•_•)", "( •_•)>⌐■-■", "(⌐■_■)", "ヾ(⌐■_■)ノ♪", "(▀̿Ĺ̯▀̿ ̿)", "＼(ﾟｰﾟ＼)", "( ﾉ ﾟｰﾟ)ﾉ"
            };
            List<string> cute = new List<string> {
                "(￣y▽,￣)╭", " (o|o) ", "(^人^)", "§(*￣▽￣*)§", "ψ(._. )>", "(/▽＼)", "(o′┏▽┓｀o) ", "◑﹏◐", "(○｀ 3′○)",
                "(ಥ _ ಥ)", "(⓿_⓿)", "(❤️´艸｀❤️)", "(ง •_•)ง", "ผ(•̀_•́ผ)", "（〃｀ 3′〃）", "(●'◡'●)", "(. ❛ ᴗ ❛.)", "ლ(╹◡╹ლ)", "o(〃＾▽＾〃)o", "(。・ω・。)",
                "(>'-'<)", "(✿◠‿◠)", "(ﾉ*ФωФ)ﾉ", "ˋ( ° ▽、° )", " (*/ω＼*)", "=￣ω￣=", "(✿◕‿◕✿)", "✍️(◔◡◔)", "(★‿★)", "╰(￣ω￣ｏ)", "~(￣▽￣)~*",
                "〜(￣▽￣〜)", "(〜￣▽￣)〜", "(～o￣3￣)～", "(っ´Ι`)っ", "ԅ(¯﹃¯ԅ)", "(￣﹃￣)", "༼ つ ◕_◕ ༽つ", "(^///^)", "(o゜▽゜)o☆",
                "( ‵▽′)ψ", "( ﹁ ﹁ ) ~→", "(❤️ ω ❤️)", "(★ ω ★)", "*(੭*ˊᵕˋ)੭*ଘ", "┏ (゜ω゜)=👉", "U•ェ•*U", "(°°)～", "ᓚᘏᗢ", "~(=^‥^)ノ", "/ᐠ｡ꞈ｡ᐟ\\",
                "(ʘ ʖ̯ ʘ)", "(ʘ ͟ʖ ʘ)", "(ʘ ͜ʖ ʘ)", "( ͡• ͜ʖ ͡• )", "( ͠° ͟ʖ ͡°)", "( ͡° ͜ʖ ͡°)", "( ͡~ ͜ʖ ͡°)", "( ͡ಠ ʖ̯ ͡ಠ)", "( ఠ ͟ʖ ఠ)", "¯\\_( ͡° ͜ʖ ͡°)_/¯", "¯\\_(ツ)_/¯"
            };
            List<string> sad = new List<string> {
                "/_ \\", "＞﹏＜", "(っ °Д °;)っ", "(ノへ￣、)", ".·´¯`(>▂<)´¯`·. ", "(;´༎ຶД༎ຶ`)", "/(ㄒoㄒ)/~~", "╯︿╰",
                "::>_<::", "〒▽〒", "(≧﹏ ≦)", "┗( T﹏T )┛", "(。﹏。*)", "X﹏X"
            };
            List<string> angry = new List<string> {
                "o(≧口≦)o", "￣へ￣", "(* ￣︿￣)", "╰(艹皿艹 )", "(°ロ°)", "(ﾟДﾟ*)ﾉ", "(ノ｀Д)ノ", "～(　TロT)σ", "(〃＞目＜)", "(╯▔皿▔)╯",
                "(ㆆ_ㆆ)", "┌( ಠ_ಠ)┘", "(╯°□°）╯︵ ┻━┻", "(╯‵□′)╯︵┻━┻", "ಠ▃ಠ", "(>ლ)", "눈_눈", "(¬_¬ )", "(¬_¬\")"
            };
            // остальные будут потом

            return new List<AvatarCreatorItemCollection> {
                new AvatarCreatorItemCollection(Locale.Get("kaomoji_group_ascii"), ascii.Select(s => new Kaomoji(s))),
                new AvatarCreatorItemCollection(Locale.Get("kaomoji_group_happy"), happy.Select(s => new Kaomoji(s))),
                new AvatarCreatorItemCollection(Locale.Get("kaomoji_group_greeting"), greeting.Select(s => new Kaomoji(s))),
                new AvatarCreatorItemCollection(Locale.Get("kaomoji_group_cute"), cute.Select(s => new Kaomoji(s))),
                new AvatarCreatorItemCollection(Locale.Get("kaomoji_group_sad"), sad.Select(s => new Kaomoji(s))),
                new AvatarCreatorItemCollection(Locale.Get("kaomoji_group_angry"), angry.Select(s => new Kaomoji(s))),
            };
        }

        public async Task<FrameworkElement> RenderAsync(RenderMode mode, bool extraFlag) {
            await Task.Yield(); // because interface requires async and "return Task.Run(...)" can crash the app!
            return new Border {
                Width = 320,
                Height = 320,
                Child = new TextBlock {
                    Text = Text,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 32,
                    LineHeight = 32,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                }
            };
        }
    }
}
