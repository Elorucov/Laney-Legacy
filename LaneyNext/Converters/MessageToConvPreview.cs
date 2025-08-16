using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Converters
{
    public class MessageToConvPreview : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is MessageViewModel msg)
            {
                ConversationViewModel ci = VKSession.Current.SessionBase.Conversations.Where(c => c.Id == msg.PeerId).FirstOrDefault();
                if (msg != null)
                {
                    string sender = "";
                    int currentId = VKSession.Current.SessionId;
                    if (msg.PeerId > 2000000000)
                    {
                        if (msg.Action != null) return new ActionMessage(msg.Action, msg.SenderId).ToString();
                        sender = msg.SenderId == currentId ? $"{Locale.Get("you")}: " : $"{msg.SenderName.Split(' ')[0]}: ";
                    }
                    else
                    {
                        sender = msg.SenderId == currentId && msg.PeerId != currentId ? $"{Locale.Get("you")}: " : "";
                    }
                    return $"{sender}{msg.ToNormalString()}";
                }
                else
                {
                    return ci != null && ci.ChatSettings != null && ci.ChatSettings.IsDisappearing ? "Messages disappeared" : "No messages";
                }
            }
            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
