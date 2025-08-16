using Microsoft.Windows.Widgets.Providers;
using System.Runtime.InteropServices;
using WidgetHelpers;

namespace LaneyWidgets {
    [ComVisible(true)]
    [ComDefaultInterface(typeof(IWidgetProvider))]
    [Guid("EDF77E90-80B6-423F-87EF-21D34751BEAF")]
    internal class WidgetProvider : WidgetProviderBase {
        static WidgetProvider() {
            RegisterWidget<FriendsOnlineWidget>("FriendsOnline");
        }
    }
}
