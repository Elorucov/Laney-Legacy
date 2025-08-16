using System;
using VK.VKUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Helpers.UI
{
    public static class SnackbarHelper
    {
        public static void ShowOnCurrentWindow(this Snackbar snackbar, VerticalAlignment alignment = VerticalAlignment.Bottom)
        {
            Grid grid = null;

            Frame frame = Window.Current.Content as Frame;
            if (frame != null)
            {
                if (frame.Content is Grid fgrid) grid = fgrid;
                if (frame.Content is Page page && page.Content is Grid pgrid) grid = pgrid;
            }
            if (grid == null) throw new Exception("Unable to find a root grid!");

            snackbar.VerticalAlignment = alignment;

            Grid.SetColumnSpan(snackbar, grid.ColumnDefinitions.Count);
            Grid.SetRowSpan(snackbar, grid.RowDefinitions.Count);

            grid.Children.Add(snackbar);
            snackbar.Dismissed += (a, b) => grid.Children.Remove(snackbar);
            snackbar.Show();
        }
    }
}