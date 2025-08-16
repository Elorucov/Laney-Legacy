using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Controls.WinUI {
    /// <summary>
    /// Represents a control that can contain multiple settings (or other) controls
    /// </summary>
    [TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
    [TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
    public partial class SettingsGroup : ItemsControl {
        public SettingsGroup() {
            DefaultStyleKey = typeof(SettingsGroup);
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header",
            typeof(string),
            typeof(SettingsGroup),
            new PropertyMetadata(default(string)));

        [Localizable(true)]
        public string Header {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        TextBlock HeaderPresenter;

        protected override void OnApplyTemplate() {
            IsEnabledChanged -= SettingsGroup_IsEnabledChanged;
            SetEnabledState();
            IsEnabledChanged += SettingsGroup_IsEnabledChanged;
            base.OnApplyTemplate();

            // by ELOR: hide HeaderPresenter if Header is null or empty
            HeaderPresenter = (TextBlock)GetTemplateChild(nameof(HeaderPresenter));
            SetHeaderVisibility();
            long id = RegisterPropertyChangedCallback(HeaderProperty, (a, b) => SetHeaderVisibility());
            Unloaded += (a, b) => UnregisterPropertyChangedCallback(HeaderProperty, id);
        }

        private void SetHeaderVisibility() {
            HeaderPresenter.Visibility = string.IsNullOrEmpty(Header) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SettingsGroup_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            SetEnabledState();
        }

        private void SetEnabledState() {
            VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", true);
        }

        protected override AutomationPeer OnCreateAutomationPeer() {
            return new SettingsGroupAutomationPeer(this);
        }
    }

    public class SettingsGroupAutomationPeer : FrameworkElementAutomationPeer {
        public SettingsGroupAutomationPeer(SettingsGroup owner) : base(owner) {
        }

        protected override string GetNameCore() {
            SettingsGroup selectedSettingsGroup = (SettingsGroup)Owner;
            return selectedSettingsGroup.Header;
        }
    }
}