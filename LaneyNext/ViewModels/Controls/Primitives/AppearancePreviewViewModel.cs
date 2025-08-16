using Elorucov.Laney.Helpers;

namespace Elorucov.Laney.ViewModels.Controls.Primitives
{
    public class AppearancePreviewViewModel : BaseViewModel
    {
        private MessagesCollection _messages;

        public MessagesCollection Messages { get { return _messages; } set { _messages = value; OnPropertyChanged(); } }

        public AppearancePreviewViewModel()
        {
            Messages = MessageViewModel.GetSampleMessages();
        }
    }
}
