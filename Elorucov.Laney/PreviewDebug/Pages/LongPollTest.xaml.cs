using Elorucov.Laney.Services.LongPoll;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.PreviewDebug.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class LongPollTest : Page {
        public LongPollTest() {
            this.InitializeComponent();
            LongPoll.DEBUGLongPollResponseReceived += async (a) => {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    TextBlock tb = new TextBlock();
                    tb.FontFamily = new FontFamily("Lucida Console");
                    tb.FontSize = 12;
                    tb.Text = a;
                    d.Children.Add(tb);
                });
            };
        }

        private void t1(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                bool res = await LongPoll.InitLongPoll();
                (sender as Button).IsEnabled = !res;
            })();
        }
    }
}
