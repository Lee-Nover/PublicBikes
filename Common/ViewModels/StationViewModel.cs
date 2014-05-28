using Bicikelj.Model;
using Caliburn.Micro;
using System;
using System.Linq;
using System.Device.Location;
using System.Reactive.Linq;

namespace Bicikelj.ViewModels
{
    public class StationViewModel : Screen, IHasCoordinate
    {
        private IEventAggregator events;
        private CityContextViewModel cityContext;
        private StationAvailabilityViewModel availability;
        public StationLocationViewModel Location { get; set; }
        public StationAvailabilityViewModel Availability
        {
            get
            {
                //CheckAvailability(false);
                return availability;
            }
            
            set
            {
                availability = value;
                NotifyOfPropertyChange(() => Availability);
                NotifyOfPropertyChange(() => IsOpen);
            }
        }

        public StationViewModel() : this(null, null)
        {
        }

        public StationViewModel(StationLocationViewModel location) : this(location, null)
        {
        }

        public StationViewModel(StationLocationViewModel location, StationAvailabilityViewModel availability)
        {
            events = IoC.Get<IEventAggregator>();
            cityContext = IoC.Get<CityContextViewModel>();
            this.Location = location;
            this.Availability = availability;
            if (location != null)
            {
                Location.ActivateWith(this);
                Location.DeactivateWith(this);
            }
        }

        public GeoCoordinate Coordinate { get { return GetCoordinate(); } set {} }

        protected virtual GeoCoordinate GetCoordinate()
        {
            return Location.GeoLocation;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            CheckAvailability(false);
        }

        protected virtual void CheckAvailability(bool forceUpdate)
        {
            if (Location == null || Location.Location == null)
                return;
            if (availability == null || forceUpdate || !cityContext.IsAvailabilityValid(Location.Location))
            {
                // we might lose the city if the sensor changes .. 
                // should not happen if the location was gotten by the nearest supported city
                // link stationLocation with city and let it continue to work
                events.Publish(BusyState.Busy("checking availability..."));

                var availObs = cityContext.GetAvailability(Location.Location, forceUpdate);
                availObs
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(a => {
                        this.Availability = new StationAvailabilityViewModel(a);
                    },
                    e => events.Publish(new ErrorState(e, "Could not get station's availability.")),
                    () => events.Publish(BusyState.NotBusy()));
            };
        }

        public void RefreshAvailability()
        {
            CheckAvailability(true);
        }

        public virtual bool CanToggleFavorite() { return true; }
        public void ToggleFavorite()
        {
            if (Location != null)
            {
                Location.ToggleFavorite();
                NotifyOfPropertyChange(() => IsFavorite);
            }
        }

        public bool IsFavorite { get { return Location != null ? Location.IsFavorite : false; } }
        public bool IsOpen { get { 
            return Availability != null ? Availability.Open : Location != null ? Location.Open : false;
        } }

        public virtual bool CanOpenDetails() { return true; }
        public void OpenDetails()
        {
            Bicikelj.NavigationExtension.NavigateTo(this, "Detail");
        }

        public bool CanRefreshRoute { get { return Location != null; } }
        public void RefreshRoute()
        {
            if (Location != null)
                Location.RefreshRoute();
        }
    }
}
