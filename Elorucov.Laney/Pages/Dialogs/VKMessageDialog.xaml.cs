using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using System.Collections.Generic;
using System.Reflection;
using Windows.UI.Xaml;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class VKMessageDialog : Modal {
        public VKMessageDialog(LMessage message) {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            MessagesLists.Children.Add(MessageUIHelper.Build(message, null, null, true, true));
        }

        public VKMessageDialog(List<LMessage> messages) {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            foreach (LMessage msg in messages) {
                FrameworkElement msgui = MessageUIHelper.Build(msg, null, null, true, true);
                msgui.Margin = new Thickness(12, 0, 12, 12);
                MessagesLists.Children.Add(msgui);
            }
        }
    }
}
