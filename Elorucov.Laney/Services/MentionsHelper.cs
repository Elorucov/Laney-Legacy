using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Services {
    public class MentionsHelper {
        private static bool GetIsValidDomainSymbol(char c) {
            if ((c < 97 || c > 122) && (c < 65 || c > 90) && ((c < 1072 || c > 1103) && (c < 1040 || c > 1103)) && (c < 48 || c > 57))
                return c == 95;
            return true;
        }

        static Dictionary<int, MentionsPicker> pickers = new Dictionary<int, MentionsPicker>();
        public static MentionsPicker MentionsPicker { get { return GetMentionsPickerForCurrentView(); } }

        private static MentionsPicker GetMentionsPickerForCurrentView() {
            return pickers[ApplicationView.GetForCurrentView().Id];
        }

        public static void RegisterMentionsPickerForCurrentView(MentionsPicker picker) {
            if (pickers.ContainsKey(ApplicationView.GetForCurrentView().Id)) {
                pickers[ApplicationView.GetForCurrentView().Id] = picker;
            } else {
                pickers.Add(ApplicationView.GetForCurrentView().Id, picker);
            }
        }

        public static bool CheckMentions(RichEditBox tb, List<long> memberIds, List<User> users, List<Group> groups) {
            tb.Document.GetText(Windows.UI.Text.TextGetOptions.NoHidden | Windows.UI.Text.TextGetOptions.AdjustCrlf, out string text);
            var selection = tb.Document.Selection;

            if (text.Contains('@') || text.Contains('*')) {
                for (int index = text.Length - 1; index >= 0; --index) {
                    char mentionStartSymbol = text[index];
                    char ch1 = index != 0 ? text[index - 1] : char.MinValue;
                    char[] chArray = new char[13] { ' ', '.', ',', ':', ';', '\'', '"', '«', '»', '(', ')', '<', '>' };
                    if ((mentionStartSymbol == 64 || mentionStartSymbol == 42) && (ch1 == 0 || (chArray).Contains(ch1))) {
                        string source = text.Remove(0, index + 1);
                        char ch2 = source.FirstOrDefault(c => !GetIsValidDomainSymbol(c));
                        if (ch2 != 0) source = source.Remove(source.IndexOf(ch2));
                        int selectionStart = selection.StartPosition;
                        int selectionLength = selection.EndPosition - selection.StartPosition;
                        if (selectionLength > 0 || selectionStart <= index || selectionStart > index + source.Length + 1) return false;
                        if ((users != null && users.Count > 0) || (groups != null && groups.Count > 0) || (memberIds != null && memberIds.Count > 0)) {
                            List<MentionItem> pickerItems = GetPickerItems(memberIds, users, groups, source);
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

        private static List<MentionItem> GetPickerItems(List<long> memberIds, List<User> users, List<Group> groups, string q) {
            List<MentionItem> mentions = new List<MentionItem>();
            if (string.IsNullOrEmpty(q)) q = "";
            if (q == "") {
                q = q.ToLower();
                List<MentionItem> list = users.Where(u => memberIds.Contains(u.Id)).Select(u => new MentionItem(u.Id, u.ScreenName, u.FullName, u.Photo)).ToList();
                var groupsList = groups.Where(g => memberIds.Contains(g.Id * -1)).Select(g => new MentionItem(g.Id, g.ScreenName, g.Name, g.Photo));
                list.AddRange(groupsList);
                var myself = list.FirstOrDefault(h => h.Id == AppParameters.UserID);
                if (myself != null) list.Remove(myself);
                return list;
            }
            foreach (User user in users) {
                if (user.Id != AppParameters.UserID && memberIds.Contains(user.Id)) {
                    bool flag = false;
                    if (user.ScreenName.ToLower().StartsWith(q)) flag = true;
                    else if (user.FirstName.ToLower().StartsWith(q) || user.LastName.ToLower().StartsWith(q)) {
                        flag = true;
                    }
                    if (flag) mentions.Add(new MentionItem(user.Id, user.ScreenName, user.FullName, user.Photo));
                }
            }
            foreach (Group group in groups) {
                if (memberIds.Contains(group.Id * -1)) {
                    bool flag = false;
                    if (group.ScreenName.ToLower().StartsWith(q)) flag = true;
                    else if (group.Name.ToLower().StartsWith(q)) {
                        flag = true;
                    }
                    if (flag) mentions.Add(new MentionItem(group.Id, group.ScreenName, group.Name, group.Photo));
                }
            }
            return mentions;
        }

    }
}