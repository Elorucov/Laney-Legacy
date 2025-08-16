// https://github.com/CommunityToolkit/Windows/blob/main/components/SettingsControls/src/SettingsCard/SettingsCard.cs

using System.Diagnostics;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Elorucov.Laney.Controls.WinUI {

    public enum ContentAlignment {
        /// <summary>
        /// The Content is aligned to the right. Default state.
        /// </summary>
        Right,
        /// <summary>
        /// The Content is left-aligned while the Header, HeaderIcon and Description are collapsed. This is commonly used for Content types such as CheckBoxes, RadioButtons and custom layouts.
        /// </summary>
        Left,
        /// <summary>
        /// The Content is vertically aligned.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// This is the base control to create consistent settings experiences, inline with the Windows 11 design language.
    /// A SettingsCard can also be hosted within a SettingsExpander.
    /// </summary>

    [TemplatePart(Name = ActionIconPresenterHolder, Type = typeof(Viewbox))]
    [TemplatePart(Name = HeaderPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = DescriptionPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = HeaderIconPresenterHolder, Type = typeof(Viewbox))]

    [TemplateVisualState(Name = NormalState, GroupName = CommonStates)]
    [TemplateVisualState(Name = PointerOverState, GroupName = CommonStates)]
    [TemplateVisualState(Name = PressedState, GroupName = CommonStates)]
    [TemplateVisualState(Name = DisabledState, GroupName = CommonStates)]

    [TemplateVisualState(Name = RightState, GroupName = ContentAlignmentStates)]
    [TemplateVisualState(Name = RightWrappedState, GroupName = ContentAlignmentStates)]
    [TemplateVisualState(Name = RightWrappedNoIconState, GroupName = ContentAlignmentStates)]
    [TemplateVisualState(Name = LeftState, GroupName = ContentAlignmentStates)]
    [TemplateVisualState(Name = VerticalState, GroupName = ContentAlignmentStates)]

    [TemplateVisualState(Name = NoContentSpacingState, GroupName = ContentSpacingStates)]
    [TemplateVisualState(Name = ContentSpacingState, GroupName = ContentSpacingStates)]

    public partial class SettingsCard : ButtonBase {
        internal const string CommonStates = "CommonStates";
        internal const string NormalState = "Normal";
        internal const string PointerOverState = "PointerOver";
        internal const string PressedState = "Pressed";
        internal const string DisabledState = "Disabled";

        internal const string ContentAlignmentStates = "ContentAlignmentStates";
        internal const string RightState = "Right";
        internal const string RightWrappedState = "RightWrapped";
        internal const string RightWrappedNoIconState = "RightWrappedNoIcon";
        internal const string LeftState = "Left";
        internal const string VerticalState = "Vertical";

        internal const string ContentSpacingStates = "ContentSpacingStates";
        internal const string NoContentSpacingState = "NoContentSpacing";
        internal const string ContentSpacingState = "ContentSpacing";

        internal const string ActionIconPresenterHolder = "PART_ActionIconPresenterHolder";
        internal const string HeaderPresenter = "PART_HeaderPresenter";
        internal const string DescriptionPresenter = "PART_DescriptionPresenter";
        internal const string HeaderIconPresenterHolder = "PART_HeaderIconPresenterHolder";

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Header"/> property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(SettingsCard),
            new PropertyMetadata(defaultValue: null, (d, e) => ((SettingsCard)d).OnHeaderPropertyChanged((object)e.OldValue, (object)e.NewValue)));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Description"/> property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description),
            typeof(object),
            typeof(SettingsCard),
            new PropertyMetadata(defaultValue: null, (d, e) => ((SettingsCard)d).OnDescriptionPropertyChanged((object)e.OldValue, (object)e.NewValue)));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="HeaderIcon"/> property.
        /// </summary>
        public static readonly DependencyProperty HeaderIconProperty = DependencyProperty.Register(
            nameof(HeaderIcon),
            typeof(IconElement),
            typeof(SettingsCard),
            new PropertyMetadata(defaultValue: null, (d, e) => ((SettingsCard)d).OnHeaderIconPropertyChanged((IconElement)e.OldValue, (IconElement)e.NewValue)));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="ActionIcon"/> property.
        /// </summary>
        public static readonly DependencyProperty ActionIconProperty = DependencyProperty.Register(
            nameof(ActionIcon),
            typeof(IconElement),
            typeof(SettingsCard),
            new PropertyMetadata(defaultValue: "\ue974"));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="ActionIconToolTip"/> property.
        /// </summary>
        public static readonly DependencyProperty ActionIconToolTipProperty = DependencyProperty.Register(
            nameof(ActionIconToolTip),
            typeof(string),
            typeof(SettingsCard),
            new PropertyMetadata(defaultValue: null));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="IsClickEnabled"/> property.
        /// </summary>
        public static readonly DependencyProperty IsClickEnabledProperty = DependencyProperty.Register(
            nameof(IsClickEnabled),
            typeof(bool),
            typeof(SettingsCard),
            new PropertyMetadata(defaultValue: false, (d, e) => ((SettingsCard)d).OnIsClickEnabledPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="ContentAlignment"/> property.
        /// </summary>
        public static readonly DependencyProperty ContentAlignmentProperty = DependencyProperty.Register(
            nameof(ContentAlignment),
            typeof(ContentAlignment),
            typeof(SettingsCard),
            new PropertyMetadata(defaultValue: ContentAlignment.Right));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="IsActionIconVisible"/> property.
        /// </summary>
        public static readonly DependencyProperty IsActionIconVisibleProperty = DependencyProperty.Register(
            nameof(IsActionIconVisible),
            typeof(bool),
            typeof(SettingsCard),
            new PropertyMetadata(defaultValue: true, (d, e) => ((SettingsCard)d).OnIsActionIconVisiblePropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

        /// <summary>
        /// Gets or sets the Header.
        /// </summary>
        public object Header {
            get => (object)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
        public new object Description
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
        {
            get => (object)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon on the left.
        /// </summary>
        public IconElement HeaderIcon {
            get => (IconElement)GetValue(HeaderIconProperty);
            set => SetValue(HeaderIconProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon that is shown when IsClickEnabled is set to true.
        /// </summary>
        public IconElement ActionIcon {
            get => (IconElement)GetValue(ActionIconProperty);
            set => SetValue(ActionIconProperty, value);
        }

        /// <summary>
        /// Gets or sets the tooltip of the ActionIcon.
        /// </summary>
        public string ActionIconToolTip {
            get => (string)GetValue(ActionIconToolTipProperty);
            set => SetValue(ActionIconToolTipProperty, value);
        }

        /// <summary>
        /// Gets or sets if the card can be clicked.
        /// </summary>
        public bool IsClickEnabled {
            get => (bool)GetValue(IsClickEnabledProperty);
            set => SetValue(IsClickEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the alignment of the Content
        /// </summary>
        public ContentAlignment ContentAlignment {
            get => (ContentAlignment)GetValue(ContentAlignmentProperty);
            set => SetValue(ContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets if the ActionIcon is shown.
        /// </summary>
        public bool IsActionIconVisible {
            get => (bool)GetValue(IsActionIconVisibleProperty);
            set => SetValue(IsActionIconVisibleProperty, value);
        }

        protected virtual void OnIsClickEnabledPropertyChanged(bool oldValue, bool newValue) {
            OnIsClickEnabledChanged();
        }
        protected virtual void OnHeaderIconPropertyChanged(IconElement oldValue, IconElement newValue) {
            OnHeaderIconChanged();
        }

        protected virtual void OnHeaderPropertyChanged(object oldValue, object newValue) {
            OnHeaderChanged();
        }

        protected virtual void OnDescriptionPropertyChanged(object oldValue, object newValue) {
            OnDescriptionChanged();
        }

        protected virtual void OnIsActionIconVisiblePropertyChanged(bool oldValue, bool newValue) {
            OnActionIconChanged();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SettingsCard"/> class.
        /// </summary>
        public SettingsCard() {
            this.DefaultStyleKey = typeof(SettingsCard);
            Debug.WriteLine("SettingsCard ctor.");
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate() {
            Debug.WriteLine("SettingsCard > OnApplyTemplate.");
            base.OnApplyTemplate();
            IsEnabledChanged -= OnIsEnabledChanged;
            OnActionIconChanged();
            OnHeaderChanged();
            OnHeaderIconChanged();
            OnDescriptionChanged();
            OnIsClickEnabledChanged();
            CheckInitialVisualState();
            SetAccessibleContentName();
            RegisterPropertyChangedCallback(ContentProperty, OnContentChanged);
            IsEnabledChanged += OnIsEnabledChanged;
        }

        private void CheckInitialVisualState() {
            VisualStateManager.GoToState(this, IsEnabled ? NormalState : DisabledState, true);

            if (GetTemplateChild("ContentAlignmentStates") is VisualStateGroup contentAlignmentStatesGroup) {
                contentAlignmentStatesGroup.CurrentStateChanged -= this.ContentAlignmentStates_Changed;
                CheckVerticalSpacingState(contentAlignmentStatesGroup.CurrentState);
                contentAlignmentStatesGroup.CurrentStateChanged += this.ContentAlignmentStates_Changed;
            }
        }

        // We automatically set the AutomationProperties.Name of the Content if not configured.
        private void SetAccessibleContentName() {
            if (Header is string headerString && headerString != string.Empty) {
                // We don't want to override an AutomationProperties.Name that is manually set, or if the Content basetype is of type ButtonBase (the ButtonBase.Content will be used then)
                if (Content is UIElement element && string.IsNullOrEmpty(AutomationProperties.GetName(element)) && element.GetType() != typeof(ButtonBase) && element.GetType() != typeof(TextBlock)) {
                    AutomationProperties.SetName(element, headerString);
                }
            }
        }

        private void EnableButtonInteraction() {
            DisableButtonInteraction();

            IsTabStop = true;
            PointerEntered += Control_PointerEntered;
            PointerExited += Control_PointerExited;
            PointerCaptureLost += Control_PointerCaptureLost;
            PointerCanceled += Control_PointerCanceled;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5)) {
                PreviewKeyDown += Control_PreviewKeyDown;
                PreviewKeyUp += Control_PreviewKeyUp;
            }
        }

        private void DisableButtonInteraction() {
            IsTabStop = false;
            PointerEntered -= Control_PointerEntered;
            PointerExited -= Control_PointerExited;
            PointerCaptureLost -= Control_PointerCaptureLost;
            PointerCanceled -= Control_PointerCanceled;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5)) {
                PreviewKeyDown -= Control_PreviewKeyDown;
                PreviewKeyUp -= Control_PreviewKeyUp;
            }
        }

        private void Control_PreviewKeyUp(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.GamepadA) {
                VisualStateManager.GoToState(this, NormalState, true);
            }
        }

        private void Control_PreviewKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.GamepadA) {
                VisualStateManager.GoToState(this, PressedState, true);
            }
        }

        public void Control_PointerEntered(object sender, PointerRoutedEventArgs e) {
            base.OnPointerEntered(e);
            VisualStateManager.GoToState(this, PointerOverState, true);
        }

        public void Control_PointerExited(object sender, PointerRoutedEventArgs e) {
            base.OnPointerExited(e);
            VisualStateManager.GoToState(this, NormalState, true);
        }

        private void Control_PointerCaptureLost(object sender, PointerRoutedEventArgs e) {
            base.OnPointerCaptureLost(e);
            VisualStateManager.GoToState(this, NormalState, true);
        }

        private void Control_PointerCanceled(object sender, PointerRoutedEventArgs e) {
            base.OnPointerCanceled(e);
            VisualStateManager.GoToState(this, NormalState, true);
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e) {
            //  e.Handled = true;
            if (IsClickEnabled) {
                base.OnPointerPressed(e);
                VisualStateManager.GoToState(this, PressedState, true);
            }
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e) {
            if (IsClickEnabled) {
                base.OnPointerReleased(e);
                VisualStateManager.GoToState(this, NormalState, true);
            }
        }

        /// <summary>
        /// Creates AutomationPeer
        /// </summary>
        /// <returns>An automation peer for <see cref="SettingsCard"/>.</returns>
        protected override AutomationPeer OnCreateAutomationPeer() {
            return new SettingsCardAutomationPeer(this);
        }

        private void OnIsClickEnabledChanged() {
            OnActionIconChanged();
            if (IsClickEnabled) {
                EnableButtonInteraction();
            } else {
                DisableButtonInteraction();
            }
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            VisualStateManager.GoToState(this, IsEnabled ? NormalState : DisabledState, true);
        }

        private void OnActionIconChanged() {
            if (GetTemplateChild(ActionIconPresenterHolder) is FrameworkElement actionIconPresenter) {
                if (IsClickEnabled && IsActionIconVisible) {
                    actionIconPresenter.Visibility = Visibility.Visible;
                } else {
                    actionIconPresenter.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OnHeaderIconChanged() {
            if (GetTemplateChild(HeaderIconPresenterHolder) is FrameworkElement headerIconPresenter) {
                headerIconPresenter.Visibility = HeaderIcon != null
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void OnDescriptionChanged() {
            if (GetTemplateChild(DescriptionPresenter) is FrameworkElement descriptionPresenter) {
                descriptionPresenter.Visibility = IsNullOrEmptyString(Description)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

        }

        private void OnHeaderChanged() {
            if (GetTemplateChild(HeaderPresenter) is FrameworkElement headerPresenter) {
                headerPresenter.Visibility = IsNullOrEmptyString(Header)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

        }

        private void ContentAlignmentStates_Changed(object sender, VisualStateChangedEventArgs e) {
            CheckVerticalSpacingState(e.NewState);
        }

        private void CheckVerticalSpacingState(VisualState s) {
            // On state change, checking if the Content should be wrapped (e.g. when the card is made smaller or the ContentAlignment is set to Vertical). If the Content and the Header or Description are not null, we add spacing between the Content and the Header/Description.

            if (s != null && (s.Name == RightWrappedState || s.Name == RightWrappedNoIconState || s.Name == VerticalState) && (Content != null) && (!IsNullOrEmptyString(Header) || !IsNullOrEmptyString(Description))) {
                VisualStateManager.GoToState(this, ContentSpacingState, true);
            } else {
                VisualStateManager.GoToState(this, NoContentSpacingState, true);
            }
        }

        private static bool IsNullOrEmptyString(object obj) {
            if (obj == null) {
                return true;
            }

            if (obj is string objString && objString == string.Empty) {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// AutomationPeer for SettingsCard
    /// </summary>
    public class SettingsCardAutomationPeer : FrameworkElementAutomationPeer {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsCard"/> class.
        /// </summary>
        /// <param name="owner">SettingsCard</param>
        public SettingsCardAutomationPeer(SettingsCard owner)
            : base(owner) {
        }

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore() {
            if (Owner is SettingsCard settingsCard && settingsCard.IsClickEnabled) {
                return AutomationControlType.Button;
            } else {
                return AutomationControlType.Group;
            }
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType,
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore() {
            return Owner.GetType().Name;
        }

        protected override string GetNameCore() {
            // We only want to announce the button card name if it is clickable, else it's just a regular card that does not receive focus
            if (Owner is SettingsCard owner && owner.IsClickEnabled) {
                string name = AutomationProperties.GetName(owner);
                if (!string.IsNullOrEmpty(name)) {
                    return name;
                } else {
                    if (owner.Header is string headerString && !string.IsNullOrEmpty(headerString)) {
                        return headerString;
                    }
                }
            }

            return base.GetNameCore();
        }
    }
}