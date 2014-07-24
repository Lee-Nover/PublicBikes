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
using System.Net;
using Caliburn.Micro.BindableAppBar;
using System.Windows;

namespace Bicikelj.ViewModels
{
    public class NavigationStartViewModel : Screen, IHandle<SystemConfig>
    {
        #region WP8 Panorama Hack
#if WP8
        public override bool Equals(object obj)
        {
            if ((obj != null) && (obj.GetType() == typeof(Microsoft.Phone.Controls.PanoramaItem)))
            {
                Microsoft.Phone.Controls.PanoramaItem thePanoItem = (Microsoft.Phone.Controls.PanoramaItem)obj;
                return base.Equals(thePanoItem.Header);
            }
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
#endif
        #endregion

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
                        IsLocationEnabled = location != null && !location.IsEmpty && location.Status != GeoPositionStatus.Disabled;
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
        public bool IsLocationEnabled { 
            get { return isLocationEnabled; }
            set { 
                if (value == isLocationEnabled) return;
                isLocationEnabled = value;
                NotifyOfPropertyChange(() => IsLocationEnabled);
            }
        }

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

        private IDisposable networkMonitorObs = null;
        private bool CheckNetworkConnection()
        {
            if (!NetworkHelper.IsNetworkAvailable())
            {
                if (networkMonitorObs == null)
                    networkMonitorObs = NetworkHelper.GetNetworkAvailability()
                        .ObserveOn(NewThreadScheduler.Default)
                        .Subscribe(NetworkStateChanged);

                if (MessageBox.Show("Seems that an internet connection isn't available. Try anyway?", "no internet connection", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                    return false;
            }
            return true;
        }

        private void NetworkStateChanged(bool networkState)
        {
            PublicBikes.Tools.UIHelpers.ShowToast(null, "internet " + (networkState ? "is back :D" : "was lost :'("), null);
        }

        public bool CanFindNearestAvailableBike { get { return IsEnabled; } }
        public void FindNearestAvailableBike()
        {
            if (!CheckNetworkConnection()) return;
            App.CurrentApp.LogAnalyticEvent("Finding nearest bike");
            FindNearestStationWithCondition(null, "finding nearest bike...", (s, a) => { return a != null && a.Open && a.Available > 0; });
        }

        public bool CanFindNearestFreeStand { get { return IsEnabled; } }
        public void FindNearestFreeStand()
        {
            if (!CheckNetworkConnection()) return;
            App.CurrentApp.LogAnalyticEvent("Finding nearest stand");
            FindNearestStationWithCondition(null, "finding nearest stand...", (s, a) => { return a != null && a.Open && a.Free > 0; });
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
#if WP7
                        return LocationHelper.SortByNearest(sl).First();
#else
                        return LocationHelper.SortByNearest(sl).Take(1).Wait();
#endif
                    else
                        return LocationHelper.SortByLocation(sl, location);
                })
                .Catch<IEnumerable<StationLocation>, WebException>(webex =>
                {
                    string msgex = "Stations could not be loaded. Check your internet connection.";
                    events.Publish(new ErrorState(webex, msgex, true));
                    return Observable.Empty<IEnumerable<StationLocation>>();
                })
                .Subscribe(sl => {
                    FindNearest(sl, condition); 
                },
                e => events.Publish(new ErrorState(e, "Stations could not be loaded.")));
        }

        private void FindNearest(IEnumerable<StationLocation> sortedStations, StationCondition condition)
        {
            sortedStations.ToObservable()
#if WP7
                .Select(station => cityContext.GetAvailability2(station).First())
#else
                .Select(station => cityContext.GetAvailability2(station).Take(1).Wait())
#endif
                    .Where(sa => sa != null && condition(sa.Station, sa.Availability))
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
                    e => events.Publish(new ErrorState(e, "Could not check station availability.")),
                    () => events.Publish(BusyState.NotBusy()));
        }

        public bool CanTakeMeTo { get { return IsEnabled; } }
        public void TakeMeTo()
        {
            if (!CheckNetworkConnection()) return;
            App.CurrentApp.LogAnalyticEvent("Opened NavigationView");
            NavigationViewModel nvm = IoC.Get<NavigationViewModel>();
            // TODO: the viewmodel must handle this itself
            // maybe pass some params in the url?
            nvm.IsFavorite = false;
            Bicikelj.NavigationExtension.NavigateTo(nvm);
        }

        public bool CanOpenMap { get { return StationsAvailable; } }
        public void OpenMap()
        {
            App.CurrentApp.LogAnalyticEvent("Opened StationMapView");
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