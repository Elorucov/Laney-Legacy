using Elorucov.Laney.Helpers;
using System;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Settings.DebugViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TextParserTest : SettingsPageBase
    {
        public TextParserTest()
        {
            this.InitializeComponent();
        }


        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            string test = "Это [club171015120|группа] приложения Laney, а [id172894294|Эльчин Оруджев] — его разработчик. Сайт разработчика: https://elor.top, почта: me@elor.top.\n";
            test += "Домен с точкой [id0|@test.az]; а это — [https://vk.com/spacevk|ссылка].";
            PlainText.Text = test;
        }

        private void OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            VKTextParser.SetText(sender.Text, ResultRTB, OnLinkClicked);
            sw.Stop();

            Paragraph p = new Paragraph();
            p.Inlines.Add(new Run { Text = $"Parsing took {sw.ElapsedMilliseconds} ms.", FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic });
            ResultRTB.Blocks.Add(p);
        }

        private async void OnLinkClicked(string link)
        {
            await new MessageDialog(link, "Link").ShowAsync();
        }
    }
}