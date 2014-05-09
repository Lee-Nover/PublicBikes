using Bicikelj.Model;
using Bicikelj.Views;
using Caliburn.Micro;
using GART.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Threading.Tasks;
#if !WP7
using PublicBikes.Tools;
using MapControls = Microsoft.Phone.Maps.Controls;
#else
using MapControls = Microsoft.Phone.Controls.Maps;
#endif

namespace Bicikelj.ViewModels
{
    public class StationMapViewModel : MapBaseViewModel
    {
        private StationMapView view;
        
        private List<StationViewModel> stations = null;

        public List<StationViewModel> Stations
        {
            get { return stations; }
            private set
            {
                stations = value;
                UpdateVisiblePins();
                NotifyOfPropertyChange(() => Stations);
            }
        }


        private StationViewModel activeItem;
        public StationViewModel ActiveItem
        {
            get { return activeItem; }
            set { SetActiveItem(value); }
        }
        
        public StationMapViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
            : base(events, config, cityContext)
        {
#if DEBUG
            IsARVisible = true;
#endif
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            this.view = view as StationMapView;
            if (this.view != null)
            {
                this.map = this.view.Map;
                this.WireupMap();
#if WP7
                var frame = App.CurrentApp.RootVisual as Microsoft.Phone.Controls.PhoneApplicationFrame;
                frame.OrientationChanged += (sender, args) =>
                {
                    this.view.ARDisplay.HandleOrientationChange(args);
                };
#else
                BindMapItems();
#endif
            }
        }

#if !WP7
        private void BindMapItems()
        {
            if (view == null || map == null) return;
            view.StationsLayer = map.Layers[1];
            view.CurrentLocationLayer = map.Layers[0];
            foreach (var overlay in view.CurrentLocationLayer)
                overlay.BindCoordinate("CurrentCoordinate", this);
            view.StationsLayer.SetItemsCollection(this.Items, view.Resources["mapItemTemplate"] as DataTemplate);
        }
#endif

        protected override void OnMapUpdated()
        {
            base.OnMapUpdated();
            UpdateVisiblePins();
        }

        private void SetActiveItem(StationViewModel value)
        {
            if (value == activeItem)
                return;
            if (value != null && !value.CanOpenDetails())
                return;

            bool? doShow = null;
            if (activeItem == null && value != null)
                doShow = true;
            else if (activeItem != null && value == null)
                doShow = false;

            #region ActiveItem in header animation
            /*
                if (doShow.HasValue)
                {
                    SlideTransition slideTransition;
                    if (doShow.Value)
                        slideTransition = new SlideTransition { Mode = SlideTransitionMode.SlideDownFadeIn };
                    else
                        slideTransition = new SlideTransition { Mode = SlideTransitionMode.SlideUpFadeOut };
                    ITransition transition = slideTransition.GetTransition(view.ActiveItemContainer);
                    EventHandler transitionComplete = (s, e) => {
                        transition.Stop();
                        if (!doShow.Value)
                            NotifyOfPropertyChange(() => ActiveItem);
                        transition.Completed -= transitionComplete;
                    };
                    transition.Completed += transitionComplete;
                    transition.Begin();
                }
                */
            #endregion

            if (activeItem != null)
                DeactivateItem(activeItem, false);
            activeItem = value;
            if (activeItem != null)
            {
                if (CurrentCoordinate != null && cityContext.IsCurrentCitySelected())
                    activeItem.Location.CurrentCoordinate = CurrentCoordinate;
                ActivateItem(activeItem);
            }
            if (!doShow.HasValue || doShow.Value)
                NotifyOfPropertyChange(() => ActiveItem);
        }

        private IDisposable trickleItemsDisp;
        private ManualResetEvent trickleEvent = new ManualResetEvent(true);
        private void UpdateVisiblePins()
        {
            if (!tilesLoaded || !zoomDone) return;
            // interrupt adding stations
            ReactiveExtensions.Dispose(ref trickleItemsDisp);
            trickleEvent.WaitOne();
            trickleEvent.Reset();
            
            if (stations == null)
            {
                this.Items.Clear();
                trickleEvent.Set();
                return;
            }

            this.view.ARDisplay.ARItems = new ObservableCollection<ARItem>(stations.Select(st => new ARItem()
            {
                Content = st,
                GeoLocation = st.Coordinate
            }));

            var map = this.map;
            var clusters = StationClusterer.ClusterStations(stations, map, Math.Min(map.ActualWidth, map.ActualHeight) / 6);
            var newCenter = map.Center;
#if WP7
            var mapRect = map.TargetBoundingRectangle;
#else
            var mapRect = map.GetBoundingRectangle();
#endif

            List<StationViewModel> visibleStations;
            if (mapRect != null)
                visibleStations = clusters.Where(s => mapRect.ContainsPoint(s.Coordinate)).Distinct().ToList();
            else
                visibleStations = clusters;
            var cc = new ClusterComparer();
            var toKeep = visibleStations.Intersect(this.Items, cc).ToList();
            var toAdd = visibleStations.Except(toKeep, cc).ToList();
            var toRemove = this.Items.Except(toKeep, cc).ToList();
            foreach (var item in toRemove)
                this.Items.Remove(item);

            var ordered = toAdd.OrderBy(s => s.Coordinate.GetDistanceTo(newCenter));

            trickleItemsDisp = ordered.ToObservable(NewThreadScheduler.Default)
                .ObserveOn(ReactiveExtensions.SyncScheduler)
                .Finally(() =>
                {
                    trickleEvent.Set();
                })
                .Subscribe(s => 
                { 
                    this.Items.Add(s);
                    System.Threading.Thread.Sleep(30);
                });
            
            NotifyOfPropertyChange(() => Items);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            UpdateVisiblePins();
            if (activeItem != null)
                ActivateItem(activeItem);
        }

        protected override void OnGotStations(List<StationLocation> sl)
        {
            base.OnGotStations(sl);
            Stations = sl.Select(s => new StationViewModel(new StationLocationViewModel(s))).ToList();
        }

        public void TapPin(StationViewModel sender, System.Windows.Input.GestureEventArgs e)
        {
            if (e == null) return;
            e.Handled = true;
            if (sender != null)
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

        private bool displayingAR = false;
        public bool DisplayingAR
        {
            get { return displayingAR; }
            set
            {
                if (value == displayingAR) return;
                displayingAR = value;
                if (this.view != null)
                    if (displayingAR)
                        this.view.ARDisplay.StartServices();
                    else
                        this.view.ARDisplay.StopServices();
                NotifyOfPropertyChange(() => DisplayingAR);
            }
        }

        public void ToggleDisplayMode()
        {
            DisplayingAR = !DisplayingAR;
        }

        public bool IsARVisible { get; set; }

        private string _ARDisplayModeStr = "VideoPreview";
        public string ARDisplayModeStr
        {
            get { return _ARDisplayModeStr; }
            set { SetARDisplayMode(value); }
        }

        public void SetARDisplayMode(string value)
        {
            if (value == _ARDisplayModeStr) return;
            _ARDisplayModeStr = value;
            NotifyOfPropertyChange(() => ARDisplayModeStr);
        }
    }
}