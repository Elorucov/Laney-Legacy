using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels.Controls;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class PlacePickerViewModel : BaseViewModel
    {
        private Geopoint _currentPosition;
        private RelayCommand _attachCommand;

        public Geopoint CurrentPosition { get { return _currentPosition; } set { _currentPosition = value; OnPropertyChanged(); } }
        public RelayCommand AttachCommand { get { return _attachCommand; } set { _attachCommand = value; OnPropertyChanged(); } }

        Modal Modal;

        public PlacePickerViewModel(Modal modal)
        {
            Modal = modal;
            AttachCommand = new RelayCommand(o =>
            {
                var pos = CurrentPosition.Position;
                OutboundAttachmentViewModel oavm = new OutboundAttachmentViewModel(pos.Latitude, pos.Longitude);
                modal.Hide(oavm);
            });
            Init();
        }

        private async void Init()
        {
            await Task.Delay(100);
            CurrentPosition = new Geopoint(new BasicGeoposition
            {
                Latitude = 59.935637,
                Longitude = 30.3259
            });
            GetToCurrentLocation();
        }

        public async void GetToCurrentLocation()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    Geolocator geolocator = new Geolocator();
                    Geoposition pos = await geolocator.GetGeopositionAsync();
                    if (pos != null) CurrentPosition = pos.Coordinate.Point;
                    break;
                case GeolocationAccessStatus.Denied:
                    break;
                case GeolocationAccessStatus.Unspecified:
                    break;
            }
        }
    }
}
