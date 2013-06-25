using System.Collections.Generic;
using Wintellect.Sterling.Serialization;

namespace Bicikelj.Model
{
    public class City
    {
        public string CityName { get; set; }
        public string Country { get; set; }
        public string ServiceName { get; set; }
        public string UrlCityName { get; set; }
        [SterlingIgnore]
        public string AlternateCityName { get; set; }
        public List<StationLocation> Stations { get; set; }
        public List<FavoriteLocation> Favorites { get; set; }
    }
}
