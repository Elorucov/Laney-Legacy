using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters.ConversationsList {
    public class InlineMessage : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value != null && value is LMessage msg) {
                string username = "";
                if (msg.Action == null) {
                    if (msg.SenderId != AppParameters.UserID) {
                        if (msg.PeerId.IsChat()) {
                            if (msg.SenderId.IsUser()) {
                                username = $"{msg.SenderName.Split(' ')[0]}: ";
                            } else if (msg.SenderId.IsGroup()) {
                                username = $"{msg.SenderName}: ";
                            }
                        }
                    } else {
                        if (msg.PeerId != AppParameters.UserID) username = $"{Locale.Get("you")}: ";
                    }
                }

                return username + msg.ToString();
            }
            return "...";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}