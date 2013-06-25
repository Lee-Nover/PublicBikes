
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
}
