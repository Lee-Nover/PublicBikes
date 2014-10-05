using System.Collections.Generic;

namespace Bicikelj.Model
{
    public class BCycleService : AzureServiceProxy
    {
        public BCycleService()
        {
            AzureServiceName = "b-cycle-v2";
        }

        public static BCycleService Instance = new BCycleService();

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Arbor", Country = "United States", ServiceName = "ArborBike", UrlCityName = "arbor", Provider = Instance },
                new City(){ CityName = "Austin", Country = "United States", ServiceName = "Austin B-cycle", UrlCityName = "austin", Provider = Instance },
                new City(){ CityName = "Battle Creek", Country = "United States", ServiceName = "B-cycle", UrlCityName = "battlecreek", Provider = Instance },
                new City(){ CityName = "Boulder", Country = "United States", ServiceName = "B-cycle", UrlCityName = "boulder", Provider = Instance },
                new City(){ CityName = "Broward", Country = "United States", ServiceName = "B-cycle", UrlCityName = "broward", Provider = Instance },
                new City(){ CityName = "Charlotte", Country = "United States", ServiceName = "B-cycle", UrlCityName = "charlotte", Provider = Instance },
                new City(){ CityName = "Cincinnati", Country = "United States", ServiceName = "Cincy Red Bike", UrlCityName = "cincinnati", Provider = Instance },
                new City(){ CityName = "Columbia County", Country = "United States", ServiceName = "B-cycle", UrlCityName = "columbiacounty", Provider = Instance },
                new City(){ CityName = "Dallas", Country = "United States", ServiceName = "Dallas Fair Park", UrlCityName = "dallas", Provider = Instance },
                new City(){ CityName = "Denver", Country = "United States", ServiceName = "Denver Bike Sharing", UrlCityName = "denver", Provider = Instance },
                new City(){ CityName = "Denver Federal Center", Country = "United States", ServiceName = "DFC Bikes", UrlCityName = "dfc", Provider = Instance },
                new City(){ CityName = "Des Moines", Country = "United States", ServiceName = "B-cycle", UrlCityName = "desmoines", Provider = Instance },
                new City(){ CityName = "Fargo", Country = "United States", ServiceName = "B-cycle", UrlCityName = "fargo", Provider = Instance },
                new City(){ CityName = "Fort Worth", Country = "United States", ServiceName = "B-cycle", UrlCityName = "fortworth", Provider = Instance },
                new City(){ CityName = "Greenville", Country = "United States", ServiceName = "B-cycle", UrlCityName = "greenville", Provider = Instance },
                new City(){ CityName = "Hawaii", Country = "United States", ServiceName = "B-cycle", UrlCityName = "hawaii", AlternateCityName = "Kailua", Provider = Instance },
                new City(){ CityName = "Houston", Country = "United States", ServiceName = "B-cycle", UrlCityName = "houston", Provider = Instance },
                new City(){ CityName = "Indianapolis", Country = "United States", ServiceName = "Indy", UrlCityName = "indy", Provider = Instance },
                new City(){ CityName = "Kansas City", Country = "United States", ServiceName = "B-cycle", UrlCityName = "kansascity", Provider = Instance },
                new City(){ CityName = "Madison", Country = "United States", ServiceName = "B-cycle", UrlCityName = "madison", Provider = Instance },
                new City(){ CityName = "Milwaukee", Country = "United States", ServiceName = "Bublr Bikes", UrlCityName = "milwaukee", Provider = Instance },
                new City(){ CityName = "Nashville", Country = "United States", ServiceName = "B-cycle", UrlCityName = "nashville", Provider = Instance },
                new City(){ CityName = "Omaha", Country = "United States", ServiceName = "B-cycle", UrlCityName = "omaha", Provider = Instance },
                new City(){ CityName = "Rapid City", Country = "United States", ServiceName = "B-cycle", UrlCityName = "rapidcity", Provider = Instance },
                new City(){ CityName = "Salt Lake City", Country = "United States", ServiceName = "GREENbike", UrlCityName = "saltlake", Provider = Instance },
                new City(){ CityName = "San Antonio", Country = "United States", ServiceName = "B-cycle", UrlCityName = "sanantonio", Provider = Instance },
                new City(){ CityName = "San Francisco", Country = "United States", ServiceName = "gRide", UrlCityName = "sanfranciscogride", Provider = Instance },
                new City(){ CityName = "Savannah", Country = "United States", ServiceName = "CAT Bike", UrlCityName = "savannah", Provider = Instance },
                new City(){ CityName = "Spartanburg", Country = "United States", ServiceName = "B-cycle", UrlCityName = "spartanburg", Provider = Instance },
                new City(){ CityName = "Whippany", Country = "United States", ServiceName = "Whippany NJ", UrlCityName = "whippany", Provider = Instance },
                // Chile
                new City(){ CityName = "Santiago", Country = "Chile", ServiceName = "Bikesantiago", UrlCityName = "santiago", Provider = Instance }
            };
            return result;
        }
    }
}
