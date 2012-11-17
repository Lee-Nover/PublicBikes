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

namespace Bicikelj.Model
{
	public enum FavoriteType
	{
		Station,
		Coordinate,
		Name
	}

	public class FavoriteLocation
	{
		public FavoriteType FavoriteType { get; set; }
		public StationLocation Station { get; set; }
		public GeoCoordinate Coordinate { get; set; }
		public string Name { get; set; }
		public string Address { get; set; }

		public FavoriteLocation()
		{
		}

		public FavoriteLocation(StationLocation station)
		{
			this.Station = station;
			this.Name = station.Name;
			this.Address = station.Address;
			this.Coordinate = new GeoCoordinate(station.Latitude, station.Longitude);
			this.FavoriteType = Model.FavoriteType.Station;
		}

		public FavoriteLocation(string name)
		{
			this.Name = name;
			this.FavoriteType = Model.FavoriteType.Name;
		}

		public override bool Equals (object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}
		
			var other = obj as FavoriteLocation;
			if (other == null)
				return false;
			// if either object has a Station then compare their instances
			if (Station != null || other.Station != null)
				return Station == other.Station;
			// if either object has a Coordinate then compare their Latitude and Longitude
			if (Coordinate != null || other.Coordinate != null)
				if (Coordinate == null || other.Coordinate == null)
					return false;
				else
					return Coordinate.Longitude == other.Coordinate.Longitude && Coordinate.Latitude == other.Coordinate.Latitude;
			// check names
			if (Name != null || other.Name != null)
				if (Name == null || other.Name == null)
					return false;
				else
					return Name.ToLower() == other.Name.ToLower();
			// check addresses
			if (Address != null || other.Address != null)
				if (Address == null || other.Address == null)
					return false;
				else
					return Address.ToLower() == other.Address.ToLower();

			return false;
		}
	
		// override object.GetHashCode
		public override int GetHashCode()
		{
			// TODO: write your implementation of GetHashCode() here
			return base.GetHashCode();
		}
	}

	public class FavoriteState
	{
		public bool IsFavorite { get; private set; }
		public FavoriteLocation Location { get; private set; }

		public FavoriteState(FavoriteLocation location, bool isFavorite)
		{
			this.Location = location;
			this.IsFavorite = isFavorite;
		}

		public static FavoriteState Favorite(FavoriteLocation location)
		{
			return new FavoriteState(location, true);
		}

		public static FavoriteState Unfavorite(FavoriteLocation location)
		{
			return new FavoriteState(location, false);
		}
	}
}
