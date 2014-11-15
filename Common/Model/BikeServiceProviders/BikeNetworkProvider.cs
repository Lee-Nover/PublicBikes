using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Bicikelj.Model
{
    public class BikeNetworkProvider : BikeServiceProvider
    {
        public List<City> Cities = new List<City>();

        private City FindCity(string cityName)
        {
            var city = Cities.Where(c => string.Equals(c.UrlCityName, cityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            return city;
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station, bool forceUpdate = false)
        {
            var city = FindCity(station.City);
            return city.Provider.GetAvailability2(station, forceUpdate);
        }

        public override IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            var stations = Cities.ToObservable()
                .SelectMany(c => c.Provider.DownloadStationsWithAvailability(c.UrlCityName));
            return stations;
        }
    }
}
