using System.Collections.Generic;

namespace Bicikelj.Model
{
    public class BikeServiceProvider
    {
        public string ServiceName { get; set; }
        public string ServiceUrl { get; set; }
        protected virtual IList<City> GetCities() { return null; }
        public static IList<City> GetAllCities()
        {
            List<City> result = new List<City>();
            foreach (var provider in providers)
            {
                var cities = provider.GetCities();
                result.AddRange(cities);
            }
            return result;
        }
        private static BikeServiceProvider[] providers = new BikeServiceProvider[] { new CycloCityService() };
    }

    public class CycloCityService : BikeServiceProvider
    {
        protected override IList<City> GetCities()
        {
            return new List<City>() {
                new City(){ CityName = "Amiens", Country = "France", ServiceName = "velam" },
                new City(){ CityName = "Paris", Country = "France", ServiceName = "cy’clic" },
                new City(){ CityName = "Ljubljana", Country = "Slovenia", ServiceName = "bicikelj" }           
            };
        }
    }
}