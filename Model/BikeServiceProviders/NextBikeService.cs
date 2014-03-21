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

        private static string StationListUrl = "http://nextbike.net/maps/nextbike-official.xml?city={0}";
        private static string StationInfoUrl = "https://nextbike.net/reservation/?action=check&format=json&start_place_id={0}";
        //private static string referer = "https://nextbike.net/reservation/?city_id_focused=0&height=400&maponly=1&language=de";

        private static List<StationAndAvailability> LoadStationsFromXML(string stationsStr, string cityName)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            List<StationAndAvailability> stations = new List<StationAndAvailability>();
            XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
            foreach (var xcountry in doc.Descendants("country"))
            {
                var country = (string)xcountry.Attribute("domain");
                country = DomainNameToCountry(country);
                foreach (var xcity in xcountry.Descendants("city"))
                {
                    var cname = (string)xcity.Attribute("name");
                    var alias = (string)xcity.Attribute("alias");
                    if (!string.Equals(cname, cityName, StringComparison.InvariantCultureIgnoreCase)
                        && !string.Equals(alias, cityName, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    foreach (var place in xcity.Descendants("place"))
                    {
                        var station = new StationLocation();
                        station.City = cityName;
                        station.Number = (int)place.Attribute("uid");
                        station.Name = (string)place.Attribute("name");
                        station.Latitude = (double)place.Attribute("lat");
                        station.Longitude = (double)place.Attribute("lng");

                        var availability = new StationAvailability();
                        var availStr = (string)place.Attribute("bikes");
                        if (availStr.EndsWith("+"))
                            availStr = availStr.Remove(availStr.Length - 1);
                        availability.Available = int.Parse(availStr);
                        var racksAttr = place.Attribute("bike_racks");
                        if (racksAttr != null)
                            availability.Total = (int)racksAttr;
                        if (availability.Total >= availability.Available)
                            availability.Free = availability.Total - availability.Available;
                        else
                            availability.Free = 1;
                        // todo find out when a station is open
                        availability.Open = true;

                        stations.Add(new StationAndAvailability(station, availability));
                    }
                }
            }

            return stations;
        }

        private static string DomainNameToCountry(string domain)
        {
            switch (domain)
            {
                // germany
                case "de":
                case "nb":
                case "sz":
                case "eg":
                case "mr": return "Germany";
                // austria
                case "at":
                case "la": return "Austria";
                case "ch": return "Switzerland";
                case "lv": return "Latvia";
                // poland
                case "pl":
                case "bb":
                case "pb":
                case "ob":
                case "kp":
                case "vp":
                case "tp": return "Poland";
                // turkey
                case "nk":
                case "tr": return "Turkey";
                // others
                case "nz": return "New Zealand";
                case "cy": return "Cyprus";
                case "az": return "Azerbaijan";
                case "me": return "United Arab Emirates";
                case "hr": return "Croatia";
                case "uk": return "United Kingdom";
                case "bg": return "Bolgaria";
                case "mb": return "Hungary";
                default:
                    return domain;
            }
        }

        protected override IList<City> GetCities()
        {
            var result = new List<City>();
            // todo: get the list of cities from the xml file
            
            var doc = XDocument.Load("Assets/nextbike-cities.xml");
            foreach (var xctry in doc.Descendants("country"))
            {
                var country = DomainNameToCountry((string)xctry.Attribute("domain"));
                var serviceName = (string)xctry.Attribute("name");
                foreach (var xcity in xctry.Descendants("city")/*.ToList()*/)
                {
                    var city = new City() { 
                        CityName = (string)xcity.Attribute("name"),
                        Country = country,
                        ServiceName = serviceName,
                        UrlCityName = (string)xcity.Attribute("alias"),
                        UID = (string)xcity.Attribute("uid"),
                        Latitude = (double)xcity.Attribute("lat"),
                        Longitude = (double)xcity.Attribute("lng"),
                        Provider = Instance
                    };
                    if (string.IsNullOrEmpty(city.UrlCityName))
                        city.UrlCityName = city.CityName;
                    result.Add(city);
                }
            }
            return result;
        }

        public override IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            var uid = (from city in BikeServiceProvider.GetAllCities() where string.Equals(city.UrlCityName, cityName, StringComparison.InvariantCultureIgnoreCase) select city.UID).FirstOrDefault();
            if (uid == null)
                uid = cityName;
            var url = string.Format(StationListUrl, uid);
            return DownloadUrl.GetAsync(url)
                .Select<string, List<StationAndAvailability>>(s =>
                {
                    var sl = LoadStationsFromXML(s, cityName);
                    UpdateAvailabilityCache(sl);
                    return sl;
                });
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            var url = string.Format(StationInfoUrl, station.Number);
            var availability = GetAvailabilityFromCache(station);
            if (availability.Availability != null)
                return Observable.Return<StationAndAvailability>(availability);
            else
                return DownloadStationsWithAvailability(station.City)
                    .Select(sl => sl.Where(sa => sa.Station.Number == station.Number).FirstOrDefault());
        }
    }
}
