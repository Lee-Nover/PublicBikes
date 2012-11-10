using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Bicikelj.Model.Bing;
using ServiceStack.Text;

namespace Bicikelj.Model
{
	public enum PinType
	{
		CurrentPosition,
		BikeStand,
		Cycling,
		Walking,
		Finish
	}

	public static class LocationHelper
	{
		public static void SortByLocation(IEnumerable<StationLocation> stations, Action<IEnumerable<StationLocation>> result)
		{
			GeoCoordinateWatcher gw = new GeoCoordinateWatcher();
			gw.StatusChanged += (sender, e) =>
			{
				if (e.Status == GeoPositionStatus.Disabled)
				{
					gw.Stop();
					if (result != null)
						result(null);
					return;
				}
				if (e.Status != GeoPositionStatus.Ready)
					return;
				GeoCoordinate location = ((GeoCoordinateWatcher)sender).Position.Location;
				var sortedStations = SortByLocation(stations, location);

				gw.Stop();
				if (result != null)
					result(sortedStations);
			};
			gw.Start();
		}

		public static IEnumerable<StationLocation> SortByLocation(IEnumerable<StationLocation> stations, GeoCoordinate location)
		{
			var sortedStations = from station in stations
								 orderby station.Coordinate.GetDistanceTo(location)
								 select station;

			return sortedStations;
		}

		public static void CalculateRoute(IEnumerable<GeoCoordinate> points, Action<NavigationResponse, Exception> result)
		{
			string query = @"http://dev.virtualearth.net/REST/v1/Routes/Walking?"
				+ "travelMode=Walking&optimize=distance&routePathOutput=Points&tl=0.00000344978&maxSolutions=1"
				+ "&key=" + BingMapsCredentials.Key;
			int pointNum = 1;
			foreach (var point in points)
				query += string.Format(CultureInfo.InvariantCulture, "&wp.{0}={1},{2}", pointNum++, point.Latitude, point.Longitude);

			var wc = new SharpGIS.GZipWebClient();
			wc.DownloadStringCompleted += (s, e) => {
				if (e.Error != null)
					result(null, e.Error);
				else if (e.Cancelled)
					result(null, null);
				else
					result(e.Result.FromJson<NavigationResponse>(), null);
			};
			wc.DownloadStringAsync(new Uri(query));
		}

		public static void FindLocation(string search, GeoCoordinate near, Action<FindLocationResponse, Exception> result)
		{
			string query = @"http://dev.virtualearth.net/REST/v1/Locations?q=" + HttpUtility.UrlEncode(search)
				+ "&key=" + BingMapsCredentials.Key;
			if (near != null)
				query += string.Format(CultureInfo.InvariantCulture, "&ul={0},{1}", near.Latitude, near.Longitude);
			
			var wc = new SharpGIS.GZipWebClient();
			wc.DownloadStringCompleted += (s, e) =>
			{
				if (e.Error != null)
					result(null, e.Error);
				else if (e.Cancelled)
					result(null, null);
				else
					result(e.Result.FromJson<FindLocationResponse>(), null);
			};
			wc.DownloadStringAsync(new Uri(query));
		}

		public static string GetDistanceString(double distance, bool imperial = false)
		{
			string[,] unit = { { "ft", "mi" }, { "m", "km" } };
			string[] format = { "#.00 ", "# " };
			bool moreThan1km = distance > 1000;
			if (imperial)
				if (moreThan1km)
					distance *= 0.000621371;
				else
					distance *= 3.28084;
			else
				if (moreThan1km)
					distance /= 1000;
			int idx1 = imperial ? 0 : 1;
			int idx2 = moreThan1km ? 1 : 0;
			return distance.ToString(format[moreThan1km ? 0 : 1]) + unit[idx1, idx2];
		}
	}
}
