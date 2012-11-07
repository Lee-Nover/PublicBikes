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
	public class FavoriteLocation
	{
		public StationLocation Station { get; set; }
		public GeoCoordinate Coordinate { get; set; }
		public string Name { get; set; }

		public FavoriteLocation()
		{
		}

		public FavoriteLocation(StationLocation station)
		{
			this.Station = station;
			this.Name = station.Address;
			this.Coordinate = new GeoCoordinate(station.Latitude, station.Longitude);
		}

		public FavoriteLocation(string name)
		{
			this.Name = name;
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
