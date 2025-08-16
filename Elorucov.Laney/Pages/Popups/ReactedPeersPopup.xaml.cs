using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Popups {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReactedPeersPopup : OverlayModal {
        long peerId;
        int cmId;

        public ReactedPeersPopup(Point position, long peerId, int cmId) {
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
            new System.Action(async () => { await GetReactedPeers(); })();
        }

        private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e) {
            Window.Current.SizeChanged -= OnSizeChanged;
            Hide();
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e) {
            if (e.OriginalSource is ContentPresenter p && p.Name == "OverlayModalFrame") Hide();
        }

        //


        private async Task GetReactedPeers() {
            var response = await Messages.GetReactedPeers(peerId, cmId);
            if (response is GetReactedPeersResponse grpr) {
                // id, count, members
                List<Tuple<int, int, List<Entity>>> tabs = new List<Tuple<int, int, List<Entity>>> {
                    new Tuple<int, int, List<Entity>>(0, grpr.Count, GetEntities(grpr.Reactions, grpr.Profiles, grpr.Groups))
                };

                var groups = grpr.Reactions.GroupBy(rp => rp.ReactionId).ToList();
                if (groups.Count > 1) {
                    foreach (var group in groups) {
                        int count = grpr.Counters.Where(r => r.ReactionId == group.Key).FirstOrDefault().Count;
                        tabs.Add(new Tuple<int, int, List<Entity>>(group.Key, count, GetEntities(group.ToList(), grpr.Profiles, grpr.Groups, true)));
                    }
                }

                MainPivot.ItemsSource = tabs;
                await Task.Delay(50);
                MainPivot.Focus(FocusState.Programmatic);
            } else {
                Functions.ShowHandledErrorDialog(response);
                Hide();
            }
        }

        private List<Entity> GetEntities(List<ReactedPeer> reactedPeers, List<User> users, List<Group> groups, bool dontAddReactionIcon = false) {
            List<Entity> entites = new List<Entity>();

            foreach (var rp in reactedPeers) {
                object rid = null;
                if (!dontAddReactionIcon) rid = rp.ReactionId;
                if (rp.UserId.IsUser()) {
                    User user = users.Where(u => u.Id == rp.UserId).FirstOrDefault();
                    if (user != null) entites.Add(new Entity {
                        Id = user.Id,
                        Title = user.FullName,
                        Image = user.Photo,
                        Object = rid
                    });
                } else if (rp.UserId.IsGroup()) {
                    Group group = groups.Where(g => g.Id == rp.UserId * -1).FirstOrDefault();
                    if (group != null) entites.Add(new Entity {
                        Id = group.Id * -1,
                        Title = group.Name,
                        Image = group.Photo,
                        Object = rid
                    });
                }
            }

            return entites;
        }

        private void OnEntityDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            ContentPresenter cp = sender as ContentPresenter;
            Entity context = args.NewValue as Entity;
            if (context == null || context.Object == null || (int)context.Object == 0) {
                cp.Visibility = Visibility.Collapsed;
                return;
            }
            int reactionId = (int)context.Object;
            cp.Visibility = Visibility.Visible;

            cp.Content = new Image {
                Width = 22, Height = 22,
                Stretch = Stretch.Uniform,
                Source = new SvgImageSource {
                    UriSource = Reaction.GetImagePathById(reactionId)
                }
            };
        }

        private void OpenProfile(object sender, ItemClickEventArgs e) {
            Entity entity = e.ClickedItem as Entity;
            VKLinks.ShowPeerInfoModal(entity.Id);
            Hide();
        }
    }
}