using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using System.Device.Location;
using Caliburn.Micro;
using System.Reactive.Linq;

namespace Bicikelj.Model
{
    public class CityConfig
    {
        public TimeSpan RemindBefore { get; set; }
        public TimeSpan FreeRentTime { get; set; }
    }

    public class City
    {
        public string UID { get; set; }
        public string CityName { get; set; }
        public string Country { get; set; }
        public string ServiceName { get; set; }
        public string UrlCityName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [IgnoreDataMember]
        public double Radius { get; set; }
        [IgnoreDataMember]
        public GeoCoordinate Coordinate { get { return new GeoCoordinate(Latitude, Longitude); } }
        [IgnoreDataMember]
        public string AlternateCityName { get; set; }
        public List<StationLocation> Stations { get; set; }
        public List<FavoriteLocation> Favorites { get; set; }
        [IgnoreDataMember]
        public BikeServiceProvider Provider;

        public IObservable<List<StationLocation>> DownloadStations()
        {
            var analytics = IoC.Get<Bicikelj.Model.Analytics.IAnalyticsService>();
            analytics.LogTimedEvent("DownloadStations", new string[] {
                "City", CityName,
                "ServiceName", ServiceName,
                "Provider", Provider.ServiceName
            });
            return Provider.DownloadStations(UrlCityName)
                .Do(sl => {
                    analytics.EndTimedEvent("DownloadStations");
                });
        }
    }
}
