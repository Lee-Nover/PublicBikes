using System.Collections.Generic;

namespace Bicikelj.Model
{
    public class HourBikeService : AzureServiceProxy
    {
        public HourBikeService()
        {
            AzureServiceName = "hourbike";
        }

        public static HourBikeService Instance = new HourBikeService();

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Liverpool", Country = "United Kingdom", ServiceName = "citybike", UrlCityName = "liverpool", Provider = Instance },
                new City(){ CityName = "Szczecin", Country = "Poland", ServiceName = "Bike_S", UrlCityName = "szczecin", Provider = Instance }
            };
            return result;
        }
    }
}
