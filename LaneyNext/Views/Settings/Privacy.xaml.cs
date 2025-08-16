using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels.Settings;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Settings
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class Privacy : SettingsPageBase
    {
        private PrivacyViewModel ViewModel { get { return DataContext as PrivacyViewModel; } }

        public Privacy()
        {
            this.InitializeComponent();
            CategoryId = Constants.SettingsPrivacyId;
            DataContext = new PrivacyViewModel();

            // По какой-то таинственной причине ToggleSwitch для winhello
            // не привязывается к свойству WindowsHelloEnabled,
            // так что приходится писать костыли, е***ый MVVM...
            ViewModel.PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(PrivacyViewModel.WindowsHelloEnabled))
                    ts1.IsOn = ViewModel.WindowsHelloEnabled;
            };
        }

        private void ComboBox_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            PrivacySetting setting = cb.DataContext as PrivacySetting;
            string oldvalue = cb.SelectedItem.ToString();

            cb.SelectionChanged += (a, b) =>
            {
                if (cb.SelectedItem.ToString() == oldvalue) return;
                if (cb.SelectedItem.ToString() == "some")
                {
                    cb.SelectedItem = oldvalue;
                }
                else
                {
                    oldvalue = cb.SelectedItem.ToString();
                    ViewModel.SetPrivacySetting(setting.Key, oldvalue);
                }
            };
        }

        private void ToggleSwitch_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ToggleSwitch ts = (ToggleSwitch)sender;
            PrivacySetting setting = ts.DataContext as PrivacySetting;
            bool oldvalue = ts.IsOn;

            ts.Toggled += (a, b) =>
            {
                oldvalue = ts.IsOn;
                ViewModel.SetPrivacySetting(setting.Key, oldvalue.ToString().ToLower());
            };
        }
    }
}
