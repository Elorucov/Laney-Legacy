using Elorucov.Laney.Core;
using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;

namespace Elorucov.Laney.Helpers
{
    public class LocationHelper
    {
        public static async Task<BasicGeoposition?> GetCurrentGeopositionAsync()
        {
            Log.General.Info("Checking access...");
            var accessStatus = await Geolocator.RequestAccessAsync();
            Log.General.Info(String.Empty, new ValueSet { { "status", accessStatus.ToString() } });
            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    Geolocator geolocator = new Geolocator();
                    Geoposition pos = await geolocator.GetGeopositionAsync();
                    if (pos != null)
                    {
                        Log.General.Info("Success");
                        return pos.Coordinate.Point.Position;
                    }
                    return null;
                default:
                    // TODO: Сообщение о запрете получении геолокации
                    return null;
            }
        }
    }
}
