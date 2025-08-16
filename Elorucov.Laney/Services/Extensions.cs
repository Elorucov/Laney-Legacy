using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Services {
    public static class Extensions {

        public static async Task ScrollIntoViewAsync(this ListView listView, object item, ScrollIntoViewAlignment alignment = ScrollIntoViewAlignment.Leading) {
            var tcs = new TaskCompletionSource<object>();
            var scrollViewer = listView.GetScrollViewerFromListView();

            EventHandler<object> layoutUpdated = (s1, e1) => tcs.TrySetResult(null);
            EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) => {
                scrollViewer.LayoutUpdated += layoutUpdated;
                scrollViewer.UpdateLayout();
            };
            try {
                scrollViewer.ViewChanged += viewChanged;
                listView.ScrollIntoView(item, alignment);
                await tcs.Task;
            } finally {
                scrollViewer.ViewChanged -= viewChanged;
                scrollViewer.LayoutUpdated -= layoutUpdated;
            }
        }

        public static string ToHumanizedDate(this DateTime dt) {
            dt = dt.ToLocalTime();
            DateTime dn = DateTime.Now;

            DateTime dayn = dn.Date;
            DateTime daym = dt.Date;

            if (dayn == daym) {
                return Locale.Get("msg_today");
            } else if (daym == dayn.AddDays(-1)) {
                return Locale.Get("msgprev_yesterday");
            } else {
                return ((DateTime.Now.Year - dt.Year) < 1 ? $"{dt.ToString("M")}" : $"{dt.ToString("M")} {dt.Year}").ToLower();
            }
        }

        public static string ToTimeOrDate(this DateTime dt) {
            if (dt.Date == DateTime.Now.Date) {
                return dt.ToShortTimeString();
            } else {
                return dt.ToHumanizedDate();
            }
        }

        public static string ToTimeAndDate(this DateTime dt) {
            if (dt.Date == DateTime.Now.Date) {
                return $"{Locale.Get("in")} {dt.ToShortTimeString()}";
            } else {
                return $"{dt.ToHumanizedDate()} {Locale.Get("in")} {dt.ToShortTimeString()}";
            }
        }

        public static string ToNormalString(this TimeSpan time) {
            return time.Hours > 0 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
        }

        public static string ToHumanizedString(this TimeSpan time) {
            string word = string.Empty;
            if (time.Days > 0) {
                word += $" {Locale.GetDeclensionForFormatSimple(time.Days, "day")}";
            }
            if (time.Hours > 0) {
                word += $" {Locale.GetDeclensionForFormatSimple(time.Hours, "hour")}";
            }
            if (time.Minutes > 0) {
                word += $" {Locale.GetDeclensionForFormatSimple(time.Minutes, "minute")}";
            }
            if (time.Seconds > 0) {
                word += $" {Locale.GetDeclensionForFormatSimple(time.Seconds, "second")}";
            }
            return word.Trim();
        }

        public static string Capitalize(this string text) {
            if (string.IsNullOrEmpty(text)) return null;
            return text[0].ToString().ToUpper() + text.Substring(1);
        }

        public static string TReplace(this string source, string template, string data) {
            if (data == null) data = string.Empty;
            return source.Replace("{{" + template + "}}", data);
        }
        public static bool HasAttachments(this LMessage msg, bool ignoreReplyMessage = false) {
            if (msg.Attachments.Count == 1 && msg.Attachments[0].Type == AttachmentType.Sticker) return false;
            return msg.Attachments.Count > 0 || msg.ForwardedMessages.Count > 0 ||
                msg.Geo != null || (!ignoreReplyMessage && msg.ReplyMessage != null) || (msg.Keyboard != null && msg.Keyboard.Inline);
        }

        public static bool HasSticker(this LMessage msg) {
            if (msg.Attachments.Count == 0) return false;
            var a = msg.Attachments.Where(q => q.Type == AttachmentType.Sticker || q.Type == AttachmentType.UGCSticker);
            return a.Count() > 0;
        }

        public static bool HasOnlyStandartSticker(this LMessage msg) {
            if (msg.Attachments.Count == 0) return false;
            var a = msg.Attachments.Where(q => q.Type == AttachmentType.Sticker);
            return a.Count() > 0;
        }

        public static bool HasOnlyGraffiti(this LMessage msg) {
            if (msg.Attachments.Count != 1) return false;
            var a = msg.Attachments.Where(q => q.Type == AttachmentType.Graffiti);
            return a.Count() == 1;
        }

        public static bool HasGift(this LMessage msg) {
            if (msg.Attachments.Count == 0) return false;
            var a = msg.Attachments.Where(q => q.Type == AttachmentType.Gift);
            return a.Count() > 0;
        }

        public static bool HasCall(this LMessage msg) {
            if (msg.Attachments.Count == 0) return false;
            var a = msg.Attachments.Where(q => q.Type == AttachmentType.Call || q.Type == AttachmentType.GroupCallInProgress);
            return a.Count() > 0;
        }

        public static bool IsPossibleToShowStoryControl(this LMessage msg) {
            if (msg.ReplyMessage != null) return false;
            if (msg.ForwardedMessages.Count > 0) return false;
            if (!string.IsNullOrEmpty(msg.Text)) return false;
            if (msg.Attachments.Count == 0) return false;
            if (msg.Attachments.Count == 2 && msg.HasSticker()) {
                var a = msg.Attachments.Where(q => q.Type == AttachmentType.Story);
                return a.Count() == 1;
            }
            if (msg.Attachments.Count == 1 && msg.Attachments[0].Type == AttachmentType.Story) return true;
            return false;
        }

        public static bool ContainsOnlyImage(this LMessage msg) {
            if (msg.ReplyMessage != null) return false;
            if (msg.ForwardedMessages.Count > 0) return false;
            if (!string.IsNullOrEmpty(msg.Text)) return false;
            if (msg.Attachments.Count == 0) return false;
            if (msg.Geo != null) return false;
            if (msg.Attachments.Count == 1 && (msg.Attachments[0].Type == AttachmentType.Sticker ||
                msg.Attachments[0].Type == AttachmentType.UGCSticker ||
                msg.Attachments[0].Type == AttachmentType.Graffiti ||
                msg.Attachments[0].Type == AttachmentType.Photo ||
                msg.Attachments[0].Type == AttachmentType.Video ||
                msg.Attachments[0].Type == AttachmentType.VideoMessage ||
                (msg.Attachments[0].Type == AttachmentType.Document && msg.Attachments[0].Document.Preview != null))) return true;
            return false;
        }

        public static bool ContainsSticker(this LMessage msg) {
            return msg.Attachments.Any(a => a.Type == AttachmentType.Sticker);
        }

        public static bool ContainsOnlyEmojis(this LMessage msg) {
            if (msg.ReplyMessage != null) return false;
            if (msg.ForwardedMessages.Count > 0) return false;
            if (msg.Attachments.Count > 0) return false;
            if (msg.Geo != null) return false;
            return msg.HasOnlyEmojis;
        }

        public static bool ContainsWidgets(this LMessage msg) {
            var a = msg.Attachments.Where(q => q.Type == AttachmentType.Widget);
            return a.Count() > 0;
        }

        public static bool CanDeleteForAll(this LMessage msg, long convId, ChatSettings settings) {
            try {
                // if (msg.Date.AddDays(1) <= DateTime.Now) return false;
                if (msg.SenderId == AppParameters.UserID && convId != AppParameters.UserID) return true;
                if (settings != null) {
                    if (settings.OwnerId == AppParameters.UserID &&
                    !settings.AdminIDs.Contains(msg.SenderId)) return true;
                }
            } catch (Exception ex) {
                Logger.Log.Error($"Error in {nameof(CanDeleteForAll)}: 0x{ex.HResult.ToString("x8")}");
            }
            return false;
        }

        public static bool CanEditMessage(this LMessage msg, LMessage pinned) {
            if (msg.HasCall() || msg.HasGift() || msg.HasSticker()) return false;
            if (msg.FromId == AppParameters.UserID && msg.PeerId == msg.FromId) return true;
            bool isNotOld = msg.Date.AddDays(1) > DateTime.Now;
            bool isPinned = msg.PinnedAt != null && msg.ConversationMessageId == pinned?.ConversationMessageId;
            return msg.FromId == AppParameters.UserID && (isNotOld || isPinned);
        }

        public static bool CanReply(this LMessage msg) {
            if (msg.Action != null) return false;
            if (msg.HasCall()) return false;
            return true;
        }

        public static bool TryGetMessageText(this LMessage msg, out string text) {
            text = msg.Text ?? string.Empty;
            int i = 1;
            if (msg.ForwardedMessages.Count > 0) {
                foreach (var m in msg.ForwardedMessages) {
                    text += $"\n\n[{Locale.Get("forwarded_msgs_link_nom")} ({i})]";
                    string stext = string.Empty;
                    if (m.TryGetMessageText(out stext)) text += $"\n{stext}";
                    i++;
                }
            }
            return !string.IsNullOrEmpty(text.Trim());
        }

        public static void Sort<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector) {
            var sortedSource = source.OrderBy(keySelector).ToList();

            for (var i = 0; i < sortedSource.Count; i++) {
                var itemToSort = sortedSource[i];

                // If the item is already at the right position, leave it and continue.
                if (source.IndexOf(itemToSort) == i) {
                    continue;
                }

                source.Remove(itemToSort);
                source.Insert(i, itemToSort);
            }
        }

        public static void SortDescending<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector) {
            var sortedSource = source.OrderByDescending(keySelector).ToList();

            for (var i = 0; i < sortedSource.Count; i++) {
                var itemToSort = sortedSource[i];

                // If the item is already at the right position, leave it and continue.
                if (source.IndexOf(itemToSort) == i) {
                    continue;
                }

                source.Remove(itemToSort);
                source.Insert(i, itemToSort);
            }
        }

        public static int GetFirstNonPinnedIndex(this ObservableCollection<LConversation> conversations) {
            if (conversations.Count == 0) return 0;
            var first = conversations.Where(c => c.SortId.MajorId == 0 || c.SortId.MajorId % 16 != 0).FirstOrDefault();
            return first != null ? conversations.IndexOf(first) : 0;
        }

        public static string ToEnumMemberAttrValue(this Enum @enum) {
            var attr =
                @enum.GetType().GetMember(@enum.ToString()).FirstOrDefault()?.
                    GetCustomAttributes(false).OfType<EnumMemberAttribute>().
                    FirstOrDefault();
            if (attr == null)
                return @enum.ToString();
            return attr.Value;
        }

        public static Entity ToEntity(this LConversation con, System.Action<object> extraButtonCommand = null) {
            string title = con.Title;
            if (con.Id.IsUser()) {
                var info = AppSession.GetNameAndAvatar(con.Id);
                if (info != null && !string.IsNullOrEmpty(info.Item1)) title = info.Item1;
            }
            var entity = new Entity(con.Id, title, null, con.Photo);
            if (extraButtonCommand != null) entity.ExtraButtonCommand = new RelayCommand(extraButtonCommand);
            return entity;
        }

        // Можно было бы прописать в классе APIHelper, но extensions проще и круче

        public static bool IsUser(this long id) {
            return (id >= 1 && id < 1.9e9) || (id >= 200e9 && id < 100e10);
        }

        public static bool IsGroup(this long id) {
            return id > -1e9 && id <= -1;
        }

        public static bool IsChat(this long id) {
            return id > 2e9 && id < 2e9 + 1e8;
        }

        public static bool IsContact(this long id) {
            return id >= 1.9e9 && id < 2e9;
        }
    }
}
