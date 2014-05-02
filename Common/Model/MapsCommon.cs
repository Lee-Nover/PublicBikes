using System.Device.Location;

namespace Bicikelj.Model
{
    public interface IAddress
    {
        string AddressLine { get; }
        string CountryRegion { get; }
        string FormattedAddress { get; }
        string AdminDistrict { get; }
        string AdminDistrict2 { get; }
        string PostalCode { get; }
        string Locality { get; }
    }

    public class RegionAndLocality
    {
        public string CountryRegion { get; set; }
        public string Locality { get; set; }
        
        public RegionAndLocality()
        {
        }

        public RegionAndLocality(string region, string locality)
        {
            this.CountryRegion = region;
            this.Locality = locality;
        }
    }

    public interface IHasCoordinate
    {
        GeoCoordinate Coordinate { get; set; }
    }
}
