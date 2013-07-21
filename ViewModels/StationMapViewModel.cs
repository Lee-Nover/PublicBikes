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
                this.Items.Clear();
                this.Items.AddRange(stations);
                NotifyOfPropertyChange(() => Stations);
                NotifyOfPropertyChange(() => Items);
            }
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
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            this.view = view as StationMapView;
        }

        protected override void OnActivate()
        {
            // remove the Items because AllActive will Activate all of them
            Items.Clear();
            base.OnActivate();
            if (stations != null)
                Items.AddRange(stations);
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
                        view.Map.ZoomBarVisibility = Visibility.Visible;
                        view.Map.ScaleVisibility = Visibility.Visible;
                        view.Map.SetView(r);
                        if (CurrentLocation.Coordinate != null)
                            view.Map.Center = CurrentLocation.Coordinate;
                        view.Map.ZoomLevel = 15;
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