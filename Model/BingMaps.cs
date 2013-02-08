using System.Collections.Generic;
using System.Linq;

namespace Bicikelj.Model.Bing
{
	public class Point
	{
		public double Latitude { get { return Coordinates[0]; } set { SetCoordinates(value, Longitude); } }
		public double Longitude { get { return Coordinates[1]; } set { SetCoordinates(Latitude, value); } }
		public double[] Coordinates { get; set; }

		public Point()
		{
			SetCoordinates(0, 0);
		}

		public Point(double lat, double lon)
		{
			SetCoordinates(lat, lon);
		}

		private void SetCoordinates(double lat, double lon)
		{
			Coordinates = new double[] { lat, lon };
		}
	}

	public class Line
	{
		public List<double[]> Coordinates { get; set; }
	}

	public class RoutePath
	{
		public Line Line { get; set; }
		private List<Point> points;
		public List<Point> Points {
			get
			{
				if (points == null || points.Count == 0)
				{
					points = new List<Point>();
					foreach (var item in Line.Coordinates)
						points.Add(new Point(item[0], item[1]));
				}
				return points;
			}
		}
	}

	public class RouteLeg
	{
		public double TravelDistance { get; set; }
		public double TravelDuration { get; set; }
	}

	public class RouteResource
	{
		public string DistanceUnit { get; set; }
		public string DurationUnit { get; set; }
		public double TravelDistance { get; set; }
		public double TravelDuration { get; set; }
		public RoutePath RoutePath { get; set; }
		public List<RouteLeg> RouteLegs { get; set; }
	}

	public class Address
	{
		public string AddressLine { get; set; }
		public string CountryRegion { get; set; }
		public string FormattedAddress { get; set; }
		public string PostalCode { get; set; }
		public string Locality { get; set; }
	}

	public enum Confidence
	{
		Low,
		Medium,
		High
	}

	public enum UsageType
	{
		Display,
		Route
	}

	public class GeocodePoint : Point
	{
		public string CalculationMethod { get; set; }
		public List<UsageType> UsageTypes { get; set; }
	}

	public class LocationResource
	{
		public string Name { get; set; }
		public Point Point { get; set; }
		public Address Address { get; set; }
		public Confidence Confidence { get; set; }
		public string EntityType { get; set; }
		public List<GeocodePoint> GeocodePoints { get; set; }
	}

	public class ResourceSet<T>
	{
		public int EstimatedTotal { get; set; }
		public List<T> Resources { get; set; }
	}

	public class CommonResponse<T>
	{
		public int StatusCode { get; set; }
		public string StatusDescription { get; set; }
		public string AuthenticationResultCode { get; set; }
		public List<ResourceSet<T>> ResourceSets { get; set; }
	}

	public class NavigationResponse : CommonResponse<RouteResource>
	{
		private RouteResource route;
		public RouteResource Route
		{
			get
			{
				if (route == null)
					route = ResourceSets[0].Resources.FirstOrDefault<RouteResource>();

				return route;
			}
		}
	}

	public class FindLocationResponse : CommonResponse<LocationResource>
	{
		private LocationResource location;
		public LocationResource Location
		{
			get
			{
				if (location == null)
					location = ResourceSets[0].Resources.FirstOrDefault<LocationResource>();
				
				return location;
			}
		}
	}
}