using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Pages.Popups {
    public sealed partial class WhoReadMessagePopup : OverlayModal {
        long peerId;
        int cmId;
        int count = 0;
        int max = 200;

        public WhoReadMessagePopup(Point position, long peerId, int cmId) {
            InitializeComponent();
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8)) {
                Root.Translation += new Vector3(0, 0, 32);
                Root.Shadow = new ThemeShadow();
            }

            double x = position.X;
            double y = position.Y;
            var bounds = Window.Current.Bounds;

            if (x > bounds.Width - Root.Width) x = bounds.Width - Root.Width - 12;
            if (y > bounds.Height - Root.Height) y = bounds.Height - Root.Height - 24;

            Root.Margin = new Thickness(x, y, 0, 0);

            this.peerId = peerId;
            this.cmId = cmId;

            Window.Current.SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Loaded -= OnLoaded;

            Title.Text = Locale.Get("loading");
            new System.Action(async () => { await LoadViewers(); })();
        }

        private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e) {
            Window.Current.SizeChanged -= OnSizeChanged;
            Hide();
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e) {
            if (e.OriginalSource is ContentPresenter p && p.Name == "OverlayModalFrame") Hide();
        }

        ObservableCollection<Entity> AllViewers = new ObservableCollection<Entity>();

        private async Task LoadViewers() {
            if (Loader.IsActive || (count > 0 && AllViewers.Count == count)) return;
            Loader.IsActive = true;

            var response = await Messages.WhoReadMessage(peerId, cmId, AllViewers.Count, max);
            if (response is VKList<long> viewers) {
                Loader.IsActive = false;
                count = viewers.TotalCount;
                Title.Text = String.Format(Locale.GetDeclensionForFormat(count, "views"), count);
                if (viewers.TotalCount > 20) FindName(nameof(ViewersSearchBox));

                foreach (long viewer in viewers.Items) {
                    if (viewer.IsUser()) {
                        var user = viewers.Profiles.Where(u => u.Id == viewer).FirstOrDefault();
                        if (user != null) {
                            AllViewers.Add(new Entity {
                                Id = viewer,
                                Title = user.FullName,
                                Image = new Uri(user.Photo100)
                            });
                        }
                    } else if (viewer.IsGroup()) {
                        var group = viewers.Groups.Where(g => g.Id == viewer * -1).FirstOrDefault();
                        if (group != null) {
                            AllViewers.Add(new Entity {
                                Id = viewer,
                                Title = group.Name,
                                Image = new Uri(group.Photo100)
                            });
                        }
                    }
                }

                if (viewers.TotalCount > max) Title.Text += $" ({Locale.Get("loaded").ToLower()}: {AllViewers.Count})";
                ViewersList.ItemsSource = AllViewers;
                if (AllViewers.Count == count) Loader.Visibility = Visibility.Collapsed;

                await Task.Delay(50);
                ViewersList.Focus(FocusState.Programmatic);
            } else {
                Functions.ShowHandledErrorDialog(response);
                Hide();
            }
        }

        private void OpenProfile(object sender, ItemClickEventArgs e) {
            Entity entity = e.ClickedItem as Entity;
            VKLinks.ShowPeerInfoModal(entity.Id);
            Hide();
        }

        private void GetScrollViewer(object sender, RoutedEventArgs e) {
            ListView listView = sender as ListView;
            listView.Loaded -= GetScrollViewer;
            Border border = VisualTreeHelper.GetChild(listView, 0) as Border;
            ScrollViewer scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;

            scrollViewer.ViewChanged += (a, b) => {
                if (b.IsIntermediate) {
                    ScrollViewer sv = a as ScrollViewer;

                    if (sv.VerticalOffset >= sv.ScrollableHeight - 72) new System.Action(async () => { await LoadViewers(); })();
                }
            };
        }

        private void DoSearch(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            if (!string.IsNullOrEmpty(args.QueryText)) {
                var found = AllViewers.Where(v => v.Title.ToLower().Contains(args.QueryText.ToLower())).ToList();
                ViewersList.ItemsSource = found;
            } else {
                ViewersList.ItemsSource = AllViewers;
            }
        }
    }
}