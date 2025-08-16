using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services.UI {
    enum MatchType { User, Group, LinkInText, Mail, Url, NewYear, MentionAboutFeedOrPost }

    class MatchInfo {
        public int Start { get; private set; }
        public int Length { get; private set; }
        public MatchType Type { get; private set; }
        public Match Match { get; private set; }

        public MatchInfo(int start, int length, MatchType type, Match match) {
            Start = start;
            Length = length;
            Type = type;
            Match = match;
        }
    }

    public enum TextChunkType : byte { Plain = 1, Bold = 2, Italic = 4, Underline = 8, Link = 16 }

    public class TextChunk {
        public string Text { get; private set; }
        public TextChunkType Type { get; private set; }
        public string Url { get; private set; }
        public bool IsInlineLink { get; private set; }

        public TextChunk(string text, TextChunkType type = TextChunkType.Plain, string url = null, bool isInlineLink = false) {
            Text = text;
            Type = type;
            Url = url;
            IsInlineLink = isInlineLink;

            string[] spec = new string[] { "ny", "post" };
            if (!string.IsNullOrEmpty(url) && !spec.Contains(url) && !url.StartsWith("https://") && !url.StartsWith("http://")) Url = "https://" + url;
            if (spec.Contains(url)) IsInlineLink = true;
        }
    }

    public class TextParsingResult {
        public string PlainText { get; set; }
        public List<TextChunk> Chunks { get; set; }
    }

    public class VKTextParser {
        static Regex urlRegex = new Regex(@"(?:(?:http|https):\/\/)?([a-z0-9.\-]*\.)?([-a-zA-Z0-9а-яА-Я]{1,256})\.([-a-zA-Z0-9а-яА-Я]{2,8})\b(?:\/[-a-zA-Z0-9а-яА-Я@:%_\+.~#?!&\/=]*)?", RegexOptions.Compiled);
        static Regex mailRegex = new Regex(@"([\w\d.]+)@([a-zA-Z0-9а-яА-Я.]{2,256}\.[a-zа-я]{2,8})", RegexOptions.Compiled);
        static Regex userRegex = new Regex(@"\[(id)(\d+)\|(.*?)\]", RegexOptions.Compiled);
        static Regex groupRegex = new Regex(@"\[(club|public|event)(\d+)\|(.*?)\]", RegexOptions.Compiled);
        static Regex linkInTextRegex = new Regex(@"\[((?:(?:http|https):\/\/)?(vk.com|vk.me|vk.ru)\/[\w\d\W.]*?)\|((.*?)+?)\]", RegexOptions.Compiled);
        static Regex newYearRegex = new Regex(@"(Н|н)ов(ый|ым|ому|ого|ыми|ом)? год(ом|ами|у|а)?|с нг|(N|n)ew year", RegexOptions.Compiled);

        #region Internal parsing methods

        private static Tuple<string, string> ParseBracketWord(Match match) {
            return new Tuple<string, string>($"https://vk.com/{match.Groups[1]}{match.Groups[2]}", match.Groups[3].Value);
        }

        private static Tuple<string, string> ParseLinkInBracketWord(Match match) {
            return new Tuple<string, string>(match.Groups[1].Value, match.Groups[3].Value);
        }

        private static List<Tuple<string, string>> GetRaw(string plain, bool dontParseUrls = false) {
            plain = plain.Trim();
            List<Tuple<string, string>> raw = new List<Tuple<string, string>>();
            List<MatchInfo> allMatches = new List<MatchInfo>();

            userRegex.Matches(plain).Cast<Match>().ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.User, m)));
            groupRegex.Matches(plain).Cast<Match>().ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.Group, m)));
            linkInTextRegex.Matches(plain).Cast<Match>().ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.LinkInText, m)));
            if (!dontParseUrls) mailRegex.Matches(plain).Cast<Match>().ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.Mail, m)));
            if (!dontParseUrls) urlRegex.Matches(plain).Cast<Match>().ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.Url, m)));
            if (Functions.IsHoliday) newYearRegex.Matches(plain).Cast<Match>().ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.NewYear, m)));

            allMatches = allMatches.OrderBy(m => m.Start).ToList();

            string word = string.Empty;
            for (int i = 0; i < plain.Length; i++) {
                var matchInfo = allMatches.Where(m => m.Start == i).FirstOrDefault();
                if (matchInfo != null) {
                    raw.Add(new Tuple<string, string>(null, word));
                    word = string.Empty;

                    Match match = matchInfo.Match;
                    switch (matchInfo.Type) {
                        case MatchType.User:
                        case MatchType.Group: raw.Add(ParseBracketWord(match)); break;
                        case MatchType.LinkInText: raw.Add(ParseLinkInBracketWord(match)); break;
                        case MatchType.Mail: raw.Add(new Tuple<string, string>($"mailto:{match}", match.Value)); break;
                        case MatchType.Url:
                            string url = match.Value;
                            if (!url.StartsWith("https://") && !url.StartsWith("http://")) url = $"https://{url}";
                            raw.Add(new Tuple<string, string>(url, match.Value));
                            break;
                        case MatchType.NewYear:
                            raw.Add(new Tuple<string, string>("ny", match.Value));
                            break;
                        case MatchType.MentionAboutFeedOrPost:
                            raw.Add(new Tuple<string, string>("post", plain.Substring(match.Index, match.Length)));
                            break;
                    }

                    i = i + matchInfo.Length - 1;
                } else {
                    word += plain[i];
                }
            }
            raw.Add(new Tuple<string, string>(null, word));

            return raw;
        }

        #endregion

        public static TextParsingResult ParseText(string plain, MessageFormatData formatData = null) {
            TextParsingResult result = new TextParsingResult();
            MessageFormatData fdata = formatData ?? new MessageFormatData();
            if (fdata.Items == null) fdata.Items = new List<MessageFormatDataItem>();

            // Parse inline links to add it to FormatData object.
            var raw = GetRaw(plain);
            StringBuilder sb = new StringBuilder();
            foreach (var rawData in raw) {
                if (!string.IsNullOrEmpty(rawData.Item1)) {
                    fdata.Items.Add(new MessageFormatDataItem {
                        Type = MessageFormatDataTypes.LINK,
                        IsInline = true,
                        Url = rawData.Item1,
                        Offset = sb.Length,
                        Length = rawData.Item2.Length
                    });
                }
                sb.Append(rawData.Item2);
            }
            result.PlainText = sb.ToString();

            // Create chunks
            result.Chunks = new List<TextChunk>();
            StringBuilder chunkSB = new StringBuilder();
            TextChunkType tcType = TextChunkType.Plain;
            string url = null;
            bool isInline = false;
            if (fdata.Items.Count > 0) {
                for (int i = 0; i < result.PlainText.Length; i++) {
                    var intersects = fdata.Items.Where(fdi => fdi.Offset <= i && fdi.Offset + fdi.Length > i);
                    if (intersects.Count() == 0) { // если буква не имеет никаких стилей или ссылок
                        if (tcType != TextChunkType.Plain) {
                            result.Chunks.Add(new TextChunk(chunkSB.ToString(), tcType, url, isInline));
                            tcType = TextChunkType.Plain;
                            chunkSB.Clear();
                        }
                        chunkSB.Append(result.PlainText[i]);
                    } else { // если имеет стили и ссылки
                        TextChunkType tcType2 = TextChunkType.Plain;
                        string url2 = null;
                        bool isInline2 = false;
                        foreach (var fdi in intersects) {
                            switch (fdi.Type) {
                                case MessageFormatDataTypes.BOLD:
                                    tcType2 = tcType2 | TextChunkType.Bold;
                                    break;
                                case MessageFormatDataTypes.ITALIC:
                                    tcType2 = tcType2 | TextChunkType.Italic;
                                    break;
                                case MessageFormatDataTypes.UNDERLINE:
                                    tcType2 = tcType2 | TextChunkType.Underline;
                                    break;
                                case MessageFormatDataTypes.LINK:
                                    tcType2 = tcType2 | TextChunkType.Link;
                                    url2 = fdi.Url;
                                    isInline2 = fdi.IsInline;
                                    break;
                            }
                        }
                        if (tcType2 != tcType || url != url2 || isInline != isInline2) {
                            result.Chunks.Add(new TextChunk(chunkSB.ToString(), tcType, url, isInline));
                            tcType = tcType2;
                            url = url2;
                            isInline = isInline2;
                            chunkSB.Clear();
                        }
                        chunkSB.Append(result.PlainText[i]);
                    }
                }
                result.Chunks.Add(new TextChunk(chunkSB.ToString(), tcType, url, isInline));
                chunkSB.Clear();
            }

            return result;
        }

        public static List<Inline> GetInlines(TextParsingResult result, Action<string, bool> linkClickedCallback = null) {
            List<Inline> inlines = new List<Inline>();
            if (result.Chunks.Count > 0) {
                foreach (var chunk in result.Chunks) {
                    if (chunk.Type.HasFlag(TextChunkType.Link)) { // In original, we use Uri.IsWellFormedUriString, but in Laney we have a custom schemes for easter eggs.
                        Hyperlink hl = new Hyperlink();
                        hl.Click += (a, b) => linkClickedCallback?.Invoke(chunk.Url, chunk.IsInlineLink);
                        hl.Inlines.Add(new Run { Text = chunk.Text });
                        if (chunk.Type.HasFlag(TextChunkType.Bold)) hl.FontWeight = FontWeights.SemiBold;
                        if (chunk.Type.HasFlag(TextChunkType.Italic)) hl.FontStyle = FontStyle.Italic;
                        if (chunk.Type.HasFlag(TextChunkType.Underline)) hl.TextDecorations = TextDecorations.Underline;
                        inlines.Add(hl);
                    } else {
                        Run run = new Run { Text = chunk.Text };
                        if (chunk.Type.HasFlag(TextChunkType.Bold)) run.FontWeight = FontWeights.SemiBold;
                        if (chunk.Type.HasFlag(TextChunkType.Italic)) run.FontStyle = FontStyle.Italic;
                        if (chunk.Type.HasFlag(TextChunkType.Underline)) run.TextDecorations = TextDecorations.Underline;
                        inlines.Add(run);
                    }
                }
            } else {
                return new List<Inline> { new Run { Text = result.PlainText } };
            }
            return inlines;
        }

        public static string GetOnlyParsedText(string plain) {
            var raw = GetRaw(plain);
            StringBuilder sb = new StringBuilder();
            foreach (var chunk in raw) sb.Append(chunk.Item2);
            return sb.ToString();
        }

        #region For RichTextBlock (without format_data)

        public static readonly Color SpecialColor = Color.FromArgb(255, 224, 0, 224);
        private static Hyperlink BuildHyperlinkForRTBStyle(string text, string link, RichTextBlock rtb, Action<string> clickedCallback) {
            string hlxaml = "<Hyperlink xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Foreground=\"{ThemeResource SystemControlForegroundAccentBrush}\"/>";
            Hyperlink h = (Hyperlink)XamlReader.Load(hlxaml);
            if (link == "ny" || link == "post") {
                h.Foreground = new SolidColorBrush(SpecialColor);
            }
            h.Inlines.Add(new Run { Text = text });
            h.Click += (a, b) => { clickedCallback?.Invoke(link); };
            return h;
        }

        public static void SetText(string plain, RichTextBlock rtb, Action<string> linksClickedCallback = null) {
            Paragraph p = new Paragraph();

            foreach (var token in GetRaw(plain)) {
                if (string.IsNullOrEmpty(token.Item1)) {
                    p.Inlines.Add(new Run {
                        Text = token.Item2
                    });
                } else {
                    Hyperlink h = BuildHyperlinkForRTBStyle(token.Item2, token.Item1, rtb, linksClickedCallback);
                    p.Inlines.Add(h);
                }
            }

            rtb.Blocks.Clear();
            rtb.Blocks.Add(p);
        }

        #endregion

        public static long GetMentionId(string plain) {
            var u = userRegex.Match(plain);
            if (u.Success) {
                return long.Parse(u.Groups[2].Value);
            } else {
                var g = groupRegex.Match(plain);
                if (g.Success) return -long.Parse(g.Groups[2].Value);
            }
            return 0;
        }
    }
}