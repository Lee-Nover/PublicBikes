using Bicikelj.Model;

namespace Bicikelj.AzureService
{
    public class StationInfo
    {
        public string name { get; set; }
        public string id { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public int status { get; set; }
        public int bikes { get; set; }
        public int freeDocks { get; set; }
        public int totalDocks { get; set; }

        public StationAvailability GetAvailability()
        {
            return new StationAvailability()
            {
                Available = this.bikes,
                Connected = this.status > 0,
                Open = this.status > 0,
                Free = this.freeDocks,
                Total = this.totalDocks
            };
        }

        public StationLocation GetStation()
        {
            return new StationLocation()
            {
                Name = this.name,
                Address = this.address,
                City = this.city,
                Latitude = this.lat,
                Longitude = this.lng,
                Number = int.Parse(string.IsNullOrEmpty(id) ? "0" : id),
                Open = this.status > 0
            };
        }
    }
}
