using Caliburn.Micro;
using Bicikelj.Model;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;
using Bicikelj.Views;
using System.Windows.Data;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Reactive.Linq;

namespace Bicikelj.ViewModels
{
    public class StationsViewModel : Conductor<StationLocationViewModel>.Collection.OneActive, IDisposable
    {
        readonly IEventAggregator events;
        readonly SystemConfig config;
        readonly CityContextViewModel cityContext;
        private List<StationLocationViewModel> stations = new List<StationLocationViewModel>();

        public StationsViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
        {
            this.events = events;
            this.config = config;
            this.cityContext = cityContext;
            FilteredItems.Filter += (s, e) => { e.Accepted = MatchesFilter(e.Item as StationLocationViewModel); };
            events.Subscribe(this);
        }

        private string filter = "";
        public string Filter
        {
            get { return filter; }
            set {
                if (value == filter)
                    return;
                filter = value;
                NotifyOfPropertyChange(() => Filter);
                FilterChanged();
            }
        }

        public CollectionViewSource FilteredItems = new CollectionViewSource();

        private StationsView view;
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            this.view = view as StationsView;
        }

        DateTimeOffset dueTime;
        protected override void OnActivate()
        {
            base.OnActivate();
            dueTime = DateTime.Now.AddMilliseconds(700);
            UpdateStations(false);
        }

        public override void ActivateItem(StationLocationViewModel item)
        {
            if (item == null)
                return;
            this.ActiveItem = null;
            if (view != null)
                view.Items.SelectedItem = null;
            item.ViewRect = LocationHelper.GetLocationRect(stations.Select(s => s.Location));
            StationViewModel svm = new StationViewModel(item);
            Bicikelj.NavigationExtension.NavigateTo(svm, "Detail");
        }

        public bool MatchesFilter(StationLocationViewModel station)
        {
            if (station == null)
                return false;
            if (string.IsNullOrWhiteSpace(Filter))
                return true;
            string filter = Filter.ToLower();
            return station.Address.ToLower().Contains(Filter) || station.StationName.ToLower().Contains(Filter);
        }

        public void FilterChanged()
        {
            if (stations == null)
                return;
            this.Items.NotifyOfPropertyChange("");
            /*foreach (var station in stations)
            {
                bool isVisible = MatchesFilter(station);
                bool hasItem = this.Items.IndexOf(station) >= 0;
                if (!isVisible)
                    this.Items.Remove(station);
                else if (isVisible && !hasItem)
                    this.Items.Add(station);
            }*/
        }

        private IDisposable stationsObs = null;
        public void UpdateStations(bool forceUpdate)
        {
            if (stationsObs == null)
            {
                stationsObs = cityContext.GetStations()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Delay(dueTime)
                    .SelectMany(s => LocationHelper.SortByNearest(s))
                    .Do(s => {
                        this.stations.Clear();
                        if (s != null)
                            this.stations.AddRange(s.Select(station => new StationLocationViewModel(station)));
                    })
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(s =>
                    {
                        this.Items.Clear();
                        this.Items.AddRange(stations);
                        FilteredItems.Source = this.Items;
                        FilterChanged();
                    },
                    e => events.Publish(new ErrorState(e, "could not update stations")));
            }
        }

        public void Dispose()
        {
            ReactiveExtensions.Dispose(ref stationsObs);
        }
    }
}
