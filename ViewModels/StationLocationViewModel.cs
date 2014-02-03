using System;
using System.Device.Location;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Media;
using Bicikelj.Model;
using Bicikelj.Model.Bing;
using Bicikelj.Views.StationLocation;
using Caliburn.Micro;
using Microsoft.Phone.Controls.Maps;
using System.Net;

namespace Bicikelj.ViewModels
{
    public class StationLocationViewModel : Screen
    {
        private StationLocation stationLocation;
        public StationLocation Location { get { return stationLocation; } set { SetLocation(value); } }
        private IEventAggregator events;
        private SystemConfig config;
        private CityContextViewModel cityCtx;

        public StationLocationViewModel(StationLocation stationLocation)
        {
            events = IoC.Get<IEventAggregator>();
            config = IoC.Get<SystemConfig>();
            cityCtx = IoC.Get<CityContextViewModel>();
            InternalSetLocation(stationLocation);
        }

        private void InternalSetLocation(StationLocation stationLocation)
        {
            this.stationLocation = stationLocation;
            if (stationLocation == null)
                GeoLocation = null;
            else
                GeoLocation = new GeoCoordinate(stationLocation.Latitude, stationLocation.Longitude);
        }

        private void SetLocation(StationLocation stationLocation)
        {
            InternalSetLocation(stationLocation);
            NotifyOfPropertyChange(() => GeoLocation);
        }

        public int Number { get { return stationLocation.Number; } }
        public string StationName { get { return stationLocation.Name; } }
        public string Address { get { return stationLocation.Address; } }
        public string FullAddress { get { return stationLocation.FullAddress; } }
        public double Latitude { get { return stationLocation.Latitude; } }
        public double Longitude { get { return stationLocation.Longitude; } }
        public bool Open { get { return stationLocation.Open; } }
        public bool IsFavorite { get { return stationLocation.IsFavorite; } set { SetFavorite(value); } }

        public GeoCoordinate GeoLocation { get; private set; }
        private GeoCoordinate currentLocation;
        public GeoCoordinate CurrentLocation {
            get { return currentLocation; }
            set {
                if (value == currentLocation) return;
                currentLocation = value;
                NotifyOfPropertyChange(() => DistanceValueString);
                NotifyOfPropertyChange(() => DistanceString);
            }
        }
        private double travelDistance = double.NaN;
        private double travelDuration = double.NaN;
        public double Distance {
            get
            {
                if (!double.IsNaN(travelDistance))
                    return travelDistance;
                double result = double.NaN;
                if (GeoLocation != null && CurrentLocation != null)
                    result = GeoLocation.GetDistanceTo(CurrentLocation);
                return result;
            }
        }

        public string DistanceString
        {
            get
            {
                if (double.IsNaN(Distance))
                    if (config.LocationEnabled.GetValueOrDefault())
                    {
                        if (cityCtx.IsCurrentCitySelected())
                            return "no location information";
                        else
                            return "currently not in the selected city";
                    }
                    else
                        return "location services are turned off";
                else
                    return string.Format("distance to station {0}", LocationHelper.GetDistanceString(Distance, config.UseImperialUnits));
            }
        }

        public string DistanceValueString
        {
            get
            {
                if (double.IsNaN(Distance))
                    return "";
                else
                    return LocationHelper.GetDistanceString(Distance, config.UseImperialUnits);
            }
        }

        public string DurationString
        {
            get
            {
                if (double.IsNaN(travelDuration))
                    return "travel duration not available";
                else
                    return string.Format("travel duration {0}", GetDurationString(travelDuration));
            }
        }

        private string GetDurationString(double travelDuration)
        {
            return TimeSpan.FromSeconds(travelDuration).ToString();
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            OptimumMapZoom(view as Detail);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            ReactiveExtensions.Dispose(ref dispCurrentAddr);
        }

        public LocationRect ViewRect;
        private Detail view;
        private IDisposable dispCurrentAddr = null;

        public void OptimumMapZoom(Detail ov)
        {
            view = ov;
            if (view != null)
            {
                if (ViewRect != null) 
                    view.Map.SetView(ViewRect);

                if (!config.LocationEnabled.GetValueOrDefault())
                {
                    var myPin = view.Map.Children.OfType<Pushpin>().Where(p => p.Name == "CurrentLocation").FirstOrDefault();
                    if (myPin != null)
                        myPin.Visibility = System.Windows.Visibility.Collapsed;
                    return;
                }
                else if (dispCurrentAddr == null)
                    dispCurrentAddr = LocationHelper.GetCurrentGeoAddress()
                        .Catch<GeoAddress, WebException>(webex =>
                        {
                            dispCurrentAddr = null;
                            string msg = "could not get the current address. check your internet connection.";
                            events.Publish(new ErrorState(webex, msg));
                            return Observable.Empty<GeoAddress>();
                        })
                        .ObserveOn(ThreadPoolScheduler.Instance)
                        // todo: do it once, then only if the user refreshes
                        .Subscribe(geoAddr => CalculateRoute(geoAddr.Coordinate, GeoLocation));
            }
        }

        public void CalculateRoute(GeoCoordinate from, GeoCoordinate to)
        {
            CurrentLocation = from;
            NotifyOfPropertyChange(() => CurrentLocation);

            if (!cityCtx.IsCurrentCitySelected())
                return;

            events.Publish(BusyState.Busy("calculating route..."));
            LocationHelper.CalculateRoute(new GeoCoordinate[] { from, to })
                //.ObserveOn(ReactiveExtensions.SyncScheduler)
                .Subscribe(
                    nav => MapRoute(nav),
                    e => events.Publish(new ErrorState(e, "could not calculate route")));
        }

        private void MapRoute(NavigationResponse routeResponse)
        {
            try
            {
                if (routeResponse == null)
                    return;

                travelDistance = 1000 * routeResponse.Route.TravelDistance;
                var walkingSpeed = LocationHelper.GetTravelSpeed(TravelType.Walking, config.WalkingSpeed, false);
                travelDuration = 3.6 * travelDistance / walkingSpeed;
                travelDuration = (int)travelDuration;
                var points = from pt in routeResponse.Route.RoutePath.Points
                             select new GeoCoordinate
                             {
                                 Latitude = pt.Latitude,
                                 Longitude = pt.Longitude
                             };
                LocationCollection locCol = new LocationCollection();
                foreach (var loc in points)
                    locCol.Add(loc);

                Execute.OnUIThread(() =>
                {
                    MapPolyline pl = new MapPolyline();
                    pl.Stroke = new SolidColorBrush(Colors.Blue);
                    pl.StrokeThickness = 5;
                    pl.Opacity = 0.7;
                    pl.Locations = locCol;
                    pl.CacheMode = new BitmapCache();
                    view.Route.Children.Clear();
                    view.Route.Children.Add(pl);
                    view.Map.SetView(LocationRect.CreateLocationRect(points));
                    NotifyOfPropertyChange(() => DistanceString);
                    NotifyOfPropertyChange(() => DurationString);
                    events.Publish(BusyState.NotBusy());
                });
            }
            finally
            {
                events.Publish(BusyState.NotBusy());
            }
        }

        public void ToggleFavorite()
        {
            SetFavorite(!IsFavorite);
        }

        private void SetFavorite(bool value)
        {
            stationLocation.IsFavorite = value;
            events.Publish(new FavoriteState(new FavoriteLocation(this.Location), IsFavorite));
            NotifyOfPropertyChange(() => IsFavorite);
        }
    }
}