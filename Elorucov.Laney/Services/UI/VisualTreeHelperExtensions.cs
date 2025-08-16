using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services.UI {
    public static class VisualTreeHelperExtensions {
        public static ScrollViewer GetScrollViewerFromListView(this ListViewBase listView) {
            return FindControl<ScrollViewer>(listView);
        }

        public static T FindControl<T>(this UIElement parent) where T : FrameworkElement {
            if (parent == null)
                return null;

            if (parent.GetType() == typeof(T)) {
                return (T)parent;
            }
            T result = null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++) {
                UIElement child = (UIElement)VisualTreeHelper.GetChild(parent, i);

                if (FindControl<T>(child) != null) {
                    result = FindControl<T>(child);
                    break;
                }
            }
            return result;
        }

        public static T FindControlByName<T>(this FrameworkElement parent, string name) where T : FrameworkElement {
            if (parent == null)
                return null;

            if (parent.GetType() == typeof(T) && ((T)parent).Name == name) {
                return (T)parent;
            }
            T result = null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++) {
                FrameworkElement child = (FrameworkElement)VisualTreeHelper.GetChild(parent, i);

                if (FindControlByName<T>(child, name) != null) {
                    result = FindControl<T>(child);
                    break;
                }
            }
            return result;
        }
    }
}