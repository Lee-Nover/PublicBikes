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

namespace Bicikelj.ViewModels
{
    public class StationMapViewModel : Screen
    {
        readonly IEventAggregator events;
        private SystemConfig config;
        private CityContextViewModel cityContext;
        private IList<StationViewModel> stations = null;
        public IList<StationViewModel> Stations {
            get { return stations; }
            private set {
                stations = value;
                NotifyOfPropertyChange(() => Stations);
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

        private StationMapView view;
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            this.view = view as StationMapView;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            var syncContext = ReactiveExtensions.SyncScheduler;
            if (currentGeo == null)
                currentGeo = LocationHelper.GetCurrentGeoAddress(false)
                    .Where(location => location != null)
                    .ObserveOn(syncContext)
                    .Subscribe(location =>
                    {
                        CurrentLocation.Coordinate = location.Coordinate;
                        FromLocation = location.Address.FormattedAddress;
                    });
            
            if (stationObs == null)
                stationObs = cityContext.GetStations()
                    .Where(sl => sl != null)
                    .ObserveOn(syncContext)
                    .Subscribe(sl => {
                        var r = LocationHelper.GetLocationRect(sl);
                        view.Map.ZoomBarVisibility = System.Windows.Visibility.Visible;
                        view.Map.SetView(r);
                        if (CurrentLocation.Coordinate != null)
                            view.Map.Center = CurrentLocation.Coordinate;
                        view.Map.ZoomLevel = 15;
                        Stations = sl.Select(s => new StationViewModel(new StationLocationViewModel(s))).ToList();
                    });

        }

        protected override void OnDeactivate(bool close)
        {
            ReactiveExtensions.Dispose(ref currentGeo);
            ReactiveExtensions.Dispose(ref stationObs);
            base.OnDeactivate(close);
        }
    }
}