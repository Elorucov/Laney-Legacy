using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Helpers
{
    public static class Extensions
    {
        public static ScrollViewer GetScrollViewer(this ListViewBase listviewbase)
        {
            try
            {
                return VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(listviewbase, 0), 0) as ScrollViewer;
            }
            catch
            {
                return null;
            }
        }

        public static T FindControl<T>(this UIElement parent) where T : FrameworkElement
        {
            if (parent == null)
                return null;

            if (parent.GetType() == typeof(T))
            {
                return (T)parent;
            }
            T result = null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                UIElement child = (UIElement)VisualTreeHelper.GetChild(parent, i);

                if (FindControl<T>(child) != null)
                {
                    result = FindControl<T>(child);
                    break;
                }
            }
            return result;
        }

        public static T GetControlByCoordinates<T>(this UIElement subtree, Point coordinates) where T : FrameworkElement
        {
            if (subtree == null)
                return null;

            var el = VisualTreeHelper.FindElementsInHostCoordinates(coordinates, subtree).ToList();
            var r = el.Where(u => u.GetType() == typeof(T));
            if (r.Count() > 0) return r.First() as T;
            return null;
        }

        public static T GetControlByCoordinates<T>(this UIElement subtree, Rect rect) where T : FrameworkElement
        {
            if (subtree == null)
                return null;

            var el = VisualTreeHelper.FindElementsInHostCoordinates(rect, subtree).ToList();
            var r = el.Where(u => u.GetType() == typeof(T));
            if (r.Count() > 0) return r.First() as T;
            return null;
        }

        public static void RegisterIncrementalLoadingEvent(this ScrollViewer scrollViewer, System.Action action, double distance = 256)
        {
            scrollViewer.ViewChanging += (a, b) =>
            {
                double h = scrollViewer.ScrollableHeight - distance;
                if (b.FinalView.VerticalOffset > h) action.Invoke();
            };
        }

        public static async Task ScrollIntoViewAsync(this ListView listView, object item, ScrollIntoViewAlignment alignment = ScrollIntoViewAlignment.Leading)
        {
            var tcs = new TaskCompletionSource<object>();
            var scrollViewer = listView.GetScrollViewer();

            EventHandler<object> layoutUpdated = (s1, e1) => tcs.TrySetResult(null);
            EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) =>
            {
                scrollViewer.LayoutUpdated += layoutUpdated;
                scrollViewer.UpdateLayout();
            };
            try
            {
                scrollViewer.ViewChanged += viewChanged;
                listView.ScrollIntoView(item, alignment);
                await tcs.Task;
            }
            finally
            {
                scrollViewer.ViewChanged -= viewChanged;
                scrollViewer.LayoutUpdated -= layoutUpdated;
            }
        }

        public static async Task<bool> ChangeViewAsync(this ScrollViewer scrollViewer, double? horizontalOffset, double? verticalOffset, bool disableAnimation)
        {
            var tcs = new TaskCompletionSource<object>();
            bool result = false;
            EventHandler<object> layoutUpdated = (s1, e1) => tcs.TrySetResult(null);
            EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) =>
            {
                scrollViewer.LayoutUpdated += layoutUpdated;
                scrollViewer.UpdateLayout();
            };
            try
            {
                scrollViewer.ViewChanged += viewChanged;
                result = scrollViewer.ChangeView(horizontalOffset, verticalOffset, null, disableAnimation);
                await tcs.Task;
            }
            finally
            {
                scrollViewer.ViewChanged -= viewChanged;
                scrollViewer.LayoutUpdated -= layoutUpdated;
            }
            return result;
        }

        public static string Combine(this List<int> items, char sym = ',')
        {
            return String.Join(sym.ToString(), items);
        }

        public static string Combine(this List<string> items, char sym = ',')
        {
            return String.Join(sym.ToString(), items);
        }

        public static string ToEnumMemberAttrValue(this Enum @enum)
        {
            var attr =
                @enum.GetType().GetMember(@enum.ToString()).FirstOrDefault()?.
                    GetCustomAttributes(false).OfType<EnumMemberAttribute>().
                    FirstOrDefault();
            if (attr == null)
                return @enum.ToString();
            return attr.Value;
        }

        public static string ToHumanizedDate(this DateTime dt)
        {
            dt = dt.ToLocalTime();
            DateTime dn = DateTime.Now;

            DateTime dayn = dn.Date;
            DateTime daym = dt.Date;

            if (dayn == daym)
            {
                return Locale.Get("today");
            }
            else if (daym == dayn.AddDays(-1))
            {
                return Locale.Get("yesterday");
            }
            else
            {
                return ((DateTime.Now.Year - dt.Year) < 1 ? $"{dt.ToString("M")}" : $"{dt.ToString("M")} {dt.Year}").ToLower();
            }
        }

        public static string ToTimeOrDate(this DateTime dt)
        {
            if (dt.Date == DateTime.Now.Date)
            {
                return dt.ToString(@"H\:mm");
            }
            else
            {
                return dt.ToHumanizedDate();
            }
        }

        public static string ToTimeAndDate(this DateTime dt)
        {
            if (dt.Date == DateTime.Now.Date)
            {
                return dt.ToString(@"H\:mm");
            }
            else
            {
                return $"{dt.ToHumanizedDate()} {Locale.Get("date_at")} {dt.ToShortTimeString()}";
            }
        }

        public static string ToNormalString(this TimeSpan time)
        {
            return time.Hours > 0 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
        }

        public static string Capitalize(this string text)
        {
            if (String.IsNullOrEmpty(text)) return null;
            return text[0].ToString().ToUpper() + text.Substring(1);
        }

        public static string ReplaceByRegex(this string str, Regex regex, string replace)
        {
            return regex.Replace(str, replace);
        }

        public static string ReplaceByRegex(this string str, string regex, string replace)
        {
            return new Regex(regex).Replace(str, replace);
        }

        public static Size ToWinSize(this ELOR.VKAPILib.Objects.Common.Size size)
        {
            return new Size(size.Width, size.Height);
        }

        public static List<Size> ToWinSize(this List<ELOR.VKAPILib.Objects.Common.Size> size)
        {
            List<Size> sizes = new List<Size>();
            size.ForEach(s => sizes.Add(new Size(s.Width, s.Height)));
            return sizes;
        }

        public static string ToFileSize(this decimal b)
        {
            if (b < 1024)
            {
                return $"{b} B";
            }
            if (b < 1048576)
            {
                return $"{Math.Round(b / 1024)} Kb";
            }
            if (b < 1073741824)
            {
                return $"{Math.Round(b / 1048576)} Mb";
            }
            return $"{Math.Round(b / 1073741824)} Gb";
        }

        public static string ToNormalString(this Message msg)
        {
            if (msg.IsExpired) return Locale.Get("msg_disappeared");
            if (String.IsNullOrEmpty(msg.Text) &&
                msg.Attachments.Count == 0 &&
                msg.ForwardedMessages.Count == 0 &&
                msg.Geo == null && msg.Action == null) return Locale.Get("empty_message");

            string str = null;

            // Attachments
            str += APIHelper.GetAttachmentsString(msg.Attachments);

            // Location
            if (msg.Geo != null) str += $"{Locale.Get("msg_attachment_geo")}, ";

            // Forwarded Messages
            if (msg.ForwardedMessages != null && msg.ForwardedMessages.Count > 0)
            {
                int c = msg.ForwardedMessages.Count;
                str += $"{c} {Locale.GetDeclension(c, "forwarded_msg_link").ToLower()}, ";
            }

            if (!String.IsNullOrEmpty(msg.Text))
            {
                if (!String.IsNullOrEmpty(str)) str = str.Capitalize();
                str += $"{VKTextParser.GetParsedText(msg.Text)}";
            }
            else
            {
                str = str != null && str.Length >= 2 ? str.Substring(0, str.Length - 2) : "";
                str = str.Capitalize();
            }

            return str;
        }

        public static string ToNormalString(this MessageViewModel msg)
        {
            if (msg.IsExpired) return Locale.Get("msg_disappeared");
            if (String.IsNullOrEmpty(msg.Text) &&
                msg.Attachments.Count == 0 &&
                msg.ForwardedMessages.Count == 0 &&
                msg.Location == null && msg.Action == null) return Locale.Get("empty_message");

            string str = null;

            // Attachments
            str += APIHelper.GetAttachmentsString(msg.Attachments);

            // Location
            if (msg.Location != null) str += $"{Locale.Get("msg_attachment_geo")}, ";

            // Forwarded Messages
            if (msg.ForwardedMessages != null && msg.ForwardedMessages.Count > 0)
            {
                int c = msg.ForwardedMessages.Count;
                str += $"{c} {Locale.GetDeclension(c, "forwarded_msg_link").ToLower()}, ";
            }

            if (!String.IsNullOrEmpty(msg.Text))
            {
                if (!String.IsNullOrEmpty(str)) str = str.Capitalize();
                str += $"{VKTextParser.GetParsedText(msg.Text)}";
            }
            else
            {
                str = str != null && str.Length >= 2 ? str.Substring(0, str.Length - 2) : "";
                str = str.Capitalize();
            }

            return str;
        }

        public static bool HasAttachments(this MessageViewModel msg, bool ignoreReplyMessage = false)
        {
            if (msg.Attachments.Count == 1 && msg.Attachments[0].Type == AttachmentType.Sticker) return false;
            return msg.Attachments.Count > 0 || msg.ForwardedMessages.Count > 0 || msg.Location != null || (!ignoreReplyMessage && msg.ReplyMessage != null);
        }

        public static bool HasSticker(this MessageViewModel msg)
        {
            var a = msg.Attachments.Where(q => q.Type == AttachmentType.Sticker);
            return a.Count() > 0;
        }

        public static bool HasGift(this MessageViewModel msg)
        {
            var a = msg.Attachments.Where(q => q.Type == AttachmentType.Gift);
            return a.Count() > 0;
        }

        public static bool IsPossibleToShowStoryControl(this MessageViewModel msg)
        {
            if (msg.ReplyMessage != null) return false;
            if (msg.ForwardedMessages.Count > 0) return false;
            if (!String.IsNullOrEmpty(msg.Text)) return false;
            if (msg.Attachments.Count == 0) return false;
            if (msg.Attachments.Count == 2 && msg.HasSticker())
            {
                var a = msg.Attachments.Where(q => q.Type == ELOR.VKAPILib.Objects.AttachmentType.Story);
                return a.Count() == 1;
            }
            if (msg.Attachments.Count == 1 && msg.Attachments[0].Type == AttachmentType.Story) return true;
            return false;
        }

        public static bool ContainsOnlyImage(this MessageViewModel msg)
        {
            if (msg.ReplyMessage != null) return false;
            if (msg.ForwardedMessages.Count > 0) return false;
            if (!String.IsNullOrEmpty(msg.Text)) return false;
            if (msg.Attachments.Count == 0) return false;
            if (msg.Attachments.Count == 1 && (msg.Attachments[0].Type == AttachmentType.Sticker ||
                msg.Attachments[0].Type == AttachmentType.Graffiti ||
                msg.Attachments[0].Type == AttachmentType.Photo ||
                msg.Attachments[0].Type == AttachmentType.Video ||
                (msg.Attachments[0].Type == AttachmentType.Document && msg.Attachments[0].Document.Preview != null))) return true;
            return false;
        }

        public static Size GetConstraintSize(this Video video, double max)
        {
            if (video.Width == video.Height) return new Size(max, max);
            bool isVertical = Math.Max(video.Width, video.Height) == video.Height;

            double width = isVertical ? (max / video.Height) * video.Width : max;
            double height = !isVertical ? (max / video.Width) * video.Height : max;
            return new Size(width, height);
        }

        public static void SortDescending<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector)
        {
            var sortedSource = source.OrderByDescending(keySelector).ToList();

            for (var i = 0; i < sortedSource.Count; i++)
            {
                var itemToSort = sortedSource[i];

                // If the item is already at the right position, leave it and continue.
                if (source.IndexOf(itemToSort) == i)
                {
                    continue;
                }

                source.Remove(itemToSort);
                source.Insert(i, itemToSort);
            }
        }

        public static Windows.UI.Color ParseFromHex(this string hex)
        {
            return UI.ColorHelper.ParseFromHex(hex);
        }

        public static Point ToPoint(this ClickableStickerAreaPoints csapoint)
        {
            return new Point(csapoint.X, csapoint.Y);
        }
    }
}
