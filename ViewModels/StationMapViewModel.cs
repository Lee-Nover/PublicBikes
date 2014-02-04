using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Media.Animation;
using Bicikelj.Model;
using Bicikelj.Views;
using Caliburn.Micro;
using Microsoft.Devices.Sensors;
using System.Windows;
using System.Device.Location;

namespace Bicikelj.ViewModels
{
    public class StationMapViewModel : Conductor<StationViewModel>.Collection.AllActive
    {
        readonly IEventAggregator events;
        private SystemConfig config;
        private CityContextViewModel cityContext;
        private List<StationViewModel> stations = null;
        public List<StationViewModel> Stations {
            get { return stations; }
            private set {
                stations = value;
                UpdateVisiblePins();
                NotifyOfPropertyChange(() => Stations);
            }
        }

        private IDisposable addingItemsDisp;
        private void UpdateVisiblePins()
        {
            if (!tilesLoaded || !zoomDone) return;

            if (stations == null)
            {
                this.Items.Clear();
                return;
            }
            
            var map = this.view.Map;
            var clusters = StationClusterer.ClusterStations(stations, map, Math.Min(map.ActualWidth, map.ActualHeight) / 6);
            var newCenter = this.view.Map.TargetCenter;
            var mapRect = this.view.Map.TargetBoundingRectangle;
            var visibleStations = clusters.Where(s => mapRect.ContainsPoint(s.Coordinate)).Distinct().ToList();

            var cc = new ClusterComparer();

            var toKeep = visibleStations.Intersect(this.Items, cc).ToList();
            var toAdd = visibleStations.Except(toKeep, cc).ToList();
            var toRemove = this.Items.Except(toKeep, cc).ToList();

            this.Items.RemoveRange(toRemove);
            if (addingItemsDisp != null)
                ReactiveExtensions.Dispose(ref addingItemsDisp);

            addingItemsDisp = toAdd.OrderBy(s => s.Coordinate.GetDistanceTo(newCenter))
                .ToObservable()
                .SubscribeOn(NewThreadScheduler.Default)
                .Do(s => { System.Threading.Thread.Sleep(30); })
                .ObserveOn(ReactiveExtensions.SyncScheduler)
                .Subscribe(s => this.Items.Add(s));
            NotifyOfPropertyChange(() => Items);
        }

        public StationMapViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
        {
            this.events = events;
            this.cityContext = cityContext;
            this.config = config;
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
        private IDisposable currentGeo;
        private IDisposable stationObs;

        private StationViewModel activeItem;
        public StationViewModel ActiveItem {
            get { return activeItem; }
            set {
                if (value == activeItem)
                    return;
                if (value != null && !value.CanOpenDetails())
                    return;

                bool? doShow = null;
                if (activeItem == null && value != null)
                    doShow = true;
                else if (activeItem != null && value == null)
                    doShow = false;
                
                /*
                if (doShow.HasValue)
                {
                    SlideTransition slideTransition;
                    if (doShow.Value)
                        slideTransition = new SlideTransition { Mode = SlideTransitionMode.SlideDownFadeIn };
                    else
                        slideTransition = new SlideTransition { Mode = SlideTransitionMode.SlideUpFadeOut };
                    ITransition transition = slideTransition.GetTransition(view.ActiveItemContainer);
                    transition.Completed += delegate {
                        transition.Stop();
                        if (!doShow.Value)
                            NotifyOfPropertyChange(() => ActiveItem);
                    };
                    transition.Begin();
                }
                 * */
                if (activeItem != null)
                    DeactivateItem(activeItem, false);
                activeItem = value;
                if (activeItem != null)
                {
                    if (CurrentLocation.Coordinate != null && cityContext.IsCurrentCitySelected())
                        activeItem.Location.CurrentLocation = CurrentLocation.Coordinate;
                    ActivateItem(activeItem);
                }
                if (!doShow.HasValue || doShow.Value)
                    NotifyOfPropertyChange(() => ActiveItem);
            }
        }

        private StationMapView view;
        private bool zoomDone = false;
        private bool tilesLoaded = false;
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            this.view = view as StationMapView;
            if (this.view != null)
            {
                var map = this.view.Map;
                map.ViewChangeStart += (sender, e) => {
                    zoomDone = false;
                    tilesLoaded = false;
                };
                map.ViewChangeEnd += (sender, e) =>
                {
                    zoomDone = map.ZoomLevel == map.TargetZoomLevel;
                    UpdateVisiblePins();
                };
                map.MapResolved += (sender, e) => {
                    tilesLoaded = true;
                    UpdateVisiblePins();
                };
            }
        }

        protected override void OnActivate()
        {
            // remove the Items because AllActive will Activate all of them
            Items.Clear();
            base.OnActivate();
            UpdateVisiblePins();
            if (activeItem != null)
                ActivateItem(activeItem);

            var syncContext = ReactiveExtensions.SyncScheduler;
            if (currentGeo == null)
                currentGeo = LocationHelper.GetCurrentGeoAddress(false)
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Where(location => location != null)
                    .ObserveOn(syncContext)
                    .Subscribe(location =>
                    {
                        CurrentLocation.Coordinate = location.Coordinate;
                        if (location.Address != null)
                            FromLocation = location.Address.FormattedAddress;
                        else
                            FromLocation = "";
                    },
                    error =>
                    {
                        currentGeo = null;
                        events.Publish(new ErrorState(error, "Could not get the current address"));
                    });
            
            if (stationObs == null)
                stationObs = cityContext.GetStations()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Where(sl => sl != null)
                    .ObserveOn(syncContext)
                    .Subscribe(sl => {
                        var r = LocationHelper.GetLocationRect(sl);
                        GeoCoordinate centerPoint;
                        if (CurrentLocation.Coordinate != null && cityContext.IsCurrentCitySelected())
                            centerPoint = CurrentLocation.Coordinate;
                        else
                            centerPoint = r.Center;
                        var stationsWithDistance = from s in sl select new { Distance = s.Coordinate.GetDistanceTo(centerPoint), Station = s };
                        var oneKmAway = (from s in stationsWithDistance where s.Distance < 500 orderby s.Distance select s.Station).ToList();
                        if (oneKmAway.Count > 1)
                        {
                            r = LocationHelper.GetLocationRect(oneKmAway);
                            view.Map.SetView(r);

                            r = view.Map.TargetBoundingRectangle;
                            var visibleStations = sl.Where(s => r.ContainsPoint(s.Coordinate)).Distinct().ToList();
                            if (visibleStations.Count > 30)
                            {
                                view.Map.ZoomLevel = view.Map.TargetZoomLevel + 1;
                            }
                        }
                        else
                            view.Map.ZoomLevel = 15;
                        view.Map.Center = centerPoint;

                        Stations = sl.Select(s => new StationViewModel(new StationLocationViewModel(s))).ToList();
                    });
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                ReactiveExtensions.Dispose(ref currentGeo);
                ReactiveExtensions.Dispose(ref stationObs);
            }
            base.OnDeactivate(close);
        }

        public void TapPin(StationViewModel sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            App.CurrentApp.LogAnalyticEvent("Tapped a station pin");
            if (sender != null && sender != ActiveItem)
            {
                Items.Remove(sender);
                Items.Add(sender);
                ActiveItem = sender;
            }
            else
                ActiveItem = null;
        }
    }
}