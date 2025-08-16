// https://github.com/CommunityToolkit/Windows/blob/main/components/SettingsControls/src/SettingsExpander/SettingsExpander.cs

using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Elorucov.Laney.Controls.WinUI {
    //// Note: ItemsRepeater will request all the available horizontal space: https://github.com/microsoft/microsoft-ui-xaml/issues/3842
    [ContentProperty(Name = nameof(Content))]
    [TemplatePart(Name = PART_ItemsRepeater, Type = typeof(ItemsRepeater))]
    public partial class SettingsExpander : Control {
        /// <summary>
        /// Fires when the SettingsExpander is opened
        /// </summary>
        public event EventHandler Expanded;

        /// <summary>
        /// Fires when the expander is closed
        /// </summary>
        public event EventHandler Collapsed;

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Header"/> property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(SettingsExpander),
            new PropertyMetadata(defaultValue: null));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Description"/> property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description),
            typeof(object),
            typeof(SettingsExpander),
            new PropertyMetadata(defaultValue: null));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="HeaderIcon"/> property.
        /// </summary>
        public static readonly DependencyProperty HeaderIconProperty = DependencyProperty.Register(
            nameof(HeaderIcon),
            typeof(IconElement),
            typeof(SettingsExpander),
            new PropertyMetadata(defaultValue: null));


        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Content"/> property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(SettingsExpander),
            new PropertyMetadata(defaultValue: null));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Content"/> property.
        /// </summary>
        public static readonly DependencyProperty ItemsHeaderProperty = DependencyProperty.Register(
            nameof(ItemsHeader),
            typeof(UIElement),
            typeof(SettingsExpander),
            new PropertyMetadata(defaultValue: null));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="Content"/> property.
        /// </summary>
        public static readonly DependencyProperty ItemsFooterProperty = DependencyProperty.Register(
            nameof(ItemsFooter),
            typeof(UIElement),
            typeof(SettingsExpander),
            new PropertyMetadata(defaultValue: null));

        /// <summary>
        /// The backing <see cref="DependencyProperty"/> for the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
         nameof(IsExpanded),
         typeof(bool),
         typeof(SettingsExpander),
         new PropertyMetadata(defaultValue: false, (d, e) => ((SettingsExpander)d).OnIsExpandedPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

        /// <summary>
        /// 
        /// <summary>
        /// Gets or sets the Header.
        /// </summary>
        public object Header {
            get => (object)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the Description.
        /// </summary>
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
        public new object Description
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
        {
            get => (object)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the HeaderIcon.
        /// </summary>
        public IconElement HeaderIcon {
            get => (IconElement)GetValue(HeaderIconProperty);
            set => SetValue(HeaderIconProperty, value);
        }

        /// <summary>
        /// Gets or sets the Content.
        /// </summary>
        public object Content {
            get => (object)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the ItemsFooter.
        /// </summary>
        public UIElement ItemsHeader {
            get => (UIElement)GetValue(ItemsHeaderProperty);
            set => SetValue(ItemsHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the ItemsFooter.
        /// </summary>
        public UIElement ItemsFooter {
            get => (UIElement)GetValue(ItemsFooterProperty);
            set => SetValue(ItemsFooterProperty, value);
        }

        /// <summary>
        /// Gets or sets the IsExpanded state.
        /// </summary>
        public bool IsExpanded {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }
        protected virtual void OnIsExpandedPropertyChanged(bool oldValue, bool newValue) {
            OnIsExpandedChanged(oldValue, newValue);

            if (newValue) {
                Expanded?.Invoke(this, EventArgs.Empty);
            } else {
                Collapsed?.Invoke(this, EventArgs.Empty);
            }
        }

        private const string PART_ItemsRepeater = "PART_ItemsRepeater";

        private ItemsRepeater _itemsRepeater;

        public IList<object> Items {
            get { return (IList<object>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IList<object>), typeof(SettingsExpander), new PropertyMetadata(null, OnItemsConnectedPropertyChanged));

        public object ItemsSource {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(SettingsExpander), new PropertyMetadata(null, OnItemsConnectedPropertyChanged));

        public object ItemTemplate {
            get { return (object)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(object), typeof(SettingsExpander), new PropertyMetadata(null));

        public StyleSelector ItemContainerStyleSelector {
            get { return (StyleSelector)GetValue(ItemContainerStyleSelectorProperty); }
            set { SetValue(ItemContainerStyleSelectorProperty, value); }
        }

        public static readonly DependencyProperty ItemContainerStyleSelectorProperty =
            DependencyProperty.Register(nameof(ItemContainerStyleSelector), typeof(StyleSelector), typeof(SettingsExpander), new PropertyMetadata(null));

        private static void OnItemsConnectedPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args) {
            if (dependencyObject is SettingsExpander expander && expander._itemsRepeater != null) {
                var datasource = expander.ItemsSource;

                if (datasource is null) {
                    datasource = expander.Items;
                }

                expander._itemsRepeater.ItemsSource = datasource;
            }
        }

        private void ItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args) {
            if (ItemContainerStyleSelector != null &&
                args.Element is FrameworkElement element &&
                element.ReadLocalValue(StyleProperty) == DependencyProperty.UnsetValue) {
                // TODO: Get item from args.Index?
                element.Style = ItemContainerStyleSelector.SelectStyle(null, element);
            }
        }

        /// <summary>
        /// The SettingsExpander is a collapsable control to host multiple SettingsCards.
        /// </summary>
        public SettingsExpander() {
            this.DefaultStyleKey = typeof(SettingsExpander);
            Items = new List<object>();
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate() {
            base.OnApplyTemplate();
            SetAccessibleName();

            if (_itemsRepeater != null) {
                _itemsRepeater.ElementPrepared -= this.ItemsRepeater_ElementPrepared;
            }

            _itemsRepeater = GetTemplateChild(PART_ItemsRepeater) as ItemsRepeater;

            if (_itemsRepeater != null) {
                _itemsRepeater.ElementPrepared += this.ItemsRepeater_ElementPrepared;

                // Update it's source based on our current items properties.
                OnItemsConnectedPropertyChanged(this, null); // Can't get it to accept type here? (DependencyPropertyChangedEventArgs)EventArgs.Empty
            }
        }

        private void SetAccessibleName() {
            if (string.IsNullOrEmpty(AutomationProperties.GetName(this))) {
                if (Header is string headerString && !string.IsNullOrEmpty(headerString)) {
                    AutomationProperties.SetName(this, headerString);
                }
            }
        }

        /// <summary>
        /// Creates AutomationPeer
        /// </summary>
        /// <returns>An automation peer for <see cref="SettingsExpander"/>.</returns>
        protected override AutomationPeer OnCreateAutomationPeer() {
            return new SettingsExpanderAutomationPeer(this);
        }

        private void OnIsExpandedChanged(bool oldValue, bool newValue) {
            var peer = FrameworkElementAutomationPeer.FromElement(this) as SettingsExpanderAutomationPeer;
            peer?.RaiseExpandedChangedEvent(newValue);
        }
    }

    /// <summary>
    /// AutomationPeer for SettingsExpander
    /// </summary>
    public class SettingsExpanderAutomationPeer : FrameworkElementAutomationPeer {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsExpander"/> class.
        /// </summary>
        /// <param name="owner">SettingsExpander</param>
        public SettingsExpanderAutomationPeer(SettingsExpander owner)
            : base(owner) {
        }

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore() {
            return AutomationControlType.Group;
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
            string name = base.GetNameCore();

            if (Owner is SettingsExpander owner) {
                if (!string.IsNullOrEmpty(AutomationProperties.GetName(owner))) {
                    name = AutomationProperties.GetName(owner);
                } else {
                    if (owner.Header is string headerString && !string.IsNullOrEmpty(headerString)) {
                        name = headerString;
                    }
                }
            }
            return name;
        }

        /// <summary>
        /// Raises the property changed event for this AutomationPeer for the provided identifier.
        /// Narrator does not announce this due to: https://github.com/microsoft/microsoft-ui-xaml/issues/3469
        /// </summary>
        /// <param name="newValue">New Expanded state</param>
        public void RaiseExpandedChangedEvent(bool newValue) {
            ExpandCollapseState newState = (newValue == true) ?
              ExpandCollapseState.Expanded :
              ExpandCollapseState.Collapsed;

            ExpandCollapseState oldState = (newState == ExpandCollapseState.Expanded) ?
              ExpandCollapseState.Collapsed :
              ExpandCollapseState.Expanded;

            RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldState, newState);
        }
    }

    public class SettingsExpanderItemStyleSelector : StyleSelector {
        /// <summary>
        /// Gets or sets the default <see cref="Style"/>.
        /// </summary>
        public Style DefaultStyle { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Style"/> when clickable.
        /// </summary>
        public Style ClickableStyle { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsExpanderItemStyleSelector"/> class.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SettingsExpanderItemStyleSelector()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        /// <inheritdoc/>
        protected override Style SelectStyleCore(object item, DependencyObject container) {
            if (container is SettingsCard card && card.IsClickEnabled) {
                return ClickableStyle;
            } else {
                return DefaultStyle;
            }
        }
    }
}