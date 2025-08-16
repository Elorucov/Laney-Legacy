using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Objects;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Controls {
    public class MessageRichEditBox1809 : RichEditBox, IMessageRichEditBox {
        public MessageRichEditBox1809() {
            Loaded += MessageRichEditBox1809_Loaded;
            Unloaded += MessageRichEditBox1809_Unloaded;
        }

        long tid = 0;
        long fid = 0;

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(MessageRichEditBox1809), new PropertyMetadata(string.Empty));

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
            nameof(Format), typeof(MessageFormatData), typeof(MessageRichEditBox1809), new PropertyMetadata(null));

        public MessageFormatData Format {
            get { return (MessageFormatData)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }

        bool _ignoreTextPropertyChangedCallback = false;
        bool _ignoreFormatPropertyChangedCallback = false;

        private void MessageRichEditBox1809_Loaded(object sender, RoutedEventArgs e) {
            SelectionFlyout.Opening += Menu_Opening;
            ContextFlyout.Opening += Menu_Opening;
            SelectionFlyout.Closing += Menu_Closing;
            ContextFlyout.Closing += Menu_Closing;
            Paste += MessageRichEditBox1809_Paste;

            tid = RegisterPropertyChangedCallback(TextProperty, OnTextChanged);
            fid = RegisterPropertyChangedCallback(FormatProperty, OnFormatChanged);

            TextChanging += MessageRichEditBox_TextChanging;
        }

        private void MessageRichEditBox1809_Unloaded(object sender, RoutedEventArgs e) {
            SelectionFlyout.Opening -= Menu_Opening;
            ContextFlyout.Opening -= Menu_Opening;
            SelectionFlyout.Closing -= Menu_Closing;
            ContextFlyout.Closing -= Menu_Closing;
            Paste -= MessageRichEditBox1809_Paste;

            UnregisterPropertyChangedCallback(TextProperty, tid);
            UnregisterPropertyChangedCallback(FormatProperty, fid);
            TextChanging -= MessageRichEditBox_TextChanging;

            Loaded -= MessageRichEditBox1809_Loaded;
            Unloaded -= MessageRichEditBox1809_Unloaded;
        }

        private void OnTextChanged(DependencyObject sender, DependencyProperty dp) {
            if (_ignoreTextPropertyChangedCallback) return;
            Document.SetText(TextSetOptions.None, Text);
            SetFormat();
        }

        private void OnFormatChanged(DependencyObject sender, DependencyProperty dp) {
            if (_ignoreFormatPropertyChangedCallback) return;
            SetFormat();
        }

        private void MessageRichEditBox_TextChanging(RichEditBox sender, RichEditBoxTextChangingEventArgs args) {
            _ignoreTextPropertyChangedCallback = true;
            Document.GetText(TextGetOptions.NoHidden | TextGetOptions.AdjustCrlf, out string text);
            Text = text;
            _ignoreTextPropertyChangedCallback = false;
        }

        private void MessageRichEditBox1809_Paste(object sender, TextControlPasteEventArgs e) {
            new System.Action(async () => await TryPasteAsync())();
        }

        private void Menu_Opening(object sender, object e) {
            Microsoft.UI.Xaml.Controls.CommandBarFlyout flyout = sender as Microsoft.UI.Xaml.Controls.CommandBarFlyout;

            AppBarButton linkButton = new AppBarButton {
                Label = Locale.Get("atch_link").Capitalize(),
                Icon = new FontIcon { Glyph = "" },
                AccessKey = "L"
            };
            ToolTipService.SetToolTip(linkButton, Locale.Get("TextCommandLabelLink"));
            linkButton.Click += LinkButton_Click;
            flyout.PrimaryCommands.Add(linkButton);
        }
        private void Menu_Closing(Windows.UI.Xaml.Controls.Primitives.FlyoutBase sender, Windows.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs args) {
            UpdateFormat();
        }

        private void LinkButton_Click(object sender, RoutedEventArgs e) {
            ShowLinkEnterFlyout();
        }

        private void ShowLinkEnterFlyout() {
            new System.Action(async () => {
                var range = Document.Selection.GetClone();
                var clone = Document.Selection.GetClone();
                clone.StartOf(TextRangeUnit.Link, true);

                if (clone.Link.Length > 0) {
                    range.Expand(TextRangeUnit.Link);
                }

                var start = Math.Min(range.StartPosition, range.EndPosition);
                var end = Math.Max(range.StartPosition, range.EndPosition);

                range.GetText(TextGetOptions.NoHidden | TextGetOptions.AdjustCrlf, out string text);

                if (text.Length == 0) return;

                LinkEditorDialog dlg = new LinkEditorDialog(text, range.Link.Length > 0 ? range.Link.Trim('"') : string.Empty);
                var result = await dlg.ShowAsync();

                if (result == ContentDialogResult.Primary) {
                    Document.BatchDisplayUpdates();
                    range.CharacterFormat.ForegroundColor = Color.FromArgb(255, 0, 122, 204);
                    range.Link = $"\"{dlg.Link}\"";
                    Document.ApplyDisplayUpdates();
                } else {
                    var aa = range.StartPosition;
                    var bb = range.EndPosition;
                    var cc = range.StoryLength;
                    Document.BatchDisplayUpdates();
                    var def = Document.GetDefaultCharacterFormat();
                    range.CharacterFormat.ForegroundColor = def.ForegroundColor;
                    if (!string.IsNullOrEmpty(range.Link)) range.Link = "";
                    Document.ApplyDisplayUpdates();
                }

                Document.Selection.SetRange(range.EndPosition, range.EndPosition);
                UpdateFormat();
            })();
        }

        private void SetFormat() {
            if (string.IsNullOrEmpty(Text)) {
                Document.BatchDisplayUpdates();
                TextFormatConverter.TryClearFormatting(Document);
                Document.ApplyDisplayUpdates();
                return;
            }
            TextFormatConverter.FromVKFormat(Document, Format);
        }

        private void UpdateFormat() {
            _ignoreFormatPropertyChangedCallback = true;
            Format = TextFormatConverter.ToVKFormat(Document);
            _ignoreFormatPropertyChangedCallback = false;
        }

        // Copied from Unigram.
        private async Task TryPasteAsync(TextControlPasteEventArgs e = null) {
            try {
                var package = Clipboard.GetContent();

                if (package.AvailableFormats.Contains(StandardDataFormats.Bitmap)) { // To prevent inserting image into textbox.
                    e.Handled = true;
                    return;
                }

                // If the user tries to paste RTF content from any TOM control (Visual Studio, Word, Wordpad, browsers)
                // we have to handle the pasting operation manually to allow plaintext only.
                if (package.AvailableFormats.Contains(StandardDataFormats.Text) /*&& package.Contains("Rich Text Format")*/) {
                    if (e != null) e.Handled = true;

                    var text = await package.GetTextAsync();
                    var start = Document.Selection.StartPosition;
                    var length = Math.Abs(Document.Selection.Length);

                    // TODO also insert link.
                    Document.Selection.SetText(TextSetOptions.Unhide, text);
                    Document.Selection.SetRange(start + text.Length, start + text.Length);
                }
            } catch { }
        }
    }
}