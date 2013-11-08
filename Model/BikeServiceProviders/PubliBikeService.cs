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
                    //foreach (var terminal in xcity.Terminals)
                    {
                        // todo: find an existing city and append the service name; eg: "SwissPass, Bulle"
                        var city = new City()
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
            }
            return result;
        }
    }
}
