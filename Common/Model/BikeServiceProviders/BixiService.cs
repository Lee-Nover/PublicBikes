using System.Collections.Generic;

namespace Bicikelj.Model
{
    public class BixiService : AzureServiceProxy
    {
        public BixiService()
        {
            AzureServiceName = "bixi";
        }

        public static BixiService Instance = new BixiService();

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Montreal", Country = "Canada", ServiceName = "BIXI", UrlCityName = "montreal", Provider = Instance },
                new City(){ CityName = "Toronto", Country = "Canada", ServiceName = "BIXI", UrlCityName = "toronto", Provider = Instance },
                new City(){ CityName = "Ottawa", Country = "Canada", ServiceName = "BIXI", UrlCityName = "ottawa", AlternateCityName = "Gatineau", Provider = Instance },
                new City(){ CityName = "Washington DC", Country = "United States", ServiceName = "Capital Bike Share", UrlCityName = "washingtondc", AlternateCityName = "washington", Provider = Instance },
                new City(){ CityName = "Minneapolis", Country = "United States", ServiceName = "Nice Ride", UrlCityName = "minneapolis", Provider = Instance },
                new City(){ CityName = "Boston", Country = "United States", ServiceName = "Hubway", UrlCityName = "boston", Provider = Instance },
                new City(){ CityName = "Chattanooga", Country = "United States", ServiceName = "Bike Chattanooga", UrlCityName = "chattanooga", Provider = Instance },
                new City(){ CityName = "Chicago", Country = "United States", ServiceName = "Divvy", UrlCityName = "chicago", Provider = Instance },
                new City(){ CityName = "New York", Country = "United States", ServiceName = "Citi Bike", UrlCityName = "newyork", Provider = Instance },
                new City(){ CityName = "San Francisco", Country = "United States", ServiceName = "Bay Area Bike Share", UrlCityName = "sanfrancisco", AlternateCityName = "bay area", Provider = Instance },
                new City(){ CityName = "London", Country = "United Kingdom", ServiceName = "Barclays Cycle Hire", UrlCityName = "london", Provider = Instance },
                new City(){ CityName = "Melbourne", Country = "Australia", ServiceName = "Melbourne bike share", UrlCityName = "melbourne", Provider = Instance }
            };
            return result;
        }
    }
}
