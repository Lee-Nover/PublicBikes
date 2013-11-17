using System.Collections.Generic;

namespace Bicikelj.Model
{
    public class BCycleService : AzureServiceProxy
    {
        public BCycleService()
        {
            AzureServiceName = "b-cycle";
        }

        public static BCycleService Instance = new BCycleService();

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Battle Creek", Country = "United States", ServiceName = "B-cycle", UrlCityName = "battlecreek", Provider = Instance },
                new City(){ CityName = "Boulder", Country = "United States", ServiceName = "B-cycle", UrlCityName = "boulder", Provider = Instance },
                new City(){ CityName = "Broward", Country = "United States", ServiceName = "B-cycle", UrlCityName = "broward", Provider = Instance },
                new City(){ CityName = "Charlotte", Country = "United States", ServiceName = "B-cycle", UrlCityName = "charlotte", Provider = Instance },
                new City(){ CityName = "Denver", Country = "United States", ServiceName = "B-cycle", UrlCityName = "denver", Provider = Instance },
                new City(){ CityName = "Des Moines", Country = "United States", ServiceName = "B-cycle", UrlCityName = "desmoines", Provider = Instance },
                new City(){ CityName = "Fort Worth", Country = "United States", ServiceName = "B-cycle", UrlCityName = "fortworth", Provider = Instance },
                new City(){ CityName = "Greenville", Country = "United States", ServiceName = "B-cycle", UrlCityName = "greenville", Provider = Instance },
                new City(){ CityName = "Hawaii", Country = "United States", ServiceName = "B-cycle", UrlCityName = "hawaii", AlternateCityName = "Kailua", Provider = Instance },
                new City(){ CityName = "Houston", Country = "United States", ServiceName = "B-cycle", UrlCityName = "houston", Provider = Instance },
                new City(){ CityName = "Kansas City", Country = "United States", ServiceName = "B-cycle", UrlCityName = "kansascity", Provider = Instance },
                new City(){ CityName = "Madison", Country = "United States", ServiceName = "B-cycle", UrlCityName = "madison", Provider = Instance },
                new City(){ CityName = "Milwaukee", Country = "United States", ServiceName = "B-cycle", UrlCityName = "milwaukee", Provider = Instance },
                new City(){ CityName = "Nashville", Country = "United States", ServiceName = "B-cycle", UrlCityName = "nashville", Provider = Instance },
                new City(){ CityName = "Omaha", Country = "United States", ServiceName = "B-cycle", UrlCityName = "omaha", Provider = Instance },
                new City(){ CityName = "San Antonio", Country = "United States", ServiceName = "B-cycle", UrlCityName = "sanantonio", Provider = Instance },
                new City(){ CityName = "Spartanburg", Country = "United States", ServiceName = "B-cycle", UrlCityName = "spartanburg", Provider = Instance }
            };
            return result;
        }
    }
}
