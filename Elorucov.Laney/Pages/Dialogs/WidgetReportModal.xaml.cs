using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetReportModal : Modal {
        public WidgetReportModal(List<Widget> widgets) {
            this.InitializeComponent();
            CommunityLink.Click += (a, b) => {
                Main.GetCurrent().ShowConversationPage(-171015120);
                Hide();
            };

            GetWidgetsCode(widgets);
        }

        private void GetWidgetsCode(List<Widget> widgets) {
            string code = string.Empty;
            foreach (Widget widget in widgets) {
                if (!string.IsNullOrEmpty(code)) code += ",\n";
                code += JsonConvert.SerializeObject(widget, Formatting.Indented);
            }
            WidgetsCode.Text = code;
        }
    }
}