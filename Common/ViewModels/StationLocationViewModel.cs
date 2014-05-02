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
using System.Net;
#if WP7
using Microsoft.Phone.Controls.Maps;
#else
using Microsoft.Phone.Maps.Controls;
using PublicBikes.Tools;
using Microsoft.Phone.Maps.Services;
#endif

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

        public StationLocationViewModel(StationLocation stationLocation, GeoCoordinate currentPos)
            : this(stationLocation)
        {
            SetCurrentCoordinate(currentPos);
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
        private GeoCoordinate currentCoordinate;
        public GeoCoordinate CurrentCoordinate {
            get { return currentCoordinate; }
            set {
                if (value == currentCoordinate) return;
                currentCoordinate = value;
                NotifyOfPropertyChange(() => DistanceValueString);
                NotifyOfPropertyChange(() => DistanceString);
                NotifyOfPropertyChange(() => IsLocationAvailable);
            }
        }
        public void SetCurrentCoordinate(GeoCoordinate value)
        {
            currentCoordinate = value;
        }

        public bool IsLocationAvailable { get { return CurrentCoordinate != null && !CurrentCoordinate.IsUnknown; } }

        private GeoCoordinate startLocation = null;
        private double travelDistance = double.NaN;
        private double travelDuration = double.NaN;
        public double Distance {
            get
            {
                if (!double.IsNaN(travelDistance) && IsNearStartLocation())
                    return travelDistance;
                double result = double.NaN;
                if (GeoLocation != null && CurrentCoordinate != null)
                    result = GeoLocation.GetDistanceTo(CurrentCoordinate);
                return result;
            }
        }

        public bool IsNearStartLocation()
        {
            return startLocation != null && CurrentCoordinate != null && startLocation.GetDistanceTo(currentCoordinate) < 50;
        }

        public bool IsLocationEnabled { get { return config.LocationEnabled.GetValueOrDefault(); } }

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

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            ReactiveExtensions.Dispose(ref dispCurrentAddr);
        }

#if WP7
        public LocationRect ViewRect;
#else
        public LocationRectangle ViewRect;
#endif

        private Detail view;
        private IDisposable dispCurrentAddr = null;
        private bool routeCalculated = false;


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            this.view = view as Detail;
#if !WP7
            BindMapItems();
#endif
        }

#if !WP7
        private void BindMapItems()
        {
            if (view.Map == null) return;
            view.RouteLayer = view.Map.Layers[0];
            view.CurrentLocationLayer = view.Map.Layers[1];
            foreach (var overlay in view.CurrentLocationLayer)
                overlay.BindCoordinate("CurrentCoordinate", this);

            view.GeoLocationLayer = view.Map.Layers[2];
            view.GeoLocationLayer[0].BindCoordinate("GeoLocation", this);
            view.StationsLayer = view.Map.Layers[3];
        }
#endif

        public void OptimumMapZoom(Detail ov)
        {
            view = ov;
            if (view != null)
            {
                if (ViewRect != null) 
                    view.Map.SetView(ViewRect);

                if (!config.LocationEnabled.GetValueOrDefault())
                {
                    
                }
                else if (dispCurrentAddr == null)
                    dispCurrentAddr = LocationHelper.GetCurrentLocation()
                        .ObserveOn(ThreadPoolScheduler.Instance)
                        .Subscribe(geoAddr => CalculateRoute(geoAddr.Coordinate, GeoLocation));
            }
        }

        public void CalculateRoute(GeoCoordinate from, GeoCoordinate to)
        {
            CurrentCoordinate = from;
            startLocation = from;
            NotifyOfPropertyChange(() => CurrentCoordinate);

            if (routeCalculated || !cityCtx.IsCurrentCitySelected())
                return;

            events.Publish(BusyState.Busy("calculating route..."));
            LocationHelper.CalculateRoute(new GeoCoordinate[] { from, to })
                //.ObserveOn(ReactiveExtensions.SyncScheduler)
                .Finally(() => routeCalculated = true)
                .Subscribe(
                    nav => MapRoute(nav),
                    e => events.Publish(new ErrorState(e, "Could not calculate route.")));
        }

        public void RefreshRoute()
        {
            routeCalculated = false;
            CalculateRoute(CurrentCoordinate, GeoLocation);
        }

        private MapPolyline mapLine = null;

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
#if WP7
                LocationCollection locCol = new LocationCollection();
                foreach (var loc in points)
                    locCol.Add(loc);
#endif

                Execute.OnUIThread(() =>
                {
#if WP7
                    mapLine = new MapPolyline();
                    mapLine.Stroke = new SolidColorBrush(Colors.Blue);
                    mapLine.StrokeThickness = 5;
                    mapLine.Opacity = 0.7;
                    mapLine.Locations = locCol;
                    mapLine.CacheMode = new BitmapCache();

                    view.Route.Children.Clear();
                    view.Route.Children.Add(mapLine);
                    view.Map.SetView(LocationRect.CreateLocationRect(points));
#else
                    if (mapLine == null)
                    {
                        mapLine = new MapPolyline();
                        mapLine.StrokeColor = Color.FromArgb(178, 0, 0, 255);
                        mapLine.StrokeThickness = 5;
                        view.Map.MapElements.Add(mapLine);
                    }

                    mapLine.Path.Clear();
                    foreach (var point in points)
                        mapLine.Path.Add(point);
                    
                    view.Map.SetView(LocationRectangle.CreateBoundingRectangle(points));
#endif

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