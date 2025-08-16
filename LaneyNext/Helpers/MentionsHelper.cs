using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Controls;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Helpers
{
    public class MentionsHelper
    {
        private static bool GetIsValidDomainSymbol(char c)
        {
            if ((c < 97 || c > 122) && (c < 65 || c > 90) && ((c < 1072 || c > 1103) && (c < 1040 || c > 1103)) && (c < 48 || c > 57))
                return c == 95;
            return true;
        }

        static Dictionary<CoreApplicationView, MentionsPicker> pickers = new Dictionary<CoreApplicationView, MentionsPicker>();
        public static MentionsPicker MentionsPicker { get { return GetMentionsPickerForCurrentView(); } }

        private static MentionsPicker GetMentionsPickerForCurrentView()
        {
            return pickers[CoreApplication.GetCurrentView()];
        }

        public static void RegisterMentionsPickerForCurrentView(MentionsPicker picker)
        {
            if (pickers.ContainsKey(CoreApplication.GetCurrentView()))
            {
                pickers[CoreApplication.GetCurrentView()] = picker;
            }
            else
            {
                pickers.Add(CoreApplication.GetCurrentView(), picker);
            }
        }

        public static bool CheckMentions(TextBox tb, List<User> users, List<Group> groups)
        {
            string text = tb.Text;
            if (text.Contains('@') || text.Contains('*'))
            {
                for (int index = text.Length - 1; index >= 0; --index)
                {
                    char mentionStartSymbol = text[index];
                    char ch1 = index != 0 ? text[index - 1] : char.MinValue;
                    char[] chArray = new char[13] { ' ', '.', ',', ':', ';', '\'', '"', '«', '»', '(', ')', '<', '>' };
                    if ((mentionStartSymbol == 64 || mentionStartSymbol == 42) && (ch1 == 0 || ((IEnumerable<char>)chArray).Contains(ch1)))
                    {
                        string source = text.Remove(0, index + 1);
                        char ch2 = source.FirstOrDefault(c => !GetIsValidDomainSymbol(c));
                        if (ch2 != 0) source = source.Remove(source.IndexOf(ch2));
                        int selectionStart = tb.SelectionStart;
                        if (tb.SelectionLength > 0 || selectionStart <= index || selectionStart > index + source.Length + 1) return false;
                        if ((users != null && users.Count > 0) || (groups != null && groups.Count > 0))
                        {
                            List<Entity> pickerItems = GetPickerItems(users, groups, source);
                            if (pickerItems.Count == 0) return false;
                            MentionsPicker.Mentions = pickerItems;
                            MentionsPicker.SearchDomain = $"{mentionStartSymbol}{source}";
                            MentionsPicker.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static List<Entity> GetPickerItems(List<User> users, List<Group> groups, string q)
        {
            List<Entity> mentions = new List<Entity>();
            if (String.IsNullOrEmpty(q)) q = "";
            if (q == "")
            {
                q = q.ToLower();
                List<Entity> list = users.Select(u => new Entity(u.Id, u.FullName, u.Domain, u.Photo)).ToList();
                list.Remove(list.First(h => h.Id == VKSession.Current.SessionId));
                return list;
            }
            foreach (User user in users)
            {
                if (user.Id != VKSession.Current.SessionId)
                {
                    bool flag = false;
                    if (user.Domain.ToLower().StartsWith(q)) flag = true;
                    else if (user.FirstName.ToLower().StartsWith(q) || user.LastName.ToLower().StartsWith(q))
                    {
                        flag = true;
                    }
                    if (flag) mentions.Add(new Entity(user.Id, user.FullName, user.Domain, user.Photo));
                }
            }
            foreach (Group group in groups)
            {
                bool flag = false;
                if (group.ScreenName.ToLower().StartsWith(q)) flag = true;
                else if (group.Name.ToLower().StartsWith(q))
                {
                    flag = true;
                }
                if (flag) mentions.Add(new Entity(group.Id, group.Name, group.ScreenName, group.Photo));
            }
            return mentions;
        }
    }
}
