using Elorucov.Laney.DataModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VK.VKUI.Popups;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Settings.DebugViews
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SettingsEditor : SettingsPageBase
    {
        public SettingsEditor()
        {
            this.InitializeComponent();
        }

        private ObservableCollection<SimpleKeyValue> LocalSettings { get; set; } = new ObservableCollection<SimpleKeyValue>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            GetSettings();
        }

        private void GetSettings()
        {
            LocalSettings.Clear();
            foreach (var i in ApplicationData.Current.LocalSettings.Values)
            {
                System.Diagnostics.Debug.WriteLine($"{i.Key} = {i.Value}");
                LocalSettings.Add(new SimpleKeyValue
                {
                    Key = i.Key,
                    Value = i.Value.ToString(),
                    AdditionalInfo = i.Value.GetType().Name
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ShowEditorDialog();
        }

        private void SettingsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as SimpleKeyValue;
            ShowEditorDialog(item.Key);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as SimpleKeyValue;
            ApplicationData.Current.LocalSettings.Values[item.Key] = null;
            LocalSettings.Remove(item);
        }

        //

        private async void ShowEditorDialog(string key = null)
        {
            TextBox ktb = new TextBox
            {
                Style = (Style)Application.Current.Resources["VKTextBox"],
                Margin = new Thickness(0, 0, 0, 12),
                Header = "Key",
                IsReadOnly = !String.IsNullOrEmpty(key),
                Text = String.IsNullOrEmpty(key) ? String.Empty : key
            };

            ComboBox tcb = new ComboBox
            {
                Style = (Style)Application.Current.Resources["VKComboBox"],
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Header = "Type",
                IsEnabled = String.IsNullOrEmpty(key),
                ItemsSource = new List<string> { "String", "Int32", "Double", "Bool" },
                SelectedIndex = 0,
            };

            TextBox vtb = new TextBox
            {
                Style = (Style)Application.Current.Resources["VKTextBox"],
                Margin = new Thickness(0, 0, 0, 12),
                Header = "Value",
            };

            ToggleSwitch vts = new ToggleSwitch
            {
                Margin = new Thickness(0, 0, 0, 12),
                Header = "Value",
                OnContent = "True",
                OffContent = "False",
                Visibility = Visibility.Collapsed
            };

            tcb.SelectionChanged += (a, b) =>
            {
                bool isBool = tcb.SelectedIndex == 3;
                vtb.Visibility = !isBool ? Visibility.Visible : Visibility.Collapsed;
                vts.Visibility = isBool ? Visibility.Visible : Visibility.Collapsed;
            };

            if (!String.IsNullOrEmpty(key))
            {
                var p = ApplicationData.Current.LocalSettings.Values[key];
                switch (p.GetType().Name)
                {
                    case nameof(String):
                        tcb.SelectedIndex = 0;
                        vtb.Text = p.ToString();
                        break;
                    case nameof(Int32):
                        tcb.SelectedIndex = 1;
                        vtb.Text = p.ToString();
                        break;
                    case nameof(Double):
                        tcb.SelectedIndex = 2;
                        vtb.Text = p.ToString();
                        break;
                    case nameof(Boolean):
                        tcb.SelectedIndex = 3;
                        vts.IsOn = (bool)p;
                        break;
                }
            }

            StackPanel sp = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            sp.Children.Add(ktb);
            sp.Children.Add(tcb);
            sp.Children.Add(vtb);
            sp.Children.Add(vts);

            Alert alert = new Alert
            {
                Header = "Setting editor",
                Content = sp,
                PrimaryButtonText = "Change",
                SecondaryButtonText = Core.Locale.Get("close")
            };
            AlertButton result = await alert.ShowAsync();
            if (result == AlertButton.Primary)
            {
                try
                {
                    switch (tcb.SelectedIndex)
                    {
                        case 0:
                            ApplicationData.Current.LocalSettings.Values[ktb.Text] = vtb.Text;
                            break;
                        case 1:
                            ApplicationData.Current.LocalSettings.Values[ktb.Text] = Int32.Parse(vtb.Text);
                            break;
                        case 2:
                            ApplicationData.Current.LocalSettings.Values[ktb.Text] = Double.Parse(vtb.Text);
                            break;
                        case 3:
                            ApplicationData.Current.LocalSettings.Values[ktb.Text] = vts.IsOn;
                            break;
                    }
                    GetSettings();
                }
                catch (Exception ex)
                {
                    await new MessageDialog($"{ex.Message.Trim()}", "Error").ShowAsync();
                }
            }
        }
    }
}