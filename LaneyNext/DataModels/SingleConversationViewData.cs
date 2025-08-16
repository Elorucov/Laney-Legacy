using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels;

namespace Elorucov.Laney.DataModels
{
    public enum SingleConversationViewMode
    {
        Default = 0, ContactPanel = 1
    }

    public class SingleConversationViewData
    {
        public VKSession Session { get; private set; }
        public ConversationViewModel Conversation { get; private set; }
        public SingleConversationViewMode ViewMode { get; private set; }

        public SingleConversationViewData(VKSession session, ConversationViewModel conversation, SingleConversationViewMode viewMode = SingleConversationViewMode.Default)
        {
            Session = session;
            Conversation = conversation;
            ViewMode = viewMode;
        }
    }
}
