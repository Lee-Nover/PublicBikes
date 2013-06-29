using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.Linq;
using System.Net;
using Bicikelj.Model.Bing;
using ServiceStack.Text;
using Microsoft.Phone.Controls.Maps;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using System.Diagnostics;

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

    public class GeoStatusAndPos
    {
        public GeoPositionStatus? Status { get; set; }
        public GeoCoordinate Coordinate { get; set; }
        public DateTimeOffset? LastUpdate { get; set; }
        public bool IsEmpty { get { return Coordinate == null; } }
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
        #region Reactive

        private static readonly GeoCoordinateWatcher geoCoordinateWatcher = new GeoCoordinateWatcher();
        private static IObservable<GeoStatusAndPos> observableGeo = null;
        private static GeoStatusAndPos geoPos = new GeoStatusAndPos();
        public static IObservable<GeoStatusAndPos> GetCurrentLocation()
        {
            if (observableGeo == null)
            {
                observableGeo = Observable.Create<GeoStatusAndPos>(observer =>
                {
                    EventHandler<GeoPositionStatusChangedEventArgs> statusChanged = (sender, e) =>
                    {
                        geoPos.Status = e.Status;
                        observer.OnNext(geoPos);
                    };
                    EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>> positionChanged = (sender, e) =>
                    {
                        geoPos.Coordinate = e.Position.Location;
                        geoPos.LastUpdate = e.Position.Timestamp;
                        observer.OnNext(geoPos);
                    };
                    geoCoordinateWatcher.StatusChanged += statusChanged;
                    geoCoordinateWatcher.PositionChanged += positionChanged;

                    if (!geoCoordinateWatcher.TryStart(false, TimeSpan.FromSeconds(15)))
                        observer.OnError(new Exception("GeoCoordinate service could not be started"));

                    return Disposable.Create(() =>
                    {
                        geoCoordinateWatcher.StatusChanged -= statusChanged;
                        geoCoordinateWatcher.PositionChanged -= positionChanged;
                        geoCoordinateWatcher.Stop();
                        observableGeo = null;
                    });
                })
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Publish(geoPos)
                .RefCount();
            }
            return observableGeo;
        }

        public static IObservable<IAddress> FindAddress(GeoCoordinate coordinate)
        {
            //string query = string.Format(CultureInfo.InvariantCulture, Bing.FindLocationResponse.ApiUrl, coordinate.Latitude, coordinate.Longitude, BingMapsCredentials.Key);
            string query = string.Format(CultureInfo.InvariantCulture, Google.FindLocationResponse.ApiUrl, coordinate.Latitude, coordinate.Longitude);

            return DownloadUrl.GetAsync<Google.FindLocationResponse>(query)
                .Select<Google.FindLocationResponse, IAddress>(addr => addr.FirstAddress());
        }

        public static IObservable<GeoAddress> GetCurrentGeoAddress()
        {
            return GetCurrentLocation()
                .Where(pos => { return !pos.IsEmpty && pos.Status.GetValueOrDefault() == GeoPositionStatus.Ready; })
                .DistinctUntilChanged(pos => pos.Coordinate)
                .SelectMany(pos =>
                    FindAddress(pos.Coordinate)
                       .Select(addr => new GeoAddress(pos, addr))
                );
        }

        public static IObservable<IAddress> GetCurrentAddress()
        {
            return GetCurrentGeoAddress().Select(addr => addr.Address);
        }

        public static IObservable<string> GetCurrentCity()
        {
            return GetCurrentAddress().Select(addr => addr.Locality);
        }

        public static IObservable<IEnumerable<StationLocation>> SortByNearest(IEnumerable<StationLocation> stations)
        {
            if (stations == null)
                return Observable.Return<IEnumerable<StationLocation>>(null);
            else
                return GetCurrentLocation()
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

            return DownloadUrl.GetAsync<FindLocationResponse>(query);
        }

        public static IObservable<NavigationResponse> CalculateRoute(IEnumerable<GeoCoordinate> points)
        {
            string query = Bing.NavigationResponse.ApiUrl;
            int pointNum = 1;
            foreach (var point in points)
                query += string.Format(CultureInfo.InvariantCulture, "&wp.{0}={1},{2}", pointNum++, point.Latitude, point.Longitude);

            return DownloadUrl.GetAsync<NavigationResponse>(query);
        }

        public static IObservable<ObjectWithState<NavigationResponse>> CalculateRouteEx(IEnumerable<GeoCoordinate> points)
        {
            string query = Bing.NavigationResponse.ApiUrl;
            int pointNum = 1;
            foreach (var point in points)
                query += string.Format(CultureInfo.InvariantCulture, "&wp.{0}={1},{2}", pointNum++, point.Latitude, point.Longitude);

            return DownloadUrl.GetAsync<NavigationResponse>(query, points);
        }

        #endregion

        #region Passive


        public static IEnumerable<StationLocation> SortByLocation(IEnumerable<StationLocation> stations, GeoCoordinate location)
        {
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

        public static LocationRect GetLocationRect(IEnumerable<StationLocation> stations)
        {
            var locations = from station in stations
                            select new GeoCoordinate
                            {
                                Latitude = station.Latitude,
                                Longitude = station.Longitude
                            };
            return LocationRect.CreateLocationRect(locations);
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
    }
}
