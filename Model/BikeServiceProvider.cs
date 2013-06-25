using System.Collections.Generic;
using System.Linq;
using System;

namespace Bicikelj.Model
{
    public class BikeServiceProvider
    {
        public string ServiceName { get; set; }
        public string ServiceUrl { get; set; }
        protected virtual IList<City> GetCities() { return null; }
        private static List<City> allCities = null;
        public static IList<City> GetAllCities()
        {
            if (allCities == null)
            {
                allCities = new List<City>();
                foreach (var provider in providers)
                {
                    var cities = provider.GetCities();
                    allCities.AddRange(cities);
                }
            }
            return allCities;
        }
        private static BikeServiceProvider[] providers = new BikeServiceProvider[] { new CycloCityService() };

        public static City FindByCityName(string cityName)
        {
            City result = null;

            var allCities = GetAllCities();
            result = allCities.Where(c =>
                c.UrlCityName.Equals(cityName, StringComparison.InvariantCultureIgnoreCase)
                || c.CityName.Equals(cityName, StringComparison.InvariantCultureIgnoreCase)
                || (!string.IsNullOrEmpty(c.AlternateCityName) && cityName.ToLowerInvariant().Contains(c.AlternateCityName.ToLowerInvariant()))).FirstOrDefault();

            return result;
        }
    }

    public class CycloCityService : BikeServiceProvider
    {
        private static IList<City> cityList = new List<City>() {
            new City(){ CityName = "Amiens", Country = "France", ServiceName = "Vélam", UrlCityName = "amiens" },
            new City(){ CityName = "Besançon", Country = "France", ServiceName = "VéloCité", UrlCityName = "besancon" },
            new City(){ CityName = "Bruxelles", Country = "Belgium", ServiceName = "Villo!", UrlCityName = "bruxelles", AlternateCityName = "Brussels" },
            new City(){ CityName = "Brisbane", Country = "Australia", ServiceName = "CityCycle", UrlCityName = "brisbane" },
            new City(){ CityName = "Cergy-Pontoise", Country = "France", ServiceName = "vélO2", UrlCityName = "cergy" },
            new City(){ CityName = "Créteil", Country = "France", ServiceName = "Cristolib", UrlCityName = "creteil" },
            new City(){ CityName = "Dublin", Country = "Ireland", ServiceName = "dublinbikes", UrlCityName = "dublin" },
            //new City(){ CityName = "Gijón", Country = "Spain", ServiceName = "Gijón-Bici", UrlCityName = "gijon" },
            new City(){ CityName = "Göteborg", Country = "Sweden", ServiceName = "styr & ställ", UrlCityName = "goteborg", AlternateCityName = "Gothenburg" },
            new City(){ CityName = "Ljubljana", Country = "Slovenia", ServiceName = "Bicikelj", UrlCityName = "ljubljana" },
            new City(){ CityName = "Luxembourg", Country = "Luxembourg", ServiceName = "vel’oh!", UrlCityName = "luxembourg" },
            new City(){ CityName = "Lyon", Country = "France", ServiceName = "vélo'v", UrlCityName = "lyon" },
            new City(){ CityName = "Marseille", Country = "France", ServiceName = "le vélo", UrlCityName = "marseille"},
            new City(){ CityName = "Mulhouse", Country = "France", ServiceName = "Vélocité", UrlCityName = "mulhouse" },
            new City(){ CityName = "Namur", Country = "Belgium", ServiceName = "Li bia velo", UrlCityName = "namur" },
            new City(){ CityName = "Nancy", Country = "France", ServiceName = "vélOstan'lib", UrlCityName = "nancy" },
            new City(){ CityName = "Nantes", Country = "France", ServiceName = "bicloo", UrlCityName = "nantes" },
            new City(){ CityName = "Paris", Country = "France", ServiceName = "Vélib’", UrlCityName = "paris" },
            new City(){ CityName = "Rouen", Country = "France", ServiceName = "cy'clic", UrlCityName = "rouen" },
            new City(){ CityName = "Santander", Country = "Spain", ServiceName = "Tusbic", UrlCityName = "santander" },
            new City(){ CityName = "Seville", Country = "Spain", ServiceName = "SEVici", UrlCityName = "seville" },
            new City(){ CityName = "Toulouse", Country = "France", ServiceName = "VélôToulouse", UrlCityName = "toulouse"},
            new City(){ CityName = "Toyama", Country = "Japan", ServiceName = "CyclOcity", UrlCityName = "toyama" },
            new City(){ CityName = "Valencia", Country = "Spain", ServiceName = "balenbisi", UrlCityName = "valencia" }
            //new City(){ CityName = "Vienna", Country = "Austria", ServiceName = "citybike" }
        };

        protected override IList<City> GetCities()
        {
            return cityList;
        }
    }
}