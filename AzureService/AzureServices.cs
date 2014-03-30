using System.Device.Location;
using System.Linq;

namespace Bicikelj.AzureService
{
    public class DataCenter
    {
        public string Name;
        public string Description;
        public string ApplicationKey;
        public GeoCoordinate Location;
    }

    public class AzureServices
    {
        private static DataCenter[] DataCenters = {
            new DataCenter() {
                Name = "publicbikes",
                Description = "EU North (Ireland)",
                ApplicationKey = "your app key",
                Location = new GeoCoordinate(53.3243201, -6.251695)
            },
            new DataCenter() {
                Name = "publicbikes-us-east",
                Description = "US East (Virginia)",
                ApplicationKey = "your app key",
                Location = new GeoCoordinate(37.5246609, -77.4932614)
            }
        };

        public static DataCenter GetClosestCenter(GeoCoordinate location)
        {
            if (location == null || location.IsUnknown || (location.Latitude == 0 && location.Longitude == 0))
                return DataCenters[0];
            return DataCenters.OrderBy(l => l.Location.GetDistanceTo(location)).FirstOrDefault();
        }
    }
}
