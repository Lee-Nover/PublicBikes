using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Device.Location;
using System.Xml.Linq;
using Caliburn.Micro;
using System.Net;

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
            if (list == null) return;
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
                /*ExportCityCoordinates();*/
                UpdateCityCoordinates();
            }
            return allCities;
        }

        private static bool AreCoordinatesOk(double latitude, double longitude)
        {
            var lat = (int)latitude;
            var lng = (int)longitude;
            return !(lat == 0 && lat == 180 || lat == 180 && lng == 0);
        }

        private static void UpdateCityCoordinates()
        {
            List<StationAndAvailability> stations = new List<StationAndAvailability>();
            var doc = XDocument.Load("Assets/city-coordinates.xml");
            foreach (var xcountry in doc.Descendants("country"))
            {
                var country = (string)xcountry.Attribute("name");
                foreach (var xcity in xcountry.Descendants("city"))
                {
                    var cname = (string)xcity.Attribute("name");
                    var city = allCities.Where(c =>
                        string.Equals(country, c.Country, StringComparison.InvariantCultureIgnoreCase) &&
                        string.Equals(cname, c.CityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (city != null && AreCoordinatesOk(city.Latitude, city.Longitude))
                    {
                        city.Latitude = (double)xcity.Attribute("lat");
                        city.Longitude = (double)xcity.Attribute("lng");
                        city.Radius = (int)xcity.Attribute("radius");
                    }
                    else if (city == null)
                    {
                        Console.WriteLine("city not found: " + cname + ", " + country);
                    }
                }
            }
        }

        public static void ExportCityCoordinates()
        {
            var log = new DebugLog(typeof(BikeServiceProvider));
            foreach (var city in GetAllCities())
            {
                if (city.Latitude != 0 && city.Longitude != 0)
                    continue;

                List<StationLocation> _stations = null;
                try
                {
                    _stations = city.Provider.DownloadStations(city.UrlCityName).Retry(1).Wait();
                }
                catch (WebExceptionEx e)
                {
                    log.Warn("Service: {0}, City: {1}, Error: {2}\n{3}", city.Provider.ServiceName, city.UrlCityName, e.URL, e.ToString());
                    _stations = null;
                }
                catch (WebException e)
                {
                    log.Warn("Service: {0}, City: {1}, Error: {2}", city.Provider.ServiceName, city.UrlCityName, e.ToString());
                    _stations = null;
                }
                catch (Exception e)
                {
                    log.Warn("Service: {0}, City: {1}, Error: {2}", city.Provider.ServiceName, city.UrlCityName, e.ToString());
                    _stations = null;
                }
                if (_stations == null)
                    continue;
                _stations = FilterInvalidStations(_stations);
                var stationsArea = LocationHelper.GetLocationRect(_stations);
                city.Latitude = stationsArea.Center.Latitude;
                city.Longitude = stationsArea.Center.Longitude;
                city.Radius = (stationsArea.Center.GetDistanceTo(stationsArea.Northeast)
                    + stationsArea.Center.GetDistanceTo(stationsArea.Northwest)
                    + stationsArea.Center.GetDistanceTo(stationsArea.Southeast)
                    + stationsArea.Center.GetDistanceTo(stationsArea.Southwest)) / 4;
            }
            SaveCityCoordinates(allCities);
        }

        private static List<StationLocation> FilterInvalidStations(List<StationLocation> _stations)
        {
            if (_stations.Count < 3) return _stations;
            var latGroups = _stations.GroupBy(s => (int)s.Latitude).OrderBy(g => g.Count());
            var lngGroups = _stations.GroupBy(s => (int)s.Longitude).OrderBy(g => g.Count());
            var lat = latGroups.Last().Average(s => s.Latitude);
            var lng = lngGroups.Last().Average(s => s.Longitude);
            var avgPoint = new GeoCoordinate(lat, lng);
            _stations = _stations.Where(s => s.Coordinate.GetDistanceTo(avgPoint) < 30000).ToList();
            return _stations;
        }

        private static void SaveCityCoordinates(List<City> allCities)
        {
            var doc = new XDocument();
            XElement xcountries = new XElement("countries");
            XElement xcountry = null;
            XElement xcity = null;
            doc.Add(xcountries);
            var country = "";
            foreach (var item in allCities.OrderBy(city => city.Country))
            {
                if (!string.Equals(country, item.Country, StringComparison.InvariantCultureIgnoreCase))
                {
                    country = item.Country;
                    xcountry = new XElement("country");
                    xcountry.SetAttributeValue("name", country);
                    xcountries.Add(xcountry);
                }
                xcity = new XElement("city");
                xcity.SetAttributeValue("name", item.CityName);
                xcity.SetAttributeValue("lat", item.Latitude);
                xcity.SetAttributeValue("lng", item.Longitude);
                xcity.SetAttributeValue("radius", (int)item.Radius);
                xcountry.Add(xcity);
            }
            var result = doc.ToString();
        }

        private static BikeServiceProvider[] providers = new BikeServiceProvider[] {
            CycloCityService.Instance, NextBikeService.Instance, SambaService.Instance, 
            ClearChannelService.Instance, SmartBikeService.Instance, BCycleService.Instance,
            BixiService.Instance, PubliBikeService.Instance
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

        public static City FindNearestCity(GeoCoordinate position, int maxDistanceKm)
        {
            City result = null;

            var allCities = GetAllCities();
            result = allCities
                .Where(city => city.Coordinate.GetDistanceTo(position) <= maxDistanceKm * 1000)
                .OrderBy(city => city.Coordinate.GetDistanceTo(position))
                .FirstOrDefault();

            return result;
        }

        #endregion
    }
}
