using Elorucov.Laney.Services.Common;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class PlacePicker : Modal {
        DelayedAction PlaceChangedAction;

        public PlacePicker() {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await Init(); })();
            PlaceChangedAction = new DelayedAction(async () => await SearchPlaces(), TimeSpan.FromSeconds(1));
        }

        private async Task Init() {
            var accessStatus = await Geolocator.RequestAccessAsync();
            switch (accessStatus) {
                case GeolocationAccessStatus.Allowed:
                    await ShowCurrentLocation();
                    break;
                case GeolocationAccessStatus.Denied:
                    LocationDisabledMessage.Visibility = Visibility.Visible;
                    break;
            }
            FindName(nameof(Map));
            Map.Center = new Geopoint(new BasicGeoposition() {
                Latitude = 59.93574, Longitude = 30.3259
            });
            AttachButton.IsEnabled = true;
        }

        private async Task ShowCurrentLocation() {
            Geolocator geolocator = new Geolocator();

            Geoposition pos = await geolocator.GetGeopositionAsync();
            if (pos != null) {
                FindName(nameof(Map));
                Map.Center = pos.Coordinate.Point;
            }
        }

        private void TrySearchPlaces(MapControl sender, object args) {
            positionCoords.Text = $"Lat: {Math.Round(Map.Center.Position.Latitude, 5)}\nLon: {Math.Round(Map.Center.Position.Longitude, 5)}";
            PlaceChangedAction.PrepareToExecute();
        }

        private async Task SearchPlaces() {
            var pos = Map.Center.Position;
            object resp = await Places.Search(pos.Latitude, pos.Longitude);
            if (resp is VKList<PlaceSearchResponse> places) {
                FoundPlacesList.ItemsSource = places.Items;
            } else {
                Functions.ShowHandledErrorTip(resp);
            }
        }

        private void AttachAndClose(object sender, RoutedEventArgs e) {
            Hide(Map.Center);
        }

        private void FoundPlacesList_ItemClick(object sender, ItemClickEventArgs e) {
            PlaceSearchResponse item = e.ClickedItem as PlaceSearchResponse;
            Geopoint point = new Geopoint(new BasicGeoposition() {
                Latitude = item.Place.Latitude, Longitude = item.Place.Longitude
            });
            // Hide(point);
            Map.Center = point;
        }
    }
}