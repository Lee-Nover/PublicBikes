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
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Threading;
using Caliburn.Micro;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;

namespace Bicikelj.Model
{
	public class StationLocationIndex
	{
		public StationLocation Location;
		public int Index;

		public StationLocationIndex()
		{
		}

		public StationLocationIndex(StationLocation location, int index)
		{
			this.Location = location;
			this.Index = index;
		}
	}

	public class StationLocationList
	{
		private IList<StationLocation> stations;
		private string stationsXML = "";

		public IList<StationLocation> Stations { get { return stations; } set { SetStations(value); } }

		private void SetStations(IList<StationLocation> value)
		{
			this.stations = value;
			if (this.stations == null)
				return;
			var locations = from station in stations
							select new GeoCoordinate
							{
								Latitude = station.Latitude,
								Longitude = station.Longitude
							};
			locationRect = LocationRect.CreateLocationRect(locations);
		}

		public string StationsXML { get { return stationsXML; } }

		private LocationRect locationRect;
		public LocationRect LocationRect { get { return locationRect;  } }

		public void GetStations(Action<IList<StationLocation>, Exception> result)
		{
			if (stations == null)
				Download(result);
			else
				result(stations, null);
		}

		public bool LoadStationsFromXML(string stationsStr)
		{
			stationsXML = stationsStr;
			if (string.IsNullOrWhiteSpace(stationsStr))
				return false;
			XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
			stations = (from s in doc.Descendants("marker")
						select new StationLocation
						{
							Number = (int)s.Attribute("number"),
							Name = (string)s.Attribute("name"),
							Address = (string)s.Attribute("address"),
							FullAddress = (string)s.Attribute("fullAddress"),
							Latitude = (double)s.Attribute("lat"),
							Longitude = (double)s.Attribute("lng"),
							Open = (bool)s.Attribute("open")
						}).ToList();

			var locations = from station in stations
							select new GeoCoordinate
							{
								Latitude = station.Latitude,
								Longitude = station.Longitude
							};
			locationRect = LocationRect.CreateLocationRect(locations);
			return true;
		}

		public static StationAvailability LoadAvailabilityFromXML(string availabilityStr)
		{
			XDocument doc = XDocument.Load(new System.IO.StringReader(availabilityStr));
			var stations = from s in doc.Descendants("station")
						   select new StationAvailability
						   {
							   Available = (int)s.Element("available"),
							   Free = (int)s.Element("free"),
							   Total = (int)s.Element("total"),
							   Connected = (bool)s.Element("connected"),
							   Open = (bool)s.Element("open")
						   };
			StationAvailability sa = stations.FirstOrDefault();
			return sa;
		}

		public void Download(Action<IList<StationLocation>, Exception> result)
		{
			WebClient wc = new SharpGIS.GZipWebClient();
			wc.DownloadStringCompleted += (s, e) =>
				{
					if (e.Cancelled)
						result(null, null);
					else if (e.Error != null)
						result(null, e.Error);
					else
					{
						ThreadPool.QueueUserWorkItem(o => {
							LoadStationsFromXML(e.Result);
							//SortByDistance(null);
						});
					}
				};
			wc.DownloadStringAsync(new Uri("http://www.bicikelj.si/service/carto"));
		}

		public static void GetAvailability(StationLocation station, Action<StationLocation, StationAvailability, Exception> result)
		{
			WebClient wc = new SharpGIS.GZipWebClient();
			wc.DownloadStringCompleted += (s, e) =>
			{
				if (e.Cancelled)
					result(station, null, null);
				else if (e.Error != null)
					result(station, null, e.Error);
				else
				{
					ThreadPool.QueueUserWorkItem(o =>
					{
						StationAvailability sa = LoadAvailabilityFromXML(e.Result);
						result(station, sa, null);
					});
				}
			};
			wc.DownloadStringAsync(new Uri("http://www.bicikelj.si/service/stationdetails/ljubljana/" + station.Number.ToString()));
		}

		public void SortByDistance(Action<IEnumerable<StationLocation>> callback)
		{
			if (stations == null)
				return;
			if (IoC.Get<SystemConfig>().LocationEnabled)
				LocationHelper.SortByLocation(stations, (r) =>
				{
					this.stations = r.ToList();
					if (callback != null)
						callback(stations);
				});
		}

		public IEnumerable<StationLocation> SortByLocation(GeoCoordinate location)
		{
			if (location == null || stations == null)
				return stations;
			var sortedStations = from station in stations
								 orderby station.Coordinate.GetDistanceTo(location)
								 select station;

			return sortedStations;
		}

		public IEnumerable<StationLocation> SortByLocation(GeoCoordinate location, GeoCoordinate location2)
		{
			//if (location2 == null)
				return SortByLocation(location);
				if (stations == null)
					return null;
			int index = 0;
			var sortedStations1 = (from station in stations
								 orderby station.Coordinate.GetDistanceTo(location)
								 select new StationLocationIndex(station, index++)).ToList();
			index = 0;
			var sortedStations2 = (from station in stations
								   orderby station.Coordinate.GetDistanceTo(location2)
								   select new StationLocationIndex(station, index++)).ToList();

			var sortedStations = from stationA in sortedStations1
								 from stationB in sortedStations2
								 orderby stationA.Index + stationB.Index
								 where stationA.Location == stationB.Location
								 select stationA.Location;

			return sortedStations;
		}
	}
}