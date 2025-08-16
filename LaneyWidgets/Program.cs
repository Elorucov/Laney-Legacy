using Microsoft.Windows.Widgets.Providers;
using System.Diagnostics;
using WidgetHelpers;

namespace LaneyWidgets {
    internal class Program {
        [MTAThread]
        static async Task Main(string[] args) {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            Debug.WriteLine($"Widgets app is starting! Args: {string.Join(" ", args)}");
            if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer") {
                // ComServer<IWidgetProvider, WidgetProvider>.Instance.Run();
                WinRT.ComWrappersSupport.InitializeComWrappers();
                Debug.WriteLine($"Registering provider...");
                using (var manager = RegistrationManager<WidgetProvider>.RegisterProvider()) {
                    Debug.WriteLine("Widget Provider registered.");

                    var existingWidgets = WidgetManager.GetDefault().GetWidgetIds();
                    if (existingWidgets != null) {
                        Debug.WriteLine($"There are {existingWidgets.Length} Widgets currently outstanding:");
                        foreach (var widgetId in existingWidgets) {
                            Debug.WriteLine($"  {widgetId}");
                        }
                    }
                    using (var disposedEvent = manager.GetDisposedEvent()) {
                        disposedEvent.WaitOne();
                    }
                }
            }
        }
    }
}
