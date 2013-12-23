using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Bicikelj.Model
{
    public class CachedAvailability
    {
        public StationAvailability Availability;
        public DateTime? LastUpdate;
        
        public void Update(StationAvailability availability)
        {
            this.Availability = availability;
            this.LastUpdate = DateTime.Now;
        }

        public bool IsOutdated(TimeSpan maxAge)
        {
            if (!LastUpdate.HasValue) return true;
            return DateTime.Now - LastUpdate.Value > maxAge;
        }
    }

    public class BikeServiceProvider
    {
        public string ServiceName { get; set; }
        public string ServiceUrl { get; set; }
        protected virtual IList<City> GetCities() { return null; }
        public virtual IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName) { return null; }
        public virtual IObservable<StationAndAvailability> GetAvailability2(StationLocation station) { return null; }
        public TimeSpan MaxCacheAge = TimeSpan.FromSeconds(60);

        public virtual IObservable<List<StationLocation>> DownloadStations(string cityName)
        {
            return DownloadStationsWithAvailability(cityName)
                .Select(sl => sl.Select(sa => sa.Station).ToList());
        }

        public virtual IObservable<StationAvailability> GetAvailability(StationLocation station) {
            return GetAvailability2(station)
                .Select(a => a.Availability);
        }

        protected Dictionary<string, CachedAvailability> AvailabilityCache = new Dictionary<string, CachedAvailability>();
        protected void UpdateAvailabilityCache(IEnumerable<StationAndAvailability> list)
        {
            foreach (var item in list)
                UpdateAvailabilityCacheItem(item);
        }

        protected void UpdateAvailabilityCacheItem(StationAndAvailability item)
        {
            CachedAvailability availability = null;
            if (AvailabilityCache.TryGetValue(item.Station.City + item.Station.Number.ToString(), out availability))
                availability.Update(item.Availability);
            else
            {
                availability = new CachedAvailability() { Availability = item.Availability, LastUpdate = DateTime.Now };
                AvailabilityCache[item.Station.City + item.Station.Number.ToString()] = availability;
            }    
        }

        protected StationAndAvailability GetAvailabilityFromCache(StationLocation station)
        {
            StationAndAvailability result = new StationAndAvailability(station, null);
            CachedAvailability availability = null;
            if (AvailabilityCache.TryGetValue(station.City + station.Number.ToString(), out availability) && !availability.IsOutdated(MaxCacheAge))
                result.Availability = availability.Availability;
            return result;
        }

        public bool IsAvailabilityValid(StationLocation station)
        {
            CachedAvailability availability;
            return (AvailabilityCache.TryGetValue(station.City + station.Number.ToString(), out availability) && !availability.IsOutdated(MaxCacheAge));
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
            ClearChannelService.Instance, ClearChannel2Service.Instance, BCycleService.Instance,
            BixiService.Instance, PubliBikeService.Instance, SmartBikeService.Instance
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
