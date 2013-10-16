using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Bicikelj.Model
{
    public class BikeServiceProvider
    {
        public string ServiceName { get; set; }
        public string ServiceUrl { get; set; }
        protected virtual IList<City> GetCities() { return null; }
        public virtual IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName) { return null; }
        public virtual IObservable<StationAndAvailability> GetAvailability2(StationLocation station) { return null; }

        public virtual IObservable<List<StationLocation>> DownloadStations(string cityName)
        {
            return DownloadStationsWithAvailability(cityName)
                .Select(sl => sl.Select(sa => sa.Station).ToList());
        }
        public virtual IObservable<StationAvailability> GetAvailability(StationLocation station) {
            return GetAvailability2(station)
                .Select(a => a.Availability);
        }

        protected Dictionary<string, StationAvailability> AvailabilityCache = new Dictionary<string, StationAvailability>();
        protected void UpdateAvailabilityCache(IEnumerable<StationAndAvailability> list)
        {
            foreach (var item in list)
                UpdateAvailabilityCacheItem(item);
        }

        protected void UpdateAvailabilityCacheItem(StationAndAvailability item)
        {
            AvailabilityCache[item.Station.City + item.Station.Number.ToString()] = item.Availability;
        }

        protected StationAndAvailability GetAvailabilityFromCache(StationLocation item)
        {
            StationAndAvailability result = new StationAndAvailability(item, null);
            StationAvailability availability = null;
            if (AvailabilityCache.TryGetValue(item.City + item.Number.ToString(), out availability))
                result.Availability = availability;
            return result;
        }

        #region Static members
        private static List<City> allCities = null;
        public static IList<City> GetAllCities()
        {
            if (allCities == null)
            {
                allCities = new List<City>();
                foreach (var provider in providers)
                {
                    var cities = provider.GetCities();
                    if (cities != null && cities.Count > 0)
                        allCities.AddRange(cities);
                }
            }
            return allCities;
        }
        private static BikeServiceProvider[] providers = new BikeServiceProvider[] {
            CycloCityService.Instance, NextBikeService.Instance, SambaService.Instance, 
            ClearChannelService.Instance, ClearChannel2Service.Instance, BCycleService.Instance
        };

        public static City FindByCityName(string cityName)
        {
            City result = null;
            if (string.IsNullOrEmpty(cityName))
                return result;

            var allCities = GetAllCities();
            result = allCities.Where(c =>
                c.UrlCityName.Equals(cityName, StringComparison.InvariantCultureIgnoreCase)
                || c.CityName.Equals(cityName, StringComparison.InvariantCultureIgnoreCase)
                || (!string.IsNullOrEmpty(c.AlternateCityName) && cityName.ToLowerInvariant().Contains(c.AlternateCityName.ToLowerInvariant()))).FirstOrDefault();

            return result;
        }

        #endregion
    }
}
