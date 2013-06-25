using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Xml.Linq;
using Caliburn.Micro;

namespace Bicikelj.Model
{
    public class StationAndAvailability
    {
        public StationLocation Station { get; set; }
        public StationAvailability Availability { get; set; }

        public StationAndAvailability(StationLocation station, StationAvailability availability)
        {
            this.Station = station;
            this.Availability = availability;
        }
    }

    public static class StationLocationList
    {
        public static IObservable<StationAvailability> GetAvailability(StationLocation station)
        {
            return DownloadUrl.GetAsync(GetStationDetailsUri(station.City) + station.Number.ToString())
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(s => LoadAvailabilityFromXML(s));
        }

        public static IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            return DownloadUrl.GetAsync(GetStationDetailsUri(station.City) + station.Number.ToString())
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(s => new StationAndAvailability(station, LoadAvailabilityFromXML(s)));
        }

        public static string GetStationListUri(string city)
        {
            return string.Format("https://abo-{0}.cyclocity.fr/service/carto", city);
        }

        public static string GetStationDetailsUri(string city)
        {
            return string.Format("https://abo-{0}.cyclocity.fr/service/stationdetails/{0}/", city);
        }

        public static List<StationLocation> LoadStationsFromXML(string stationsStr, string city)
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
    }
}