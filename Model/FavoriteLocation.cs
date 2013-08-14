using System;
using System.Device.Location;
using System.Runtime.Serialization;

namespace Bicikelj.Model
{
    public enum FavoriteType
    {
        Station,
        Coordinate,
        Address,
        Name
    }

    public class FavoriteLocation
    {
        public FavoriteType FavoriteType { get; set; }
        public StationLocation Station { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        private GeoCoordinate coordinate;

        public FavoriteType GetFavoriteType()
        {
            var c = Coordinate;
            if (Station != null)
                return Model.FavoriteType.Station;
            if (c.IsUnknown || (c.Latitude == 0 && c.Longitude == 0))
            {
                if (string.IsNullOrEmpty(Address))
                    return Model.FavoriteType.Name;
                else
                    return Model.FavoriteType.Address;
            }
            else
                return Model.FavoriteType.Coordinate;
        }

        [IgnoreDataMember]
        public GeoCoordinate Coordinate
        {
            get
            {
                if (coordinate == null || coordinate.IsUnknown || (coordinate.Longitude == 0 && coordinate.Latitude == 0))
                    coordinate = new GeoCoordinate(Latitude, Longitude);
                return coordinate;
            }
            set { 
                coordinate = value;
                if (coordinate != null)
                {
                    Latitude = coordinate.Latitude;
                    Longitude = coordinate.Longitude;
                }
            }
        }

        public FavoriteLocation()
        {
        }

        public FavoriteLocation(StationLocation station)
        {
            this.Station = station;
            this.Name = station.Name;
            this.City = station.City;
            this.Address = station.Address;
            this.Coordinate = new GeoCoordinate(station.Latitude, station.Longitude);
            this.FavoriteType = Model.FavoriteType.Station;
        }

        public FavoriteLocation(string name)
        {
            this.Name = name;
            this.FavoriteType = Model.FavoriteType.Name;
        }

        // just to remove the warning
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
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
                    return Name.Equals(other.Name, StringComparison.InvariantCultureIgnoreCase);
            // check addresses
            if (Address != null || other.Address != null)
                if (Address == null || other.Address == null)
                    return false;
                else
                    return Address.Equals(other.Address, StringComparison.InvariantCultureIgnoreCase);

            return false;
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
