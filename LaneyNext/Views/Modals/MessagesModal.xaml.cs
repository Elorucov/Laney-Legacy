using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using Elorucov.Toolkit.UWP.Controls;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MessagesModal : Modal
    {
        IEnumerable<MessageViewModel> Messages;

        public MessagesModal(IEnumerable<MessageViewModel> messages, string title)
        {
            this.InitializeComponent();
            Title = title;
            Messages = messages;
        }

        public MessagesModal(MessageViewModel message, string title)
        {
            this.InitializeComponent();
            Title = title;
            Messages = new List<MessageViewModel> { message };
        }

        private void Modal_Loaded(object sender, RoutedEventArgs e)
        {
            if (Messages != null && Messages.Count() > 0)
            {
                foreach (MessageViewModel mvm in Messages)
                {
                    Border b = new Border();
                    new MessageView(mvm, null, true, b, b.ActualWidth);
                    MessagesPanel.Children.Add(b);
                }
            }
        }
    }
}