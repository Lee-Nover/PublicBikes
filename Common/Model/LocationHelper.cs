using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bicikelj.Model.Bing;
using Microsoft.Devices.Sensors;

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

    public enum TravelType
    {
        Walking,
        Cycling
    }

    public enum TravelSpeed
    {
        Slow,
        Normal,
        Fast
    }

    public class GeoAddress : GeoStatusAndPos
    {
        public IAddress Address { get; set; }
        new public bool IsEmpty { get { return Coordinate == null || Address == null; } }

        public GeoAddress()
        {
        }

        public GeoAddress(GeoStatusAndPos pos)
        {
            Status = pos.Status;
            Coordinate = pos.Coordinate;
            LastUpdate = pos.LastUpdate;
        }

        public GeoAddress(GeoStatusAndPos pos, IAddress address)
            : this(pos)
        {
            Address = address;
        }
    }

    public static class LocationHelper
    {
        public const string UnknownLocation = "(unknown location)";

        public static bool IsLocationEnabled { get { return Sensors.IsLocationEnabled; } set { Sensors.IsLocationEnabled = value; } }

        public static GeoStatusAndPos LastPosition { get { return Sensors.LastPosition; } }

        public static GeoCoordinate LastCoordinate { get {
            var lastPos = LastPosition;
            if (!IsLocationEnabled || lastPos == null || lastPos.IsEmpty || lastPos.Status != GeoPositionStatus.Ready)
                return null;
            else
                return lastPos.Coordinate; 
        } }

        #region Reactive

        public static IObservable<GeoStatusAndPos> GetCurrentLocation() { return Sensors.GetCurrentLocation(); }

        public static IObservable<IAddress> FindAddress(GeoCoordinate coordinate)
        {
            string bingQuery = string.Format(CultureInfo.InvariantCulture, Bing.FindLocationResponse.ApiUrl, coordinate.Latitude, coordinate.Longitude, BingMapsCredentials.Key);
            string googleQuery = string.Format(CultureInfo.InvariantCulture, Google.FindLocationResponse.ApiUrl, coordinate.Latitude, coordinate.Longitude);
            // first try google then bing, WP8 should also try the integrated maps service
            return DownloadUrl.GetAsync<Google.FindLocationResponse>(googleQuery)
                    .Retry(1)
                    .Select(addr => addr == null ? null : addr.FirstAddress() as IAddress)
                .Catch(
                    DownloadUrl.GetAsync<Bing.FindLocationResponse>(bingQuery)
                    .Retry(1)
                    .Select(addr => addr == null ? null : addr.FirstAddress() as IAddress));
        }

        public static IObservable<GeoAddress> GetCurrentGeoAddress(bool distinctOnly = true)
        {
            if (distinctOnly)
                return Sensors.GetCurrentLocation()
                    .Where(pos => { return !pos.IsEmpty && pos.Status.GetValueOrDefault() == GeoPositionStatus.Ready; })
                    .DistinctUntilChanged(pos => pos.Coordinate)
                    .SelectMany(pos =>
                        FindAddress(pos.Coordinate)
                            .Select(addr => new GeoAddress(pos, addr))
                    );
            else
                return Sensors.GetCurrentLocation()
                    .Where(pos => { return !pos.IsEmpty && pos.Status.GetValueOrDefault() == GeoPositionStatus.Ready; })
                    .SelectMany(pos =>
                        FindAddress(pos.Coordinate)
                            .Select(addr => new GeoAddress(pos, addr))
                    );
        }

        public static IObservable<IAddress> GetCurrentAddress()
        {
            return GetCurrentGeoAddress().Select(addr => addr == null ? null : addr.Address);
        }

        public static IObservable<string> GetCurrentCity()
        {
            return GetCurrentAddress().Select(addr => addr == null ? UnknownLocation : addr.Locality);
        }
        
        public static IObservable<IEnumerable<StationLocation>> SortByNearest(IEnumerable<StationLocation> stations)
        {
            if (stations == null)
                return Observable.Return<IEnumerable<StationLocation>>(null);
            else
                return Sensors.GetCurrentLocation()
                    .Where(pos => { 
                        return !pos.IsEmpty && pos.Status.GetValueOrDefault() != GeoPositionStatus.Initializing
                            /*&& pos.Coordinate.HorizontalAccuracy < 100*/;
                    })
                    .Take(1)
                    .Select<GeoStatusAndPos, IEnumerable<StationLocation>>(pos => SortByLocation(stations, pos.Coordinate));
        }

        public static IObservable<FindLocationResponse> FindLocation(string search, GeoCoordinate near)
        {
            string query = string.Format(CultureInfo.InvariantCulture, Bing.FindLocationResponse.ApiUrlLocation, HttpUtility.UrlEncode(search), BingMapsCredentials.Key);
            if (near != null)
                query += string.Format(CultureInfo.InvariantCulture, "&ul={0},{1}", near.Latitude, near.Longitude);

            return DownloadUrl.GetAsync<FindLocationResponse>(query).Retry(1);
        }

        public static IObservable<NavigationResponse> CalculateRoute(IEnumerable<GeoCoordinate> points)
        {
            return CalculateRouteEx(points).Select(r => r.Object);
        }

        public static IObservable<ObjectWithState<NavigationResponse>> CalculateRouteEx(IEnumerable<GeoCoordinate> points)
        {
            string query = Bing.NavigationResponse.ApiUrl;
            int pointNum = 1;
            foreach (var point in points)
                query += string.Format(CultureInfo.InvariantCulture, "&wp.{0}={1},{2}", pointNum++, point.Latitude, point.Longitude);

            return DownloadUrl.GetAsync<NavigationResponse>(query, points).Retry(1);
        }

        #endregion

        #region Passive


        public static IEnumerable<StationLocation> SortByLocation(IEnumerable<StationLocation> stations, GeoCoordinate location)
        {
            if (location == null || location.IsUnknown)
                return stations;

            var sortedStations = from station in stations
                                 orderby station.Coordinate.GetDistanceTo(location)
                                 select station;
            
            return sortedStations;
        }

        public static IEnumerable<StationLocation> SortByLocation(IEnumerable<StationLocation> stations, GeoCoordinate location, GeoCoordinate location2)
        {
            if (location2 == null)
                return SortByLocation(stations, location);
            if (stations == null)
                return null;

            var sortedStations = from station in stations
                                 orderby station.Coordinate.GetDistanceTo(location) * 2 + station.Coordinate.GetDistanceTo(location2)
                                 select station;

            return sortedStations;
        }

        public static IEnumerable<StationLocation> SortByLocation(IEnumerable<StationLocation> stations, GeoCoordinate location, double weight1, GeoCoordinate location2, double weight2)
        {
            if (location2 == null)
                return SortByLocation(stations, location);
            if (stations == null)
                return null;

            var sortedStations = from station in stations
                                 orderby station.Coordinate.GetDistanceTo(location) * weight1 + station.Coordinate.GetDistanceTo(location2) * weight2
                                 select station;

            return sortedStations;
        }

#if WP7
        public static Microsoft.Phone.Controls.Maps.LocationRect GetLocationRect(IEnumerable<StationLocation> stations)
        {
            if (stations == null || stations.Count() == 0)
                return new Microsoft.Phone.Controls.Maps.LocationRect();
            var locations = from station in stations
                            select new GeoCoordinate
                            {
                                Latitude = station.Latitude,
                                Longitude = station.Longitude
                            };
            return Microsoft.Phone.Controls.Maps.LocationRect.CreateLocationRect(locations);
        }

#else

        public static Microsoft.Phone.Maps.Controls.LocationRectangle GetLocationRect(IEnumerable<StationLocation> stations)
        {
            if (stations == null || stations.Count() == 0)
                return new Microsoft.Phone.Maps.Controls.LocationRectangle();

            var locations = from station in stations
                            select new GeoCoordinate
                            {
                                Latitude = station.Latitude,
                                Longitude = station.Longitude
                            };
            return Microsoft.Phone.Maps.Controls.LocationRectangle.CreateBoundingRectangle(locations);
        }
#endif

        public static string GetDistanceString(double distance, bool imperial = false)
        {
            string[,] unit = { { "ft", "mi" }, { "m", "km" } };
            string[] format = { "# ", "0.00 " };
            bool mOrKm = false;
            if (imperial)
            {
                var miles = distance * 0.000621371;
                mOrKm = miles >= 0.5;
                distance *= mOrKm ? 0.000621371 : 3.28084;
            }
            else
            {
                mOrKm = distance >= 1000;
                if (mOrKm)
                    distance /= 1000;
            }
            int idx1 = imperial ? 0 : 1;
            int idx2 = mOrKm ? 1 : 0;
            return distance.ToString(format[idx2]) + unit[idx1, idx2];
        }

        public static double GetTravelSpeed(TravelType type, TravelSpeed speed, bool imperial = false)
        {
            int idxSpeed = speed == TravelSpeed.Slow ? 0 : speed == TravelSpeed.Normal ? 1 : 2;
            int idxImp = imperial ? 0 : 1;
            int[,] walkingSpeed = { { 2, 3, 5 }, { 3, 5, 8 } };
            int[,] cyclingSpeed = { { 6, 10, 15 }, { 10, 15, 22 } };

            double result = 0;
            if (type == TravelType.Walking)
                result = walkingSpeed[idxImp, idxSpeed];
            else
                result = cyclingSpeed[idxImp, idxSpeed];
            return result;
        }

        public static string GetTravelSpeedString(TravelType type, TravelSpeed speed, bool imperial = false)
        {
            var travelSpeed = GetTravelSpeed(type, speed, imperial);
            int idxImp = imperial ? 0 : 1;
            string[] label = { " mph", " km/h" };
            string result = travelSpeed.ToString() + label[idxImp];
            return result;
        }

        #endregion

#if WP7
        public static bool ContainsPoint(this Microsoft.Phone.Controls.Maps.LocationRect rect, GeoCoordinate point)
        {
            return point != null && rect != null &&
                point.Latitude <= rect.North &&
                point.Latitude >= rect.South &&
                point.Longitude >= rect.West &&
                point.Longitude <= rect.East;
        }

#else

        public static bool ContainsPoint(this Microsoft.Phone.Maps.Controls.LocationRectangle rect, GeoCoordinate point)
        {
            return point != null && rect != null &&
                point.Latitude <= rect.North &&
                point.Latitude >= rect.South &&
                point.Longitude >= rect.West &&
                point.Longitude <= rect.East;
        }
#endif
    }
}
