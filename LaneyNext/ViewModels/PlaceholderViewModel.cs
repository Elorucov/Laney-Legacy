using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using VK.VKUI.Controls;
using Windows.UI.Xaml;

namespace Elorucov.Laney.ViewModels
{
    public class PlaceholderViewModel : BaseViewModel
    {
        private VKIconName _icon;
        private DataTemplate _iconTemplate;
        private string _header;
        private string _content;
        private string _actionButton;
        private RelayCommand _actionButtonCommand;
        private object _data;

        public VKIconName Icon { get { return _icon; } private set { _icon = value; OnPropertyChanged(); } }
        public DataTemplate IconTemplate { get { return _iconTemplate; } set { _iconTemplate = value; OnPropertyChanged("IconTemplate"); } }
        public string Header { get { return _header; } private set { _header = value; OnPropertyChanged(); } }
        public string Content { get { return _content; } private set { _content = value; OnPropertyChanged(); } }
        public string ActionButton { get { return _actionButton; } private set { _actionButton = value; OnPropertyChanged(); } }
        public RelayCommand ActionButtonCommand { get { return _actionButtonCommand; } private set { _actionButtonCommand = value; OnPropertyChanged(); } }
        public object Data { get { return _data; } private set { _data = value; OnPropertyChanged(); } }

        public static PlaceholderViewModel GetForException(Exception ex, Action action = null)
        {
            var err = ExceptionHelper.GetDefaultErrorInfo(ex);
            return new PlaceholderViewModel()
            {
                Data = ex,
                Icon = VKIconName.Icon56ErrorOutline,
                Header = err.Item1,
                Content = err.Item2,
                ActionButton = Locale.Get("retry"),
                ActionButtonCommand = action != null ? new RelayCommand(o => { action.Invoke(); }) : null,
            };
        }

        public static PlaceholderViewModel GetForContent(string content)
        {
            return new PlaceholderViewModel()
            {
                Content = content
            };
        }

        public static PlaceholderViewModel ForEmptyConversations
        {
            get
            {
                return new PlaceholderViewModel()
                {
                    Icon = VKIconName.Icon56MailOutline,
                    Content = Locale.Get("no_conversations")
                };
            }
        }

        public static PlaceholderViewModel ForEmptyUnreadConversations
        {
            get
            {
                return new PlaceholderViewModel()
                {
                    Icon = VKIconName.Icon56MessageReadOutline,
                    Content = Locale.Get("no_conversations_unread")
                };
            }
        }

        public static PlaceholderViewModel ForEmptyUnansweredConversations
        {
            get
            {
                return new PlaceholderViewModel()
                {
                    Icon = VKIconName.Icon56MailOutline,
                    Content = Locale.Get("no_conversations_unanswered")
                };
            }
        }

        public static PlaceholderViewModel ForEmptyImportantConversations
        {
            get
            {
                return new PlaceholderViewModel()
                {
                    Icon = VKIconName.Icon56MailOutline,
                    Content = Locale.Get("no_conversations_important")
                };
            }
        }

        public static PlaceholderViewModel ForEmptyConversation
        {
            get
            {
                return new PlaceholderViewModel()
                {
                    Icon = VKIconName.Icon56WriteOutline,
                    Content = Locale.Get("empty_conversation_default")
                };
            }
        }

        public static PlaceholderViewModel ForEmptyRestrictedConversation
        {
            get
            {
                return new PlaceholderViewModel()
                {
                    Icon = VKIconName.Icon56MailOutline,
                    Content = Locale.Get("empty_conversation_default_restricted")
                };
            }
        }

        public static PlaceholderViewModel ForEmptyCasperChat
        {
            get
            {
                return new PlaceholderViewModel()
                {
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon56CasperOutline"],
                    Content = Locale.Get("empty_conversation_casper")
                };
            }
        }
    }
}