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
        public virtual IObservable<StationAndAvailability> GetAvailability2(StationLocation station) { return null; }
        public virtual IObservable<StationAvailability> GetAvailability(StationLocation station) { return GetAvailability2(station).Select(a => a.Availability); }
        public virtual IObservable<List<StationLocation>> DownloadStations(string cityName) { return null; }

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
            CycloCityService.Instance, VeloService.Instance, NextBikeService.Instance, SambaService.Instance, BicingService.Instance
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
