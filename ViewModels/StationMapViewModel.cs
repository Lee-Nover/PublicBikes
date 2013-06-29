using System;
using System.Linq;
using System.Reactive.Linq;
using Bicikelj.Model;
using Caliburn.Micro;
using System.Collections.Generic;

namespace Bicikelj.ViewModels
{
    public class StationMapViewModel : Screen
    {
        readonly IEventAggregator events;
        private SystemConfig config;
        private CityContextViewModel cityContext;
        public IList<StationViewModel> Stations { get; private set; }

        public StationMapViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
        {
            this.events = events;
            this.cityContext = cityContext;
            this.config = config;
            this.CurrentLocation = new LocationViewModel();
        }

        public LocationViewModel CurrentLocation { get; set; }
        private IDisposable currentGeo;
        private IDisposable stationObs;

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            var syncContext = ReactiveExtensions.SyncScheduler;
            if (currentGeo == null)
                currentGeo = LocationHelper.GetCurrentGeoAddress()
                    .Where(location => location != null)
                    .ObserveOn(syncContext)
                    .Subscribe(location =>
                    {
                        CurrentLocation.Coordinate = location.Coordinate;
                        //FromLocation = location.Address.FormattedAddress;
                    });
            
            if (stationObs == null)
                stationObs = cityContext.GetStations()
                    .Do(sl => SetStations(sl))
                    .Where(s => s != null)
                    .Select(s => LocationHelper.GetLocationRect(s))
                    .Where(r => r != null)
                    .ObserveOn(syncContext)
                    .Subscribe(r => { /* update map view */ });

        }

        protected override void OnDeactivate(bool close)
        {
            ReactiveExtensions.Dispose(ref currentGeo);
            ReactiveExtensions.Dispose(ref stationObs);
            base.OnDeactivate(close);
        }

        private void SetStations(List<StationLocation> sl)
        {
            Stations = sl.Select(s => new StationViewModel(new StationLocationViewModel(s))).ToList();
            NotifyOfPropertyChange(() => Stations);
        }

    }
}