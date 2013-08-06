using System;
using System.Linq;
using System.Reactive.Linq;
using Bicikelj.Model;
using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Views;
using Microsoft.Phone.Controls.Maps;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Data;
using System.Device.Location;
using System.Windows.Input;
using System.Windows;
using Microsoft.Phone.Controls;
using System.Reactive.Concurrency;
using System.Windows.Media.Animation;

namespace Bicikelj.ViewModels
{
    public class StationMapViewModel : Conductor<StationViewModel>.Collection.AllActive
    {
        readonly IEventAggregator events;
        private SystemConfig config;
        private CityContextViewModel cityContext;
        private IList<StationViewModel> stations = null;
        public IList<StationViewModel> Stations {
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
            
            var newCenter = this.view.Map.TargetCenter;
            var mapRect = this.view.Map.TargetBoundingRectangle;
            var visibleStations = stations.Where(s => 
                s.Coordinate.Latitude <= mapRect.North &&
                s.Coordinate.Latitude >= mapRect.South &&
                s.Coordinate.Longitude >= mapRect.West &&
                s.Coordinate.Longitude <= mapRect.East
            ).Distinct();
            
            var toKeep = visibleStations.Intersect(this.Items);
            var toAdd = visibleStations.Except(toKeep).ToList();
            var toRemove = this.Items.Except(toKeep).ToList();
            this.Items.RemoveRange(toRemove);
            if (addingItemsDisp != null)
            {
                addingItemsDisp.Dispose();
            }
            
            addingItemsDisp = toAdd.OrderBy(s => s.Coordinate.GetDistanceTo(newCenter))
                .ToObservable()
                .SubscribeOn(NewThreadScheduler.Default)
                .Do(s => { System.Threading.Thread.Sleep(40); })
                .ObserveOn(ReactiveExtensions.SyncScheduler)
                .Subscribe(s => this.Items.Add(s));
            //this.Items.AddRange(toAdd);
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
        private IDisposable compassObs;

        private StationViewModel activeItem;
        public StationViewModel ActiveItem {
            get { return activeItem; }
            set {
                if (value == activeItem)
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
                    ActivateItem(activeItem);
                if (!doShow.HasValue || doShow.Value)
                    NotifyOfPropertyChange(() => ActiveItem);
            }
        }

        private StationMapView view;
        private bool zoomDone = false;
        private bool tilesLoaded = false;
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            this.view = view as StationMapView;
            if (view != null)
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
                        FromLocation = location.Address.FormattedAddress;
                    });
            
            if (stationObs == null)
                stationObs = cityContext.GetStations()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Where(sl => sl != null)
                    .ObserveOn(syncContext)
                    .Subscribe(sl => {
                        var r = LocationHelper.GetLocationRect(sl);
                        var oneKmAway = sl.Where(s => s.Coordinate.GetDistanceTo(r.Center) < 500).ToList();
                        r = LocationHelper.GetLocationRect(oneKmAway);
                        view.Map.SetView(r);
                        if (CurrentLocation.Coordinate != null && cityContext.IsCurrentCitySelected())
                            view.Map.Center = CurrentLocation.Coordinate;
                        // todo: else use City.Center (GeoCoordinate)
                            
                        //view.Map.ZoomLevel = 15;
                        Stations = sl.Select(s => new StationViewModel(new StationLocationViewModel(s))).ToList();
                    });
            
            if (compassObs == null)
                compassObs = LocationHelper.GetCurrentHeading()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .ObserveOn(syncContext)
                    .Subscribe(heading =>
                    {
                        CurrentHeading = heading;
                    });
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                ReactiveExtensions.Dispose(ref currentGeo);
                ReactiveExtensions.Dispose(ref stationObs);
                ReactiveExtensions.Dispose(ref compassObs);
            }
            base.OnDeactivate(close);
        }

        public void TapPin(StationViewModel sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            
            if (sender != null && sender != ActiveItem)
            {
                Items.Remove(sender);
                Items.Add(sender);
                ActiveItem = sender;
            }
            else
                ActiveItem = null;
        }

        private double currentHeading = 0;
        public double CurrentHeading
        {
            get { return currentHeading; }
            set
            {
                if (value == currentHeading) return;
                currentHeading = value;
                if (view != null)
                {
                    view.daAnimateHeading.To = currentHeading;
                    if (view.sbAnimateHeading.GetCurrentState() == ClockState.Stopped)
                        view.sbAnimateHeading.Stop();
                    view.sbAnimateHeading.Begin();
                }
                
                NotifyOfPropertyChange(() => CurrentHeading);
            }
        }
    }
}