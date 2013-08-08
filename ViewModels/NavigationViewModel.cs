using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Bicikelj.Model;
using Bicikelj.Model.Bing;
using Bicikelj.Views;
using Caliburn.Micro;
using Caliburn.Micro.Contrib.Dialogs;
using Microsoft.Phone.Controls.Maps;
using System.Windows;
using System.Collections.ObjectModel;

namespace Bicikelj.ViewModels
{
    public class NavigationViewModel : Screen
    {
        readonly IEventAggregator events;
        private SystemConfig config;
        private CityContextViewModel cityContext;

        public NavigationViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
        {
            this.events = events;
            this.cityContext = cityContext;
            this.config = config;
            this.CurrentLocation = new LocationViewModel();
            this.DestinationLocation = new LocationViewModel();
            this.RouteLegs = new ObservableCollection<NavigationRouteLegViewModel>();
        }

        private double travelDistance = double.NaN;
        private double travelDuration = double.NaN;
        private NavigationView view;

        public LocationViewModel NavigateRequest { get; set; }

        public string DistanceString
        {
            get
            {
                if (double.IsNaN(travelDistance))
                    return "travel distance not available";
                else
                    return string.Format("travel distance {0}", LocationHelper.GetDistanceString(travelDistance, config.UseImperialUnits));
            }
        }

        public string DurationString
        {
            get
            {
                if (double.IsNaN(travelDuration))
                    return "travel duration not available";
                else
                    return string.Format("travel duration {0}", TimeSpan.FromSeconds(travelDuration).ToString());
            }
        }

        private string fromLocation;
        public string FromLocation
        {
            get { return fromLocation; }
            set
            {
                if (value == fromLocation)
                    return;
                fromLocation = value;
                NotifyOfPropertyChange(() => FromLocation);
            }
        }

        private string toLocation;
        public string ToLocation {
            get { return toLocation; }
            set
            {
                if (value == toLocation)
                    return;
                toLocation = value;
                NotifyOfPropertyChange(() => ToLocation);
            }
        }

        public string Address {
            get { return DestinationLocation != null ? DestinationLocation.Address : ""; }
            set {
                if (DestinationLocation != null)
                {
                    DestinationLocation.Address = value;
                    NotifyOfPropertyChange(() => Address);
                }
            }
        }

        public LocationViewModel CurrentLocation { get; set; }
        public LocationViewModel DestinationLocation { get; set; }
        private IDisposable currentGeo;
        private IDisposable stationObs;
        private GeoCoordinate lastCoordinate = null;
        private Map map;

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            var navView = view as NavigationView;
            if (this.view == navView || navView == null)
                return;
            this.view = navView;
            this.map = navView.Map;
            map.Tap += HandleMapTap;
            map.MouseLeftButtonDown += HandleMapMouseLeftButtonDown;
        }

        void HandleMapMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(map);
            var c = map.ViewportPointToLocation(p);
            lastCoordinate = c;
        }

        void HandleMapTap(object sender, GestureEventArgs e)
        {
            var p = e.GetPosition(map);
            var c = map.ViewportPointToLocation(p);
            lastCoordinate = c;
            // maybe show a popup with location info and a command for navigate to
        }

        private void NavigateTo(GeoCoordinate c)
        {
            LocationHelper.FindAddress(c).Subscribe(addr =>
            {
                if (addr != null)
                    ToLocation = addr.FormattedAddress;
            });
            TakeMeTo(c);
        }

        public void NavigateToLastCoordinate()
        {
            if (lastCoordinate != null)
                NavigateTo(lastCoordinate);
        }

        private void CheckNavigateRequest()
        {
            if (NavigateRequest != null)
            {
                IsFavorite = true;
                ToLocation = NavigateRequest.LocationName;
                if (string.IsNullOrWhiteSpace(ToLocation))
                    ToLocation = NavigateRequest.Address;
                Address = NavigateRequest.Address;
                DestinationLocation.LocationName = NavigateRequest.LocationName;
                DestinationLocation.Coordinate = NavigateRequest.Coordinate;
                if (NavigateRequest.Coordinate != null)
                    TakeMeTo(NavigateRequest.Coordinate);
                else
                    TakeMeTo(NavigateRequest.LocationName);
                NavigateRequest = null;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            var syncContext = ReactiveExtensions.SyncScheduler;
            if (currentGeo == null)
                currentGeo = LocationHelper.GetCurrentGeoAddress()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Where(location => location != null)
                    .ObserveOn(syncContext)
                    .Subscribe(location =>
                    {
                        CurrentLocation.Coordinate = location.Coordinate;
                        FromLocation = location.Address.FormattedAddress;
                    });
            
            if (stationObs == null)
                stationObs = cityContext.GetStations()// load stations in background
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Where(s => s != null)
                    .Select(s => LocationHelper.GetLocationRect(s))
                    .Where(r => r != null)
                    .ObserveOn(syncContext)
                    .Subscribe(r => this.view.Map.SetView(r));

            CheckNavigateRequest();
        }

        protected override void OnDeactivate(bool close)
        {
            ReactiveExtensions.Dispose(ref currentGeo);
            ReactiveExtensions.Dispose(ref stationObs);
            base.OnDeactivate(close);
        }

        private void FindBestRoute(GeoCoordinate fromLocation, GeoCoordinate toLocation)
        {
            events.Publish(BusyState.Busy("calculating route..."));
            FindNearestStations(fromLocation, toLocation);
        }

        private void HandleAvailabilityError(object sender, ResultCompletionEventArgs e)
        {
            events.Publish(BusyState.NotBusy());
            if (e.Error != null)
                events.Publish(new ErrorState(e.Error, "could not get station availability"));
        }

        private void FindNearestStations(GeoCoordinate fromLocation, GeoCoordinate toLocation)
        {
            cityContext.GetStations().Where(s => s != null)
                .Take(1)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(s  => {
                    StationLocation fromStation = null;
                    StationLocation toStation = null;
                    var walkingSpeed = LocationHelper.GetTravelSpeed(TravelType.Walking, config.WalkingSpeed, false);
                    var cyclingSpeed = LocationHelper.GetTravelSpeed(TravelType.Cycling, config.CyclingSpeed, false);

                    var nearStart = LocationHelper.SortByLocation(s, fromLocation, cyclingSpeed, toLocation, walkingSpeed).ToObservable()
                        .ObserveOn(ThreadPoolScheduler.Instance)
                        .Select(station => StationLocationList.GetAvailability2(station).First())
                        .Where(avail => avail.Availability.Available > 0)
                        .Do(avail => fromStation = avail.Station)
                        .Take(1);

                    var nearFinish = LocationHelper.SortByLocation(s, toLocation, cyclingSpeed, fromLocation, walkingSpeed).ToObservable()
                        .ObserveOn(ThreadPoolScheduler.Instance)
                        .Select(station => StationLocationList.GetAvailability2(station).First())
                        .Where(avail => avail.Availability.Free > 0)
                        .Do(avail => toStation = avail.Station)
                        .Take(1); // add to options how many stations to try

                    List<ObjectWithState<NavigationResponse>> routes = new List<ObjectWithState<NavigationResponse>>();

                    nearFinish
                        .ObserveOn(ThreadPoolScheduler.Instance)
                        .SelectMany(stationAvail =>
                        {
                            IList<GeoCoordinate> navPoints = new List<GeoCoordinate>();
                            fromStation = nearStart.First().Station;
                            toStation = stationAvail.Station;
                            navPoints.Add(fromLocation);
                            // if closest bike station is the same as the destination station or the destination is closer than the station then don't use the bikes
                            if (fromStation != toStation
                                && fromLocation.GetDistanceTo(fromStation.Coordinate) < fromLocation.GetDistanceTo(toLocation)
                                && toLocation.GetDistanceTo(toStation.Coordinate) < fromLocation.GetDistanceTo(toLocation))
                            {
                                navPoints.Add(fromStation.Coordinate);
                                navPoints.Add(toStation.Coordinate);
                            }
                            navPoints.Add(toLocation);
                            return LocationHelper.CalculateRouteEx(navPoints);
                        })
                        .Finally(() => events.Publish(BusyState.NotBusy()))
                        .Subscribe(nav => {
                            routes.Add(nav);
                        },
                        () =>
                        {
                            var shortestRoute = routes.OrderBy(nav => GetTravelDuration(nav.Object, config)).FirstOrDefault();
                            MapRoute(shortestRoute.Object, shortestRoute.State as IEnumerable<GeoCoordinate>);
                        });
                });
        }

        public static double GetTravelDuration(NavigationResponse routeResponse, SystemConfig config)
        {
            double travelDist;
            return GetTravelDuration(routeResponse, config, out travelDist);
        }

        public static double GetTravelDuration(NavigationResponse routeResponse, SystemConfig config, out double travelDistance)
        {
            travelDistance = 1000 * routeResponse.Route.TravelDistance;
            var travelDuration = routeResponse.Route.TravelDuration;
            if (routeResponse.Route.RouteLegs != null)
            {
                var routeLegs = routeResponse.Route.RouteLegs;
                if (routeLegs.Count == 1 || routeLegs.Count == 3)
                {
                    var walkingDistance = routeLegs[0].TravelDistance;
                    if (routeLegs.Count == 3)
                        walkingDistance += routeLegs[2].TravelDistance;
                    var walkingSpeed = LocationHelper.GetTravelSpeed(TravelType.Walking, config.WalkingSpeed, false);
                    travelDuration = 3600 * walkingDistance / walkingSpeed;
                    if (routeLegs.Count == 3)
                    {
                        var cyclingDistance = routeLegs[1].TravelDistance;
                        var cyclingSpeed = LocationHelper.GetTravelSpeed(TravelType.Cycling, config.CyclingSpeed, false);
                        travelDuration += 3600 * cyclingDistance / cyclingSpeed;
                    }
                    travelDuration = (int)travelDuration;
                }
            }
            return travelDuration;
        }

        private void CalculateRoute(IEnumerable<GeoCoordinate> navPoints)
        {
            LocationHelper.CalculateRoute(navPoints)
                .Subscribe(
                    n => MapRoute(n, null),
                    e => events.Publish(new ErrorState(e, "could not calculate route")),
                    () => events.Publish(BusyState.NotBusy()));
        }

        private void MapRoute(NavigationResponse routeResponse, IEnumerable<GeoCoordinate> navPoints)
        {
            if (routeResponse == null)
                return;

            travelDuration = GetTravelDuration(routeResponse, config, out travelDistance);
            var points = from pt in routeResponse.Route.RoutePath.Points
                            select new GeoCoordinate
                            {
                                Latitude = pt.Latitude,
                                Longitude = pt.Longitude
                            };

            MapRoute(points, navPoints);
        }

        public ObservableCollection<NavigationRouteLegViewModel> RouteLegs { get; set; }

        private void MapRoute(IEnumerable<GeoCoordinate> points, IEnumerable<GeoCoordinate> navPoints)
        {
            LocationCollection locCol = new LocationCollection();
            foreach (var loc in points)
                locCol.Add(loc);

            LocationRect viewRect = LocationRect.CreateLocationRect(points);

            Execute.OnUIThread(() =>
            {
                MapPolyline pl = new MapPolyline();
                pl.Stroke = new SolidColorBrush(Colors.Blue);
                pl.StrokeThickness = 5;
                pl.Opacity = 0.7;
                pl.Locations = locCol;
                    
                // clear the route and remove pins other than CurrentPos and Destination
                view.Route.Children.Clear();
                view.Route.Children.Add(pl);
                RouteLegs.Clear();
                    
                int idxPoint = 0;
                int idxDest = navPoints.Count() - 1;
                foreach (var point in navPoints)
                {
                    if (idxPoint == 0)
                        CurrentLocation.Coordinate = point;
                    else if (idxPoint == idxDest)
                        DestinationLocation.Coordinate = point;
                    else
                    {
                        PinType pinType = PinType.BikeStand;
                        if (idxPoint == idxDest - 1)
                            pinType = PinType.Walking;
                        RouteLegs.Add(new NavigationRouteLegViewModel() { Coordinate = point, LegType = pinType });
                    }
                    idxPoint++;
                }
                view.Map.SetView(viewRect);
                    
                view.Map.SetView(LocationRect.CreateLocationRect(points));
                NotifyOfPropertyChange(() => DistanceString);
                NotifyOfPropertyChange(() => DurationString);
                NotifyOfPropertyChange(() => CanToggleFavorite);
            });
        }

        public void TrySearch()
        {
            TakeMeTo(ToLocation);
        }

        public void TakeMeTo(string address)
        {
            Address = "";
            IsFavorite = false;
            events.Publish(BusyState.Busy("searching..."));
            var localAddress = address;
            if (!string.IsNullOrWhiteSpace(config.City) && !localAddress.ToLowerInvariant().Contains(config.City.ToLowerInvariant()))
                localAddress = localAddress + ", " + config.City;
            LocationHelper.FindLocation(localAddress, CurrentLocation.Coordinate).Subscribe(r =>
            {
                if (r == null || r.Location == null)
                {
                    events.Publish(new ErrorState(new Exception(), "could not find location"));
                    return;
                }
                if (r.Location.Address != null)
                    Address = r.Location.Address.FormattedAddress;
                    
                if (string.IsNullOrEmpty(Address))
                    Address = address;
                this.DestinationLocation.LocationName = address;
                NotifyOfPropertyChange(() => CanToggleFavorite);
                TakeMeTo(new GeoCoordinate(r.Location.Point.Latitude, r.Location.Point.Longitude));
            },
            e => events.Publish(new ErrorState(e, "could not find location")),
            () => events.Publish(BusyState.NotBusy()));
        }

        public void TakeMeTo(GeoCoordinate location)
        {
            if (config.LocationEnabled.GetValueOrDefault())
                LocationHelper.GetCurrentLocation().Take(1).Subscribe(c => {
                        CurrentLocation.Coordinate = c.Coordinate;
                        FindBestRoute(c.Coordinate, location);
                },
                e => events.Publish(new ErrorState(e, "could not get current location")));
            else if (CurrentLocation.Coordinate != null)
                FindBestRoute(CurrentLocation.Coordinate, location);
        }

        private bool isFavorite;
        public bool IsFavorite
        {
            get { return isFavorite; }
            set { SetFavorite(value); }
        }
        
        public bool CanToggleFavorite
        {
            get { return !string.IsNullOrWhiteSpace(DestinationLocation.LocationName) || DestinationLocation.Coordinate != null; }
        }

        public void ToggleFavorite()
        {
            SetFavorite(!IsFavorite);
            if (string.IsNullOrWhiteSpace(DestinationLocation.LocationName) && DestinationLocation.Coordinate == null)
                return;
            events.Publish(new FavoriteState(GetFavorite(DestinationLocation), IsFavorite));
        }

        private static FavoriteLocation GetFavorite(LocationViewModel location)
        {
            return new FavoriteLocation(location.LocationName)
            {
                Address = location.Address,
                Coordinate = location.Coordinate
            };
        }

        private void SetFavorite(bool value)
        {
            if (value == isFavorite) return;
            isFavorite = value;
            NotifyOfPropertyChange(() => IsFavorite);
            NotifyOfPropertyChange(() => CanEditName);
        }

        public bool CanEditName
        {
            get { return IsFavorite; }
        }

        public void EditName_()
        {
            LocationViewModel lvm = new LocationViewModel();
            lvm.Address = DestinationLocation.Address;
            lvm.LocationName = DestinationLocation.LocationName;
            IWindowManager wm = IoC.Get<IWindowManager>();
        }

        public IEnumerable<IResult> EditName()
        {
            LocationViewModel lvm = new LocationViewModel();
            if (string.IsNullOrWhiteSpace(Address))
                Address = DestinationLocation.LocationName;
            lvm.Address = DestinationLocation.Address;
            lvm.LocationName = DestinationLocation.LocationName;
            
            var question = new Dialog<Answer>(DialogType.Question,
                "location name",							  
                lvm,
                Answer.Ok,
                Answer.Cancel);

            yield return question.AsResult();

            if (question.GivenResponse == Answer.Ok)
            {
                events.Publish(new FavoriteState(GetFavorite(DestinationLocation), false));
                DestinationLocation.LocationName = lvm.LocationName;
                events.Publish(new FavoriteState(GetFavorite(DestinationLocation), true));
            };
        }
    }
}