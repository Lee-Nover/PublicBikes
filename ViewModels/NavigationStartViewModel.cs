using System.Collections.Generic;
using System.Device.Location;
using Bicikelj.Model;
using Caliburn.Micro;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System;
using System.Threading;
using System.Diagnostics;

namespace Bicikelj.ViewModels
{
    public class NavigationStartViewModel : Screen, IHandle<SystemConfig>
    {
        readonly IEventAggregator events;
        readonly SystemConfig config;
        readonly CityContextViewModel cityContext;
        private IDisposable subCity;
        private IDisposable subLocation;

        public NavigationStartViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
        {
            this.events = events;
            this.config = config;
            this.cityContext = cityContext;
            events.Subscribe(this);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (subCity == null)
                subCity = cityContext.CityObservable
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(city => UpdateIsEnabled());

            if (subLocation == null)
                subLocation = LocationHelper.GetCurrentLocation()
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(location => {
                        isLocationEnabled = location != null && !location.IsEmpty && location.Status != GeoPositionStatus.Disabled;
                        UpdateIsEnabled();
                    });

            UpdateIsEnabled();
        }

        private void UpdateIsEnabled()
        {
            isEnabled = isLocationEnabled && cityContext.IsCitySupported;
            if (cityContext.IsCitySupported)
            {
                this.DisplayName = cityContext.City.ServiceName;
                UsedLocation = string.Format("{0}, {1}", cityContext.City.CityName, cityContext.City.Country);
            }
            else if (cityContext.City != null)
            {
                this.DisplayName = "bikes N/A";
                UsedLocation = string.Format("{0}, {1}", cityContext.City.CityName, cityContext.City.Country);
            }
            else
            {
                this.DisplayName = "city N/A";
                UsedLocation = "";
            }
            NotifyChanges();
        }

        private bool isEnabled;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set {
                if (value == isEnabled)
                    return;
                isEnabled = value;
                NotifyChanges();
            }
        }

        private void NotifyChanges()
        {
            NotifyOfPropertyChange(() => IsEnabled);
            NotifyOfPropertyChange(() => IsLocationEnabled);
            NotifyOfPropertyChange(() => CanFindNearestAvailableBike);
            NotifyOfPropertyChange(() => CanFindNearestFreeStand);
            NotifyOfPropertyChange(() => CanTakeMeTo);
            NotifyOfPropertyChange(() => CanOpenMap);
        }

        private bool isLocationEnabled = false;
        public bool IsLocationEnabled { get { return isLocationEnabled; } }

        public bool NavigationDisabled { get { return !IsEnabled; } }

        public bool StationsAvailable { get { return cityContext.IsCitySupported; } }

        private string usedLocation;

        public string UsedLocation
        {
            get { return usedLocation; }
            set {
                if (value == usedLocation)
                    return;
                usedLocation = value;
                NotifyOfPropertyChange(() => UsedLocation);
            }
        }

        public bool CanFindNearestAvailableBike { get { return IsEnabled; } }
        public void FindNearestAvailableBike()
        {
            FindNearestStationWithCondition(null, "finding nearest bike...", (s, a) => { return a.Available > 0; });
        }

        public bool CanFindNearestFreeStand { get { return IsEnabled; } }
        public void FindNearestFreeStand()
        {
            FindNearestStationWithCondition(null, "finding nearest stand...", (s, a) => { return a.Free > 0; });
        }

        private void FindNearestStationWithCondition(GeoCoordinate location, string msg, StationCondition condition)
        {
            events.Publish(BusyState.Busy(msg));
            cityContext.GetStations()
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Where(sl => sl != null)
                .Take(1)
                .Select(sl => {
                    if (location == null)
                        return LocationHelper.SortByNearest(sl).First();
                    else
                        return LocationHelper.SortByLocation(sl, location);
                })
                .Subscribe(sl => {
                    FindNearest(sl, condition); 
                },
                e => events.Publish(new ErrorState(e, "stations could not be loaded")));
        }

        private void FindNearest(IEnumerable<StationLocation> sortedStations, StationCondition condition)
        {
            sortedStations.ToObservable()
                    .Select(station => cityContext.GetAvailability2(station).First())
                    .Where(sa => condition(sa.Station, sa.Availability))
                    .Take(1)
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(sa =>
                    {
                        StationLocationViewModel vm = new StationLocationViewModel(sa.Station);
                        StationAvailabilityViewModel am = new StationAvailabilityViewModel(sa.Availability);
                        vm.ViewRect = LocationHelper.GetLocationRect(sortedStations);
                        StationViewModel svm = new StationViewModel(vm, am);
                        Bicikelj.NavigationExtension.NavigateTo(svm, "Detail");
                        events.Publish(BusyState.NotBusy());
                    },
                    e => events.Publish(new ErrorState(e, "could not check station availability")),
                    () => events.Publish(BusyState.NotBusy()));
        }

        public bool CanTakeMeTo { get { return IsEnabled; } }
        public void TakeMeTo()
        {
            NavigationViewModel nvm = IoC.Get<NavigationViewModel>();
            // TODO: the viewmodel must handle this itself
            // maybe pass some params in the url?
            nvm.IsFavorite = false;
            Bicikelj.NavigationExtension.NavigateTo(nvm);
        }

        public bool CanOpenMap { get { return StationsAvailable; } }
        public void OpenMap()
        {
            Bicikelj.NavigationExtension.NavigateTo(IoC.Get<StationMapViewModel>());
        }

        public void OpenConfig()
        {
            Bicikelj.NavigationExtension.NavigateTo(IoC.Get<SystemConfigViewModel>());
        }

        public void Handle(SystemConfig config)
        {
            UpdateIsEnabled();
        }
    }
}