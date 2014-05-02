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
#if WP7
using Microsoft.Phone.Controls.Maps;
#else
using Microsoft.Phone.Maps.Controls;
using PublicBikes.Tools;
#endif
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections;

namespace Bicikelj.ViewModels
{
    public class NavigationViewModel : MapBaseViewModel
    {
        public NavigationViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
            : base(events, config, cityContext)
        {
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

        public LocationViewModel DestinationLocation { get; set; }
        private GeoCoordinate lastCoordinate = null;

        public GeoCoordinate DestinationCoordinate
        {
            get { return DestinationLocation != null ? DestinationLocation.Coordinate : null; }
            set { SetDestinatioCoordinate(value); }
        }

        private void SetDestinatioCoordinate(GeoCoordinate value)
        {
            if (DestinationLocation == null) return;
            DestinationLocation.Coordinate = value;
            NotifyOfPropertyChange(() => DestinationCoordinate);
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            this.view = view as NavigationView;
            if (this.view != null)
            {
                this.map = this.view.Map;
                this.WireupMap();
                map.Tap += HandleMapTap;
                map.MouseLeftButtonDown += HandleMapMouseLeftButtonDown;
#if !WP7
                BindMapItems();
#endif
            }
        }

#if !WP7
        private void BindMapItems()
        {
            if (view == null || map == null) return;
            view.RouteLayer = map.Layers[0];
            view.CurrentLocationLayer = map.Layers[1];
            foreach (var overlay in view.CurrentLocationLayer)
                overlay.BindCoordinate("CurrentCoordinate", this);

            view.DestinationLocationLayer = map.Layers[2];
            view.DestinationLocationLayer[0].BindCoordinate("DestinationCoordinate", this);
            view.RouteLayer.SetItemsCollection(this.RouteLegs, view.Resources["routeLegPinTemplate"] as DataTemplate);
        }
#endif

        void HandleMapMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(map);
            MapTappedOn(p);
        }

        void HandleMapTap(object sender, GestureEventArgs e)
        {
            var p = e.GetPosition(map);
            MapTappedOn(p);
        }

        private void MapTappedOn(System.Windows.Point p)
        {
#if WP7
            var c = map.ViewportPointToLocation(p);
#else
            var c = map.ConvertViewportPointToGeoCoordinate(p);
#endif
            lastCoordinate = c;
        }

        private void NavigateTo(GeoCoordinate c)
        {
            LocationHelper.FindAddress(c).Subscribe(addr =>
            {
                if (addr != null)
                {
                    ToLocation = addr.FormattedAddress;
                    Address = ToLocation;
                }
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
                DestinationCoordinate = NavigateRequest.Coordinate;

                var c = NavigateRequest.Coordinate;
                if (c != null && !c.IsUnknown && !(c.Latitude == 0 && c.Longitude == 0))
                    TakeMeTo(NavigateRequest.Coordinate);
                else if (!string.IsNullOrEmpty(NavigateRequest.Address))
                    TakeMeTo(NavigateRequest.Address);
                else
                    TakeMeTo(NavigateRequest.LocationName);
                NavigateRequest = null;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            CheckNavigateRequest();
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
                events.Publish(new ErrorState(e.Error, "Could not get station availability."));
        }

        private void FindNearestStations(GeoCoordinate fromLocation, GeoCoordinate toLocation)
        {
            cityContext.GetStations()
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Where(s => s != null)
                .Take(1)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(s  => {
                    StationLocation fromStation = null;
                    StationLocation toStation = null;
                    var walkingSpeed = LocationHelper.GetTravelSpeed(TravelType.Walking, config.WalkingSpeed, false);
                    var cyclingSpeed = LocationHelper.GetTravelSpeed(TravelType.Cycling, config.CyclingSpeed, false);

                    var nearStart = LocationHelper.SortByLocation(s, fromLocation, cyclingSpeed, toLocation, walkingSpeed).ToObservable()
                        .ObserveOn(ThreadPoolScheduler.Instance)
#if WP7
                        .Select(station => cityContext.GetAvailability2(station).First())
#else
                        .Select(station => cityContext.GetAvailability2(station).Take(1).Wait())
#endif
                        .Where(avail => avail.Availability.Open && avail.Availability.Available > 0)
                        .Do(avail => fromStation = avail.Station)
                        .Take(1);

                    var nearFinish = LocationHelper.SortByLocation(s, toLocation, cyclingSpeed, fromLocation, walkingSpeed).ToObservable()
                        .ObserveOn(ThreadPoolScheduler.Instance)
#if WP7
                        .Select(station => cityContext.GetAvailability2(station).First())
#else
                        .Select(station => cityContext.GetAvailability2(station).Take(1).Wait())
#endif
                        .Where(avail => avail.Availability.Open && avail.Availability.Free > 0)
                        .Do(avail => toStation = avail.Station)
                        .Take(1); // add to options how many stations to try

                    List<ObjectWithState<NavigationResponse>> routes = new List<ObjectWithState<NavigationResponse>>();

                    nearFinish
                        .ObserveOn(ThreadPoolScheduler.Instance)
                        .SelectMany(stationAvail =>
                        {
                            IList<GeoCoordinate> navPoints = new List<GeoCoordinate>();
#if WP7
                            fromStation = nearStart.First().Station;
#else
                            fromStation = nearStart.Take(1).Wait().Station;
#endif
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
                        error => {
                            events.Publish(new ErrorState(error, "Could not find nearest stations."));
                        },
                        () =>
                        {
                            var validRoutes = routes.Where(r => !r.Object.HasErrors).ToList();
                            var navErrors = routes.Where(r => r.Object.HasErrors).ToList();
                            if (validRoutes.Count > 0)
                            {
                                var shortestRoute = validRoutes.OrderBy(nav => GetTravelDuration(nav.Object, config)).FirstOrDefault();
                                MapRoute(shortestRoute.Object, shortestRoute.State as IEnumerable<GeoCoordinate>);
                            }
                            else if (navErrors.Count > 0)
                            {
                                string errorStr = "";
                                foreach (var err in navErrors)
                                    errorStr += err.Object.ErrorString;
                                
                                events.Publish(new ErrorState(new Exception(errorStr), "Could not calculate route."));
                            }
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

        public ObservableCollection<NavigationRouteLegViewModel> RouteLegs { get; private set; }

        private MapPolyline mapLine = null;
        private void MapRoute(IEnumerable<GeoCoordinate> points, IEnumerable<GeoCoordinate> navPoints)
        {
#if WP7
            LocationCollection locCol = new LocationCollection();
            foreach (var loc in points)
                locCol.Add(loc);
            LocationRect viewRect = LocationRect.CreateLocationRect(points);
#else
            LocationRectangle viewRect = LocationRectangle.CreateBoundingRectangle(points);
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
                    
                // clear the route and remove pins other than CurrentPos and Destination
                view.Route.Children.Clear();
                view.Route.Children.Add(mapLine);
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
#endif
                RouteLegs.Clear();
                int idxPoint = 0;
                int idxDest = navPoints.Count() - 1;
                foreach (var point in navPoints)
                {
                    if (idxPoint == 0)
                        CurrentCoordinate = point;
                    else if (idxPoint == idxDest)
                        DestinationCoordinate = point;
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
            });

            NotifyOfPropertyChange(() => DistanceString);
            NotifyOfPropertyChange(() => DurationString);
            NotifyOfPropertyChange(() => CanToggleFavorite);
        }

        public void TrySearch()
        {
            App.CurrentApp.LogAnalyticEvent("Navigation: Search");
            TakeMeTo(ToLocation);
        }

        public void TakeMeTo(string address)
        {
            if (string.IsNullOrEmpty(address)) return;
            var wasFavorite = IsFavorite;
            Address = "";
            IsFavorite = false;
            events.Publish(BusyState.Busy("searching..."));
            var localAddress = address;
            if (!string.IsNullOrWhiteSpace(config.City) && !localAddress.ToLowerInvariant().Contains(config.City.ToLowerInvariant()))
                localAddress = localAddress + ", " + config.City;
            LocationHelper.FindLocation(localAddress, CurrentCoordinate).Subscribe(r =>
            {
                if (r == null || r.Location == null)
                {
                    string errorStr = "";
                    if (r.HasErrors)
                        errorStr = r.ErrorString;
                    events.Publish(new ErrorState(new Exception(errorStr), "Could not find location."));
                    return;
                }
                if (r.Location.Address != null)
                    Address = r.Location.Address.FormattedAddress;
                    
                if (string.IsNullOrEmpty(Address))
                    Address = address;
                this.DestinationLocation.LocationName = address;
                NotifyOfPropertyChange(() => CanToggleFavorite);
                var coordinate = new GeoCoordinate(r.Location.Point.Latitude, r.Location.Point.Longitude);
                if (wasFavorite)
                {
                    DestinationCoordinate = coordinate;
                    events.Publish(new FavoriteState(GetFavorite(DestinationLocation), true));
                }
                TakeMeTo(coordinate);
            },
            e => events.Publish(new ErrorState(e, "Could not find location.")),
            () => events.Publish(BusyState.NotBusy()));
        }

        public void TakeMeTo(GeoCoordinate location)
        {
            lastCoordinate = location;
            if (config.LocationEnabled.GetValueOrDefault())
                LocationHelper.GetCurrentLocation()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(5))
                    .Subscribe(c => {
                        CurrentCoordinate = c.Coordinate;
                        FindBestRoute(c.Coordinate, location);
                },
                e => events.Publish(new ErrorState(e, "Could not get current location.")));
            else if (CurrentCoordinate != null)
                FindBestRoute(CurrentCoordinate, location);
            NotifyOfPropertyChange(() => CanRefreshRoute);
        }

        public bool CanRefreshRoute { get { return lastCoordinate != null && !lastCoordinate.IsUnknown; } }
        public void RefreshRoute()
        {
            if (CanRefreshRoute)
                TakeMeTo(lastCoordinate);
        }

        private bool isFavorite;
        public bool IsFavorite
        {
            get { return isFavorite; }
            set { SetFavorite(value); }
        }
        
        public bool CanToggleFavorite
        {
            get { return !string.IsNullOrWhiteSpace(DestinationLocation.LocationName) || DestinationCoordinate != null; }
        }

        public void ToggleFavorite()
        {
            SetFavorite(!IsFavorite);
            if (string.IsNullOrWhiteSpace(DestinationLocation.LocationName) && DestinationCoordinate == null)
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