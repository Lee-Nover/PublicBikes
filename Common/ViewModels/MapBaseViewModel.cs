using Bicikelj.Model;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
#if !WP7
using PublicBikes.Tools;
using MapControls = Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Controls;
#else
using MapControls = Microsoft.Phone.Controls.Maps;
#endif

namespace Bicikelj.ViewModels
{
    public class MapBaseViewModel : Conductor<StationViewModel>.Collection.AllActive
    {
        protected MapControls.Map map = null;
        private IDisposable currentGeo;
        private IDisposable stationObs;
        private IDisposable cityObs;
        private ICompassProvider compassProvider;
        protected IEventAggregator events;
        protected SystemConfig config;
        protected CityContextViewModel cityContext;
        protected bool zoomDone = false;
        protected bool tilesLoaded = false;

        public MapBaseViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
        {
            this.events = events;
            this.config = config;
            this.cityContext = cityContext;
            this.CurrentLocation = new LocationViewModel();
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

        public LocationViewModel CurrentLocation { get; set; }
        public GeoCoordinate CurrentCoordinate
        {
            get { return CurrentLocation != null ? CurrentLocation.Coordinate : null; }
            set { SetCurrentCoordinate(value); }
        }

        protected virtual void SetCurrentCoordinate(GeoCoordinate value)
        {
            if (CurrentLocation == null) return;
            CurrentLocation.Coordinate = value;
            NotifyOfPropertyChange(() => CurrentCoordinate);
            NotifyOfPropertyChange(() => IsLocationAvailable);
            NotifyOfPropertyChange(() => CanCenterCurrentLocation);
        }

        public bool IsLocationAvailable { get { return CurrentLocation != null ? CurrentLocation.IsAvailable : false; } }

        protected override void OnActivate()
        {
            // remove the Items because AllActive will Activate all of them
            Items.Clear();
            InitObservables();
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                ReactiveExtensions.Dispose(ref currentGeo);
                ReactiveExtensions.Dispose(ref stationObs);
                ReactiveExtensions.Dispose(ref cityObs);
                if (compassProvider != null)
                    compassProvider.HeadingChanged -= compassProvider_HeadingChanged;
                compassProvider = null;
            }
            base.OnDeactivate(close);
        }

        private void InitObservables()
        {
            var syncContext = ReactiveExtensions.SyncScheduler;
            if (currentGeo == null)
            {
                if (LocationHelper.IsLocationEnabled)
                    CurrentCoordinate = LocationHelper.LastCoordinate;
                else
                    CurrentCoordinate = null;

                currentGeo = LocationHelper.GetCurrentLocation()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Where(location => location != null)
                    .ObserveOn(syncContext)
                    .Subscribe(OnLocationChanged, OnLocationChangeError);
            }

            if (stationObs == null)
                stationObs = cityContext.GetStations()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Where(sl => sl != null)
                    .ObserveOn(syncContext)
                    .Subscribe(OnGotStations, OnGetStationsError);

            if (cityObs == null)
                cityObs = cityContext.CityObservable
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Subscribe(city => {
                        initialZoomDone = false;
                    });

            if (compassProvider == null)
            {
                if (App.Current.Resources.Contains("CompassProvider"))
                    compassProvider = App.Current.Resources["CompassProvider"] as ICompassProvider;
                else
                    compassProvider = new CompassProvider();

                compassProvider.HeadingChanged += compassProvider_HeadingChanged;
            }
        }

        private double lastHeading = 0;
        void compassProvider_HeadingChanged(object sender, HeadingAndAccuracy e)
        {
            var headingDelta = Math.Abs(lastHeading - e.Heading);
            if (headingDelta >= 2)
            {
                lastHeading = e.Heading;
                if (!isCentering && MapFollowsHeading)
                    map.SetView(map.Center, map.ZoomLevel, lastHeading, 60, MapAnimationKind.Parabolic);
            }
        }

        private bool mapFollowsHeading;

        public bool MapFollowsHeading
        {
            get { return mapFollowsHeading; }
            set { 
                mapFollowsHeading = value;
                CheckMapHeading();
            }
        }


        private void CheckMapHeading()
        {
            if (MapFollowsHeading)
            {
                isCentering = true;
                var newCenter = CanCenterCurrentLocation ? CurrentCoordinate : map.Center;
                map.TransformCenter = new System.Windows.Point(0.5, 0.75);
                map.SetView(newCenter, Math.Max(16, map.ZoomLevel), lastHeading, 60, MapAnimationKind.Parabolic);
                compassProvider.HeadingChanged += compassProvider_HeadingChanged;
            }
            else if (!MapFollowsHeading)
            {
                map.TransformCenter = new System.Windows.Point(0.5, 0.5);
                map.SetView(map.Center, map.ZoomLevel, 0, 0, MapAnimationKind.Parabolic);
            }
        }

        public void ToggleMapHeading()
        {
            MapFollowsHeading = !MapFollowsHeading;
            if (MapFollowsHeading)
                FixedHeading = 0;
            else
                FixedHeading = null;
        }

        private double? fixedHeading;

        public double? FixedHeading
        {
            get { return fixedHeading; }
            set { 
                fixedHeading = value;
                NotifyOfPropertyChange(() => FixedHeading);
            }
        }
        

        private void OnLocationChanged(GeoStatusAndPos pos)
        {
            var status = pos.Status.GetValueOrDefault();
            if (status != GeoPositionStatus.Ready && status != GeoPositionStatus.NoData)
            {
                CurrentCoordinate = null;
                FromLocation = "";
            }
            else
            {
                if (CurrentCoordinate != null && CurrentCoordinate.GetDistanceTo(pos.Coordinate) > 1000)
                    initialZoomDone = false;
                CurrentCoordinate = pos.Coordinate;
                LocationHelper.FindAddress(pos.Coordinate)
                    .Select(addr => new GeoAddress(pos, addr))
                    .Subscribe(OnLocationChanged, OnLocationChangeError);
            }
            //ZoomMap();
        }

        private void OnLocationChanged(GeoAddress location)
        {
            if (location.Address != null)
                FromLocation = location.Address.FormattedAddress;
            else
                FromLocation = "";
        }

        private void OnLocationChangeError(Exception error)
        {
            currentGeo = null;
            events.Publish(new ErrorState(error, "Could not get the current address."));
        }

        private void OnGetStationsError(Exception error)
        {
            stationObs = null;
            events.Publish(new ErrorState(error, "Stations could not be loaded."));
        }

        private List<StationLocation> stationLocations = null;
        protected virtual void OnGotStations(List<StationLocation> sl)
        {
            stationLocations = sl;
            ZoomMap();
        }

        private bool initialZoomDone = false;
        private void ZoomMap()
        {
            if (!initialZoomDone && map != null && stationLocations != null && (!LocationHelper.IsLocationEnabled || CurrentCoordinate != null))
            {
                initialZoomDone = true;
                ZoomMap(stationLocations, map, cityContext, CurrentCoordinate);
            }
        }

        public static void ZoomMap(List<StationLocation> sl, MapControls.Map map, CityContextViewModel cityContext, GeoCoordinate currentCoordinate)
        {
            var r = LocationHelper.GetLocationRect(sl);
            GeoCoordinate centerPoint = null;
            if (currentCoordinate != null && cityContext.IsCurrentCitySelected())
                centerPoint = currentCoordinate;
            else if (sl.Count > 0)
                centerPoint = r.Center;
            else if (cityContext.City != null && cityContext.City.Coordinate != null)
                centerPoint = cityContext.City.Coordinate;

            if (centerPoint != null)
                map.Center = centerPoint;

            /*var stationsWithDistance = from s in sl select new { Distance = s.Coordinate.GetDistanceTo(centerPoint), Station = s };
            var oneKmAway = (from s in stationsWithDistance where s.Distance < 500 orderby s.Distance select s.Station).ToList();
            if (oneKmAway.Count > 1)
            {
                r = LocationHelper.GetLocationRect(oneKmAway);
                map.SetView(r);
                var visibleStations = r == null ? sl : sl.Where(s => r.ContainsPoint(s.Coordinate)).Distinct().ToList();
                if (visibleStations.Count > 30)
                {
#if WP7
                    map.ZoomLevel = map.TargetZoomLevel + 1;
#else
                    map.ZoomLevel = Math.Max(15, map.ZoomLevel) + 1;
#endif
                }
            }*/
        }

        protected void WireupMap()
        {
            if (map == null) return;

#if WP7
            map.ViewChangeStart += (sender, e) => {
                zoomDone = false;
                tilesLoaded = false;
            };
            map.ViewChangeEnd += (sender, e) =>
            {
                zoomDone = map.ZoomLevel == map.TargetZoomLevel;
                OnMapUpdated();
            };
            map.MapResolved += (sender, e) => {
                tilesLoaded = true;
                OnMapUpdated();
            };
#else
            // TODO change this into a throttled observable
            map.ViewChanging += (sender, e) =>
            {
                zoomDone = false;
                tilesLoaded = false;
            };
            map.ViewChanged += (sender, e) =>
            {
                zoomDone = true;
                OnMapUpdated();
            };
            map.ResolveCompleted += (sender, e) =>
            {
                tilesLoaded = true;
                zoomDone = true;
                isCentering = false;
                OnMapUpdated();
            };
#endif
        }

        protected virtual void OnMapUpdated() { }

        private bool isCentering = false;
        public bool CanCenterCurrentLocation { get { return (map != null && IsLocationAvailable); } }

        public void CenterCurrentLocation()
        {
            if (!CanCenterCurrentLocation) return;
            isCentering = true;
#if WP7
            map.SetView(CurrentCoordinate, Math.Max(15, map.ZoomLevel));
#else
            map.SetView(CurrentCoordinate, Math.Max(15, map.ZoomLevel), Microsoft.Phone.Maps.Controls.MapAnimationKind.Parabolic);
#endif
        }
    }
}
