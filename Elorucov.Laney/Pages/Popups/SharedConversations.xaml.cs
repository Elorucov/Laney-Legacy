using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Popups {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SharedConversations : OverlayModal {
        long userId;

        public SharedConversations(Point position, long userId) {
            this.InitializeComponent();

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

            this.userId = userId;

            Window.Current.SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Loaded -= OnLoaded;
            Title.Text = Locale.Get("shared_chats");
            new System.Action(async () => { await GetConvs(); })();
        }

        private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e) {
            Window.Current.SizeChanged -= OnSizeChanged;
            Hide();
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e) {
            if (e.OriginalSource is ContentPresenter p && p.Name == "OverlayModalFrame") Hide();
        }

        private async Task GetConvs() {
            if (Loader.IsActive) return;
            Loader.IsActive = true;

            ObservableCollection<Entity> Conversations = new ObservableCollection<Entity>();


            var response = await Messages.GetSharedConversations(userId);
            if (response is VKList<Conversation> convs) {
                Loader.IsActive = false;
                Title.Text = $"{Locale.Get("shared_chats")} ({convs.Count})";

                foreach (Conversation conv in convs.Items) {
                    Uri avatar = conv.ChatSettings.Photo != null ? new Uri(conv.ChatSettings.Photo.Small) : null;

                    Conversations.Add(new Entity {
                        Id = conv.Peer.Id,
                        Title = conv.ChatSettings.Title,
                        Image = avatar
                    });
                }

                ConvsList.ItemsSource = Conversations;

                await Task.Delay(50);
                ConvsList.Focus(FocusState.Programmatic);
            } else {
                Functions.ShowHandledErrorDialog(response);
                Hide();
            }
        }

        private void OpenConv(object sender, ItemClickEventArgs e) {
            Entity entity = e.ClickedItem as Entity;
            Hide(entity.Id);
        }
    }
}