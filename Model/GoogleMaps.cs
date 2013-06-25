using System.Linq;
using System.Runtime.Serialization;

namespace Bicikelj.Model.Google
{
	[DataContract]
	public class AddressComponent
	{
		[DataMember(Name = "long_name")]
		public string LongName { get; set; }
		[DataMember(Name = "short_name")]
		public string ShortName { get; set; }
		[DataMember(Name = "types")]
		public string[] Types { get; set; }
	}

	[DataContract]
	public class Address : IAddress
	{
		public string AddressLine { get; set; }
		public string CountryRegion { get { return (from ac in AddressComponents where ac.Types.Contains("country") select ac.LongName).FirstOrDefault(); } }
		[DataMember(Name = "formatted_address")]
		public string FormattedAddress { get; set; }
        public string AdminDistrict { get; set; }
		public string AdminDistrict2 { get; set; }
        public string PostalCode { get; set; }
        public string Locality { get; set; }

        private AddressComponent[] addressComponents;
		[DataMember(Name = "address_components")]
		public AddressComponent[] AddressComponents {
            get { return addressComponents; }
            set {
                if (value == addressComponents)
                    return;
                addressComponents = value;
                string addrLine = "";
                addrLine += (from ac in AddressComponents where ac.Types.Contains("street_number") select ac.LongName).FirstOrDefault();
                addrLine += " " + (from ac in AddressComponents where ac.Types.Contains("route") select ac.LongName).FirstOrDefault();
                AddressLine = addrLine;

                AdminDistrict = (from ac in AddressComponents where ac.Types.Contains("administrative_area_level_1") select ac.LongName).FirstOrDefault();
                AdminDistrict2 = (from ac in AddressComponents where ac.Types.Contains("administrative_area_level_2") select ac.LongName).FirstOrDefault();
                PostalCode = (from ac in AddressComponents where ac.Types.Contains("postal_code") select ac.LongName).FirstOrDefault();
                Locality = (from ac in AddressComponents where ac.Types.Contains("locality") select ac.LongName).FirstOrDefault();
            }
        }
	}

	public class FindLocationResponse
	{
		public static string ApiUrl = "http://maps.googleapis.com/maps/api/geocode/json?latlng={0},{1}&sensor=false";

		[DataMember(Name = "results")]
		public Address[] Results { get; set; }
        public Address FirstAddress()
        {
            if (Results == null || Results.Length == 0)
                return null;
            else
                return Results[0];
        }
	}
}