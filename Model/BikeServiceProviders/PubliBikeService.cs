using System.Collections.Generic;
using System.Linq;
using System;
using System.Xml.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Net;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Text;
using SharpGIS;

namespace Bicikelj.Model
{
    public class PubliBikeService : BikeServiceProvider
    {
        public static PubliBikeService Instance = new PubliBikeService();

        private static string StationListUrl = "https://www.publibike.ch/en/stations.html";

        private static string GetStationDetailsUri(string stationId)
        {
            return string.Format("https://publicbikes.azure-mobile.net/api/stationstatus?serviceName=publibike&id={0}", stationId);
        }

        private static string GetStationListUri(string city)
        {
            return string.Format("https://publicbikes.azure-mobile.net/api/stationlist?serviceName=publibike&city={0}", city);
        }
        
        #region Internal classes
        
        [Flags]
        public enum BikeType
        {
            Normal,
            Electronic
        }

        public class Bike
        {
            public BikeType Type { get; set; }
            public int Available { get; set; }
        }

        public class BikeHolder
        {
            public BikeType Type { get; set; }
            public int Holders { get; set; }
            public int HoldersFree { get; set; }
        }

        public class Terminal
        {
            public string Name { get; set; }
            public string TerminalId { get; set; }
            public string Street { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public int Status { get; set; }
            public BikeHolder[] BikeHolders { get; set; }
            public Bike[] Bikes { get; set; }
        }

        public class AboService
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public int Status { get; set; }
            public Terminal[] Terminals { get; set; }
        }

        public class AboProvider
        {
            public string Key { get; set; }
            public AboService Abo { get; set; }
        }
        #endregion

        private static string DomainNameToCountry(string domain)
        {
            switch (domain.ToLower())
            {
                case "de": return "Germany";
                case "nz": return "New Zealand";
                case "at": return "Austria";
                case "ch": return "Switzerland";
                case "fr": return "France";
                case "pl": return "Poland";
                case "tr": return "Turkey";
                case "cy": return "Cyprus";
                case "hr": return "Croatia";
                default:
                    return domain;
            }
        }

        protected override IList<City> GetCities()
        {
            var result = new List<City>();
            string dataJson = "";

            var csr = App.GetResourceStream(new Uri("Assets/PubliBike-services.zip", UriKind.Relative)).Stream;
            var uz = new UnZipper(csr);
            using (var fs = uz.GetFileStream(uz.FileNamesInZip.FirstOrDefault()))
            {
                var sr = new StreamReader(fs);
                dataJson = sr.ReadToEnd();
            }
            
            var allServices = dataJson.FromJson<List<AboProvider>>();
            foreach (var abo in allServices)
            {
                var serviceName = abo.Abo.Name;
                var grouppedTerminals = from term in abo.Abo.Terminals group term by term.City into byCity select new { City = byCity.Key, Country = DomainNameToCountry(byCity.First().Country), Terminals = byCity.ToList() };
                foreach (var xcity in grouppedTerminals)
                {
                    // todo: find an existing city and append the service name; eg: "SwissPass, Bulle"
                    var city = (from c in result where string.Equals(c.CityName, xcity.City, StringComparison.InvariantCultureIgnoreCase) select c).FirstOrDefault();
                    if (city == null)
                    {
                        city = new City()
                        {
                            CityName = xcity.City,
                            Country = DomainNameToCountry(xcity.Country),
                            ServiceName = serviceName,
                            UrlCityName = xcity.City,
                            //UID = (string)xcity.Attribute("uid"),
                            Provider = Instance
                        };
                        if (string.IsNullOrEmpty(city.UrlCityName))
                            city.UrlCityName = city.CityName;
                        result.Add(city);
                    }
                    else if (city.ServiceName.IndexOf(serviceName, StringComparison.InvariantCultureIgnoreCase) < 0)
                        city.ServiceName += ", " + serviceName;
                }
            }
            return result;
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            var availability = GetAvailabilityFromCache(station);
            if (availability.Availability != null)
                return Observable.Return<StationAndAvailability>(availability);
            else
                return DownloadUrl.GetAsync<AzureService.StationInfo>(GetStationDetailsUri(station.Number.ToString()), AzureServiceCredentials.AuthHeaders)
                    .ObserveOn(ThreadPoolScheduler.Instance)
                    .Select(s =>
                    {
                        var sa = new StationAndAvailability(station, s.GetAvailability());
                        UpdateAvailabilityCacheItem(sa);
                        return sa;
                    });
        }

        public override IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            return DownloadUrl.GetAsync<List<AzureService.StationInfo>>(GetStationListUri(cityName), AzureServiceCredentials.AuthHeaders)
                .Select(s =>
                {
                    var sl = LoadStationsFromSI(s, cityName);
                    UpdateAvailabilityCache(sl);
                    return sl;
                });
        }

        private static List<StationAndAvailability> LoadStationsFromSI(List<AzureService.StationInfo> s, string cityName)
        {
            var result = s.Select(si =>
            {
                return new StationAndAvailability()
                {
                    Station = si.GetStation(),
                    Availability = si.GetAvailability()
                };
            }).ToList();
            return result;
        }
    }
}
