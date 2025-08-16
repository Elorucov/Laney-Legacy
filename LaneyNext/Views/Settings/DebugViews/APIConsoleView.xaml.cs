using ELOR.VKAPILib;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Settings.DebugViews
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class APIConsoleView : SettingsPageBase
    {
        public APIConsoleView()
        {
            this.InitializeComponent();
            CategoryId = Constants.SettingsDebugId;
            Title = "API Console";

            var sendButton = new PageHeaderButton { Icon = VKIconName.Icon28Send, Text = "Send" };
            sendButton.Click += SendAPIRequest;
            RightButtons.Add(sendButton);

            ShowWarn();
        }

        private async void ShowWarn()
        {
            string warn = "Эта страница предназначена только для экспертов.\n";
            warn += "Сюда рекомендуется зайти только по просьбе разработчика (в случае, если Вы нашли баг). Иначе, закройте это окно, вероятнее всего, Вас обманывают!\n\n";
            warn += "TIPS: Вы можете не прописывть параметры access_token, v и lang, в таком случае будут отправлены значения из текущей сессии.";

            Alert alert = new Alert
            {
                Header = "Внимание",
                Text = warn,
                PrimaryButtonText = "Я всё понял!",
                SecondaryButtonText = "Закрыть"
            };
            AlertButton result = await alert.ShowAsync();
            if (result != AlertButton.Primary)
            {
                Frame.GoBack();
            }
        }

        private VKAPI API;
        private ObservableCollection<SimpleKeyValue> MethodParameters { get; set; } = new ObservableCollection<SimpleKeyValue>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            API = e.Parameter as VKAPI;
        }

        private void ChangeLayout(object sender, SizeChangedEventArgs e)
        {
            double x = e.NewSize.Width;
            double y = e.NewSize.Height;
            if (Math.Max(x, y) == x && x >= 720)
            {
                Grid.SetColumn(ParametersList, 0);
                Grid.SetColumnSpan(ParametersList, 1);
                Grid.SetRow(ParametersList, 0);
                Grid.SetRowSpan(ParametersList, 2);
                Grid.SetColumn(ResultBox, 1);
                Grid.SetColumnSpan(ResultBox, 1);
                Grid.SetRow(ResultBox, 0);
                Grid.SetRowSpan(ResultBox, 2);
            }
            else
            {
                Grid.SetColumn(ParametersList, 0);
                Grid.SetColumnSpan(ParametersList, 2);
                Grid.SetRow(ParametersList, 0);
                Grid.SetRowSpan(ParametersList, 1);
                Grid.SetColumn(ResultBox, 0);
                Grid.SetColumnSpan(ResultBox, 2);
                Grid.SetRow(ResultBox, 1);
                Grid.SetRowSpan(ResultBox, 1);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MethodParameters.Add(new SimpleKeyValue() { IsEnabled = true });
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as SimpleKeyValue;
            MethodParameters.Remove(item);
        }

        bool isSending = false;
        private async void SendAPIRequest(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Method.Text) || isSending) return;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            foreach (var i in MethodParameters)
            {
                if (!i.IsEnabled) continue;
                if (!String.IsNullOrEmpty(i.Key) && !String.IsNullOrEmpty(i.Value)) parameters.Add(i.Key, i.Value);
            }
            if (!parameters.ContainsKey("access_token")) parameters.Add("access_token", API.AccessToken);
            if (!parameters.ContainsKey("lang")) parameters.Add("lang", API.Language);
            if (!parameters.ContainsKey("v")) parameters.Add("v", VKAPI.Version);

            ResultBox.Text = "Sending...";
            isSending = true;
            try
            {
                string response = await API.SendRequestAsync(Method.Text, parameters);
                JToken jt = JToken.Parse(response);
                ResultBox.Text = jt.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                ResultBox.Text = $"Exception was thrown!\nHResult: 0x{ex.HResult.ToString("x8")}\nMessage: {ex.Message.Trim()}";
            }
            isSending = false;
        }
    }
}
