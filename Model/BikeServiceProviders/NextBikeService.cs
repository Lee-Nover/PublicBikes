using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace Bicikelj.Model
{
    public class NextBikeService : AzureServiceProxy
    {
        public NextBikeService()
        {
            AzureServiceName = "nextbike";
        }

        public static NextBikeService Instance = new NextBikeService();

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
    }
}
