using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace Bicikelj.Model
{
    public class NextBikeService : BikeServiceProvider
    {
        public static NextBikeService Instance = new NextBikeService();

        private static string StationListUrl = "http://nextbike.net/maps/nextbike-official.xml";
        private static string StationInfoUrl = "https://nextbike.net/reservation/?action=check&format=json&start_place_id={0}";
        //private static string referer = "https://nextbike.net/reservation/?city_id_focused=0&height=400&maponly=1&language=de";

        private static List<StationLocation> LoadStationsFromXML(string stationsStr, string cityName)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            List<StationLocation> stations = null;
            XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
            foreach (var xcountry in doc.Descendants("country"))
            {
                var country = (string)xcountry.Attribute("domain");
                country = DomainNameToCountry(country);
                foreach (var xcity in xcountry.Descendants("city"))
                {
                    var cname = (string)xcity.Attribute("name");
                    if (!string.Equals(cname, cityName, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var city = new City();
                    city.CityName = (string)xcity.Attribute("name");
                    if (!string.IsNullOrEmpty(xcity.Attribute("alias").Value))
                        city.AlternateCityName = (string)xcity.Attribute("alias");
                    city.Country = country;
                    city.ServiceName = (string)xcountry.Attribute("name");
                    city.Provider = Instance;

                    stations = (from s in xcity.Descendants("place")
                                select new StationLocation
                                {
                                    Number = (int)s.Attribute("number"),
                                    Name = (string)s.Attribute("name"),
                                    //Address = (string)s.Attribute("address"),
                                    //FullAddress = (string)s.Attribute("fullAddress"),
                                    Latitude = (double)s.Attribute("lat"),
                                    Longitude = (double)s.Attribute("lng"),
                                    City = city.CityName
                                }).ToList();

                    city.Stations = stations;
                }
            }
            

            return stations;
        }

        private static string DomainNameToCountry(string domain)
        {
            switch (domain)
            {
                case "de": return "Germany";
                case "nz": return "New Zealand";
                case "at": return "Austria";
                case "la": return "Austria";
                case "ch": return "Switzerland";
                case "lv": return "Latvia";
                case "pl": return "Poland";
                case "nb": return "Germany";
                case "tr": return "Turkey";
                case "mr": return "Germany";
                case "cy": return "Cyprus";
                case "bb": return "Poland";
                case "pb": return "Poland";
                case "ob": return "Poland";
                case "az": return "Azerbaijan";
                case "vp": return "Poland";
                case "nk": return "Turkey";
                case "me": return "Dubai";
                case "hr": return "Croatia";
                case "sz": return "Germany";
                case "eg": return "Germany";
                default:
                    return domain;
            }
        }

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Leipzig", Country = "Germany", ServiceName = "nextbike Germany", UrlCityName = "leipzig", Provider = Instance },
                new City(){ CityName = "Frankfurt", Country = "Germany", ServiceName = "nextbike Germany", UrlCityName = "frankfurt", Provider = Instance },
                new City(){ CityName = "Berlin", Country = "Germany", ServiceName = "nextbike Germany", UrlCityName = "berlin", Provider = Instance }
            };
            return result;
        }

        public class NextBikeAvailability
        {
            public int Available { get; set; }
        }

        public override IObservable<List<StationLocation>> DownloadStations(string cityName)
        {
            return DownloadUrl.GetAsync(StationListUrl)
                .Select<string, List<StationLocation>>(s =>
                {
                    var sl = LoadStationsFromXML(s, cityName);
                    return sl;
                });
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            var url = string.Format(StationInfoUrl, station.Number);
            return DownloadUrl.GetAsync<NextBikeAvailability>(url, station)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(s => new StationAndAvailability(station, LoadAvailabilityFromNBA(s.Object)));
        }

        private StationAvailability LoadAvailabilityFromNBA(NextBikeAvailability nba)
        {
            return new StationAvailability()
            {
                Available = nba.Available
            };
        }
    }
}
