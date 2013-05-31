using System;
using System.Net;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Threading;
using Caliburn.Micro;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;

namespace Bicikelj.Model
{
    public class StationLocationIndex
    {
        public StationLocation Location;
        public int Index;

        public StationLocationIndex()
        {
        }

        public StationLocationIndex(StationLocation location, int index)
        {
            this.Location = location;
            this.Index = index;
        }
    }

    public enum StationListState
    {
        Empty,
        Updating,
        Ready
    }

    public class StationLocationList
    {
        private string stationsXML = "";
        public string StationsXML { get { return stationsXML; } }
        private StationListState state = StationListState.Empty;
        public StationListState State { get { return state; } set { state = value; } }
        public List<Action<IList<StationLocation>, Exception>> ListUpdatedOnce = new List<Action<IList<StationLocation>,Exception>>();

        private string city = "ljubljana";
        public string City {
            get { return city; } 
            set {
                if (value == city)
                    return;
                if (IsCitySupported(value))
                {
                    city = value;
                    state = StationListState.Empty;
                    stations = null;
                }
            } 
        }

        private LocationRect locationRect;
        public LocationRect LocationRect { get { return locationRect; } }

        private IList<StationLocation> stations;
        public IList<StationLocation> Stations { get { return stations; } set { SetStations(value); } }

        private void SetStations(IList<StationLocation> value, Exception ex = null)
        {
            this.stations = value;
            if (this.stations != null)
            {
                var locations = from station in stations
                                select new GeoCoordinate
                                {
                                    Latitude = station.Latitude,
                                    Longitude = station.Longitude
                                };
                locationRect = LocationRect.CreateLocationRect(locations);
            }
            state = stations == null ? StationListState.Empty : StationListState.Ready;
            NotifyChange(this.stations, ex);
        }

        public void GetStations(Action<IList<StationLocation>, Exception> result, bool forceUpdate = false)
        {
            if (stations == null || forceUpdate)
            {
                if (state != StationListState.Updating)
                    Download(result);
                else
                    ListUpdatedOnce.Add(result);
            }
            else
                result(stations, null);
        }

        public static string GetStationListUri(string city)
        {
            return string.Format("https://abo-{0}.cyclocity.fr/service/carto", city);
        }

        public static string GetStationDetailsUri(string city)
        {
            return string.Format("https://abo-{0}.cyclocity.fr/service/stationdetails/{0}/", city);
        }

        public static bool IsCitySupported(string city)
        {
            if (string.IsNullOrEmpty(city))
                return false;
            city = city.Split(',', ';', ' ')[0].ToLower();
            var allCities = BikeServiceProvider.GetAllCities();
            foreach (var x in allCities)
            {
                if (x.CityName.ToLower().Contains(city))
                    return true;
            }
            return false;
        }

        private void NotifyChange(IList<StationLocation> items, Exception ex)
        {
            if (ListUpdatedOnce == null)
                return;
            foreach (var act in ListUpdatedOnce)
                act(items, ex);
            ListUpdatedOnce.Clear();
        }

        private void Download(Action<IList<StationLocation>, Exception> result)
        {
            ListUpdatedOnce.Add(result);
            WebClient wc = new SharpGIS.GZipWebClient();
            wc.DownloadStringCompleted += (s, e) =>
                {
                    if (e.Cancelled)
                        NotifyChange(null, null);
                    else if (e.Error != null)
                        NotifyChange(null, e.Error);
                    else
                    {
                        ThreadPool.QueueUserWorkItem(o => {
                            var sl = LoadStationsFromXML(e.Result, city);
                            SetStations(sl, e.Error);
                        });
                    }
                };
            wc.DownloadStringAsync(new Uri(GetStationListUri(City)));
        }

        private static IList<StationLocation> LoadStationsFromXML(string stationsStr, string city)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
            var stations = (from s in doc.Descendants("marker")
                            select new StationLocation
                            {
                                Number = (int)s.Attribute("number"),
                                Name = (string)s.Attribute("name"),
                                Address = (string)s.Attribute("address"),
                                FullAddress = (string)s.Attribute("fullAddress"),
                                Latitude = (double)s.Attribute("lat"),
                                Longitude = (double)s.Attribute("lng"),
                                Open = (bool)s.Attribute("open"),
                                City = city
                            }).ToList();

            return stations;
        }

        public static StationAvailability LoadAvailabilityFromXML(string availabilityStr)
        {
            XDocument doc = XDocument.Load(new System.IO.StringReader(availabilityStr));
            var stations = from s in doc.Descendants("station")
                           select new StationAvailability
                           {
                               Available = (int)s.Element("available"),
                               Free = (int)s.Element("free"),
                               Total = (int)s.Element("total"),
                               Connected = (bool)s.Element("connected"),
                               Open = (bool)s.Element("open")
                           };
            StationAvailability sa = stations.FirstOrDefault();
            return sa;
        }

        public static void GetAvailability(StationLocation station, Action<StationLocation, StationAvailability, Exception> result)
        {
            WebClient wc = new SharpGIS.GZipWebClient();
            wc.DownloadStringCompleted += (s, e) =>
            {
                if (e.Cancelled)
                    result(station, null, null);
                else if (e.Error != null)
                    result(station, null, e.Error);
                else
                {
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        StationAvailability sa = LoadAvailabilityFromXML(e.Result);
                        result(station, sa, null);
                    });
                }
            };
            wc.DownloadStringAsync(new Uri(GetStationDetailsUri(station.City) + station.Number.ToString()));
        }

        public void SortByDistance(Action<IEnumerable<StationLocation>> callback)
        {
            if (stations == null)
                return;
            if (IoC.Get<SystemConfig>().LocationEnabled)
                LocationHelper.SortByLocation(stations, (r) =>
                {
                    this.stations = r.ToList();
                    if (callback != null)
                        callback(stations);
                });
            else
                if (callback != null)
                    callback(stations);
        }

        public IEnumerable<StationLocation> SortByLocation(GeoCoordinate location)
        {
            if (location == null || stations == null)
                return stations;
            var sortedStations = from station in stations
                                 orderby station.Coordinate.GetDistanceTo(location)
                                 select station;

            return sortedStations;
        }

        public IEnumerable<StationLocation> SortByLocation(GeoCoordinate location, GeoCoordinate location2)
        {
            if (location2 == null)
                return SortByLocation(location);
            if (stations == null)
                return null;
            
            var sortedStations = from station in stations
                                 orderby station.Coordinate.GetDistanceTo(location) * 2 + station.Coordinate.GetDistanceTo(location2)
                                 select station;

            return sortedStations;
        }
    }
}