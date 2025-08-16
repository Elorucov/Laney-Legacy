using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.PreviewDebug.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MessageUITest : Page {
        public MessageUITest() {
            this.InitializeComponent();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (e.NewSize.Width >= 876) {
                Grid.SetColumnSpan(code, 1);
                Grid.SetRowSpan(code, 2);
                Grid.SetColumnSpan(preview, 1);
                Grid.SetRowSpan(preview, 2);
                Grid.SetRow(code, 0);
                Grid.SetColumn(preview, 1);
            } else {
                Grid.SetColumnSpan(code, 2);
                Grid.SetRowSpan(code, 1);
                Grid.SetColumnSpan(preview, 2);
                Grid.SetRowSpan(preview, 1);
                Grid.SetRow(code, 1);
                Grid.SetColumn(preview, 0);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            try {
                Message msg = JsonConvert.DeserializeObject<Message>(json.Text);
                LMessage mvm = new LMessage(msg);
                mvm.UISentMessageState = SentMessageState.Unread;
                BubbleMessageUI bui = new BubbleMessageUI(mvm, preview, false);
                preview.Content = bui;
            } catch (Exception ex) {
                preview.Content = new TextBlock {
                    Text = $"Error while rendering a message UI!\n{ex.GetType()}: {ex.Message} (HResult: 0x{ex.HResult.ToString("x8")})",
                    Foreground = new SolidColorBrush(Colors.Red)
                };
            }
        }
    }
}
