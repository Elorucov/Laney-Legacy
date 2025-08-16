using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Objects;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Controls {
    public interface IMessageRichEditBox {
        string Text { get; set; }
        MessageFormatData Format { get; set; }
    }

    public class MessageRichEditBox : RichEditBox, IMessageRichEditBox {
        public MessageRichEditBox() {
            IsSpellCheckEnabled = false;
            IsTextPredictionEnabled = false;
            Loaded += MessageRichEditBox_Loaded;
            Unloaded += MessageRichEditBox_Unloaded;
        }

        long tid = 0;
        long fid = 0;

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(MessageRichEditBox), new PropertyMetadata(string.Empty));

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
            nameof(Format), typeof(MessageFormatData), typeof(MessageRichEditBox), new PropertyMetadata(null));

        public MessageFormatData Format {
            get { return (MessageFormatData)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }

        const string FlyoutXaml = "<StackPanel xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" Background=\"{ThemeResource SolidBackgroundFillColorBaseBrush}\" BorderBrush=\"{ThemeResource SurfaceStrokeColorFlyoutBrush}\" BorderThickness=\"1\" CornerRadius=\"{StaticResource OverlayCornerRadius}\" Padding=\"4\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"><StackPanel.Resources><SolidColorBrush x:Key=\"ButtonBackgroundDisabled\" Color=\"Transparent\" /><SolidColorBrush x:Key=\"ToggleButtonBackgroundDisabled\" Color=\"Transparent\" /><Style TargetType=\"Button\" BasedOn=\"{StaticResource DefaultButtonStyle}\"><Setter Property=\"Width\" Value=\"32\"/><Setter Property=\"Height\" Value=\"32\"/><Setter Property=\"Padding\" Value=\"0\"/><Setter Property=\"HorizontalContentAlignment\" Value=\"Center\"/><Setter Property=\"VerticalContentAlignment\" Value=\"Center\"/><Setter Property=\"Background\" Value=\"Transparent\"/><Setter Property=\"BorderBrush\" Value=\"Transparent\"/><Setter Property=\"BorderThickness\" Value=\"0\"/></Style><Style TargetType=\"ToggleButton\" BasedOn=\"{StaticResource DefaultToggleButtonStyle}\"><Setter Property=\"Width\" Value=\"32\"/><Setter Property=\"Height\" Value=\"32\"/><Setter Property=\"Padding\" Value=\"0\"/><Setter Property=\"HorizontalContentAlignment\" Value=\"Center\"/><Setter Property=\"VerticalContentAlignment\" Value=\"Center\"/><Setter Property=\"Background\" Value=\"Transparent\"/><Setter Property=\"BorderBrush\" Value=\"Transparent\"/><Setter Property=\"BorderThickness\" Value=\"0\"/></Style></StackPanel.Resources></StackPanel>";

        Popup _popup;
        StackPanel _root;
        StackPanel _mainRow;
        StackPanel _extendedRow;

        ToggleButton _boldTB;
        ToggleButton _italicTB;
        ToggleButton _underlineTB;
        Button _linkB;
        Button _moreB;

        Button _undoB;
        Button _redoB;
        Button _cutB;
        Button _copyB;
        Button _pasteB;
        Button _linkB2;

        bool _canCloseFlyoutIfLostFocus = true;
        bool _ignoreTextPropertyChangedCallback = false;
        bool _ignoreFormatPropertyChangedCallback = false;
        bool _ignoreFlyoutButtonEvents = false;
        ITextSelection _textSelection;
        PointerEventHandler pointerEventHandler;

        private CoreWindow ParentWindow => CoreApplication.GetCurrentView().CoreWindow;

        private void MessageRichEditBox_Loaded(object sender, RoutedEventArgs e) {
            Setup();
        }

        private void Setup() {
            tid = RegisterPropertyChangedCallback(TextProperty, OnTextChanged);
            fid = RegisterPropertyChangedCallback(FormatProperty, OnFormatChanged);

            TextChanging += MessageRichEditBox_TextChanging;
            Paste += MessageRichEditBox_Paste;
            SelectionChanged += MessageRichEditBox_SelectionChanged;

            _boldTB = GenerateToggleButton("", Locale.Get("TextCommandLabelBold"), "B");
            _boldTB.Checked += BoldTB_Checked;
            _boldTB.Unchecked += BoldTB_Unchecked;

            _italicTB = GenerateToggleButton("", Locale.Get("TextCommandLabelItalic"), "I");
            _italicTB.Checked += ItalicTB_Checked;
            _italicTB.Unchecked += ItalicTB_Unchecked;

            _underlineTB = GenerateToggleButton("", Locale.Get("TextCommandLabelUnderline"), "U");
            _underlineTB.Checked += UnderlineTB_Checked;
            _underlineTB.Unchecked += UnderlineTB_Unchecked;

            _linkB = GenerateButton("", Locale.Get("atch_link").Capitalize());
            _linkB.Click += LinkTB_Click;

            _moreB = GenerateButton("", Locale.Get("more"));
            _moreB.Click += More_Click;

            _undoB = GenerateButton("", Locale.Get("TextCommandLabelUndo"), "Z");
            _undoB.Click += Undo_Click;

            _redoB = GenerateButton("", Locale.Get("TextCommandLabelRedo"), "Y");
            _redoB.Click += Redo_Click;

            _cutB = GenerateButton("", Locale.Get("TextCommandLabelCut"), "X");
            _cutB.Click += Cut_Click;

            _copyB = GenerateButton("", Locale.Get("TextCommandLabelCopy"), "C");
            _copyB.Click += Copy_Click;

            _pasteB = GenerateButton("", Locale.Get("TextCommandLabelPaste"), "V");
            _pasteB.Click += Paste_Click;

            _linkB2 = GenerateButton("", Locale.Get("TextCommandLabelEditLink"));
            _linkB2.Click += LinkTB_Click;

            SetupFlyout();

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) {
                SelectionFlyout = null;
            }
            ContextFlyout = null;

            ContextMenuOpening += OnContextMenuOpening;
            ContextRequested += MessageRichEditBox_ContextRequested;
            GotFocus += MessageRichEditBox_GotFocus;
            LostFocus += MessageRichEditBox_LostFocus;
            CoreApplication.GetCurrentView().CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            _root.PointerEntered += FlyoutRoot_PointerEntered;
            _root.PointerExited += FlyoutRoot_PointerExited;
            ParentWindow.PointerReleased += Window_PointerReleased;

            pointerEventHandler = new PointerEventHandler(OnPointerReleased);
            AddHandler(RichEditBox.PointerReleasedEvent, pointerEventHandler, true);
        }

        private void MessageRichEditBox_Unloaded(object sender, RoutedEventArgs e) {
            UnregisterPropertyChangedCallback(TextProperty, tid);
            UnregisterPropertyChangedCallback(FormatProperty, fid);

            TextChanging -= MessageRichEditBox_TextChanging;
            Paste -= MessageRichEditBox_Paste;
            SelectionChanged -= MessageRichEditBox_SelectionChanged;

            _boldTB.Checked -= BoldTB_Checked;
            _boldTB.Unchecked -= BoldTB_Unchecked;
            _italicTB.Checked -= ItalicTB_Checked;
            _italicTB.Unchecked -= ItalicTB_Unchecked;
            _underlineTB.Checked -= UnderlineTB_Checked;
            _underlineTB.Unchecked -= UnderlineTB_Unchecked;
            _linkB.Click -= LinkTB_Click;
            _moreB.Click -= More_Click;

            _undoB.Click -= Undo_Click;
            _redoB.Click -= Redo_Click;
            _cutB.Click -= Cut_Click;
            _copyB.Click -= Copy_Click;
            _pasteB.Click -= Paste_Click;
            _linkB2.Click -= LinkTB_Click;

            ContextMenuOpening -= OnContextMenuOpening;
            ContextRequested -= MessageRichEditBox_ContextRequested;
            GotFocus -= MessageRichEditBox_GotFocus;
            LostFocus -= MessageRichEditBox_LostFocus;
            CoreApplication.GetCurrentView().CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            _root.PointerEntered -= FlyoutRoot_PointerEntered;
            _root.PointerExited -= FlyoutRoot_PointerExited;
            ParentWindow.PointerReleased -= Window_PointerReleased;
            RemoveHandler(RichEditBox.PointerReleasedEvent, pointerEventHandler);

            Loaded -= MessageRichEditBox_Loaded;
            Unloaded -= MessageRichEditBox_Unloaded;
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

        private void Window_PointerReleased(CoreWindow sender, PointerEventArgs args) {
            if (Document.Selection.Length != 0 && FocusState != FocusState.Unfocused) {
                var position = args.CurrentPoint.Position;
                ShowFlyout(position, args.CurrentPoint.PointerDevice.PointerDeviceType == PointerDeviceType.Touch);
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e) {
            if (Document.Selection.Length == 0 && _popup != null) _popup.IsOpen = false;
        }

        // to avoid displaying system popup on old versions of Win10.
        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e) {
            e.Handled = true;
        }

        private void MessageRichEditBox_ContextRequested(UIElement sender, ContextRequestedEventArgs args) {
            args.Handled = true;
            args.TryGetPosition(Window.Current.Content, out Point position);
            ShowFlyout(position, false, true);
        }

        private void MessageRichEditBox_GotFocus(object sender, RoutedEventArgs e) {
            if (_textSelection != null) {
                Document.Selection.StartPosition = _textSelection.StartPosition;
                Document.Selection.EndPosition = _textSelection.EndPosition;
            }
        }

        private void MessageRichEditBox_LostFocus(object sender, RoutedEventArgs e) {
            if (_canCloseFlyoutIfLostFocus && _popup != null) _popup.IsOpen = false;
        }

        private void CoreWindow_PointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args) {
            if (_canCloseFlyoutIfLostFocus && _popup != null) _popup.IsOpen = false;
        }


        private void FlyoutRoot_PointerEntered(object sender, PointerRoutedEventArgs e) {
            _canCloseFlyoutIfLostFocus = false;
        }

        private void FlyoutRoot_PointerExited(object sender, PointerRoutedEventArgs e) {
            _canCloseFlyoutIfLostFocus = true;
        }

        private void MessageRichEditBox_Paste(object sender, TextControlPasteEventArgs e) {
            new System.Action(async () => await TryPasteAsync(e))();
        }

        private void MessageRichEditBox_SelectionChanged(object sender, RoutedEventArgs args) {
            _textSelection = Document.Selection;
            _mainRow.Visibility = _textSelection.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            _linkB2.Visibility = _textSelection.Length == 0 && !string.IsNullOrEmpty(_textSelection.Link) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BoldTB_Checked(object sender, RoutedEventArgs e) {
            if (_ignoreFlyoutButtonEvents) return;
            Document.Selection.CharacterFormat.Bold = FormatEffect.On;
            UpdateFormat();
        }

        private void BoldTB_Unchecked(object sender, RoutedEventArgs e) {
            if (_ignoreFlyoutButtonEvents) return;
            Document.Selection.CharacterFormat.Bold = FormatEffect.Off;
            UpdateFormat();
        }

        private void ItalicTB_Checked(object sender, RoutedEventArgs e) {
            if (_ignoreFlyoutButtonEvents) return;
            Document.Selection.CharacterFormat.Italic = FormatEffect.On;
            UpdateFormat();
        }

        private void ItalicTB_Unchecked(object sender, RoutedEventArgs e) {
            if (_ignoreFlyoutButtonEvents) return;
            Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
            UpdateFormat();
        }

        private void UnderlineTB_Checked(object sender, RoutedEventArgs e) {
            if (_ignoreFlyoutButtonEvents) return;
            Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
            UpdateFormat();
        }

        private void UnderlineTB_Unchecked(object sender, RoutedEventArgs e) {
            if (_ignoreFlyoutButtonEvents) return;
            Document.Selection.CharacterFormat.Underline = UnderlineType.None;
            UpdateFormat();
        }

        private void LinkTB_Click(object sender, RoutedEventArgs e) {
            ShowLinkEnterFlyout(_linkB);
        }

        private void More_Click(object sender, RoutedEventArgs e) {
            bool isHidden = _extendedRow.Visibility == Visibility.Collapsed;
            _extendedRow.Visibility = isHidden ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Undo_Click(object sender, RoutedEventArgs e) {
            if (Document.CanUndo()) Document.Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e) {
            if (Document.CanRedo()) Document.Redo();
        }

        private void Cut_Click(object sender, RoutedEventArgs e) {
            if (Document.CanCopy()) Document.Selection.Cut();
        }

        private void Copy_Click(object sender, RoutedEventArgs e) {
            if (Document.CanCopy()) Document.Selection.Copy();
        }

        private void Paste_Click(object sender, RoutedEventArgs e) {
            new System.Action(async () => await TryPasteAsync())();
        }

        #region Flyout

        private Button GenerateButton(string glyph, string label, string accessKey = null) {
            Button b = new Button {
                Content = new FixedFontIcon { Glyph = glyph }
            };
            string tt = string.IsNullOrEmpty(accessKey) ? label : $"{label} (Ctrl + {accessKey})";
            ToolTipService.SetToolTip(b, tt);
            return b;
        }

        private ToggleButton GenerateToggleButton(string glyph, string label, string accessKey = null) {
            ToggleButton tb = new ToggleButton {
                Content = new FixedFontIcon { Glyph = glyph }
            };
            string tt = string.IsNullOrEmpty(accessKey) ? label : $"{label} (Ctrl + {accessKey})";
            ToolTipService.SetToolTip(tb, tt);
            return tb;
        }

        private void SetupFlyout() {
            if (_popup == null) _popup = new Popup();

            if (_popup.Child == null) {
                _root = XamlReader.Load(FlyoutXaml) as StackPanel;
                _popup.Child = _root;

                _mainRow = new StackPanel {
                    Orientation = Orientation.Horizontal,
                    Visibility = Visibility.Collapsed
                };
                _mainRow.Children.Add(_boldTB);
                _mainRow.Children.Add(_italicTB);
                _mainRow.Children.Add(_underlineTB);
                _mainRow.Children.Add(_linkB);
                _mainRow.Children.Add(_moreB);
                _root.Children.Add(_mainRow);

                _extendedRow = new StackPanel {
                    Orientation = Orientation.Horizontal,
                };
                _extendedRow.Children.Add(_undoB);
                _extendedRow.Children.Add(_redoB);
                _extendedRow.Children.Add(_cutB);
                _extendedRow.Children.Add(_copyB);
                _extendedRow.Children.Add(_pasteB);
                _extendedRow.Children.Add(_linkB2);
                _root.Children.Add(_extendedRow);
            }
        }

        private void ShowFlyout(Point position, bool fixTouch = false, bool extended = false) {
            if (!extended && _popup.IsOpen) return;

            // Formatting
            if (_textSelection != null && _textSelection.Length != 0) {
                var format = _textSelection.CharacterFormat;

                _ignoreFlyoutButtonEvents = true;
                _boldTB.IsChecked = format.Bold == FormatEffect.On;
                _italicTB.IsChecked = format.Italic == FormatEffect.On;
                _underlineTB.IsChecked = format.Underline == UnderlineType.Single;
                _ignoreFlyoutButtonEvents = false;
            }

            // Commands
            _undoB.Visibility = Document.CanUndo() ? Visibility.Visible : Visibility.Collapsed;
            _redoB.Visibility = Document.CanRedo() ? Visibility.Visible : Visibility.Collapsed;

            bool canCopy = _textSelection != null && _textSelection.Length > 0 && Document.CanCopy();
            _cutB.Visibility = canCopy ? Visibility.Visible : Visibility.Collapsed;
            _copyB.Visibility = canCopy ? Visibility.Visible : Visibility.Collapsed;

            var clipboard = Clipboard.GetContent();
            _pasteB.Visibility = clipboard.Contains(StandardDataFormats.Text) && Document.CanPaste() ? Visibility.Visible : Visibility.Collapsed;

            var clone = Document.Selection.GetClone();
            clone.StartOf(TextRangeUnit.Link, true);
            bool canShowLinkEditorBtn = _mainRow.Visibility == Visibility.Collapsed && !string.IsNullOrEmpty(clone.Link);
            string test = clone.Link;
            _linkB2.Visibility = canShowLinkEditorBtn ? Visibility.Visible : Visibility.Collapsed;

            // Flyout positions
            _extendedRow.Visibility = extended ? Visibility.Visible : Visibility.Collapsed;
            _root.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var size = _root.DesiredSize;
            var fw = size.Width;
            var fh = size.Height;

            double left = Math.Min(Window.Current.Bounds.Width - fw, Math.Max(0, position.X - (fw / 2)));
            double top = Math.Max(0, position.Y - (fixTouch ? 72 : 36) - fh);

            _popup.RenderTransform = new CompositeTransform {
                TranslateX = left,
                TranslateY = top
            };
            _popup.IsOpen = true;
        }

        #endregion

        private void ShowLinkEnterFlyout(FrameworkElement target) {
            new System.Action(async () => {
                var range = Document.Selection.GetClone();
                var clone = Document.Selection.GetClone();
                clone.StartOf(TextRangeUnit.Link, true);

                if (clone.Link.Length > 0) {
                    range.Expand(TextRangeUnit.Link);
                }

                var start = Math.Min(range.StartPosition, range.EndPosition);
                var end = Math.Max(range.StartPosition, range.EndPosition);
                _popup.IsOpen = false;

                range.GetText(TextGetOptions.NoHidden | TextGetOptions.AdjustCrlf, out string text);

                LinkEditorDialog dlg = new LinkEditorDialog(text, range.Link.Length > 0 ? range.Link.Trim('"') : string.Empty);
                var result = await dlg.ShowAsync();

                if (result == ContentDialogResult.Primary) {
                    Document.BatchDisplayUpdates();
                    range.CharacterFormat.ForegroundColor = Color.FromArgb(255, 0, 122, 204);
                    range.Link = $"\"{dlg.Link}\"";
                    Document.ApplyDisplayUpdates();
                } else {
                    Document.BatchDisplayUpdates();
                    var def = Document.GetDefaultCharacterFormat();
                    range.CharacterFormat.ForegroundColor = def.ForegroundColor;
                    range.Link = "\"\"";
                    Document.ApplyDisplayUpdates();
                }

                Document.Selection.SetRange(range.EndPosition, range.EndPosition);
                UpdateFormat();
            })();
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
    }
}