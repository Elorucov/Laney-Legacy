using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.PreviewDebug.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class XAMLTest : Page {
        public XAMLTest() {
            this.InitializeComponent();
        }

        private void CheckXAML(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(xamlCode.Text)) {
                result.Text = "Empty!";
                return;
            }
            try {
                object test = XamlReader.Load(xamlCode.Text);
                result.Text = $"Parsed: {test}";
                if (test is UIElement el) parsed.Child = el;
            } catch (Exception ex) {
                result.Text = $"Error 0x{ex.HResult.ToString("x8")}: {ex.Message.Split('\n').LastOrDefault()}";
            }
        }
    }
}