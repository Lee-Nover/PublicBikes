using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using System.Device.Location;

namespace Bicikelj.Model
{
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
        public GeoCoordinate Coordinate { get { return new GeoCoordinate(Latitude, Longitude); } }
        [IgnoreDataMember]
        public string AlternateCityName { get; set; }
        public List<StationLocation> Stations { get; set; }
        public List<FavoriteLocation> Favorites { get; set; }
        [IgnoreDataMember]
        public BikeServiceProvider Provider;

        public IObservable<List<StationLocation>> DownloadStations()
        {
            return Provider.DownloadStations(UrlCityName);
        }
    }
}
