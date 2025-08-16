using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Text;

namespace Elorucov.Laney.Services.UI {
    public class TextFormatConverter {
        struct FormatInfo {
            public string Type;
            public int Offset;
            public int Length;
            public string Url;
        }

        public static void TryClearFormatting(ITextDocument document) {
            var range = document.GetRange(0, 0);
            range.CharacterFormat = document.GetDefaultCharacterFormat();
            range.ParagraphFormat = document.GetDefaultParagraphFormat();
            range.CharacterFormat.Bold = FormatEffect.Off;
            range.CharacterFormat.Italic = FormatEffect.Off;
            range.CharacterFormat.Underline = UnderlineType.None;
        }

        // From VK's MessageFormat to Windows' format
        public static void FromVKFormat(ITextDocument document, MessageFormatData format) {
            document.BatchDisplayUpdates();
            document.GetText(TextGetOptions.AdjustCrlf | TextGetOptions.NoHidden, out string text);
            document.Selection.CharacterFormat = document.GetDefaultCharacterFormat();
            document.SetText(TextSetOptions.None, string.Empty);
            TryClearFormatting(document);
            document.SetText(TextSetOptions.None | TextSetOptions.ApplyRtfDocumentDefaults, text);
            if (format == null) {
                document.ApplyDisplayUpdates();
                return;
            }
            var textLength = text.Length;
            foreach (var item in format.Items) {
                SetFormatByItem(document, item, textLength);
            }
            document.ApplyDisplayUpdates();
        }

        private static void SetFormatByItem(ITextDocument document, MessageFormatDataItem item, int maxLength) {
            int start = item.Offset;
            int end = Math.Min(maxLength, start + item.Length);

            var range = document.GetRange(start, end);
            var format = range.CharacterFormat;
            var u = format.Underline;

            switch (item.Type) {
                case MessageFormatDataTypes.BOLD:
                    format.Bold = FormatEffect.On;
                    break;
                case MessageFormatDataTypes.ITALIC:
                    format.Italic = FormatEffect.On;
                    break;
                case MessageFormatDataTypes.UNDERLINE:
                    format.Underline = UnderlineType.Single;
                    break;
                case MessageFormatDataTypes.LINK:
                    range.Link = $"\"{item.Url}\"";
                    format.ForegroundColor = Color.FromArgb(255, 0, 122, 204);
                    format.Underline = UnderlineType.Dash;
                    break;
            }
        }

        // From Windows' format to VK's MessageFormat
        public static MessageFormatData ToVKFormat(ITextDocument document) {
            MessageFormatData data = new MessageFormatData {
                Version = 1,
                Items = new List<MessageFormatDataItem>()
            };
            data.Items.Clear();

            document.GetText(TextGetOptions.AdjustCrlf, out string text);
            int length = text.Length;
            int hidden = 0;

            FormatInfo bold = new FormatInfo { Type = MessageFormatDataTypes.BOLD, Offset = 0, Length = 0 };
            FormatInfo italic = new FormatInfo { Type = MessageFormatDataTypes.ITALIC, Offset = 0, Length = 0 };
            FormatInfo underline = new FormatInfo { Type = MessageFormatDataTypes.UNDERLINE, Offset = 0, Length = 0 };
            FormatInfo link = new FormatInfo { Type = MessageFormatDataTypes.LINK, Offset = 0, Length = 0 };

            for (int i = 0; i < length; i++) {
                var range = document.GetRange(i, i + 1);
                if (range.CharacterFormat.Hidden == FormatEffect.On) {
                    range.Expand(TextRangeUnit.Hidden);
                    hidden += range.Length;
                    i += range.Length - 1;
                    continue;
                }

                var format = range.CharacterFormat;
                bool last = length - 1 == i;
                CheckTextFormat(i - hidden, format.Bold, ref bold, data.Items);
                CheckTextFormat(i - hidden, format.Italic, ref italic, data.Items);
                CheckTextFormatUnderline(i - hidden, format.Underline, ref underline, data.Items);
                CheckTextFormatLink(i - hidden, range.Link, ref link, data.Items);
            }

            CheckTextFormat(length - hidden, FormatEffect.Off, ref bold, data.Items);
            CheckTextFormat(length - hidden, FormatEffect.Off, ref italic, data.Items);
            CheckTextFormatUnderline(length - hidden, UnderlineType.None, ref underline, data.Items);
            CheckTextFormatLink(length - hidden, null, ref link, data.Items);

            return data;
        }

        private static void CheckTextFormat(int offset, FormatEffect effect, ref FormatInfo formatInfo, List<MessageFormatDataItem> items) {
            if (effect == FormatEffect.On) {
                if (formatInfo.Length > 0) {
                    formatInfo.Length++;
                } else {
                    formatInfo.Offset = offset;
                    formatInfo.Length = 1;
                }
            } else if (effect == FormatEffect.Off && formatInfo.Length > 0) {
                items.Add(new MessageFormatDataItem {
                    Type = formatInfo.Type,
                    Offset = formatInfo.Offset,
                    Length = formatInfo.Length
                });
                formatInfo.Length = 0;
            }
        }

        private static void CheckTextFormatUnderline(int offset, UnderlineType effect, ref FormatInfo formatInfo, List<MessageFormatDataItem> items) {
            if (effect == UnderlineType.Single) {
                if (formatInfo.Length > 0) {
                    formatInfo.Length++;
                } else {
                    formatInfo.Offset = offset;
                    formatInfo.Length = 1;
                }
            } else if (effect != UnderlineType.Single && formatInfo.Length > 0) {
                items.Add(new MessageFormatDataItem {
                    Type = formatInfo.Type,
                    Offset = formatInfo.Offset,
                    Length = formatInfo.Length
                });
                formatInfo.Length = 0;
            }
        }

        private static void CheckTextFormatLink(int offset, string link, ref FormatInfo formatInfo, List<MessageFormatDataItem> items) {
            if (!string.IsNullOrEmpty(link)) {
                if (formatInfo.Length > 0) {
                    formatInfo.Length++;
                } else {
                    formatInfo.Offset = offset;
                    formatInfo.Length = 1;
                    formatInfo.Url = link.Trim('"');
                }
            } else if (link != formatInfo.Url && formatInfo.Length > 0) {
                items.Add(new MessageFormatDataItem {
                    Type = formatInfo.Type,
                    Offset = formatInfo.Offset,
                    Length = formatInfo.Length,
                    Url = formatInfo.Url,
                });
                formatInfo.Length = 0;
                formatInfo.Url = null;
            }
        }
    }
}